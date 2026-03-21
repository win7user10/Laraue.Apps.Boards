using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.Services;
using Laraue.Core.DataAccess.EFCore.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Telegram.Bot;

namespace Laraue.Apps.StructuredMessages.WebApiHost.Controllers;

[ApiController]
[Route("/api/telegram-files")]
public class TelegramFilesController(
    DatabaseContext db,
    IFileStorage fileStorage,
    ITelegramBotClient botClient,
    IHttpClientFactory httpClientFactory,
    IOptions<TelegramOptions> options)
    : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPhoto(
        Guid id,
        CancellationToken cancellationToken)
    {
        var fileData = await db.TelegramFiles
            .Where(x => x.Id == id)
            .Select(x => new { x.FileUniqueId, x.MimeType, x.FileId })
            .FirstOrThrowNotFoundEFAsync(cancellationToken);

        var fileExtension = ExtensionUtility.GetExtension(fileData.MimeType);
        var physicalPath = ShardedPathStrategy.GetPath(fileData.FileUniqueId, fileExtension);
        
        if (await fileStorage.FileExists(physicalPath, cancellationToken))
            return File(
                await fileStorage.ReadFile(physicalPath, cancellationToken),
                fileData.MimeType ?? "application/octet-stream");

        var tgFile = await botClient.GetFile(fileData.FileId, cancellationToken);
        var botToken = options.Value.Token;
        var downloadUrl = $"https://api.telegram.org/file/bot{botToken}/{tgFile.FilePath}";
        
        var httpClient = httpClientFactory.CreateClient();
        var response = await httpClient.GetAsync(
            downloadUrl, 
            HttpCompletionOption.ResponseHeadersRead, 
            cancellationToken);
        
        response.EnsureSuccessStatusCode();
        
        // Return the stream directly - it will be disposed by ASP.NET Core after response
        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return File(stream, fileData.MimeType ?? "application/octet-stream");
    }
}
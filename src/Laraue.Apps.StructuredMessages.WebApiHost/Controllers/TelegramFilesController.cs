using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.Services;
using Laraue.Core.DataAccess.EFCore.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Laraue.Apps.StructuredMessages.WebApiHost.Controllers;

[ApiController]
[Route("/api/telegram-files")]
public class TelegramFilesController(DatabaseContext db, IFileStorage fileStorage)
    : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPhoto(
        Guid id,
        CancellationToken cancellationToken)
    {
        var fileData = await db.TelegramFiles
            .Where(x => x.Id == id)
            .Select(x => new { x.FileUniqueId, x.MimeType })
            .FirstOrThrowNotFoundEFAsync(cancellationToken);

        var fileExtension = ExtensionUtility.GetExtension(fileData.MimeType);
        var physicalPath = ShardedPathStrategy.GetPath(fileData.FileUniqueId, fileExtension);

        return File(
            await fileStorage.ReadFileAsync(physicalPath, cancellationToken),
            fileData.MimeType ?? "application/octet-stream");
    }
}
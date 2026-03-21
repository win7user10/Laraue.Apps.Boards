using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Models;
using Laraue.Apps.StructuredMessages.Services;
using Laraue.Core.Exceptions.Web;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;

namespace Laraue.Apps.StructuredMessages.TelegramServices.Services.Messages;

public interface ITelegramSaveMessageService
{
    Task<GetOrCreateMessageResult> Save(
        SaveMessageTelegramRequest request,
        CancellationToken cancellationToken);
}

public class TelegramSaveMessageService(
    DatabaseContext context,
    IFileStorage fileStorage,
    ITelegramBotClient botClient)
    : ITelegramSaveMessageService
{
    public Task<GetOrCreateMessageResult> Save(
        SaveMessageTelegramRequest request,
        CancellationToken cancellationToken)
    {
        return request switch
        {
            SaveImageMessageTelegramRequest saveImageRequest =>
                SaveImageEntity(saveImageRequest, cancellationToken),
            SaveTextMessageTelegramRequest saveTextRequest =>
                SaveMessageEntity(saveTextRequest, cancellationToken),
            SaveVideoMessageTelegramRequest saveVideoRequest =>
                SaveVideoEntity(saveVideoRequest, cancellationToken),
            _ => throw new NotImplementedException(request.GetType().Name)
        };
    }


    private async Task<GetOrCreateMessageResult> SaveVideoEntity(
        SaveVideoMessageTelegramRequest request,
        CancellationToken cancellationToken)
    {
        var getOrCreateResult = await GetOrCreateMessageEntityId(request, cancellationToken);

        var videoFile = new MessageTelegramVideo
        {
            Height = request.Height,
            Width = request.Width,
            MessageId = getOrCreateResult.MessageId,
        };
        
        if (request.Thumbnail is not null)
        {
            videoFile.ThumbnailFileId = await GetOrCreateMessageFileId(
                request.Thumbnail,
                saveFileToStorage: true,
                cancellationToken);
            videoFile.ThumbnailHeight = request.Thumbnail.Height;
            videoFile.ThumbnailWidth = request.Thumbnail.Width;
        }
        
        videoFile.FileId = await GetOrCreateMessageFileId(
            request.Video,
            saveFileToStorage: false,
            cancellationToken);
        
        context.Add(videoFile);
        await context.SaveChangesAsync(cancellationToken);
        return getOrCreateResult;
    }
    
    private async Task<GetOrCreateMessageResult> SaveImageEntity(
        SaveImageMessageTelegramRequest request,
        CancellationToken cancellationToken)
    {
        var getOrCreateResult = await GetOrCreateMessageEntityId(request, cancellationToken);
        if (request.Photos.Length == 0)
            return getOrCreateResult;
        
        var thumbnailPhoto = request.Photos[0];
        var originalPhoto = request.Photos.Last();
        var photos = new List<(PhotoSize, PhotoType)>
        {
            (thumbnailPhoto!, PhotoType.Thumbnail)
        };
        
        if (originalPhoto != thumbnailPhoto)
            photos.Add((originalPhoto!, PhotoType.Original));

        var groupId = Guid.NewGuid();
        foreach (var (photo, type) in photos)
        {
            var fileId = await GetOrCreateMessageFileId(
                photo,
                saveFileToStorage: type == PhotoType.Thumbnail,
                cancellationToken);

            var messageFile = new MessageTelegramPhoto
            {
                MessageId = getOrCreateResult.MessageId,
                TelegramFileId = fileId,
                Height = photo.Height,
                Width = photo.Width,
                PhotoType = type,
                GroupId = groupId,
            };
        
            context.Add(messageFile);
        }
        
        await context.SaveChangesAsync(cancellationToken);
        return getOrCreateResult;
    }

    /// <summary>
    /// Store file entity and upload it to storage if required.
    /// </summary>
    /// <param name="file"></param>
    /// <param name="saveFileToStorage">
    /// Store the file directly to storage.
    /// If false - then when requesting the file it will be requesting directly from TG.
    /// We can't request always from tg - static content will make too many calls.
    /// And we can't store content always - it takes too much space.
    /// </param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<Guid> GetOrCreateMessageFileId(
        File file,
        bool saveFileToStorage,
        CancellationToken cancellationToken)
    {
        var oldFileData = await context.TelegramFiles
            .Where(x => x.FileUniqueId == file.FileUniqueId)
            .Select(x => new { x.Id })
            .FirstOrDefaultAsync(cancellationToken);

        if (oldFileData is not null)
            return oldFileData.Id;
        
        if (saveFileToStorage)
        {
            var botFile = await botClient.GetFile(
                file.FileId,
                cancellationToken);

            var stream = new MemoryStream();
            await botClient.DownloadFile(
                botFile,
                stream,
                cancellationToken);
        
            var extension = ExtensionUtility.GetExtension(file.MimeType);
            var filePath = ShardedPathStrategy.GetPath(
                botFile.FileUniqueId,
                extension);
        
            stream.Position = 0;
            await fileStorage.WriteFile(
                filePath,
                stream,
                null,
                cancellationToken);
        }
        
        var telegramFile = new TelegramFile
        {
            FileId = file.FileId,
            FileUniqueId = file.FileUniqueId,
            Name = file.FileName,
            Size = file.FileSize,
            MimeType = file.MimeType,
        };
            
        context.Add(telegramFile);
        await context.SaveChangesAsync(cancellationToken);

        return telegramFile.Id;
    }

    private async Task<GetOrCreateMessageResult> GetOrCreateMessageEntityId(
        SaveMessageTelegramRequest request,
        CancellationToken cancellationToken)
    {
        if (request.MediaGroupId is null)
            return await SaveMessageEntity(request, cancellationToken);
        
        var oldMessageData = await context.Messages
            .Where(x => x.TelegramMediaGroupId == request.MediaGroupId)
            .Select(x => new { x.Id, x.UserId })
            .FirstOrDefaultAsync(cancellationToken);

        if (oldMessageData is not null && oldMessageData.UserId != request.UserId)
            throw new ForbiddenException(
                $"Media group id {request.MediaGroupId} belongs to other person");

        if (oldMessageData is not null)
            return new GetOrCreateMessageResult
            {
                MessageId = oldMessageData.Id,
                WasCreated = false,
            };
        
        return await SaveMessageEntity(request, cancellationToken);
    }

    private async Task<GetOrCreateMessageResult> SaveMessageEntity(
        SaveMessageTelegramRequest request,
        CancellationToken cancellationToken)
    {
        var message = new Message
        {
            Content = request.Text,
            TelegramMediaGroupId = request.MediaGroupId,
            UserId = request.UserId,
            CreatedAt = request.SentAt,
            TelegramMessageId = request.TelegramMessageId,
        };

        context.Add(message);
        await context.SaveChangesAsync(cancellationToken);
        return new GetOrCreateMessageResult
        {
            MessageId = message.Id,
            WasCreated = true,
        };
    }
}

public class GetOrCreateMessageResult
{
    public long MessageId { get; set; }
    public bool WasCreated { get; set; }
}
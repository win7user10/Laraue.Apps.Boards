using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Models;
using Laraue.Apps.StructuredMessages.Services;
using LinqToDB.EntityFrameworkCore;
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
        var getOrCreateResult = await SaveMessageEntity(request, cancellationToken);

        var videoFile = new TelegramMessageVideo
        {
            Height = request.Height,
            Width = request.Width,
            TelegramMessageId = request.TelegramMessageId,
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
        var getOrCreateResult = await SaveMessageEntity(request, cancellationToken);
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

            var messageFile = new TelegramMessagePhoto
            {
                TelegramMessageId = request.TelegramMessageId,
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
        
    // New msg, old group
    // Old msg, old group
    // if first msg in group then update content
    private Task<GetOrCreateMessageResult> SaveMessageEntity(
        SaveMessageTelegramRequest request,
        CancellationToken cancellationToken)
    {
        return request.MediaGroupId == null
            ? SaveSingleMessageEntity(request, cancellationToken)
            : SaveGroupMessageEntity(request, cancellationToken);
    }

    /// <summary>
    /// Difficult case. Message is one message of the group.
    /// </summary>
    private async Task<GetOrCreateMessageResult> SaveGroupMessageEntity(
        SaveMessageTelegramRequest request,
        CancellationToken cancellationToken)
    {
        // Try to find the message
        var savedMessage = await context.TelegramMessages
            .Where(x => x.Id == request.TelegramMessageId)
            .FirstOrDefaultAsync(cancellationToken);
        
        // When group id already stored in message - remain it as is. It can't change
        // Otherwise, save it if it was presented.
        var groupId = savedMessage?.TelegramMediaGroupId;
        if (groupId is null && request.MediaGroupId is not null)
            groupId = await GetOrCreateTelegramMediaGroupId(
                request.MediaGroupId,
                cancellationToken);

        // When it is the message group, only the first message content is stored to card
        var firstGroupMessageData = await context.TelegramMessages
            .Where(x => x.TelegramMediaGroupId == groupId)
            .Select(x => new { x.Id, CardId = x.Card!.Id })
            .FirstOrDefaultAsyncEF(cancellationToken);
        
        // Skip the card creating
        var isFirstMessageInGroup = firstGroupMessageData is null
            || firstGroupMessageData.Id == request.TelegramMessageId;

        if (savedMessage is null)
        {
            var telegramMessage = new TelegramMessage
            {
                Id = request.TelegramMessageId,
                TelegramMediaGroupId = groupId,
            };

            if (isFirstMessageInGroup)
            {
                var card = new Card
                {
                    Content = request.Text,
                    UserId = request.UserId,
                    CreatedAt = request.SentAt,
                    TelegramMessageId = request.TelegramMessageId,
                    TelegramMessage = telegramMessage
                };
                
                context.Add(card);
            }
            else
            {
                context.Add(telegramMessage);
            }

            await context.SaveChangesAsync(cancellationToken);

            return new GetOrCreateMessageResult
            {
                Result = isFirstMessageInGroup ? Result.MainMessageCreated : null,
            };
        }
            
        if (isFirstMessageInGroup)
            // If a card is based on this message it will be updated
            await context.Cards
                .Where(x => x.TelegramMessageId == request.TelegramMessageId)
                .ExecuteUpdateAsync(upd => upd
                        .SetProperty(x => x.Content, request.Text),
                    cancellationToken);

        return new GetOrCreateMessageResult
        {
            Result = isFirstMessageInGroup ? Result.MainMessageUpdated : null,
        };
    }
    
    /// <summary>
    /// Simple case. One message without groups.
    /// </summary>
    private async Task<GetOrCreateMessageResult> SaveSingleMessageEntity(
        SaveMessageTelegramRequest request,
        CancellationToken cancellationToken)
    {
        // Try to find the message
        var savedMessage = await context.TelegramMessages
            .Where(x => x.Id == request.TelegramMessageId)
            .Select(x => new
            {
                CardId = x.Card!.Id,
            })
            .FirstOrDefaultAsync(cancellationToken);
        
        // Message is not stored, save it // TODO - store only if it is the first message
        if (savedMessage is null)
        {
            var card = new Card
            {
                Content = request.Text,
                UserId = request.UserId,
                CreatedAt = request.SentAt,
                TelegramMessageId = request.TelegramMessageId,
                TelegramMessage = new TelegramMessage
                {
                    Id = request.TelegramMessageId,
                }
            };

            context.Add(card);
            await context.SaveChangesAsync(cancellationToken);
            return new GetOrCreateMessageResult
            {
                Result = Result.MainMessageCreated,
            };
        }
        
        await context.Cards
            .Where(x => x.TelegramMessageId == request.TelegramMessageId)
            .ExecuteUpdateAsync(upd => upd
                .SetProperty(x => x.Content, request.Text),
                cancellationToken);
        
        return new GetOrCreateMessageResult
        {
            Result = Result.MainMessageUpdated,
        };
    }

    private async Task<long> GetOrCreateTelegramMediaGroupId(
        string groupId,
        CancellationToken cancellationToken)
    {
        var data = await context.TelegramMediaGroups
            .Where(x => x.ExternalId == groupId)
            .Select(x => new { x.Id })
            .FirstOrDefaultAsync(cancellationToken);

        if (data is not null)
            return data.Id;

        var group = new TelegramMediaGroup
        {
            ExternalId = groupId,
        };
        
        context.Add(group);
        await context.SaveChangesAsync(cancellationToken);
        
        return group.Id;
    }
}

public class GetOrCreateMessageResult
{
    public required Result? Result { get; set; }
}

public enum Result
{
    MainMessageCreated,
    MainMessageUpdated,
}
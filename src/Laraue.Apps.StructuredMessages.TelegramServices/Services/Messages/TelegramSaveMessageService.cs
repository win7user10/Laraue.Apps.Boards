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
            TelegramMessageId = getOrCreateResult.TelegramMessageId,
        };
        
        await DeleteOldAttachments(getOrCreateResult.TelegramMessageId, cancellationToken);
        
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
        
        // If this unique file id already stored for file then skip
        // If not stored, then remove previous and store
        var thumbnailPhoto = request.Photos[0];
        var originalPhoto = request.Photos.Last();
        var photos = new List<(PhotoSize, PhotoType)>
        {
            (thumbnailPhoto!, PhotoType.Thumbnail)
        };
        
        if (originalPhoto != thumbnailPhoto)
            photos.Add((originalPhoto!, PhotoType.Original));

        await DeleteOldAttachments(getOrCreateResult.TelegramMessageId, cancellationToken);
        
        var groupId = Guid.NewGuid();
        foreach (var (photo, type) in photos)
        {
            var fileId = await GetOrCreateMessageFileId(
                photo,
                saveFileToStorage: type == PhotoType.Thumbnail,
                cancellationToken);

            var messageFile = new TelegramMessagePhoto
            {
                TelegramMessageId = getOrCreateResult.TelegramMessageId,
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

    private async Task DeleteOldAttachments(long telegramMessageId, CancellationToken cancellationToken)
    {
        await context.TelegramPhotos
            .Where(x => x.TelegramMessageId == telegramMessageId)
            .ExecuteDeleteAsync(cancellationToken);

        await context.TelegramVideos
            .Where(x => x.TelegramMessageId == telegramMessageId)
            .ExecuteDeleteAsync(cancellationToken);
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
            .Where(x => x.TelegramMessageId == request.TelegramMessageId)
            .Where(x => x.TelegramChatId == request.TelegramUserId)
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
            .OrderBy(x => x.Id)
            .Select(x => new
            {
                x.Id,
                CardId = x.Card == null ? (long?)null : x.Card.Id
            })
            .FirstOrDefaultAsyncEF(cancellationToken);
        
        if (savedMessage is null)
        {
            savedMessage = new TelegramMessage
            {
                TelegramMessageId = request.TelegramMessageId,
                TelegramChatId = request.TelegramUserId,
                TelegramMediaGroupId = groupId,
            };

            context.Add(savedMessage);
            await context.SaveChangesAsync(cancellationToken);
        }
        
        var cardForMessageIsCreated = (firstGroupMessageData?.CardId).HasValue;
        if (!cardForMessageIsCreated)
        {
            var card = new Card
            {
                Content = request.Text,
                UserId = request.UserId,
                CreatedAt = request.SentAt,
                TelegramMessageId = request.TelegramMessageId,
                TelegramMessage = savedMessage
            };
                
            context.Add(card);
            await context.SaveChangesAsync(cancellationToken);
            
            return new GetOrCreateMessageResult
            {
                Result = Result.MainMessageUpdated,
                TelegramMessageId = savedMessage.Id,
            };
        }
        
        // The case when first message was deleted and text added to the second
        if (request.Text is not null && firstGroupMessageData is not null)
        {
            // TODO - here we can detect and remove previous messages. But should we?
            await context.Cards
                .Where(x => x.Id == firstGroupMessageData.CardId)
                .ExecuteUpdateAsync(upd => upd
                        .SetProperty(x => x.Content, request.Text),
                    cancellationToken);
                
            return new GetOrCreateMessageResult
            {
                Result = Result.MainMessageUpdated,
                TelegramMessageId = savedMessage.Id,
            };
        }
        
        return new GetOrCreateMessageResult
        {
            Result = null,
            TelegramMessageId = savedMessage.Id,
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
            .Where(x => x.TelegramMessageId == request.TelegramMessageId)
            .Where(x => x.TelegramChatId == request.TelegramUserId)
            .Select(x => new
            {
                CardId = x.Card != null ? (long?)x.Card.Id : null,
                x.Id,
            })
            .FirstOrDefaultAsync(cancellationToken);
        
        // Message is not stored, save it // TODO - store only if it is the first message
        if (savedMessage?.CardId is null)
        {
            var card = new Card
            {
                Content = request.Text,
                UserId = request.UserId,
                CreatedAt = request.SentAt,
                TelegramMessageId = savedMessage?.Id,
                TelegramMessage = savedMessage is null
                    ? new TelegramMessage
                    {
                        TelegramMessageId = request.TelegramMessageId,
                        TelegramChatId = request.TelegramUserId,
                    }
                    : null
            };

            context.Add(card);
            await context.SaveChangesAsync(cancellationToken);
            return new GetOrCreateMessageResult
            {
                Result = Result.MainMessageCreated,
                TelegramMessageId = savedMessage?.Id ?? card.TelegramMessage?.Id ?? 0
            };
        }
        
        await context.Cards
            .Where(x => x.TelegramMessageId == savedMessage.Id)
            .ExecuteUpdateAsync(upd => upd
                .SetProperty(x => x.Content, request.Text),
                cancellationToken);
        
        return new GetOrCreateMessageResult
        {
            Result = Result.MainMessageUpdated,
            TelegramMessageId = savedMessage.Id,
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
    public required long TelegramMessageId { get; set; }
    public required Result? Result { get; set; }
}

public enum Result
{
    MainMessageCreated,
    MainMessageUpdated,
}
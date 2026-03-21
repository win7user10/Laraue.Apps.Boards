using Laraue.Apps.StructuredMessages.TelegramHost.Resources;
using Laraue.Apps.StructuredMessages.TelegramServices;
using Laraue.Apps.StructuredMessages.TelegramServices.Services.Messages;
using Laraue.Telegram.NET.Abstractions;
using Laraue.Telegram.NET.Core.Extensions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using File = Laraue.Apps.StructuredMessages.TelegramServices.Services.Messages.File;
using PhotoSize = Laraue.Apps.StructuredMessages.TelegramServices.Services.Messages.PhotoSize;

namespace Laraue.Apps.StructuredMessages.TelegramHost;

public class HandleAllMessagesMiddleware(
    RequestContext context,
    ITelegramMessageService telegramMessageService,
    ITelegramBotClient botClient)
    : ITelegramMiddleware
{
    private const string ThumbnailMimeType = "image/jpg";
    
    public async Task InvokeAsync(Func<CancellationToken, Task> next, CancellationToken ct)
    {
        await next(ct);
        
        if (context.GetExecutedRoute() is null && context.Update.Type == UpdateType.Message)
        {
            var message = context.Update.Message!;
            var text = message.Text!;

            SaveMessageTelegramRequest? request = message.Type switch
            {
                MessageType.Text => GetMessageRequest(message),
                MessageType.Photo => GetPhotoRequest(message),
                MessageType.Video => GetVideoRequest(message),
                MessageType.Animation => GetAnimationRequest(message),
                _ => null
            };

            if (request is not null)
            {
                await telegramMessageService.HandleSaveMessage(request, ct);
                context.SetExecutedRoute(
                    new ExecutedRouteInfo("HandleAllMessagesMiddleware", text));
            }
            else
            {
                await botClient.SendMessage(
                    context.Update.GetUserId(),
                    string.Format(Phrases.MessageTypeIsNotAvailable, message.Type),
                    cancellationToken: ct);
            }
        }
    }
    
    private SaveTextMessageTelegramRequest GetMessageRequest(Message message)
    {
        var text = message.Text;
        
        return new SaveTextMessageTelegramRequest
        {
            Text = text,
            TelegramMessageId = message.MessageId,
            UserId = context.UserId,
            TelegramUserId = context.Update.GetUserId(),
            SentAt = message.Date,
            From = message.From?.Username,
            MediaGroupId = message.MediaGroupId,
        };
    }

    private SaveImageMessageTelegramRequest GetPhotoRequest(Message message)
    {
        var text = message.Caption;
        return new SaveImageMessageTelegramRequest
        {
            Text = text,
            TelegramMessageId = message.MessageId,
            UserId = context.UserId,
            TelegramUserId = context.Update.GetUserId(),
            SentAt = message.Date,
            From = message.From?.Username,
            MediaGroupId = message.MediaGroupId,
            Photos = message.Photo!
                .Select(photo => new PhotoSize
                {
                    FileId = photo.FileId,
                    FileUniqueId = photo.FileUniqueId,
                    Height = photo.Height,
                    Width = photo.Width,
                    FileSize = photo.FileSize,
                    FileName = null,
                    MimeType = ThumbnailMimeType,
                })
                .ToArray(),
        };
    }
    
    private SaveVideoMessageTelegramRequest GetVideoRequest(Message message)
    {
        var text = message.Caption;
        var video = message.Video!;
        
        return new SaveVideoMessageTelegramRequest
        {
            Text = text,
            TelegramMessageId = message.MessageId,
            UserId = context.UserId,
            TelegramUserId = context.Update.GetUserId(),
            SentAt = message.Date,
            From = message.From?.Username,
            MediaGroupId = message.MediaGroupId,
            Height = video.Height,
            Width = video.Width,
            Thumbnail = video.Thumbnail is not null
                ? new PhotoSize
                {
                    FileSize = video.Thumbnail.FileSize,
                    FileName = null,
                    FileId = video.Thumbnail.FileId,
                    FileUniqueId = video.Thumbnail.FileUniqueId,
                    Height = video.Thumbnail.Height,
                    Width = video.Thumbnail.Width,
                    MimeType = ThumbnailMimeType
                } : null,
            Video = new File
            {
                FileSize = video.FileSize,
                FileName = video.FileName,
                FileId = video.FileId,
                FileUniqueId = video.FileUniqueId,
                MimeType = video.MimeType,
            },
            Duration = video.Duration,
        };
    }
    
    private SaveVideoMessageTelegramRequest GetAnimationRequest(Message message)
    {
        var text = message.Caption;
        var video = message.Animation!;
        
        return new SaveVideoMessageTelegramRequest
        {
            Text = text,
            TelegramMessageId = message.MessageId,
            UserId = context.UserId,
            TelegramUserId = context.Update.GetUserId(),
            SentAt = message.Date,
            From = message.From?.Username,
            MediaGroupId = message.MediaGroupId,
            Height = video.Height,
            Width = video.Width,
            Thumbnail = video.Thumbnail is not null
                ? new PhotoSize
                {
                    FileSize = video.Thumbnail.FileSize,
                    FileName = null,
                    FileId = video.Thumbnail.FileId,
                    FileUniqueId = video.Thumbnail.FileUniqueId,
                    Height = video.Thumbnail.Height,
                    Width = video.Thumbnail.Width,
                    MimeType = ThumbnailMimeType
                } : null,
            Video = new File
            {
                FileSize = video.FileSize,
                FileName = video.FileName,
                FileId = video.FileId,
                FileUniqueId = video.FileUniqueId,
                MimeType = video.MimeType,
            },
            Duration = video.Duration,
        };
    }
}
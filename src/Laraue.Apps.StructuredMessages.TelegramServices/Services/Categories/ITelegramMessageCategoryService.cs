namespace Laraue.Apps.StructuredMessages.TelegramServices.Services.Categories;

public interface ITelegramMessageCategoryService
{
    Task HandleGetCategories(
        ReplyData reply,
        CancellationToken cancellationToken);
}
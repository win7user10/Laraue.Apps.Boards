using System.ComponentModel.DataAnnotations;
using Laraue.Telegram.NET.Authentication.Models;

namespace Laraue.Apps.StructuredMessages.DataAccess.Models;

public class User : ITelegramUser<Guid>
{
    public Guid Id { get; init; }
    public long TelegramId { get; init; }
    public string? TelegramUserName { get; init; }
    public string? TelegramLanguageCode { get; init; }
    public string? TelegramLastName { get; init; }
    public string? TelegramFirstName { get; init; }
    
    [MaxLength(7)]
    public string? Color { get; set; }
    public DateTime CreatedAt { get; init; }
    public IList<Epic>? Epics { get; set; }
    public IList<Space>? Spaces { get; set; }
    public IList<Organization>? Organizations { get; set; }
}
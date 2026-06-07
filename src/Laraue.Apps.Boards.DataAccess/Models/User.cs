using System.ComponentModel.DataAnnotations;
using Laraue.Telegram.NET.Authentication.Models;

namespace Laraue.Apps.Boards.DataAccess.Models;

public class User : ITelegramUser<Guid>
{
    public Guid Id { get; set; }
    public long TelegramId { get; set; }
    public string? TelegramUserName { get; set; }
    public string? TelegramLanguageCode { get; set; }
    public string? TelegramLastName { get; set; }
    public string? TelegramFirstName { get; set; }
    
    [MaxLength(7)]
    public string Color { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public IList<Epic>? Epics { get; set; }
    public IList<Space>? Spaces { get; set; }
    public IList<Organization>? Organizations { get; set; }
}
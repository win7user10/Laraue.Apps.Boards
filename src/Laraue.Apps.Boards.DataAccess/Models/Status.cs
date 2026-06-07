using System.ComponentModel.DataAnnotations;

namespace Laraue.Apps.Boards.DataAccess.Models;

public class Status
{
    public long Id { get; set; }

    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(7)]
    public string Color { get; set; } = string.Empty;
    
    public long EpicId { get; set; }
    public Epic? Epic { get; set; }

    public int SortOrder { get; set; }
    
    public IList<Issue>? Issues { get; set; }
}
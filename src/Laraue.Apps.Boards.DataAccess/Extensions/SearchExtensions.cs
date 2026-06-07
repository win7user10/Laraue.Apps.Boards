namespace Laraue.Apps.Boards.DataAccess.Extensions;

public static class SearchExtensions
{
    public static string AsSearchable(this string searchText)
    {
        return $"%{searchText}%";
    }
}
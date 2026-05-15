using LinqToDB;

namespace Laraue.Apps.StructuredMessages.Services;

public static class Linq2DbSqlExtensions
{
    [Sql.ExpressionAttribute("({0} ILIKE {1})", ServerSideOnly = true, IsPredicate = true)]
    public static bool ILike(this string column, string pattern)
        => throw new InvalidOperationException("This method is for LINQ translation only.");
}
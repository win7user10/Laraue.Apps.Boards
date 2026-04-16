namespace Laraue.Apps.StructuredMessages.Services;

public static class IdService
{
    private const long NullId = 0;

    public static long? ToNullableId(long? id)
    {
        return id == NullId ? null : id;
    }
    
    public static long ToNotNullableId(long? id)
    {
        return id ?? NullId;
    }
}
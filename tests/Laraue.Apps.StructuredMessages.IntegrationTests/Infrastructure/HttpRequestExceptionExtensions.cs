namespace Laraue.Apps.StructuredMessages.IntegrationTests.Infrastructure;

public static class HttpRequestExceptionExtensions
{
    public static T HasInnerException<T>(this HttpRequestException exception) where T : Exception
    {
        var innerException = Assert.IsType<T>(exception.InnerException);

        return innerException;
    }
}
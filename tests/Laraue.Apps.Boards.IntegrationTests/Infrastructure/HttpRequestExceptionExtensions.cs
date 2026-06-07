namespace Laraue.Apps.Boards.IntegrationTests.Infrastructure;

public static class HttpRequestExceptionExtensions
{
    public static T HasInnerException<T>(this HttpRequestException exception) where T : Exception
    {
        if (exception.InnerException is null)
            throw new InvalidOperationException("No inner exception thrown.", exception);
        
        var innerException = Assert.IsType<T>(exception.InnerException);

        return innerException;
    }
}
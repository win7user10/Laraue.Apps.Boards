using System.Security.Cryptography;

namespace Laraue.Apps.StructuredMessages.Services;

public static class StringGenerator
{
    private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    public static string GenerateJoinCode()
    {
        return GenerateRandomString(8);
    }
    
    public static string GenerateOrganizationPostfix()
    {
        return GenerateRandomString(4);
    }
    
    private static string GenerateRandomString(int length)
    {
        return string.Create(length, (chars: Chars, length), (span, state) =>
        {
            var charsArr = state.chars;
            for (var i = 0; i < state.length; i++)
                span[i] = charsArr[RandomNumberGenerator.GetInt32(charsArr.Length)];
        });
    }
}
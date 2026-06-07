using System.Security.Cryptography;
using System.Text;

namespace Laraue.Apps.Boards.Services;

public class ShardedPathStrategy
{
    // Shard by ID to distribute files
    public static string GetPath(string fileId, string? extension)
    {
        // Use first few characters of hash
        var hash = ComputeHash(fileId);
        var shard1 = hash[..2];  // 00-FF (256 directories)
        var shard2 = hash.Substring(2, 2);  // Another 256 directories
        
        var path = extension is null ? fileId : $"{fileId}.{extension}";
        return Path.Combine(shard1, shard2, path);
    }
    
    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}
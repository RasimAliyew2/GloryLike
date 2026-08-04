using System.Security.Cryptography;
using System.Text;

namespace GloryLikeBackend.Services.Security;

public static class TeamInvitationToken
{
    public static string Create()
    {
        return Convert
            .ToBase64String(
                RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public static string Hash(string token)
    {
        var bytes = SHA256.HashData(
            Encoding.UTF8.GetBytes(token.Trim()));

        return Convert.ToHexString(bytes);
    }
}

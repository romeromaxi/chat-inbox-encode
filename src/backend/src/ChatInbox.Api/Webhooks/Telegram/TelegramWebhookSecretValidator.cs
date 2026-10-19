using System.Security.Cryptography;
using System.Text;

namespace ChatInbox.Api.Webhooks.Telegram;

public sealed class TelegramWebhookSecretValidator
{
    public bool IsValid(string? providedSecret, string expectedSecret)
    {
        if (string.IsNullOrEmpty(providedSecret) || string.IsNullOrEmpty(expectedSecret))
        {
            return false;
        }

        var providedBytes = Encoding.UTF8.GetBytes(providedSecret);
        var expectedBytes = Encoding.UTF8.GetBytes(expectedSecret);

        return providedBytes.Length == expectedBytes.Length
            && CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
    }
}

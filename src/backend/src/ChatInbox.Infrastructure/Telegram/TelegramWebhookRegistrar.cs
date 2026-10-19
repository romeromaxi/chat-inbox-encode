using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace ChatInbox.Infrastructure.Telegram;

public sealed class TelegramWebhookRegistrar(
    HttpClient httpClient,
    IOptions<TelegramOptions> options)
    : ITelegramWebhookRegistrar
{
    public async Task RegisterAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        using var response = await httpClient.PostAsJsonAsync(
            $"/bot{settings.BotToken}/setWebhook",
            new
            {
                url = settings.WebhookUrl,
                secret_token = settings.WebhookSecretToken,
                allowed_updates = new[] { "message" }
            },
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<TelegramApiResponse>(cancellationToken);
        if (result is not { Ok: true })
        {
            throw new InvalidOperationException(result?.Description ?? "Telegram rejected the webhook");
        }
    }

    private sealed record TelegramApiResponse(
        [property: JsonPropertyName("ok")] bool Ok,
        [property: JsonPropertyName("description")] string? Description);
}

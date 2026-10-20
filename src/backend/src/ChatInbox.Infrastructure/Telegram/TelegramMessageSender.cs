using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ChatInbox.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace ChatInbox.Infrastructure.Telegram;

public sealed class TelegramMessageSender(
    HttpClient httpClient,
    IOptions<TelegramOptions> options) : ITelegramMessageSender
{
    public async Task<TelegramSentMessage> SendTextMessageAsync(
        long chatId,
        string text,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(
            $"/bot{options.Value.BotToken}/sendMessage",
            new { chat_id = chatId, text },
            cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<TelegramResponse>(
            cancellationToken: cancellationToken);

        if (!response.IsSuccessStatusCode || payload is not { Ok: true, Result: { } result })
        {
            throw new HttpRequestException(
                $"Telegram rejected sendMessage with status {(int)response.StatusCode}: {payload?.Description ?? "Unknown error"}",
                null,
                response.StatusCode);
        }

        return new TelegramSentMessage(
            result.MessageId,
            DateTimeOffset.FromUnixTimeSeconds(result.Date));
    }

    private sealed record TelegramResponse(
        [property: JsonPropertyName("ok")] bool Ok,
        [property: JsonPropertyName("result")] TelegramMessage? Result,
        [property: JsonPropertyName("description")] string? Description);

    private sealed record TelegramMessage(
        [property: JsonPropertyName("message_id")] long MessageId,
        [property: JsonPropertyName("date")] long Date);
}

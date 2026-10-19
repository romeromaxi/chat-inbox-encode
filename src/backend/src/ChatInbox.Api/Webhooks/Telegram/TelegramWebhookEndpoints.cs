using System.Globalization;
using System.Text.Json;
using ChatInbox.Contracts.IntegrationEvents;
using ChatInbox.Infrastructure.Messaging;
using ChatInbox.Infrastructure.Telegram;
using Microsoft.Extensions.Options;

namespace ChatInbox.Api.Webhooks.Telegram;

public static class TelegramWebhookEndpoints
{
    private const string SecretHeaderName = "X-Telegram-Bot-Api-Secret-Token";

    public static IEndpointRouteBuilder MapTelegramWebhook(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/webhooks/telegram", HandleAsync);
        return endpoints;
    }

    private static async Task<IResult> HandleAsync(
        HttpRequest request,
        TelegramWebhookSecretValidator secretValidator,
        IOptions<TelegramOptions> options,
        TelegramUpdateMapper mapper,
        IIntegrationEventPublisher publisher,
        CancellationToken cancellationToken)
    {
        var secretValues = request.Headers[SecretHeaderName];
        if (secretValues.Count != 1
            || !secretValidator.IsValid(secretValues[0], options.Value.WebhookSecretToken))
        {
            return Results.Unauthorized();
        }

        TelegramUpdateDto? update;

        try
        {
            update = await request.ReadFromJsonAsync<TelegramUpdateDto>(cancellationToken);
        }
        catch (JsonException)
        {
            return Results.BadRequest();
        }

        if (update is null)
        {
            return Results.BadRequest();
        }

        var mapping = mapper.Map(update);

        if (mapping.Status == TelegramUpdateMappingStatus.Ignored)
        {
            return Results.Ok();
        }

        if (mapping is not { Status: TelegramUpdateMappingStatus.Accepted, Message: { } message })
        {
            return Results.BadRequest();
        }

        await publisher.PublishAsync(
            message,
            RabbitMqTopology.IncomingTextRoutingKey,
            message.UpdateId.ToString(CultureInfo.InvariantCulture),
            cancellationToken);

        return Results.Ok();
    }
}

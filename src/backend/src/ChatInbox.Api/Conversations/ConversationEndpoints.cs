using ChatInbox.Application.Abstractions;
using ChatInbox.Application.Conversations;
using ChatInbox.Application.Messages;
using ChatInbox.Contracts.Inbox;
using ChatInbox.Contracts.Realtime;
using Microsoft.AspNetCore.Http.HttpResults;
using ChatInbox.Infrastructure.Messaging;
using ChatInbox.Contracts.IntegrationEvents;

namespace ChatInbox.Api.Conversations;

public static class ConversationEndpoints
{
    public static IEndpointRouteBuilder MapConversationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var conversations = endpoints.MapGroup("/api/conversations");

        conversations.MapGet("", GetConversationsAsync);
        conversations.MapGet("/{conversationId:guid}/messages", GetMessagesAsync);
        conversations.MapPost("/{conversationId:guid}/read", MarkAsReadAsync);
        conversations.MapPost("/{conversationId:guid}/messages", SendMessageAsync);

        return endpoints;
    }

    private static async Task<IResult> SendMessageAsync(
        Guid conversationId,
        SendMessageRequest request,
        IQueueOutgoingMessageHandler handler,
        IIntegrationEventPublisher publisher,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Text) || request.Text.Length > 4096)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.Text)] = ["Text is required and cannot exceed 4096 characters."]
            });
        }

        var result = await handler.HandleAsync(
            conversationId,
            request.Text,
            timeProvider.GetUtcNow(),
            cancellationToken);

        if (result is null)
        {
            return TypedResults.NotFound();
        }

        await publisher.PublishAsync(
            result.OutgoingMessage,
            RabbitMqTopology.OutgoingTextRoutingKey,
            result.Message.Id.ToString(),
            cancellationToken);
        await publisher.PublishAsync(
            result.ConversationUpdated,
            RabbitMqTopology.ConversationUpdatedRoutingKey,
            result.Message.Id.ToString(),
            cancellationToken);

        return TypedResults.Accepted(uri: (string?)null, value: result.Message);
    }

    private sealed record SendMessageRequest(string Text);

    private static async Task<Ok<IReadOnlyList<ConversationSummary>>> GetConversationsAsync(
        IConversationReader reader,
        CancellationToken cancellationToken,
        bool? unreadOnly = null)
    {
        var conversations = await reader.GetConversationsAsync(
            unreadOnly ?? false,
            cancellationToken);
        return TypedResults.Ok(conversations);
    }

    private static async Task<Results<Ok<IReadOnlyList<MessageSummary>>, NotFound>> GetMessagesAsync(
        Guid conversationId,
        IConversationReader reader,
        CancellationToken cancellationToken)
    {
        var messages = await reader.GetMessagesAsync(conversationId, cancellationToken);
        return messages is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(messages);
    }

    private static async Task<Results<NoContent, NotFound>> MarkAsReadAsync(
        Guid conversationId,
        IMarkConversationReadHandler handler,
        CancellationToken cancellationToken)
    {
        var found = await handler.HandleAsync(conversationId, cancellationToken);
        return found
            ? TypedResults.NoContent()
            : TypedResults.NotFound();
    }
}

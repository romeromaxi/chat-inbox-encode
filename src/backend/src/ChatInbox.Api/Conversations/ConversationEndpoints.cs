using ChatInbox.Application.Abstractions;
using ChatInbox.Application.Conversations;
using ChatInbox.Contracts.Inbox;
using ChatInbox.Contracts.Realtime;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ChatInbox.Api.Conversations;

public static class ConversationEndpoints
{
    public static IEndpointRouteBuilder MapConversationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var conversations = endpoints.MapGroup("/api/conversations");

        conversations.MapGet("", GetConversationsAsync);
        conversations.MapGet("/{conversationId:guid}/messages", GetMessagesAsync);
        conversations.MapPost("/{conversationId:guid}/read", MarkAsReadAsync);

        return endpoints;
    }

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

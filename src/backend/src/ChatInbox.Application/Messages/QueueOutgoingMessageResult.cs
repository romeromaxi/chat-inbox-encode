using ChatInbox.Contracts.IntegrationEvents;
using ChatInbox.Contracts.Realtime;

namespace ChatInbox.Application.Messages;

public sealed record QueueOutgoingMessageResult(
    MessageSummary Message,
    OutgoingTextMessageIntegrationEvent OutgoingMessage,
    ConversationUpdatedIntegrationEvent ConversationUpdated);

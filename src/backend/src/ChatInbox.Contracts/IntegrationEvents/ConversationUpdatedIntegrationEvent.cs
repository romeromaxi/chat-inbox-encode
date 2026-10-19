using ChatInbox.Contracts.Realtime;

namespace ChatInbox.Contracts.IntegrationEvents;

public sealed record ConversationUpdatedIntegrationEvent(
    ConversationUpdatedEvent Conversation);

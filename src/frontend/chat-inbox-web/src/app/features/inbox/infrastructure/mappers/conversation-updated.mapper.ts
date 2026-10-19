import { Injectable } from '@angular/core';
import { ConversationSummary } from '../../domain/models/conversation-summary';
import { ConversationUpdatedEvent } from '../realtime/contracts/conversation-updated-event';

@Injectable({ providedIn: 'root' })
export class ConversationUpdatedMapper {
  toDomain(event: ConversationUpdatedEvent): ConversationSummary {
    return {
      conversationId: event.conversationId,
      contact: {
        id: event.contact.id,
        firstName: event.contact.firstName,
        lastName: event.contact.lastName,
        username: event.contact.username,
      },
      lastMessage: {
        id: event.lastMessage.id,
        text: event.lastMessage.text,
        direction: event.lastMessage.direction,
        telegramSentAt: event.lastMessage.telegramSentAt,
        receivedAt: event.lastMessage.receivedAt,
      },
      unreadCount: event.unreadCount,
      lastMessageAt: event.lastMessageAt,
    };
  }
}

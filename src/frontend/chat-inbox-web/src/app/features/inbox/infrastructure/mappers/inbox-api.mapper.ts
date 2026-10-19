import { Injectable } from '@angular/core';
import { ConversationSummary, MessageSummary } from '../../domain/models/conversation-summary';
import { ConversationSummaryDto, MessageSummaryDto } from '../api/contracts/inbox-api.contracts';

@Injectable({ providedIn: 'root' })
export class InboxApiMapper {
  toConversation(dto: ConversationSummaryDto): ConversationSummary {
    return {
      conversationId: dto.conversationId,
      contact: {
        id: dto.contact.id,
        firstName: dto.contact.firstName,
        lastName: dto.contact.lastName,
        username: dto.contact.username,
      },
      lastMessage: this.toMessage(dto.lastMessage),
      unreadCount: dto.unreadCount,
      lastMessageAt: dto.lastMessageAt,
    };
  }

  toMessage(dto: MessageSummaryDto): MessageSummary {
    return {
      id: dto.id,
      text: dto.text,
      direction: dto.direction,
      telegramSentAt: dto.telegramSentAt,
      receivedAt: dto.receivedAt,
    };
  }
}

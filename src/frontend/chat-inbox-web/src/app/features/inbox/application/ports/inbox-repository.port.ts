import { InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';
import { ConversationSummary, MessageSummary } from '../../domain/models/conversation-summary';

export interface InboxRepositoryPort {
  getConversations(): Observable<readonly ConversationSummary[]>;
  getMessages(conversationId: string): Observable<readonly MessageSummary[]>;
  markAsRead(conversationId: string): Observable<void>;
}

export const INBOX_REPOSITORY_PORT = new InjectionToken<InboxRepositoryPort>('InboxRepositoryPort');

import { InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';
import { ConversationSummary } from '../../domain/models/conversation-summary';

export interface ConversationUpdatesPort {
  readonly conversationUpdates: Observable<ConversationSummary>;

  connect(): Observable<void>;
  disconnect(): Promise<void>;
}

export const CONVERSATION_UPDATES_PORT = new InjectionToken<ConversationUpdatesPort>(
  'ConversationUpdatesPort',
);

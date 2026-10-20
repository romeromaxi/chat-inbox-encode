import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { InboxRepositoryPort } from '../../application/ports/inbox-repository.port';
import { ConversationSummary, MessageSummary } from '../../domain/models/conversation-summary';
import { InboxApiMapper } from '../mappers/inbox-api.mapper';
import {
  ConversationSummaryDto,
  MessageSummaryDto,
  SendMessageRequestDto,
} from './contracts/inbox-api.contracts';

const CONVERSATIONS_URL = '/api/conversations';

@Injectable()
export class HttpInboxRepositoryAdapter implements InboxRepositoryPort {
  private readonly httpClient = inject(HttpClient);
  private readonly mapper = inject(InboxApiMapper);

  getConversations(): Observable<readonly ConversationSummary[]> {
    return this.httpClient
      .get<ConversationSummaryDto[]>(CONVERSATIONS_URL)
      .pipe(map((conversations) => conversations.map((item) => this.mapper.toConversation(item))));
  }

  getMessages(conversationId: string): Observable<readonly MessageSummary[]> {
    return this.httpClient
      .get<MessageSummaryDto[]>(`${CONVERSATIONS_URL}/${conversationId}/messages`)
      .pipe(map((messages) => messages.map((item) => this.mapper.toMessage(item))));
  }

  markAsRead(conversationId: string): Observable<void> {
    return this.httpClient.post<void>(`${CONVERSATIONS_URL}/${conversationId}/read`, null);
  }

  sendMessage(conversationId: string, text: string): Observable<MessageSummary> {
    const request: SendMessageRequestDto = { text };

    return this.httpClient
      .post<MessageSummaryDto>(`${CONVERSATIONS_URL}/${conversationId}/messages`, request)
      .pipe(map((message) => this.mapper.toMessage(message)));
  }
}

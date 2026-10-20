import { DOCUMENT } from '@angular/common';
import { DestroyRef, inject, Injectable } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, defer, EMPTY, finalize, map, Observable, take, tap, throwError } from 'rxjs';
import { INBOX_REPOSITORY_PORT } from '../ports/inbox-repository.port';
import { CONVERSATION_UPDATES_PORT } from '../ports/conversation-updates.port';
import { InboxStore } from '../state/inbox.store';
import { InboxFilter } from '../../domain/models/inbox-filter';

@Injectable({ providedIn: 'root' })
export class InboxFacade {
  private readonly destroyRef = inject(DestroyRef);
  private readonly document = inject(DOCUMENT);
  private readonly store = inject(InboxStore);
  private readonly inboxRepository = inject(INBOX_REPOSITORY_PORT);
  private readonly conversationUpdates = inject(CONVERSATION_UPDATES_PORT);
  private initialized = false;

  constructor() {
    this.destroyRef.onDestroy(() => void this.conversationUpdates.disconnect());
  }

  initialize(): void {
    if (this.initialized) {
      return;
    }

    this.initialized = true;

    this.conversationUpdates.conversationUpdates
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((conversation) => {
        this.store.upsertConversation(conversation);
        this.store.upsertMessage(conversation.conversationId, conversation.lastMessage);
      });

    this.conversationUpdates.connectionStatus
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((status) => this.store.setConnectionStatus(status));

    this.conversationUpdates.connect().pipe(takeUntilDestroyed(this.destroyRef)).subscribe();

    this.inboxRepository
      .getConversations()
      .pipe(
        take(1),
        catchError(() => EMPTY),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((conversations) => this.store.mergeConversations(conversations));
  }

  openConversation(conversationId: string): void {
    const previousConversationId = this.store.selectedConversationId();

    if (previousConversationId === conversationId) {
      return;
    }

    if (previousConversationId && this.canMarkAsRead()) {
      this.markAsRead(previousConversationId);
    }

    this.store.selectConversation(conversationId);
    this.loadMessages(conversationId);

    if (this.canMarkAsRead()) {
      this.markAsRead(conversationId);
    }
  }

  closeConversation(): void {
    this.store.selectConversation(null);
  }

  setFilter(filter: InboxFilter): void {
    this.closeConversation();
    this.store.setActiveFilter(filter);
  }

  sendMessage(text: string): Observable<void> {
    return defer(() => {
      const conversationId = this.store.selectedConversationId();

      if (!conversationId) {
        return EMPTY;
      }

      const snapshot = this.store.optimisticallySendMessage(conversationId, text);

      if (!snapshot) {
        return EMPTY;
      }

      return this.inboxRepository.sendMessage(conversationId, text).pipe(
        take(1),
        tap((message) => this.store.confirmOptimisticMessage(snapshot, message)),
        map(() => undefined),
        catchError((error: unknown) => {
          this.store.rollbackOptimisticMessage(snapshot);
          return throwError(() => error);
        }),
        takeUntilDestroyed(this.destroyRef),
      );
    });
  }

  private loadMessages(conversationId: string): void {
    this.store.setHistoryLoading(conversationId, true);

    this.inboxRepository
      .getMessages(conversationId)
      .pipe(
        take(1),
        catchError(() => EMPTY),
        finalize(() => this.store.setHistoryLoading(conversationId, false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((messages) => this.store.mergeMessages(conversationId, messages));
  }

  private markAsRead(conversationId: string): void {
    const snapshot = this.store.optimisticallyMarkAsRead(conversationId);

    if (!snapshot) {
      return;
    }

    this.inboxRepository
      .markAsRead(conversationId)
      .pipe(
        take(1),
        catchError(() => {
          this.store.rollbackRead(snapshot);
          return EMPTY;
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe();
  }

  private canMarkAsRead(): boolean {
    return this.document.visibilityState === 'visible' && this.document.hasFocus();
  }
}

import { computed, Injectable, signal } from '@angular/core';
import { ConversationSummary, MessageSummary } from '../../domain/models/conversation-summary';
import { ConnectionStatus } from '../../domain/models/connection-status';
import { InboxFilter } from '../../domain/models/inbox-filter';

export interface OptimisticReadSnapshot {
  readonly conversationId: string;
  readonly lastMessageId: string;
  readonly unreadCount: number;
}

export interface OptimisticSendSnapshot {
  readonly conversationId: string;
  readonly temporaryMessageId: string;
  readonly previousConversation: ConversationSummary;
}

@Injectable({ providedIn: 'root' })
export class InboxStore {
  private readonly activeFilterState = signal<InboxFilter>('all');
  private readonly selectedConversationIdState = signal<string | null>(null);
  private readonly conversationsState = signal<ReadonlyMap<string, ConversationSummary>>(new Map());
  private readonly messagesState = signal<ReadonlyMap<string, readonly MessageSummary[]>>(
    new Map(),
  );
  private readonly loadingHistoriesState = signal<ReadonlySet<string>>(new Set());
  private readonly connectionStatusState = signal<ConnectionStatus>('disconnected');

  readonly activeFilter = this.activeFilterState.asReadonly();
  readonly selectedConversationId = this.selectedConversationIdState.asReadonly();
  readonly conversations = computed(() =>
    Array.from(this.conversationsState().values()).sort(
      (left, right) =>
        Date.parse(right.lastMessageAt) - Date.parse(left.lastMessageAt) ||
        left.conversationId.localeCompare(right.conversationId),
    ),
  );
  readonly selectedConversation = computed(() => {
    const conversationId = this.selectedConversationId();
    return conversationId ? (this.conversationsState().get(conversationId) ?? null) : null;
  });
  readonly selectedMessages = computed(() => {
    const conversationId = this.selectedConversationId();
    return conversationId ? (this.messagesState().get(conversationId) ?? []) : [];
  });
  readonly selectedHistoryLoading = computed(() => {
    const conversationId = this.selectedConversationId();
    return conversationId ? this.loadingHistoriesState().has(conversationId) : false;
  });
  readonly connectionStatus = this.connectionStatusState.asReadonly();
  readonly visibleConversations = computed(() => {
    const conversations = this.conversations();
    const selectedConversationId = this.selectedConversationId();

    return this.activeFilter() === 'unread'
      ? conversations.filter(
          ({ conversationId, unreadCount }) =>
            unreadCount > 0 || conversationId === selectedConversationId,
        )
      : conversations;
  });

  setActiveFilter(filter: InboxFilter): void {
    this.activeFilterState.set(filter);
  }

  selectConversation(conversationId: string | null): void {
    this.selectedConversationIdState.set(conversationId);
  }

  setConnectionStatus(status: ConnectionStatus): void {
    this.connectionStatusState.set(status);
  }

  mergeConversations(conversations: readonly ConversationSummary[]): void {
    this.conversationsState.update((current) => {
      const merged = new Map(conversations.map((item) => [item.conversationId, item]));

      current.forEach((currentConversation, conversationId) => {
        const loadedConversation = merged.get(conversationId);

        if (
          !loadedConversation ||
          Date.parse(currentConversation.lastMessageAt) >=
            Date.parse(loadedConversation.lastMessageAt)
        ) {
          merged.set(conversationId, currentConversation);
        }
      });

      return merged;
    });
  }

  upsertConversation(conversation: ConversationSummary): void {
    this.conversationsState.update((current) => {
      const next = new Map(current);
      const existing = current.get(conversation.conversationId);
      const preserveReadState =
        existing?.lastMessage.id === conversation.lastMessage.id && existing.unreadCount === 0;

      next.set(
        conversation.conversationId,
        preserveReadState ? { ...conversation, unreadCount: 0 } : conversation,
      );
      return next;
    });
  }

  optimisticallyMarkAsRead(conversationId: string): OptimisticReadSnapshot | null {
    const conversation = this.conversationsState().get(conversationId);

    if (!conversation || conversation.unreadCount === 0) {
      return null;
    }

    const snapshot: OptimisticReadSnapshot = {
      conversationId,
      lastMessageId: conversation.lastMessage.id,
      unreadCount: conversation.unreadCount,
    };

    this.conversationsState.update((current) => {
      const next = new Map(current);
      next.set(conversationId, { ...conversation, unreadCount: 0 });
      return next;
    });

    return snapshot;
  }

  rollbackRead(snapshot: OptimisticReadSnapshot): void {
    this.conversationsState.update((current) => {
      const conversation = current.get(snapshot.conversationId);

      if (
        !conversation ||
        conversation.lastMessage.id !== snapshot.lastMessageId ||
        conversation.unreadCount !== 0
      ) {
        return current;
      }

      const next = new Map(current);
      next.set(snapshot.conversationId, {
        ...conversation,
        unreadCount: snapshot.unreadCount,
      });
      return next;
    });
  }

  mergeMessages(conversationId: string, messages: readonly MessageSummary[]): void {
    this.messagesState.update((current) => {
      const existing = current.get(conversationId) ?? [];
      const merged = new Map(messages.map((message) => [message.id, message]));

      existing.forEach((message) => {
        if (!merged.has(message.id)) {
          merged.set(message.id, message);
        }
      });

      const next = new Map(current);
      next.set(
        conversationId,
        Array.from(merged.values()).sort(
          (left, right) => Date.parse(left.telegramSentAt) - Date.parse(right.telegramSentAt),
        ),
      );
      return next;
    });
  }

  upsertMessage(conversationId: string, message: MessageSummary): void {
    if (this.selectedConversationId() !== conversationId) {
      return;
    }

    this.messagesState.update((current) => {
      const messages = current.get(conversationId) ?? [];

      const next = new Map(current);
      const existingIndex = messages.findIndex(({ id }) => id === message.id);
      const updatedMessages =
        existingIndex === -1
          ? [...messages, message]
          : messages.map((existingMessage, index) =>
              index === existingIndex ? message : existingMessage,
            );
      next.set(conversationId, updatedMessages);
      return next;
    });
  }

  optimisticallySendMessage(conversationId: string, text: string): OptimisticSendSnapshot | null {
    const conversation = this.conversationsState().get(conversationId);

    if (!conversation) {
      return null;
    }

    const now = new Date().toISOString();
    const message: MessageSummary = {
      id: crypto.randomUUID(),
      text,
      direction: 'outgoing',
      status: 'pending',
      telegramSentAt: now,
      receivedAt: now,
    };
    const snapshot: OptimisticSendSnapshot = {
      conversationId,
      temporaryMessageId: message.id,
      previousConversation: conversation,
    };

    this.upsertMessage(conversationId, message);
    this.conversationsState.update((current) => {
      const next = new Map(current);
      next.set(conversationId, {
        ...conversation,
        lastMessage: message,
        lastMessageAt: now,
      });
      return next;
    });

    return snapshot;
  }

  confirmOptimisticMessage(snapshot: OptimisticSendSnapshot, message: MessageSummary): void {
    this.messagesState.update((current) => {
      const messages = current.get(snapshot.conversationId) ?? [];
      const withoutTemporary = messages.filter(({ id }) => id !== snapshot.temporaryMessageId);
      const existingIndex = withoutTemporary.findIndex(({ id }) => id === message.id);
      const existingMessage = existingIndex === -1 ? null : withoutTemporary[existingIndex];
      const confirmedMessage =
        existingMessage && existingMessage.status !== 'pending' ? existingMessage : message;
      const confirmedMessages =
        existingIndex === -1
          ? [...withoutTemporary, confirmedMessage]
          : withoutTemporary.map((currentMessage, index) =>
              index === existingIndex ? confirmedMessage : currentMessage,
            );
      const next = new Map(current);
      next.set(snapshot.conversationId, confirmedMessages);
      return next;
    });

    this.conversationsState.update((current) => {
      const conversation = current.get(snapshot.conversationId);

      if (!conversation || conversation.lastMessage.id !== snapshot.temporaryMessageId) {
        return current;
      }

      const next = new Map(current);
      next.set(snapshot.conversationId, {
        ...conversation,
        lastMessage: message,
        lastMessageAt: message.telegramSentAt,
      });
      return next;
    });
  }

  rollbackOptimisticMessage(snapshot: OptimisticSendSnapshot): void {
    this.messagesState.update((current) => {
      const messages = current.get(snapshot.conversationId) ?? [];
      const next = new Map(current);
      next.set(
        snapshot.conversationId,
        messages.filter(({ id }) => id !== snapshot.temporaryMessageId),
      );
      return next;
    });

    this.conversationsState.update((current) => {
      const conversation = current.get(snapshot.conversationId);

      if (!conversation || conversation.lastMessage.id !== snapshot.temporaryMessageId) {
        return current;
      }

      const next = new Map(current);
      next.set(snapshot.conversationId, snapshot.previousConversation);
      return next;
    });
  }

  setHistoryLoading(conversationId: string, loading: boolean): void {
    this.loadingHistoriesState.update((current) => {
      const next = new Set(current);

      if (loading) {
        next.add(conversationId);
      } else {
        next.delete(conversationId);
      }

      return next;
    });
  }
}

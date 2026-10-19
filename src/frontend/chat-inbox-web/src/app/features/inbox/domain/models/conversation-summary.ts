export interface ContactSummary {
  readonly id: string;
  readonly firstName: string;
  readonly lastName: string | null;
  readonly username: string | null;
}

export interface MessageSummary {
  readonly id: string;
  readonly text: string;
  readonly direction: 'incoming' | 'outgoing';
  readonly telegramSentAt: string;
  readonly receivedAt: string;
}

export interface ConversationSummary {
  readonly conversationId: string;
  readonly contact: ContactSummary;
  readonly lastMessage: MessageSummary;
  readonly unreadCount: number;
  readonly lastMessageAt: string;
}

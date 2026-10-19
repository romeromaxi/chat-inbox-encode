export interface ConversationUpdatedEvent {
  readonly conversationId: string;
  readonly contact: {
    readonly id: string;
    readonly firstName: string;
    readonly lastName: string | null;
    readonly username: string | null;
  };
  readonly lastMessage: {
    readonly id: string;
    readonly text: string;
    readonly direction: 'incoming';
    readonly telegramSentAt: string;
    readonly receivedAt: string;
  };
  readonly unreadCount: number;
  readonly lastMessageAt: string;
}

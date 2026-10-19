export interface ContactSummaryDto {
  readonly id: string;
  readonly firstName: string;
  readonly lastName: string | null;
  readonly username: string | null;
}

export interface MessageSummaryDto {
  readonly id: string;
  readonly text: string;
  readonly direction: 'incoming' | 'outgoing';
  readonly telegramSentAt: string;
  readonly receivedAt: string;
}

export interface ConversationSummaryDto {
  readonly conversationId: string;
  readonly contact: ContactSummaryDto;
  readonly lastMessage: MessageSummaryDto;
  readonly unreadCount: number;
  readonly lastMessageAt: string;
}

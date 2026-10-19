import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { ConversationSummary } from '../../../domain/models/conversation-summary';

@Component({
  selector: 'app-conversation-list-item',
  templateUrl: './conversation-list-item.component.html',
  styleUrl: './conversation-list-item.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConversationListItemComponent {
  readonly conversation = input.required<ConversationSummary>();
  readonly selected = input(false);
  readonly conversationOpened = output<string>();

  protected readonly fullName = computed(() => {
    const { firstName, lastName } = this.conversation().contact;
    return [firstName.trim(), lastName?.trim()].filter(Boolean).join(' ');
  });

  protected readonly primaryName = computed(() => {
    const username = this.conversation().contact.username?.trim();
    return username ? `@${username}` : this.fullName();
  });

  protected readonly secondaryName = computed(() => {
    const username = this.conversation().contact.username?.trim();
    return username && this.fullName() ? this.fullName() : null;
  });

  protected readonly initials = computed(() => {
    const { firstName, lastName, username } = this.conversation().contact;
    const nameInitials = [firstName, lastName]
      .map((value) => value?.trim().charAt(0).toUpperCase())
      .filter(Boolean)
      .join('');

    return nameInitials || username?.trim().charAt(0).toUpperCase() || null;
  });

  protected openConversation(): void {
    this.conversationOpened.emit(this.conversation().conversationId);
  }
}

import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { InboxFacade } from '../../../application/facades/inbox.facade';
import { InboxStore } from '../../../application/state/inbox.store';
import { ConversationListItemComponent } from '../conversation-list-item/conversation-list-item.component';

@Component({
  selector: 'app-conversation-list',
  imports: [ConversationListItemComponent],
  templateUrl: './conversation-list.component.html',
  styleUrl: './conversation-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConversationListComponent {
  protected readonly store = inject(InboxStore);
  private readonly inboxFacade = inject(InboxFacade);

  protected openConversation(conversationId: string): void {
    this.inboxFacade.openConversation(conversationId);
  }
}

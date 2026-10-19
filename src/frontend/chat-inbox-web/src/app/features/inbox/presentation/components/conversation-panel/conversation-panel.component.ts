import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { InboxStore } from '../../../application/state/inbox.store';
import { ConversationHeaderComponent } from '../conversation-header/conversation-header.component';
import { EmptyConversationStateComponent } from '../empty-conversation-state/empty-conversation-state.component';
import { MessageListComponent } from '../message-list/message-list.component';

@Component({
  selector: 'app-conversation-panel',
  imports: [ConversationHeaderComponent, EmptyConversationStateComponent, MessageListComponent],
  templateUrl: './conversation-panel.component.html',
  styleUrl: './conversation-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConversationPanelComponent {
  protected readonly store = inject(InboxStore);
}

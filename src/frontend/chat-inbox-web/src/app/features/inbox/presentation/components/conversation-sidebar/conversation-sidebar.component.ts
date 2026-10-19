import { ChangeDetectionStrategy, Component } from '@angular/core';
import { ConversationFilterComponent } from '../conversation-filter/conversation-filter.component';
import { ConversationListComponent } from '../conversation-list/conversation-list.component';

@Component({
  selector: 'app-conversation-sidebar',
  imports: [ConversationFilterComponent, ConversationListComponent],
  templateUrl: './conversation-sidebar.component.html',
  styleUrl: './conversation-sidebar.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConversationSidebarComponent {}

import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { InboxFacade } from '../../../application/facades/inbox.facade';
import { ConversationPanelComponent } from '../../components/conversation-panel/conversation-panel.component';
import { ConversationSidebarComponent } from '../../components/conversation-sidebar/conversation-sidebar.component';

@Component({
  selector: 'app-inbox-page',
  imports: [ConversationPanelComponent, ConversationSidebarComponent],
  templateUrl: './inbox-page.component.html',
  styleUrl: './inbox-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InboxPageComponent implements OnInit {
  private readonly inboxFacade = inject(InboxFacade);

  ngOnInit(): void {
    this.inboxFacade.initialize();
  }
}

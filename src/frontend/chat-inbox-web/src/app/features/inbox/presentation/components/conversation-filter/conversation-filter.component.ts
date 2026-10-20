import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { InboxStore } from '../../../application/state/inbox.store';
import { InboxFacade } from '../../../application/facades/inbox.facade';
import { InboxFilter } from '../../../domain/models/inbox-filter';

@Component({
  selector: 'app-conversation-filter',
  templateUrl: './conversation-filter.component.html',
  styleUrl: './conversation-filter.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConversationFilterComponent {
  protected readonly store = inject(InboxStore);
  private readonly inboxFacade = inject(InboxFacade);

  protected selectFilter(filter: InboxFilter): void {
    this.inboxFacade.setFilter(filter);
  }
}

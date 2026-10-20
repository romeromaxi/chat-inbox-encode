import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { ConversationSummary } from '../../../domain/models/conversation-summary';
import { ConnectionStatus } from '../../../domain/models/connection-status';

@Component({
  selector: 'app-conversation-header',
  templateUrl: './conversation-header.component.html',
  styleUrl: './conversation-header.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConversationHeaderComponent {
  readonly conversation = input.required<ConversationSummary>();
  readonly connectionStatus = input.required<ConnectionStatus>();
  readonly closeRequested = output<void>();

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

  protected readonly connectionLabel = computed(() => {
    const labels: Record<ConnectionStatus, string> = {
      connected: 'Connected',
      connecting: 'Connecting',
      reconnecting: 'Reconnecting',
      disconnected: 'Disconnected',
    };

    return labels[this.connectionStatus()];
  });
}

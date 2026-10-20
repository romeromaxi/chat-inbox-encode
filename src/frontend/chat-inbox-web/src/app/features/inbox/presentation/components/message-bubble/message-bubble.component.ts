import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { MessageSummary } from '../../../domain/models/conversation-summary';
import { InboxDateTimePipe } from '../../pipes/inbox-date-time.pipe';

@Component({
  selector: 'app-message-bubble',
  imports: [InboxDateTimePipe],
  templateUrl: './message-bubble.component.html',
  styleUrl: './message-bubble.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MessageBubbleComponent {
  readonly message = input.required<MessageSummary>();

  protected readonly statusLabel = computed(() => {
    const labels: Record<MessageSummary['status'], string> = {
      received: 'Recibido',
      pending: 'Pendiente',
      sent: 'Enviado',
      failed: 'Error',
    };

    return labels[this.message().status];
  });
}

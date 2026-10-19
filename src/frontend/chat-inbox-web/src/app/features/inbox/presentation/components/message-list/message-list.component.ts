import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MessageSummary } from '../../../domain/models/conversation-summary';
import { MessageBubbleComponent } from '../message-bubble/message-bubble.component';

@Component({
  selector: 'app-message-list',
  imports: [MessageBubbleComponent],
  templateUrl: './message-list.component.html',
  styleUrl: './message-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MessageListComponent {
  readonly messages = input.required<readonly MessageSummary[]>();
}

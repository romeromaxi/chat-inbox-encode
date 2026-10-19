import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-empty-conversation-state',
  templateUrl: './empty-conversation-state.component.html',
  styleUrl: './empty-conversation-state.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EmptyConversationStateComponent {}

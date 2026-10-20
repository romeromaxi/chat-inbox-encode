import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { InboxFacade } from '../../../application/facades/inbox.facade';

const MAX_MESSAGE_LENGTH = 4096;

@Component({
  selector: 'app-message-composer',
  imports: [ReactiveFormsModule],
  templateUrl: './message-composer.component.html',
  styleUrl: './message-composer.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MessageComposerComponent {
  private readonly inboxFacade = inject(InboxFacade);

  protected readonly maxMessageLength = MAX_MESSAGE_LENGTH;
  protected readonly sending = signal(false);
  protected readonly sendFailed = signal(false);
  protected readonly messageControl = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required, Validators.maxLength(MAX_MESSAGE_LENGTH)],
  });

  protected send(event: Event): void {
    event.preventDefault();

    const text = this.messageControl.value;

    if (this.sending() || this.messageControl.invalid || !text.trim()) {
      this.messageControl.markAsTouched();
      return;
    }

    this.sending.set(true);
    this.sendFailed.set(false);
    this.messageControl.setValue('');

    this.inboxFacade.sendMessage(text).subscribe({
      complete: () => this.sending.set(false),
      error: () => {
        this.sending.set(false);
        this.sendFailed.set(true);

        if (!this.messageControl.value) {
          this.messageControl.setValue(text);
        }
      },
    });
  }
}

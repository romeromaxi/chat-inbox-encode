import { EnvironmentProviders, makeEnvironmentProviders } from '@angular/core';
import { INBOX_REPOSITORY_PORT } from './application/ports/inbox-repository.port';
import { CONVERSATION_UPDATES_PORT } from './application/ports/conversation-updates.port';
import { HttpInboxRepositoryAdapter } from './infrastructure/api/http-inbox-repository.adapter';
import { SignalRConversationUpdatesAdapter } from './infrastructure/realtime/signalr-conversation-updates.adapter';

export function provideInboxFeature(): EnvironmentProviders {
  return makeEnvironmentProviders([
    {
      provide: INBOX_REPOSITORY_PORT,
      useClass: HttpInboxRepositoryAdapter,
    },
    {
      provide: CONVERSATION_UPDATES_PORT,
      useClass: SignalRConversationUpdatesAdapter,
    },
  ]);
}

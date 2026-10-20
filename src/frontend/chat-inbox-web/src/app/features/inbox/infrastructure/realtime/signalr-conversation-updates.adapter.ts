import { inject, Injectable } from '@angular/core';
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr';
import { BehaviorSubject, defer, Observable, retry, Subject, timer } from 'rxjs';
import { ConversationUpdatesPort } from '../../application/ports/conversation-updates.port';
import { ConnectionStatus } from '../../domain/models/connection-status';
import { ConversationSummary } from '../../domain/models/conversation-summary';
import { ConversationUpdatedMapper } from '../mappers/conversation-updated.mapper';
import { ConversationUpdatedEvent } from './contracts/conversation-updated-event';

const HUB_URL = '/hubs/inbox';
const CONVERSATION_UPDATED_EVENT = 'conversationUpdated';

@Injectable()
export class SignalRConversationUpdatesAdapter implements ConversationUpdatesPort {
  private readonly mapper = inject(ConversationUpdatedMapper);
  private readonly updatesSubject = new Subject<ConversationSummary>();
  private readonly connectionStatusSubject = new BehaviorSubject<ConnectionStatus>('disconnected');
  private readonly connection: HubConnection;

  readonly conversationUpdates = this.updatesSubject.asObservable();
  readonly connectionStatus = this.connectionStatusSubject.asObservable();

  constructor() {
    this.connection = new HubConnectionBuilder()
      .withUrl(HUB_URL)
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    this.connection.on(CONVERSATION_UPDATED_EVENT, (event: ConversationUpdatedEvent) => {
      this.updatesSubject.next(this.mapper.toDomain(event));
    });

    this.connection.onreconnecting(() => this.connectionStatusSubject.next('reconnecting'));
    this.connection.onreconnected(() => this.connectionStatusSubject.next('connected'));
    this.connection.onclose(() => this.connectionStatusSubject.next('disconnected'));
  }

  connect(): Observable<void> {
    return defer(async () => {
      if (this.connection.state === HubConnectionState.Disconnected) {
        this.connectionStatusSubject.next('connecting');
        await this.connection.start();
        this.connectionStatusSubject.next('connected');
      }
    }).pipe(
      retry({
        delay: (_, retryCount) => timer(Math.min(retryCount * 1_000, 5_000)),
      }),
    );
  }

  async disconnect(): Promise<void> {
    if (this.connection.state !== HubConnectionState.Disconnected) {
      await this.connection.stop();
    }

    this.connectionStatusSubject.next('disconnected');
  }
}

import { Injectable, inject, signal } from '@angular/core';
import { Subject } from 'rxjs';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { environment } from '../../../../environments/environment';
import { AuthService } from '../../../core/auth/auth.service';
import { ChatService } from './chat.service';
import { ChatMessage } from '../models/chat.models';

@Injectable({ providedIn: 'root' })
export class ChatHubService {
  private readonly auth = inject(AuthService);
  private readonly chatService = inject(ChatService);

  private hub?: HubConnection;

  readonly activeConversationId = signal<string | null>(null);

  readonly messageReceived$ = new Subject<ChatMessage>();
  readonly messagesRead$ = new Subject<{ convId: string; readAt: string }>();
  readonly userTyping$ = new Subject<{ convId: string; name: string }>();
  readonly userStoppedTyping$ = new Subject<string>();

  connect() {
    const token = this.auth.accessToken();
    if (!token || this.hub) return;

    this.hub = new HubConnectionBuilder()
      .withUrl(`${environment.apiUrl}/hubs/chat`, {
        // Factory is re-invoked on every (re)negotiate, so retries pick up a refreshed token.
        accessTokenFactory: () => this.auth.accessToken() ?? '',
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    this.hub.on('ReceiveMessage', (msg: ChatMessage) => {
      this.messageReceived$.next(msg);
      if (
        msg.conversationId !== this.activeConversationId() &&
        msg.senderUserId !== (this.auth.currentUser()?.userId ?? '')
      ) {
        this.chatService.markConversationUnread(msg.conversationId);
      }
    });

    this.hub.on('MessagesRead', (convId: string, readAt: string) =>
      this.messagesRead$.next({ convId, readAt })
    );

    this.hub.on('UserTyping', (convId: string, name: string) =>
      this.userTyping$.next({ convId, name })
    );

    this.hub.on('UserStoppedTyping', (convId: string) =>
      this.userStoppedTyping$.next(convId)
    );

    this.startWithRetry();
  }

  // The negotiate request bypasses the HTTP interceptor, so an expired token 401s the
  // first attempt; retry with backoff — by then the proactive/interceptor refresh has run.
  private startWithRetry(attempt = 0) {
    this.hub?.start().catch(err => {
      if (this.hub && attempt < 5) {
        setTimeout(() => this.startWithRetry(attempt + 1), Math.min(2000 * (attempt + 1), 10000));
      } else {
        console.error('SignalR connection failed:', err);
      }
    });
  }

  disconnect() {
    this.hub?.stop();
    this.hub = undefined;
  }

  invoke(method: string, ...args: unknown[]): Promise<void> {
    return this.hub?.invoke(method, ...args) ?? Promise.resolve();
  }
}

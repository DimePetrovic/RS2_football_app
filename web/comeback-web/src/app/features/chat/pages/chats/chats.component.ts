import {
  ChangeDetectionStrategy, Component, ElementRef, OnDestroy, OnInit,
  ViewChild, computed, inject, signal,
} from '@angular/core';
import { MatTooltipModule } from '@angular/material/tooltip';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { Subject, Subscription, debounceTime, distinctUntilChanged, switchMap } from 'rxjs';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatMenuModule } from '@angular/material/menu';
import { MatDialog } from '@angular/material/dialog';
import { AuthService } from '../../../../core/auth/auth.service';
import { ProfileService } from '../../../profile/services/profile.service';
import { ProfileSearchResult } from '../../../profile/models/profile.models';
import { TranslatePipe } from '../../../../core/i18n/translate.pipe';
import { ChatService } from '../../services/chat.service';
import { ChatHubService } from '../../services/chat-hub.service';
import { ChatMessage, ConversationSummary, GroupMember } from '../../models/chat.models';
import { TranslationService } from '../../../../core/i18n/translation.service';
import { ConfirmDialogComponent } from '../../../../shared/components/confirm-dialog/confirm-dialog.component';
import { PlayerBadgeComponent } from '../../../../shared/components/player-badge/player-badge.component';

@Component({
  selector: 'app-chats',
  imports: [
    MatTooltipModule,FormsModule, MatIconModule, MatButtonModule, MatMenuModule, MatProgressSpinnerModule, TranslatePipe, PlayerBadgeComponent],
  templateUrl: './chats.component.html',
  styleUrl: './chats.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChatsComponent implements OnInit, OnDestroy {
  @ViewChild('messagesEnd') private messagesEnd?: ElementRef<HTMLElement>;

  private readonly chatService = inject(ChatService);
  private readonly hub = inject(ChatHubService);
  private readonly auth = inject(AuthService);
  private readonly profileService = inject(ProfileService);
  private readonly route = inject(ActivatedRoute);
  private readonly dialog = inject(MatDialog);
  private readonly i18n = inject(TranslationService);

  readonly conversations = signal<ConversationSummary[]>([]);
  readonly selectedId = signal<string | null>(null);
  readonly messages = signal<ChatMessage[]>([]);
  readonly messageText = signal('');
  readonly loadingConvs = signal(true);
  readonly loadingMsgs = signal(false);
  readonly sending = signal(false);
  readonly myUserId = computed(() => this.auth.currentUser()?.userId ?? '');
  readonly typingName = signal<string | null>(null);
  readonly groupMembers = signal<GroupMember[]>([]);
  readonly memberCount = computed(() => this.groupMembers().length);
  readonly showMembers = signal(false);

  readonly showSearch = signal(false);
  readonly searchQuery = signal('');
  readonly searchResults = signal<ProfileSearchResult[]>([]);
  readonly searching = signal(false);

  private typingTimer?: ReturnType<typeof setTimeout>;
  private isTyping = false;
  private readonly searchSubject = new Subject<string>();
  private readonly subscriptions = new Subscription();

  readonly selectedConversation = computed(() =>
    this.conversations().find(c => c.conversationId === this.selectedId()) ?? null
  );

  ngOnInit() {
    this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      switchMap(q => {
        this.searching.set(true);
        return this.profileService.searchProfiles(q);
      }),
    ).subscribe({
      next: results => {
        const myId = this.myUserId();
        this.searchResults.set(results.filter(r => r.userId !== myId));
        this.searching.set(false);
      },
      error: () => this.searching.set(false),
    });

    this.subscribeToHub();
    this.loadConversations().then(() => {
      const groupId = this.route.snapshot.queryParamMap.get('groupId');
      if (groupId) { this.openGroupConversation(groupId); return; }
      const userId = this.route.snapshot.queryParamMap.get('userId');
      const name = this.route.snapshot.queryParamMap.get('name');
      if (userId && name) this.openOrCreateConversation(userId, name);
    });
  }

  ngOnDestroy() {
    this.clearTypingState();
    this.hub.activeConversationId.set(null);
    this.subscriptions.unsubscribe();
    this.searchSubject.complete();
  }

  private subscribeToHub() {
    this.subscriptions.add(
      this.hub.messagesRead$.subscribe(({ convId, readAt }) => {
        if (convId !== this.selectedId()) return;
        const ts = new Date(readAt).getTime();
        this.messages.update(msgs =>
          msgs.map(m => m.senderUserId === this.myUserId() && new Date(m.sentAt).getTime() <= ts
            ? { ...m, isRead: true } : m)
        );
      })
    );

    this.subscriptions.add(
      this.hub.userTyping$.subscribe(({ convId, name }) => {
        if (convId === this.selectedId()) this.typingName.set(name);
      })
    );

    this.subscriptions.add(
      this.hub.userStoppedTyping$.subscribe(convId => {
        if (convId === this.selectedId()) this.typingName.set(null);
      })
    );

    this.subscriptions.add(
      this.hub.messageReceived$.subscribe(msg => {
        const isActive = msg.conversationId === this.selectedId();
        const isMine = msg.senderUserId === this.myUserId();

        if (isActive) {
          this.messages.update(msgs => [...msgs, msg]);
          this.scrollToBottom();
          if (!isMine) this.hub.invoke('MarkAsRead', msg.conversationId).catch(() => {});
        }

        this.upsertConversationFromMessage(msg, isActive, isMine);
      })
    );
  }

  // Adds/updates a conversation in the sidebar from an incoming message and bubbles it to the top.
  // Handles conversations that are not yet in the local list: brand-new chats and ones
  // the user deleted for themselves (they reappear the moment a new message arrives).
  private upsertConversationFromMessage(msg: ChatMessage, isActive: boolean, isMine: boolean) {
    const markUnread = !isActive && !isMine;
    this.conversations.update(convs => {
      const existing = convs.find(c => c.conversationId === msg.conversationId);

      if (existing) {
        const updated: ConversationSummary = {
          ...existing,
          lastMessagePreview: msg.content,
          lastMessageAt: msg.sentAt,
          hasUnread: existing.hasUnread || markUnread,
        };
        return [updated, ...convs.filter(c => c.conversationId !== msg.conversationId)];
      }

      // Not in the local list (new chat, new group, or one deleted-for-me). A message alone
      // can't describe a group conversation, so pull the fresh summary from the server.
      if (!isMine) this.quietRefreshConversations();
      return convs;
    });
  }

  private quietRefreshConversations() {
    this.chatService.getConversations().subscribe({
      next: convs => {
        this.conversations.set(convs);
        this.chatService.syncUnreadConversations(convs);
      },
    });
  }

  openGroupConversation(groupId: string) {
    const existing = this.conversations().find(c => c.groupId === groupId);
    if (existing) { this.selectConversation(existing); this.closeSearch(); return; }
    this.chatService.getOrCreateGroupConversation(groupId).subscribe({
      next: conv => {
        this.conversations.update(convs => [conv, ...convs.filter(c => c.conversationId !== conv.conversationId)]);
        this.selectConversation(conv);
        this.closeSearch();
      },
    });
  }

  private loadGroupMembers(conversationId: string) {
    this.chatService.getGroupMembers(conversationId).subscribe({
      next: members => this.groupMembers.set(members),
      error: () => this.groupMembers.set([]),
    });
  }

  toggleMembers() {
    this.showMembers.update(v => !v);
  }

  isGroup(conv: ConversationSummary | null): boolean {
    return conv?.type === 'Group';
  }

  convName(conv: ConversationSummary | null): string {
    if (!conv) return '';
    return conv.type === 'Group' ? (conv.title ?? '') : (conv.otherUserDisplayName ?? '');
  }

  convInitial(conv: ConversationSummary | null): string {
    const name = this.convName(conv);
    return name ? name[0] : '?';
  }

  // Group chats show the sender badge above a message when the sender changes (WhatsApp-style grouping).
  showSenderBadge(index: number): boolean {
    const conv = this.selectedConversation();
    if (!conv || conv.type !== 'Group') return false;
    const msgs = this.messages();
    const m = msgs[index];
    if (!m || m.senderUserId === this.myUserId()) return false;
    return index === 0 || msgs[index - 1].senderUserId !== m.senderUserId;
  }

  private loadConversations(): Promise<void> {
    return new Promise(resolve => {
      this.chatService.getConversations().subscribe({
        next: convs => {
          this.conversations.set(convs);
          this.chatService.syncUnreadConversations(convs);
          this.loadingConvs.set(false);
          resolve();
        },
        error: () => {
          this.loadingConvs.set(false);
          resolve();
        },
      });
    });
  }

  selectConversation(conv: ConversationSummary) {
    if (this.selectedId() === conv.conversationId) return;
    this.clearTypingState();
    this.typingName.set(null);
    this.selectedId.set(conv.conversationId);
    this.hub.activeConversationId.set(conv.conversationId);
    this.messages.set([]);
    this.showMembers.set(false);
    this.groupMembers.set([]);
    if (conv.type === 'Group') this.loadGroupMembers(conv.conversationId);
    this.loadMessages(conv.conversationId);
    this.hub.invoke('JoinConversation', conv.conversationId).catch(() => {});
    this.hub.invoke('MarkAsRead', conv.conversationId).catch(() => {});
    this.conversations.update(convs =>
      convs.map(c => c.conversationId === conv.conversationId ? { ...c, hasUnread: false } : c)
    );
    this.chatService.markConversationRead(conv.conversationId);
  }

  private loadMessages(conversationId: string) {
    this.loadingMsgs.set(true);
    this.chatService.getMessages(conversationId).subscribe({
      next: msgs => {
        this.messages.set(msgs);
        this.loadingMsgs.set(false);
        this.scrollToBottom();
      },
      error: () => this.loadingMsgs.set(false),
    });
  }

  openOrCreateConversation(otherUserId: string, otherUserDisplayName: string) {
    if (otherUserId === this.myUserId()) return;
    const existing = this.conversations().find(c => c.otherUserId === otherUserId);
    if (existing) {
      this.selectConversation(existing);
      this.closeSearch();
      return;
    }
    this.chatService.startConversation({ otherUserId, otherUserDisplayName }).subscribe({
      next: conv => {
        this.conversations.update(convs => [conv, ...convs]);
        this.selectConversation(conv);
        this.closeSearch();
      },
    });
  }

  onSearchInput(q: string) {
    this.searchQuery.set(q);
    if (q.length >= 2) this.searchSubject.next(q);
    else this.searchResults.set([]);
  }

  closeSearch() {
    this.showSearch.set(false);
    this.searchQuery.set('');
    this.searchResults.set([]);
  }

  send() {
    const content = this.messageText().trim();
    const convId = this.selectedId();
    if (!content || !convId || this.sending()) return;

    this.clearTypingState();
    this.sending.set(true);
    this.messageText.set('');

    this.hub.invoke('SendMessage', convId, content)
      .then(() => this.sending.set(false))
      .catch(() => {
        this.messageText.set(content);
        this.sending.set(false);
      });
  }

  onTyping() {
    const convId = this.selectedId();
    if (!convId) return;

    if (!this.isTyping) {
      this.isTyping = true;
      this.hub.invoke('StartTyping', convId).catch(() => {});
    }

    clearTimeout(this.typingTimer);
    this.typingTimer = setTimeout(() => this.clearTypingState(), 2000);
  }

  private clearTypingState() {
    if (!this.isTyping) return;
    clearTimeout(this.typingTimer);
    this.typingTimer = undefined;
    this.isTyping = false;
    const convId = this.selectedId();
    if (convId) this.hub.invoke('StopTyping', convId).catch(() => {});
  }

  onKeydown(event: KeyboardEvent) {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.send();
    }
  }

  deleteConversation(conv: ConversationSummary, event: Event) {
    event.stopPropagation();
    const ref = this.dialog.open(ConfirmDialogComponent, {
      width: '320px',
      data: {
        titleKey: 'chat.deleteConversation.title',
        messageKey: 'chat.deleteConversation.message',
        messageParams: { name: this.convName(conv) },
        confirmLabelKey: 'chat.deleteConversation.confirm',
      },
    });
    ref.afterClosed().subscribe((confirmed: boolean) => {
      if (!confirmed) return;
      this.chatService.deleteConversation(conv.conversationId).subscribe({
        next: () => {
          this.conversations.update(list => list.filter(c => c.conversationId !== conv.conversationId));
          this.chatService.markConversationRead(conv.conversationId);
          if (this.selectedId() === conv.conversationId) {
            this.selectedId.set(null);
            this.messages.set([]);
            this.hub.activeConversationId.set(null);
          }
        },
      });
    });
  }

  deleteMessage(msg: ChatMessage, event: Event) {
    event.stopPropagation();
    this.chatService.deleteMessage(msg.conversationId, msg.id).subscribe({
      next: () => this.messages.update(list => list.filter(m => m.id !== msg.id)),
    });
  }

  formatTime(iso: string): string {
    const d = new Date(iso);
    return `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`;
  }

  formatDate(iso: string | null): string {
    if (!iso) return '';
    const d = new Date(iso);
    return `${d.getDate()}.${d.getMonth() + 1}.`;
  }

  private scrollToBottom() {
    setTimeout(() => {
      this.messagesEnd?.nativeElement.scrollIntoView({ behavior: 'smooth' });
    }, 50);
  }
}

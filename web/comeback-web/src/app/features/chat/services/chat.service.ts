import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { skipErrorToast } from '../../../core/notifications/error.interceptor';
import { ChatMessage, ConversationSummary, GroupMember, StartConversationRequest } from '../models/chat.models';

@Injectable({ providedIn: 'root' })
export class ChatService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  private readonly unreadConversationIds = new Set<string>();
  readonly unreadCount = signal(0);

  loadUnreadCount() {
    this.http.get<ConversationSummary[]>(`${this.base}/api/chat/conversations`, { context: skipErrorToast() }).subscribe({
      next: convs => this.syncUnreadConversations(convs),
      error: () => {},
    });
  }

  syncUnreadConversations(convs: ConversationSummary[]) {
    this.unreadConversationIds.clear();
    convs.filter(c => c.hasUnread).forEach(c => this.unreadConversationIds.add(c.conversationId));
    this.unreadCount.set(this.unreadConversationIds.size);
  }

  markConversationUnread(conversationId: string) {
    if (this.unreadConversationIds.has(conversationId)) return;
    this.unreadConversationIds.add(conversationId);
    this.unreadCount.set(this.unreadConversationIds.size);
  }

  markConversationRead(conversationId: string) {
    if (!this.unreadConversationIds.delete(conversationId)) return;
    this.unreadCount.set(this.unreadConversationIds.size);
  }

  getConversations() {
    return this.http.get<ConversationSummary[]>(`${this.base}/api/chat/conversations`);
  }

  startConversation(req: StartConversationRequest) {
    return this.http.post<ConversationSummary>(`${this.base}/api/chat/conversations`, req);
  }

  getOrCreateGroupConversation(groupId: string) {
    return this.http.post<ConversationSummary>(`${this.base}/api/chat/groups/${groupId}`, {});
  }

  getGroupMembers(conversationId: string) {
    return this.http.get<GroupMember[]>(`${this.base}/api/chat/conversations/${conversationId}/members`);
  }

  getMessages(conversationId: string, before?: string) {
    const params = before ? `?before=${encodeURIComponent(before)}` : '';
    return this.http.get<ChatMessage[]>(`${this.base}/api/chat/conversations/${conversationId}/messages${params}`);
  }

  deleteConversation(conversationId: string) {
    return this.http.delete<void>(`${this.base}/api/chat/conversations/${conversationId}`);
  }

  deleteMessage(conversationId: string, messageId: string) {
    return this.http.delete<void>(`${this.base}/api/chat/conversations/${conversationId}/messages/${messageId}`);
  }
}

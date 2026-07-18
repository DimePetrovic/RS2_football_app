export type ConversationType = 'Direct' | 'Group';

export interface ConversationSummary {
  conversationId: string;
  type: ConversationType;
  otherUserId: string | null;
  otherUserDisplayName: string | null;
  groupId: string | null;
  title: string | null;
  avatarUrl: string | null;
  lastMessagePreview: string | null;
  lastMessageAt: string | null;
  hasUnread: boolean;
}

export interface ChatMessage {
  id: string;
  conversationId: string;
  senderUserId: string;
  senderDisplayName: string;
  senderUsername: string | null;
  senderAvatarUrl: string | null;
  senderNationality: string | null;
  content: string;
  sentAt: string;
  isRead: boolean;
}

export interface GroupMember {
  userId: string;
  displayName: string;
  username: string | null;
  avatarUrl: string | null;
  nationality: string | null;
}

export interface StartConversationRequest {
  otherUserId: string;
  otherUserDisplayName: string;
}

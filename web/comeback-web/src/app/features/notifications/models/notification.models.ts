export interface NotificationResponse {
  id: string;
  type: string;
  payload: string | null;
  legacyTitle: string | null;
  legacyBody: string | null;
  isRead: boolean;
  createdAt: string;
  readAt: string | null;
}

export interface UnreadCountResponse {
  count: number;
}

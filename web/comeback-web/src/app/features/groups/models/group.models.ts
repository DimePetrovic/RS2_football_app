export interface GroupMember {
  profileId: string;
  userId: string;
  username: string;
  firstName: string;
  lastName: string;
  displayName: string | null;
  avatarUrl: string | null;
  role: 'Member' | 'Captain';
  joinedAt: string;
}

export interface GroupSummary {
  id: string;
  name: string;
  avatarUrl: string | null;
  memberCount: number;
  myRole: 'Member' | 'Captain';
  createdAt: string;
}

export interface GroupSearchResult {
  id: string;
  name: string;
  avatarUrl: string | null;
  memberCount: number;
}

export interface GroupDetail {
  id: string;
  name: string;
  avatarUrl: string | null;
  members: GroupMember[];
  myRole: 'Member' | 'Captain';
  createdAt: string;
  updatedAt: string;
}

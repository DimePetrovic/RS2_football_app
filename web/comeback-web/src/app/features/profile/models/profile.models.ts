export interface ProfileResponse {
  userId: string;
  username: string;
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  preferredPosition: string;
  canPlayGoalkeeper: boolean;
  youthSeasons: number;
  seniorSeasons: number;
  displayName: string | null;
  bio: string | null;
  skillLevel: string | null;
  avatarUrl: string | null;
  nationality: string | null;
  createdAt: string;
}

export interface ProfileSearchResult {
  userId: string;
  username: string;
  firstName: string;
  lastName: string;
  displayName: string | null;
  avatarUrl: string | null;
  nationality: string | null;
}

export interface UpdateProfileRequest {
  displayName?: string | null;
  bio?: string | null;
  position?: string | null;
  skillLevel?: string | null;
  avatarUrl?: string | null;
  nationality?: string | null;
}

export interface FollowCounts {
  followers: number;
  following: number;
}

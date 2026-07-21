export interface PlayerXpData {
  totalXp: number;
  level: number;
  careerXp: number;
  matchXp: number;
  youthSeasons: number;
  seniorSeasons: number;
  xpToNextLevel: number;
  updatedAt: string;
}

export interface PlayerProfileBffResponse {
  profile: ProfileData;
  rating: PlayerXpData | null;
}

export interface ProfileData {
  userId: string;
  firstName: string;
  lastName: string;
  displayName: string | null;
  bio: string | null;
  avatarUrl: string | null;
  preferredPosition: string | null;
  canPlayGoalkeeper: boolean;
  youthSeasons: number;
  seniorSeasons: number;
  skillLevel: string | null;
  createdAt: string;
}

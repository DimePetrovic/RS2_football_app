export interface PlayerComment {
  reviewerDisplayName: string;
  comment: string;
}

export interface PostPlayer {
  userId: string;
  displayName: string;
  username: string | null;
  avatarUrl: string | null;
  nationality: string | null;
  team: 'Home' | 'Away';
  isCaptain: boolean;
  goals: number;
  assists: number;
  ownGoals: number;
  overallRating: number | null;
  goalkeepingRating: number | null;
  defenseRating: number | null;
  attackRating: number | null;
  effortRating: number | null;
  comments: PlayerComment[];
}

export interface FeedPost {
  id: string;
  type: string;
  matchId: string;
  matchTitle: string;
  homeScore: number;
  awayScore: number;
  location: string | null;
  playedAt: string | null;
  homeTeamName: string | null;
  awayTeamName: string | null;
  createdAt: string;
  likeCount: number;
  likedByMe: boolean;
  commentCount: number;
  canInteract: boolean;
  organizerUserId: string | null;
  organizerDisplayName: string | null;
  organizerUsername: string | null;
  organizerAvatarUrl: string | null;
  organizerNationality: string | null;
  position: string | null;
  viewerAlreadyIn: boolean;
  players: PostPlayer[];
}

export interface PostComment {
  id: string;
  authorUserId: string;
  authorDisplayName: string;
  content: string;
  createdAt: string;
}

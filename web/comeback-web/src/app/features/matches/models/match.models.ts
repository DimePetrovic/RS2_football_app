export interface ParticipantResponse {
  id: string;
  userId: string;
  displayName: string;
  isOrganizer: boolean;
  isCaptain: boolean;
  team: 'None' | 'Home' | 'Away';
  status: string;
  invitedAt: string;
  respondedAt: string | null;
  isBench: boolean;
  isGuest: boolean;
  username: string | null;
  avatarUrl: string | null;
  nationality: string | null;
}

export interface GoalResponse {
  scorerUserId: string;
  scorerDisplayName: string;
  scoringTeam: 'None' | 'Home' | 'Away';
  isOwnGoal: boolean;
  assistUserId: string | null;
  assistDisplayName: string | null;
}

export interface GoalEntryRequest {
  scorerUserId: string;
  isOwnGoal: boolean;
  assistUserId: string | null;
}

export interface MatchSummaryResponse {
  id: string;
  title: string;
  type: string;
  status: string;
  organizerUserId: string;
  location: string | null;
  startsAt: string;
  durationMinutes: number | null;
  playersPerTeam: number;
  acceptedCount: number;
  createdAt: string;
}

export interface MatchDetailResponse {
  id: string;
  title: string;
  type: string;
  status: string;
  organizerUserId: string;
  location: string | null;
  startsAt: string;
  durationMinutes: number | null;
  playersPerTeam: number;
  maxSubstitutes: number;
  homeScore: number | null;
  awayScore: number | null;
  resultSubmittedAt: string | null;
  createdAt: string;
  participants: ParticipantResponse[];
  goals: GoalResponse[];
  groupId: string | null;
  groupName: string | null;
  opponentGroupId: string | null;
  opponentGroupName: string | null;
  opponentGroupCaptainUserId: string | null;
  opponentGroupCaptainDisplayName: string | null;
  opponentGroupInviteStatus: 'Pending' | 'Accepted' | 'Declined' | null;
  secondOrganizerUserId: string | null;
  myXpChange: number | null;
}

export interface CreateMatchInviteeDto {
  userId: string;
  displayName: string;
}

export interface CreateMatchRequest {
  title: string;
  type: number;
  location: string | null;
  startsAt: string;
  durationMinutes: number | null;
  playersPerTeam: number;
  maxSubstitutes: number;
  invitees: CreateMatchInviteeDto[];
  groupId: string | null;
  opponentGroupId: string | null;
  guestNames?: string[];
}

export interface SubmitResultRequest {
  homeScore: number;
  awayScore: number;
  goals: GoalEntryRequest[];
}

export interface UpdateMatchDetailsRequest {
  title: string;
  location: string | null;
  startsAt: string;
  durationMinutes: number | null;
}

export interface InvitePlayersRequest {
  invitees: CreateMatchInviteeDto[];
  guestNames?: string[];
}

export interface MatchReviewResponse {
  reviewerParticipantId: string;
  reviewedParticipantId: string;
  overallRating: number;
  goalkeepingRating: number | null;
  defenseRating: number | null;
  attackRating: number | null;
  effortRating: number | null;
  comment: string | null;
  createdAt: string;
}

export interface SubmitReviewRequest {
  reviewedParticipantId: string;
  overallRating: number;
  goalkeepingRating?: number | null;
  defenseRating?: number | null;
  attackRating?: number | null;
  effortRating?: number | null;
  comment?: string | null;
}

export interface PlayerReceivedReviewItem {
  matchId: string;
  matchTitle: string;
  reviewerUserId: string;
  reviewerDisplayName: string;
  reviewerUsername: string | null;
  reviewerAvatarUrl: string | null;
  reviewerNationality: string | null;
  overallRating: number;
  goalkeepingRating: number | null;
  defenseRating: number | null;
  attackRating: number | null;
  effortRating: number | null;
  comment: string | null;
  createdAt: string;
}

export interface PlayerMatchHistoryItem {
  matchId: string;
  title: string;
  status: string;
  startsAt: string;
  homeScore: number | null;
  awayScore: number | null;
  team: string;
}

export interface MatchMediaResponse {
  id: string;
  uploadedByUserId: string;
  uploaderDisplayName: string;
  mediaType: 'Image' | 'Video';
  url: string;
  thumbnailUrl: string | null;
  format: string | null;
  sizeInBytes: number | null;
  durationInSeconds: number | null;
  width: number | null;
  height: number | null;
  createdAt: string;
}

export interface AddMatchMediaRequest {
  mediaType: 'Image' | 'Video';
  storagePublicId: string;
  url: string;
  thumbnailUrl?: string | null;
  format?: string | null;
  sizeInBytes?: number | null;
  durationInSeconds?: number | null;
  width?: number | null;
  height?: number | null;
}

export interface PlayerStatsTimelineItem {
  matchId: string;
  startsAt: string;
  outcome: 'Win' | 'Draw' | 'Loss';
}

export interface PlayerOpponentStat {
  userId: string;
  displayName: string;
  username: string;
  nationality: string | null;
  avatarUrl: string | null;
  count: number;
}

export type PlayedWithRelation = 'All' | 'Teammate' | 'Opponent';

export interface GroupPlayStat {
  groupId: string;
  groupName: string;
  count: number;
}

export interface PlayerStatsResponse {
  organizedCount: number;
  organizedWithResult: number;
  playedCount: number;
  wins: number;
  draws: number;
  losses: number;
  goals: number;
  assists: number;
  timeline: PlayerStatsTimelineItem[];
  topBeaten: PlayerOpponentStat[];
  topLostTo: PlayerOpponentStat[];
  groupsPlayedWith: GroupPlayStat[];
}

export interface GroupOpponentStat {
  groupId: string;
  groupName: string;
  played: number;
  wins: number;
  draws: number;
  losses: number;
}

export interface GroupStatsResponse {
  playedCount: number;
  wins: number;
  draws: number;
  losses: number;
  opponents: GroupOpponentStat[];
}

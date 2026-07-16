import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { skipErrorToast } from '../../../core/notifications/error.interceptor';
import { CloudinaryUploadSignature } from '../../../core/media/media.models';
import {
  AddMatchMediaRequest,
  CreateMatchRequest,
  GroupStatsResponse,
  InvitePlayersRequest,
  MatchDetailResponse,
  MatchMediaResponse,
  PlayedWithRelation,
  PlayerStatsResponse,
  MatchReviewResponse,
  MatchSummaryResponse,
  PlayerMatchHistoryItem,
  PlayerReceivedReviewItem,
  SubmitResultRequest,
  SubmitReviewRequest,
  UpdateMatchDetailsRequest,
} from '../models/match.models';

@Injectable({ providedIn: 'root' })
export class MatchService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  getMyMatches() {
    return this.http.get<MatchSummaryResponse[]>(`${this.base}/api/matches`);
  }

  getMatch(id: string) {
    return this.http.get<MatchDetailResponse>(`${this.base}/api/matches/${id}`);
  }

  createMatch(req: CreateMatchRequest) {
    return this.http.post<{ id: string }>(`${this.base}/api/matches`, req, { context: skipErrorToast() });
  }

  respondToInvitation(matchId: string, accept: boolean) {
    return this.http.post<void>(`${this.base}/api/matches/${matchId}/respond`, { accept });
  }

  respondToGroupInvite(matchId: string, accept: boolean) {
    return this.http.post<void>(`${this.base}/api/matches/${matchId}/group-invite/respond`, { accept });
  }

  getGroupMatchHistory(groupId: string) {
    return this.http.get<MatchSummaryResponse[]>(`${this.base}/api/matches/groups/${groupId}/history`, { context: skipErrorToast() });
  }

  withdraw(matchId: string) {
    return this.http.post<void>(`${this.base}/api/matches/${matchId}/withdraw`, {});
  }

  submitResult(matchId: string, req: SubmitResultRequest) {
    return this.http.post<void>(`${this.base}/api/matches/${matchId}/result`, req, { context: skipErrorToast() });
  }

  joinViaPublicCall(matchId: string) {
    return this.http.post<void>(`${this.base}/api/matches/${matchId}/join`, {});
  }

  requestPlayers(matchId: string, position: string | null) {
    return this.http.post<void>(`${this.base}/api/matches/${matchId}/request-players`, { position });
  }

  cancelMatch(matchId: string) {
    return this.http.delete<void>(`${this.base}/api/matches/${matchId}`);
  }

  updateMatchDetails(matchId: string, req: UpdateMatchDetailsRequest) {
    return this.http.put<void>(`${this.base}/api/matches/${matchId}`, req);
  }

  inviteAdditionalPlayers(matchId: string, req: InvitePlayersRequest) {
    return this.http.post<void>(`${this.base}/api/matches/${matchId}/invite`, req);
  }

  removeParticipant(matchId: string, targetUserId: string) {
    return this.http.delete<void>(`${this.base}/api/matches/${matchId}/participants/${targetUserId}`);
  }

  assignToTeam(matchId: string, targetUserId: string, team: 'Home' | 'Away' | 'None') {
    return this.http.post<void>(`${this.base}/api/matches/${matchId}/teams/assign`, { targetUserId, team });
  }

  randomizeTeams(matchId: string) {
    return this.http.post<void>(`${this.base}/api/matches/${matchId}/teams/randomize`, {});
  }

  randomizeTeamsWithCaptains(matchId: string, homeCaptainId: string, awayCaptainId: string) {
    return this.http.post<void>(`${this.base}/api/matches/${matchId}/teams/randomize-captains`, { homeCaptainId, awayCaptainId });
  }

  balanceTeams(matchId: string) {
    return this.http.post<void>(`${this.base}/api/matches/${matchId}/teams/balance`, {});
  }

  getReviews(matchId: string) {
    return this.http.get<MatchReviewResponse[]>(`${this.base}/api/matches/${matchId}/reviews`);
  }

  submitReview(matchId: string, req: SubmitReviewRequest) {
    return this.http.post<void>(`${this.base}/api/matches/${matchId}/reviews`, req);
  }

  getMedia(matchId: string) {
    return this.http.get<MatchMediaResponse[]>(`${this.base}/api/matches/${matchId}/media`);
  }

  getMediaUploadSignature(matchId: string) {
    return this.http.post<CloudinaryUploadSignature>(`${this.base}/api/matches/${matchId}/media/upload-signature`, {});
  }

  addMedia(matchId: string, req: AddMatchMediaRequest) {
    return this.http.post<MatchMediaResponse>(`${this.base}/api/matches/${matchId}/media`, req);
  }

  deleteMedia(matchId: string, mediaId: string) {
    return this.http.delete<void>(`${this.base}/api/matches/${matchId}/media/${mediaId}`);
  }

  getPlayerMatchHistory(userId: string) {
    return this.http.get<PlayerMatchHistoryItem[]>(`${this.base}/api/matches/players/${userId}/history`, { context: skipErrorToast() });
  }

  getPlayerReceivedReviews(userId: string) {
    return this.http.get<PlayerReceivedReviewItem[]>(`${this.base}/api/matches/players/${userId}/reviews`, { context: skipErrorToast() });
  }

  getPlayerStats(userId: string) {
    return this.http.get<PlayerStatsResponse>(`${this.base}/api/matches/players/${userId}/stats`);
  }

  getPlayedWithMatches(userId: string, withId: string, relation: PlayedWithRelation) {
    return this.http.get<PlayerMatchHistoryItem[]>(
      `${this.base}/api/matches/players/${userId}/stats/matches`,
      { params: { withId, relation } });
  }

  getGroupStats(groupId: string) {
    return this.http.get<GroupStatsResponse>(`${this.base}/api/matches/groups/${groupId}/stats`);
  }
}

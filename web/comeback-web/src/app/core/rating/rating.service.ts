import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { skipErrorToast } from '../notifications/error.interceptor';
import { PlayerProfileBffResponse } from './rating.models';

@Injectable({ providedIn: 'root' })
export class RatingService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  getPlayerProfile(userId: string) {
    return this.http.get<PlayerProfileBffResponse>(`${this.base}/api/bff/player-profiles/${userId}`, { context: skipErrorToast() });
  }
}

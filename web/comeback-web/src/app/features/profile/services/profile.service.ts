import { Injectable, inject } from '@angular/core';
import { skipErrorToast } from '../../../core/notifications/error.interceptor';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { FollowCounts, ProfileResponse, ProfileSearchResult, UpdateProfileRequest } from '../models/profile.models';
import { CloudinaryUploadSignature } from '../../../core/media/media.models';

@Injectable({ providedIn: 'root' })
export class ProfileService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  getMyProfile() {
    return this.http.get<ProfileResponse>(`${this.base}/api/profiles/me`);
  }

  getProfile(userId: string) {
    return this.http.get<ProfileResponse>(`${this.base}/api/profiles/${userId}`);
  }

  updateMyProfile(req: UpdateProfileRequest) {
    return this.http.put<ProfileResponse>(`${this.base}/api/profiles/me`, req);
  }

  getAvatarUploadSignature() {
    return this.http.post<CloudinaryUploadSignature>(`${this.base}/api/profiles/me/avatar/upload-signature`, {});
  }

  searchProfiles(query: string) {
    return this.http.get<ProfileSearchResult[]>(`${this.base}/api/profiles/search`, {
      params: { query },
    });
  }

  getFollowing() {
    return this.http.get<ProfileSearchResult[]>(`${this.base}/api/profiles/me/following`);
  }

  getFollowCounts(userId: string) {
    return this.http.get<FollowCounts>(`${this.base}/api/profiles/${userId}/follow-counts`, { context: skipErrorToast() });
  }

  getFollowers(userId: string) {
    return this.http.get<ProfileSearchResult[]>(`${this.base}/api/profiles/${userId}/followers`);
  }

  getFollowingOf(userId: string) {
    return this.http.get<ProfileSearchResult[]>(`${this.base}/api/profiles/${userId}/following`);
  }

  getFollowStatus(userId: string) {
    return this.http.get<{ isFollowing: boolean }>(`${this.base}/api/profiles/${userId}/follow-status`, { context: skipErrorToast() });
  }

  follow(userId: string) {
    return this.http.post<void>(`${this.base}/api/profiles/${userId}/follow`, {});
  }

  unfollow(userId: string) {
    return this.http.delete<void>(`${this.base}/api/profiles/${userId}/follow`);
  }
}

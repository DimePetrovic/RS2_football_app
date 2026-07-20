import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { FeedPost, PostComment } from '../models/feed.models';

@Injectable({ providedIn: 'root' })
export class FeedService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  getFeed(page: number, pageSize = 20) {
    return this.http.get<FeedPost[]>(`${this.base}/api/feed`, {
      params: { page, pageSize },
    });
  }

  getPost(postId: string) {
    return this.http.get<FeedPost>(`${this.base}/api/posts/${postId}`);
  }

  toggleLike(postId: string) {
    return this.http.post<{ liked: boolean }>(`${this.base}/api/posts/${postId}/reactions`, {});
  }

  getComments(postId: string) {
    return this.http.get<PostComment[]>(`${this.base}/api/posts/${postId}/comments`);
  }

  addComment(postId: string, content: string) {
    return this.http.post<PostComment>(`${this.base}/api/posts/${postId}/comments`, { content });
  }
}

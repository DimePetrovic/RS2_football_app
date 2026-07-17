import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { GroupDetail, GroupSearchResult, GroupSummary } from '../models/group.models';

@Injectable({ providedIn: 'root' })
export class GroupService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/groups`;

  getMyGroups() {
    return this.http.get<GroupSummary[]>(`${this.base}/mine`);
  }

  getGroupById(groupId: string) {
    return this.http.get<GroupDetail>(`${this.base}/${groupId}`);
  }

  searchGroups(query: string, excludeOverlappingWithGroupId?: string) {
    const params: Record<string, string> = { query };
    if (excludeOverlappingWithGroupId) params['excludeOverlappingWithGroupId'] = excludeOverlappingWithGroupId;
    return this.http.get<GroupSearchResult[]>(`${this.base}/search`, { params });
  }

  createGroup(payload: { name: string; avatarUrl?: string | null; memberUserIds: string[] }) {
    return this.http.post<GroupSummary>(this.base, payload);
  }

  updateGroup(groupId: string, payload: { name: string; avatarUrl?: string | null }) {
    return this.http.put<void>(`${this.base}/${groupId}`, payload);
  }

  addMember(groupId: string, memberUserId: string) {
    return this.http.post<void>(`${this.base}/${groupId}/members`, { memberUserId });
  }

  removeMember(groupId: string, memberUserId: string) {
    return this.http.delete<void>(`${this.base}/${groupId}/members/${memberUserId}`);
  }

  leaveGroup(groupId: string) {
    return this.http.post<void>(`${this.base}/${groupId}/leave`, {});
  }

  deleteGroup(groupId: string) {
    return this.http.delete<void>(`${this.base}/${groupId}`);
  }
}

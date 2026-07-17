import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { AdminUserListItem } from '../models/admin.models';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  getAllUsers() {
    return this.http.get<AdminUserListItem[]>(`${this.base}/api/profiles/admin/users`);
  }
}

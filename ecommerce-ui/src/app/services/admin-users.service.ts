import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { AdminUser } from '../models/admin-user.model';
import { BaseApiService } from './base-api.service';
import { ApiResponse } from '../models/api-response.model';

interface PagedResult<T> { items: T[]; totalCount: number; }

@Injectable({ providedIn: 'root' })
export class AdminUsersService extends BaseApiService {
  private adminUrl = `${this.baseUrl}/admin`;

  constructor(http: HttpClient) {
    super(http);
  }

  private authHeaders() {
    return { Authorization: `Bearer ${localStorage.getItem('accessToken')}` };
  }

  getUsers(opts: { pageNumber?: number; pageSize?: number; searchTerm?: string; status?: string; role?: string; }): Observable<PagedResult<AdminUser>> {
    let params = new HttpParams();
    if (opts.pageNumber) params = params.set('PageNumber', opts.pageNumber);
    if (opts.pageSize) params = params.set('PageSize', opts.pageSize);
    if (opts.searchTerm) params = params.set('SearchTerm', opts.searchTerm);
    if (opts.status) params = params.set('Status', opts.status);
    if (opts.role) params = params.set('Role', opts.role);
    return this.handleRequest(
      this.http.get<ApiResponse<PagedResult<AdminUser>>>(`${this.adminUrl}/users`, { headers: this.authHeaders(), params })
    );
  }

  getUserById(id: number): Observable<AdminUser> {
    return this.handleRequest(
      this.http.get<ApiResponse<AdminUser>>(`${this.adminUrl}/users/${id}`, { headers: this.authHeaders() })
    );
  }

  assignRole(userId: number, role: string): Observable<any> {
    return this.http.post<any>(`${this.adminUrl}/users/assign-role`, { userId, role }, { headers: this.authHeaders() });
  }

  resetPassword(userId: number, newPassword: string): Observable<any> {
    return this.http.post<any>(`${this.adminUrl}/users/reset-password`, { userId, newPassword }, { headers: this.authHeaders() });
  }

  deactivate(userId: number): Observable<any> {
    return this.http.post<any>(`${this.adminUrl}/users/deactivate`, { userId }, { headers: this.authHeaders() });
  }

  activate(userId: number): Observable<any> {
    return this.http.post<any>(`${this.adminUrl}/users/activate`, { userId }, { headers: this.authHeaders() });
  }
}



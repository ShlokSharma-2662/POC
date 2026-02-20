import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BaseApiService } from './base-api.service';
import { ApiResponse } from '../models/api-response.model';

@Injectable({ providedIn: 'root' })
export class MonitoringService extends BaseApiService {
  private logUrl = `${this.baseUrl}/admin/metrics/log`;

  constructor(http: HttpClient) {
    super(http);
  }

  logMetric(endpoint: string, responseTimeMs: number): void {
    const payload = { endpoint, responseTimeMs };

    const headers = {
      Authorization: `Bearer ${localStorage.getItem('accessToken')}`,
    };

    this.handleRequest(
      this.http.post<ApiResponse<any>>(this.logUrl, payload, { headers })
    ).subscribe({
      next: () => {},
      error: (err) => {
        // Silent fail for monitoring
      },
    });
  }
}

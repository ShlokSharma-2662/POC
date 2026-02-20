import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { BaseApiService } from './base-api.service';
import { ApiResponse } from '../models/api-response.model';

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
}

export interface SystemMetric {
  id: number;
  endpoint: string;
  method: string;
  statusCode: number;
  responseTimeMs: number;
  timestamp: string;
  userAgent?: string;
  ipAddress?: string;
}

export interface ApiAlert {
  id: number;
  message: string;
  stackTrace?: string;
  path: string;
  timestamp: string;
  severity: string;
  userAgent?: string;
  ipAddress?: string;
}

@Injectable({
  providedIn: 'root'
})
export class AdminMetricsService extends BaseApiService {
  private adminUrl = `${this.baseUrl}/admin`;

  constructor(http: HttpClient) {
    super(http);
  }

  private getAuthHeaders() {
    return { Authorization: `Bearer ${localStorage.getItem('accessToken')}` };
  }

  getSystemMetrics(
    pageNumber: number = 1,
    pageSize: number = 10,
    endpointFilter?: string,
    statusCodeFilter?: number,
    fromDate?: Date,
    toDate?: Date
  ): Observable<PagedResult<SystemMetric>> {
    let params = new HttpParams()
      .set('PageNumber', pageNumber.toString())
      .set('PageSize', pageSize.toString());

    if (endpointFilter) {
      params = params.set('EndpointFilter', endpointFilter);
    }
    if (statusCodeFilter) {
      params = params.set('StatusCodeFilter', statusCodeFilter.toString());
    }
    if (fromDate) {
      params = params.set('FromDate', fromDate.toISOString());
    }
    if (toDate) {
      params = params.set('ToDate', toDate.toISOString());
    }

    const url = `${this.adminUrl}/metrics`;
    const headers = this.getAuthHeaders();

    return this.handleRequest(
      this.http.get<ApiResponse<PagedResult<SystemMetric>>>(url, {
        headers,
        params
      })
    );
  }

  getErrorLogs(
    pageNumber: number = 1,
    pageSize: number = 10,
    severityFilter?: string,
    searchTerm?: string
  ): Observable<PagedResult<ApiAlert>> {
    let params = new HttpParams()
      .set('PageNumber', pageNumber.toString())
      .set('PageSize', pageSize.toString());

    if (severityFilter) {
      params = params.set('SeverityFilter', severityFilter);
    }
    if (searchTerm) {
      params = params.set('SearchTerm', searchTerm);
    }

    return this.handleRequest(
      this.http.get<ApiResponse<PagedResult<ApiAlert>>>(`${this.adminUrl}/errors`, {
        headers: this.getAuthHeaders(),
        params
      })
    );
  }



  seedMetrics(): Observable<any> {
    return this.handleRequest(
      this.http.post<ApiResponse<any>>(`${this.adminUrl}/metrics/seed`, {}, {
        headers: this.getAuthHeaders()
      })
    );
  }
}

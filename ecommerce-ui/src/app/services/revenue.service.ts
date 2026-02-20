import { Injectable } from '@angular/core';
import { HttpClient, HttpParams, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { BaseApiService } from './base-api.service';
import { ApiResponse } from '../models/api-response.model';

export interface RevenuePeriod {
  startDate: string;
  endDate: string;
  revenue: number;
  orderCount: number;
  averageOrderValue: number;
  periodLabel: string;
}

export interface RevenueReport {
  totalRevenue: number;
  totalOrders: number;
  periods: RevenuePeriod[];
  reportStartDate: string;
  reportEndDate: string;
  reportType: string;
}

export interface RevenueSummary {
  totalRevenue: number;
  totalOrders: number;
  averageOrderValue: number;
  revenueGrowth: number;
  orderGrowth: number;
  lastUpdated: string;
}

@Injectable({
  providedIn: 'root'
})
export class RevenueService extends BaseApiService {
  private apiUrl = `${this.baseUrl}/admin/revenue`;

  constructor(http: HttpClient) {
    super(http);
  }

  getRevenueReport(params: {
    reportType?: string;
    startDate?: string;
    endDate?: string;
    month?: number;
    year?: number;
  }): Observable<RevenueReport> {
    let httpParams = new HttpParams();
    
    if (params.reportType) httpParams = httpParams.set('reportType', params.reportType);
    if (params.startDate) httpParams = httpParams.set('startDate', params.startDate);
    if (params.endDate) httpParams = httpParams.set('endDate', params.endDate);
    if (params.month) httpParams = httpParams.set('month', params.month.toString());
    if (params.year) httpParams = httpParams.set('year', params.year.toString());

    return this.handleRequest(
      this.http.get<ApiResponse<RevenueReport>>(`${this.apiUrl}/report`, {
        params: httpParams,
        headers: this.getAuthHeaders()
      })
    );
  }

  getRevenueSummary(period: string = '30days'): Observable<RevenueSummary> {
    const params = new HttpParams().set('period', period);
    
    return this.handleRequest(
      this.http.get<ApiResponse<RevenueSummary>>(`${this.apiUrl}/summary`, {
        params,
        headers: this.getAuthHeaders()
      })
    );
  }

  private getAuthHeaders(): HttpHeaders {
    const token = localStorage.getItem('accessToken');
    return token ? new HttpHeaders({ 'Authorization': `Bearer ${token}` }) : new HttpHeaders();
  }
}

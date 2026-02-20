import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { MonitoringService } from './monitoring.service';
import { BaseApiService } from './base-api.service';
import { ApiResponse } from '../models/api-response.model';

export interface Category {
  id: number;
  name: string;
}

@Injectable({
  providedIn: 'root',
})
export class CategoryService extends BaseApiService {
  private apiUrl = `${this.baseUrl}/categories`;

  constructor(
    http: HttpClient,
    private monitoring: MonitoringService
  ) {
    super(http);
  }

  getCategories(): Observable<Category[]> {
    const start = performance.now();

    return this.handleRequest(
      this.http.get<ApiResponse<Category[]>>(this.apiUrl)
    ).pipe(
      tap(() => {
        const responseTime = performance.now() - start;
      })
    );
  }
}

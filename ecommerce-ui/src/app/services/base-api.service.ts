import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError, catchError, map } from 'rxjs';
import { ApiResponse } from '../models/api-response.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class BaseApiService {
  protected baseUrl = environment.apiUrl;

  constructor(protected http: HttpClient) {}

  /**
   * Handles HTTP requests and extracts data from standardized API response
   */
  protected handleRequest<T>(request: Observable<ApiResponse<T>>): Observable<T> {
    return request.pipe(
      map((response: ApiResponse<T>) => {
        if (response.isSuccessful) {
          return response.data as T;
        } else {
          throw new Error(response.statusReason || 'API request failed');
        }
      }),
      catchError(this.handleError)
    );
  }

  /**
   * Handles HTTP requests that return the full API response
   */
  protected handleFullResponse<T>(request: Observable<ApiResponse<T>>): Observable<ApiResponse<T>> {
    return request.pipe(
      catchError(this.handleError)
    );
  }

  /**
   * Common error handler
   */
  private handleError(error: HttpErrorResponse | Error): Observable<never> {
    let errorMessage = 'An error occurred';

    if (error instanceof HttpErrorResponse) {
      console.error('HTTP Error Response:', {
        status: error.status,
        statusText: error.statusText,
        error: error.error,
        url: error.url
      });
      
      // Server-side error
      if (error.error && typeof error.error === 'object' && 'statusReason' in error.error) {
        errorMessage = error.error.statusReason;
      } else if (error.status === 0) {
        errorMessage = 'Unable to connect to server';
      } else if (error.status === 401) {
        errorMessage = 'Unauthorized access';
      } else if (error.status === 403) {
        errorMessage = 'Access forbidden';
      } else if (error.status === 404) {
        errorMessage = 'Resource not found';
      } else if (error.status >= 500) {
        errorMessage = 'Server error occurred';
      } else {
        errorMessage = error.message || `HTTP ${error.status} error`;
      }
    } else {
      // Client-side error
      console.error('Client Error:', error);
      errorMessage = error.message;
    }

    return throwError(() => new Error(errorMessage));
  }
}

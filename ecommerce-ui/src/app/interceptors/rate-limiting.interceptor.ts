import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { RateLimitingService } from '../services/rate-limiting.service';

export interface RateLimitInfo {
  limit: number;
  remaining: number;
  resetTime: number;
}

@Injectable()
export class RateLimitingInterceptor implements HttpInterceptor {
  private rateLimitInfo: RateLimitInfo | null = null;

  constructor(private rateLimitingService: RateLimitingService) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    return next.handle(req).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error.status === 429) {
          // Extract rate limit information from headers
          this.extractRateLimitInfo(error);
          
          // Handle rate limiting error using service
          this.rateLimitingService.handleRateLimitError(error);
        }
        return throwError(() => error);
      })
    );
  }

  private extractRateLimitInfo(error: HttpErrorResponse): void {
    const headers = error.headers;
    
    this.rateLimitInfo = {
      limit: parseInt(headers.get('X-RateLimit-Limit') || '0'),
      remaining: parseInt(headers.get('X-RateLimit-Remaining') || '0'),
      resetTime: parseInt(headers.get('X-RateLimit-Reset') || '0')
    };
  }

  private handleRateLimitError(error: HttpErrorResponse): void {
    const resetTime = this.rateLimitInfo?.resetTime;
    const remaining = this.rateLimitInfo?.remaining;
    
    if (resetTime) {
      const resetDate = new Date(resetTime * 1000);
      const timeUntilReset = Math.ceil((resetDate.getTime() - Date.now()) / 1000);
      
      // Show user-friendly error message
      const errorMessage = `Rate limit exceeded. Please try again in ${timeUntilReset} seconds.`;
      
      // You can emit this to a service or show a toast notification
    }
  }

  /**
   * Get current rate limit information
   */
  getRateLimitInfo(): RateLimitInfo | null {
    return this.rateLimitInfo;
  }

  /**
   * Clear rate limit information
   */
  clearRateLimitInfo(): void {
    this.rateLimitInfo = null;
  }
}

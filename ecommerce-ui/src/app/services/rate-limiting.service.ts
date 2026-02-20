import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

export interface RateLimitError {
  message: string;
  retryAfter: number;
  remaining: number;
  limit: number;
  resetTime: Date;
}

@Injectable({
  providedIn: 'root'
})
export class RateLimitingService {
  private rateLimitErrorSubject = new BehaviorSubject<RateLimitError | null>(null);
  public rateLimitError$ = this.rateLimitErrorSubject.asObservable();

  private isRateLimitedSubject = new BehaviorSubject<boolean>(false);
  public isRateLimited$ = this.isRateLimitedSubject.asObservable();

  /**
   * Handle rate limiting error
   */
  handleRateLimitError(error: any): void {
    const retryAfter = this.extractRetryAfter(error);
    const remaining = this.extractRemaining(error);
    const limit = this.extractLimit(error);
    const resetTime = this.calculateResetTime(retryAfter);

    const rateLimitError: RateLimitError = {
      message: this.generateErrorMessage(retryAfter),
      retryAfter,
      remaining,
      limit,
      resetTime
    };

    this.rateLimitErrorSubject.next(rateLimitError);
    this.isRateLimitedSubject.next(true);

    // Auto-clear rate limit after retry time
    setTimeout(() => {
      this.clearRateLimitError();
    }, retryAfter * 1000);
  }

  /**
   * Clear rate limiting error
   */
  clearRateLimitError(): void {
    this.rateLimitErrorSubject.next(null);
    this.isRateLimitedSubject.next(false);
  }

  /**
   * Get current rate limit error
   */
  getCurrentError(): RateLimitError | null {
    return this.rateLimitErrorSubject.value;
  }

  /**
   * Check if currently rate limited
   */
  isCurrentlyRateLimited(): boolean {
    return this.isRateLimitedSubject.value;
  }

  /**
   * Get formatted time until reset
   */
  getTimeUntilReset(): string {
    const error = this.getCurrentError();
    if (!error) return '';

    const now = new Date();
    const diff = error.resetTime.getTime() - now.getTime();
    
    if (diff <= 0) return 'Rate limit has been reset';

    const minutes = Math.floor(diff / 60000);
    const seconds = Math.floor((diff % 60000) / 1000);

    if (minutes > 0) {
      return `${minutes}m ${seconds}s`;
    } else {
      return `${seconds}s`;
    }
  }

  private extractRetryAfter(error: any): number {
    // Try to get retry-after from headers
    const retryAfter = error.headers?.get('Retry-After') || 
                      error.headers?.get('X-RateLimit-Reset') ||
                      error.error?.retryAfter;
    
    return retryAfter ? parseInt(retryAfter) : 60; // Default to 60 seconds
  }

  private extractRemaining(error: any): number {
    return parseInt(error.headers?.get('X-RateLimit-Remaining') || '0');
  }

  private extractLimit(error: any): number {
    return parseInt(error.headers?.get('X-RateLimit-Limit') || '0');
  }

  private calculateResetTime(retryAfter: number): Date {
    return new Date(Date.now() + (retryAfter * 1000));
  }

  private generateErrorMessage(retryAfter: number): string {
    if (retryAfter < 60) {
      return `Too many requests. Please wait ${retryAfter} seconds before trying again.`;
    } else {
      const minutes = Math.ceil(retryAfter / 60);
      return `Too many requests. Please wait ${minutes} minute${minutes > 1 ? 's' : ''} before trying again.`;
    }
  }
}

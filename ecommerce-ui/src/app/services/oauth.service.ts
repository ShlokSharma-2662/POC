import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject } from 'rxjs';
import { BaseApiService } from './base-api.service';
import { ApiResponse } from '../models/api-response.model';

export interface OAuthUserInfo {
  sub: string;
  name: string;
  given_name: string;
  family_name: string;
  email: string;
  picture?: string;
}

export interface OAuthAuthResult {
  isAuthenticated: boolean;
  user?: OAuthUserInfo;
  accessToken?: string;
  idToken?: string;
  refreshToken?: string;
}

@Injectable({
  providedIn: 'root'
})
export class OAuthService extends BaseApiService {
  private readonly oauthUrl = `${this.baseUrl}/oauth`;
  private oauthStateSubject = new BehaviorSubject<boolean>(false);
  public oauthState$ = this.oauthStateSubject.asObservable();

  constructor(http: HttpClient) {
    super(http);
  }

  /**
   * Initiate OAuth login flow (Microsoft)
   * @param returnUrl Optional return URL after successful authentication
   */
  initiateLogin(returnUrl?: string): void {
    const params = returnUrl ? `?returnUrl=${encodeURIComponent(returnUrl)}` : '';
    window.location.href = `${this.oauthUrl}/login${params}`;
  }

  /**
   * Initiate Google OAuth login flow
   * @param returnUrl Optional return URL after successful authentication (should be full URL)
   */
  initiateGoogleLogin(returnUrl?: string): void {
    // Default to the OAuth callback URL if no returnUrl is provided
    const callbackUrl = returnUrl || `${window.location.origin}/oauth/callback`;
    // Ensure it's a full URL (not relative)
    const fullReturnUrl = callbackUrl.startsWith('http') ? callbackUrl : `${window.location.origin}${callbackUrl}`;
    const params = `?returnUrl=${encodeURIComponent(fullReturnUrl)}`;
    window.location.href = `${this.baseUrl}/google-oauth/login${params}`;
  }

  /**
   * Get current user information from OAuth (Microsoft)
   */
  getUserInfo(): Observable<OAuthUserInfo> {
    return this.handleRequest(
      this.http.get<ApiResponse<OAuthUserInfo>>(`${this.oauthUrl}/userinfo`)
    );
  }

  /**
   * Get current user information from Google OAuth
   */
  getGoogleUserInfo(): Observable<OAuthUserInfo> {
    return this.handleRequest(
      this.http.get<ApiResponse<OAuthUserInfo>>(`${this.baseUrl}/google-oauth/userinfo`)
    );
  }

  /**
   * Logout from OAuth provider
   */
  logout(): Observable<{ logoutUrl: string }> {
    return this.handleRequest(
      this.http.post<ApiResponse<{ logoutUrl: string }>>(`${this.oauthUrl}/logout`, {})
    );
  }

  /**
   * Check if user is authenticated via OAuth
   */
  isOAuthAuthenticated(): boolean {
    // Check if we have OAuth session data
    return sessionStorage.getItem('oauth_authenticated') === 'true';
  }

  /**
   * Set OAuth authentication state
   */
  setOAuthState(isAuthenticated: boolean): void {
    if (isAuthenticated) {
      sessionStorage.setItem('oauth_authenticated', 'true');
    } else {
      sessionStorage.removeItem('oauth_authenticated');
    }
    this.oauthStateSubject.next(isAuthenticated);
  }

  /**
   * Clear OAuth authentication state
   */
  clearOAuthState(): void {
    sessionStorage.removeItem('oauth_authenticated');
    this.oauthStateSubject.next(false);
  }
}

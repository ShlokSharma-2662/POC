import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, tap, BehaviorSubject } from 'rxjs';
import { MonitoringService } from './services/monitoring.service';
import { BaseApiService } from './services/base-api.service';
import { ApiResponse } from './models/api-response.model';
import { OAuthUserInfo } from './services/oauth.service';

export interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  role: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthResponse {
  userId: number;
  firstName: string;
  lastName: string;
  email: string;
  token: string;
  role: string;
}

export interface ChangePasswordRequest {
  oldPassword: string;
  newPassword: string;
  confirmPassword: string;
}

@Injectable({
  providedIn: 'root',
})
export class AuthService extends BaseApiService {
  private readonly apiUrl = `${this.baseUrl}/auth`;
  
  // BehaviorSubject to emit authentication state changes
  private authStateSubject = new BehaviorSubject<boolean>(this.isLoggedIn());
  public authState$ = this.authStateSubject.asObservable();
  
  // OAuth user information
  private oauthUser: OAuthUserInfo | null = null;

  constructor(
    http: HttpClient,
    private monitoring: MonitoringService
  ) {
    super(http);
  }

  register(data: RegisterRequest): Observable<AuthResponse> {
    const start = performance.now();
    return this.handleRequest(
      this.http.post<ApiResponse<AuthResponse>>(`${this.apiUrl}/register`, data)
    ).pipe(
      tap((response) => {
        this.saveToken(response.token);
        this.updateAuthState(true); // Emit auth state change
        const end = performance.now();
        const responseTimeMs = end - start;
      })
    );
  }

  login(data: LoginRequest): Observable<AuthResponse> {
    const start = performance.now();
  
    return this.handleRequest(
      this.http.post<ApiResponse<AuthResponse>>(`${this.apiUrl}/login`, data)
    ).pipe(
      tap(response => {
        this.saveToken(response.token);
        this.updateAuthState(true); // Emit auth state change
        const responseTime = performance.now() - start;
      })
    );
  }
  

  private saveToken(token: string): void {
    // Store under both keys for backward compatibility with existing guards/components
    localStorage.setItem('authToken', token);
    localStorage.setItem('accessToken', token);
  }

  updateAuthState(isLoggedIn: boolean): void {
    this.authStateSubject.next(isLoggedIn);
  }

  getToken(): string | null {
    return localStorage.getItem('authToken') || localStorage.getItem('accessToken');
  }

  getRole(): string | null {
    const token = this.getToken();
    if (!token) return null;

    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      // 🔑 fix: use correct key
      return (
        payload[
          'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'
        ] || null
      );
    } catch {
      return null;
    }
  }

  // OAuth-specific methods
  setOAuthUser(userInfo: OAuthUserInfo): void {
    this.oauthUser = userInfo;
    // For OAuth users, we don't have a traditional JWT token
    // Instead, we'll use session-based authentication
    sessionStorage.setItem('oauth_user', JSON.stringify(userInfo));
    this.updateAuthState(true);
  }

  getOAuthUser(): OAuthUserInfo | null {
    if (this.oauthUser) {
      return this.oauthUser;
    }
    
    const stored = sessionStorage.getItem('oauth_user');
    if (stored) {
      this.oauthUser = JSON.parse(stored);
      return this.oauthUser;
    }
    
    return null;
  }

  isOAuthAuthenticated(): boolean {
    return !!this.getOAuthUser();
  }

  // Override isLoggedIn to check both traditional and OAuth authentication
  isLoggedIn(): boolean {
    return !!this.getToken() || this.isOAuthAuthenticated();
  }

  // Override logout to handle both authentication types
  logout(): void {
    localStorage.removeItem('authToken');
    localStorage.removeItem('accessToken');
    sessionStorage.removeItem('oauth_user');
    this.oauthUser = null;
    this.updateAuthState(false);
  }

  // Get user display name (works for both auth types)
  getUserDisplayName(): string | null {
    const oauthUser = this.getOAuthUser();
    if (oauthUser) {
      return oauthUser.name || `${oauthUser.given_name} ${oauthUser.family_name}`.trim();
    }
    
    // For traditional auth, we'd need to decode the token or make an API call
    // For now, return null for traditional auth
    return null;
  }

  // Get user email (works for both auth types)
  getUserEmail(): string | null {
    const oauthUser = this.getOAuthUser();
    if (oauthUser) {
      return oauthUser.email;
    }
    
    // For traditional auth, we'd need to decode the token or make an API call
    // For now, return null for traditional auth
    return null;
  }

  // Change password method
  changePassword(data: ChangePasswordRequest): Observable<any> {
    return this.handleRequest(
      this.http.post<ApiResponse<any>>(`${this.apiUrl}/change-password`, data)
    );
  }
}

import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { LoaderComponent } from '../shared/loader/loader.component';
import { OAuthService } from '../services/oauth.service';
import { AuthService } from '../auth.service';

@Component({
  selector: 'app-oauth-callback',
  standalone: true,
  imports: [CommonModule, LoaderComponent],
  template: `
    <div class="container mt-5">
      <div class="row justify-content-center">
        <div class="col-md-6">
          <div class="card">
            <div class="card-body text-center">
              <div *ngIf="loading" class="mb-3">
                <app-loader></app-loader>
                <p class="mt-3">Completing authentication...</p>
              </div>
              
              <div *ngIf="error" class="alert alert-danger">
                <h5>Authentication Failed</h5>
                <p>{{ error }}</p>
                <button class="btn btn-primary" (click)="redirectToLogin()">
                  Try Again
                </button>
              </div>
              
              <div *ngIf="success" class="alert alert-success">
                <h5>Authentication Successful</h5>
                <p>Redirecting to your dashboard...</p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .card {
      box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
    }
    
    .alert {
      border-radius: 8px;
    }
  `]
})
export class OAuthCallbackComponent implements OnInit {
  loading = true;
  error: string | null = null;
  success = false;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private oauthService: OAuthService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.handleOAuthCallback();
  }

  private handleOAuthCallback(): void {
    // Check for error parameters
    this.route.queryParams.subscribe(params => {
      if (params['error']) {
        this.error = `OAuth authentication failed: ${params['error']}`;
        this.loading = false;
        return;
      }

      // Check for JWT token in query parameters
      if (params['jwt_token']) {
        // Store the JWT token for API access
        localStorage.setItem('authToken', params['jwt_token']);
        localStorage.setItem('accessToken', params['jwt_token']);
      }

      // If we reach this point, OAuth was successful
      // The backend has already handled the authentication
      this.handleSuccessfulAuth();
    });
  }

  private handleSuccessfulAuth(): void {
    // Set OAuth authentication state
    this.oauthService.setOAuthState(true);
    
    // Try to get user information from Microsoft OAuth first
    this.oauthService.getUserInfo().subscribe({
      next: (userInfo) => {
        // Store user information in auth service
        this.authService.setOAuthUser(userInfo);
        this.success = true;
        this.loading = false;
        
        // Redirect after a short delay
        setTimeout(() => {
          this.redirectToDashboard();
        }, 2000);
      },
      error: (error) => {
        // If Microsoft OAuth fails, try Google OAuth
        this.oauthService.getGoogleUserInfo().subscribe({
          next: (userInfo) => {
            // Store user information in auth service
            this.authService.setOAuthUser(userInfo);
            this.success = true;
            this.loading = false;
            
            // Redirect after a short delay
            setTimeout(() => {
              this.redirectToDashboard();
            }, 2000);
          },
          error: (googleError) => {
            this.error = 'Failed to retrieve user information. Please try again.';
            this.loading = false;
          }
        });
      }
    });
  }

  private redirectToDashboard(): void {
    // Check if user is admin
    const isAdmin = this.authService.getRole() === 'Admin';
    const redirectUrl = isAdmin ? '/admin' : '/home';
    this.router.navigate([redirectUrl]);
  }

  redirectToLogin(): void {
    this.router.navigate(['/login']);
  }
}

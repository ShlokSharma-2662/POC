import { Component } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../auth.service';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { LoaderComponent } from '../shared/loader/loader.component';
import { OAuthService } from '../services/oauth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss'],
  imports: [FormsModule, CommonModule, RouterModule, LoaderComponent]
})
export class RegisterComponent {
  firstName = '';
  lastName = '';
  email = '';
  password = '';
  role: 'User' | 'Admin' = 'User';
  error = '';
  showPassword = false;
  loading: boolean = false;

  constructor(
    private auth: AuthService,
    private router: Router,
    private oauthService: OAuthService
  ) {}

  onRegister(): void {
    this.loading = true;
    this.error = '';
    
    this.auth.register({
      firstName: this.firstName,
      lastName: this.lastName,
      email: this.email,
      password: this.password,
      role: this.role
    }).subscribe({
      next: () => this.router.navigate(['/login']),
      error: () => {
        this.error = 'Registration failed. Please try again.';
        this.loading = false;
      }
    });
  }

  onGoogleOAuthLogin(): void {
    this.loading = true;
    this.error = '';
    
    // Get full frontend URL to redirect back after OAuth
    const returnUrl = `${window.location.origin}${this.router.url}`;
    
    // Initiate Google OAuth login
    this.oauthService.initiateGoogleLogin(returnUrl);
  }
}

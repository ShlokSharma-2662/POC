import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { AuthService } from '../auth.service';
import { Router, RouterModule, ActivatedRoute } from '@angular/router';
import { ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { LoaderComponent } from '../shared/loader/loader.component';
import { OAuthService } from '../services/oauth.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss'],
  imports: [ReactiveFormsModule, CommonModule, LoaderComponent, RouterModule],
})
export class LoginComponent implements OnInit {
  loginForm: FormGroup;
  error: string = '';
  showPassword = false;
  loading: boolean = false;
  showForgotPasswordModal = false;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute,
    private oauthService: OAuthService
  ) {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', Validators.required]
    });
  }

  ngOnInit(): void {
    // Check for JWT token in query parameters (from OAuth callback)
    this.route.queryParams.subscribe(params => {
      if (params['jwt_token']) {
        console.log('JWT token detected in URL, processing OAuth login...');
        this.handleOAuthToken(params['jwt_token']);
      }
    });
  }

  private handleOAuthToken(token: string): void {
    this.loading = true;
    this.error = '';

    try {
      console.log('Processing OAuth token...');
      
      // Store the JWT token
      localStorage.setItem('accessToken', token);
      localStorage.setItem('authToken', token);
      console.log('Token stored in localStorage');

      // Decode JWT to get user info
      // Backend uses: JwtRegisteredClaimNames.Email ("email"), "FirstName", "LastName", ClaimTypes.Role
      const payload = JSON.parse(atob(token.split('.')[1]));
      console.log('Decoded JWT payload:', payload);
      
      const email = payload['email'] || 
                    payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] || 
                    payload['Email'] || '';
      const firstName = payload['FirstName'] || 
                        payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname'] || 
                        payload['given_name'] || '';
      const lastName = payload['LastName'] || 
                       payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname'] || 
                       payload['family_name'] || '';
      const role = payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || 
                   payload['role'] || 
                   'User';
      const sub = payload['sub'] || 
                  payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] || 
                  email;

      console.log('Extracted user info:', { email, firstName, lastName, role, sub });

      // Set OAuth user info
      if (email) {
        this.authService.setOAuthUser({
          sub: sub,
          email: email,
          name: `${firstName} ${lastName}`.trim() || email,
          given_name: firstName,
          family_name: lastName
        });
        console.log('OAuth user info set');
        // Note: setOAuthUser already calls updateAuthState internally
      } else {
        throw new Error('Email not found in JWT token');
      }

      console.log('Auth state updated');

      // Redirect based on role
      console.log(`Redirecting to ${role === 'Admin' ? '/admin' : '/home'}...`);
      setTimeout(() => {
        if (role === 'Admin') {
          this.router.navigate(['/admin'], { replaceUrl: true });
        } else {
          this.router.navigate(['/home'], { replaceUrl: true });
        }
      }, 500);
    } catch (error: any) {
      console.error('Error handling OAuth token:', error);
      this.error = `Failed to process authentication: ${error.message}. Please try again.`;
      this.loading = false;
    }
  }

  onSubmit(): void {
    if (this.loginForm.valid) {
      this.loading = true;
      this.error = '';
      
      this.authService.login(this.loginForm.value).subscribe({
        next: (res) => {
          localStorage.setItem('accessToken', res.token);
      
          const role = this.authService.getRole();
          if (role === 'Admin') {
            this.router.navigate(['/admin']); // ✅ Correct
          } else {
            this.router.navigate(['/home']);
          }
        },
        error: (err) => {
          this.error = err.error?.message || 'Login failed. Try again.';
          this.loading = false;
        }
      });
    }
  }

  onOAuthLogin(): void {
    this.loading = true;
    this.error = '';
    
    // Get current URL to redirect back after OAuth
    const returnUrl = this.router.url;
    
    // Initiate Microsoft OAuth login
    this.oauthService.initiateLogin(returnUrl);
  }

  onGoogleOAuthLogin(): void {
    this.loading = true;
    this.error = '';
    
    // Get full frontend URL to redirect back after OAuth
    const returnUrl = `${window.location.origin}${this.router.url}`;
    
    // Initiate Google OAuth login
    this.oauthService.initiateGoogleLogin(returnUrl);
  }

  onForgotPassword(event: Event): void {
    event.preventDefault();
    this.showForgotPasswordModal = true;
  }

  closeForgotPasswordModal(): void {
    this.showForgotPasswordModal = false;
  }

  onContactSupport(): void {
    // You can implement contact support functionality here
    // For now, we'll just close the modal
    this.closeForgotPasswordModal();
    alert('Please contact our support team at support@eshop.com for password assistance.');
  }
}

import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router, NavigationEnd, RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { NavbarComponent } from './navbar/navbar.component';
import { AdminNavbarComponent } from './admin-navbar/admin-navbar.component';
import { filter, takeUntil } from 'rxjs/operators';
import { Subject } from 'rxjs';
import { AuthService } from './auth.service';
import { RateLimitNotificationComponent } from './shared/rate-limit-notification/rate-limit-notification.component';

@Component({
  selector: 'app-root',
  standalone: true,
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss'],
  imports: [RouterOutlet, NavbarComponent, AdminNavbarComponent, CommonModule, RateLimitNotificationComponent],
})
export class AppComponent implements OnInit, OnDestroy {
  isLoginPage = false;
  isLoggedIn = false;
  isAdmin = false;
  private destroy$ = new Subject<void>();

  constructor(private auth: AuthService, private router: Router) {}

  ngOnInit(): void {
    // Subscribe to authentication state changes
    this.auth.authState$.pipe(
      takeUntil(this.destroy$)
    ).subscribe(isLoggedIn => {
      this.isLoggedIn = isLoggedIn;
      this.isAdmin = this.auth.getRole() === 'Admin';
    });

    // Listen to route changes
    this.router.events
      .pipe(
        filter(event => event instanceof NavigationEnd),
        takeUntil(this.destroy$)
      )
      .subscribe((event: NavigationEnd) => {
        const url = event.urlAfterRedirects;
        this.isLoginPage = url.includes('/login') || url.includes('/register');
        this.isAdmin = this.auth.getRole() === 'Admin';
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}

import { Component, OnDestroy, OnInit } from '@angular/core';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { Router, NavigationEnd } from '@angular/router';
import { filter, takeUntil } from 'rxjs/operators';
import { Subject } from 'rxjs';
import { AuthService } from '../auth.service';
import { LogoutConfirmationComponent } from '../shared/logout-confirmation/logout-confirmation.component';

@Component({
  selector: 'app-admin-navbar',
  imports: [RouterModule, CommonModule, LogoutConfirmationComponent],
  templateUrl: './admin-navbar.component.html',
  styleUrls: ['./admin-navbar.component.scss']
})
export class AdminNavbarComponent implements OnInit, OnDestroy {
  showMenus: boolean = true;
  isLoggedIn: boolean = false;
  showLogoutConfirmation = false;
  private destroy$ = new Subject<void>();

  constructor(private router: Router, private auth: AuthService) {
    this.router.events
      .pipe(filter(event => event instanceof NavigationEnd), takeUntil(this.destroy$))
      .subscribe((event: NavigationEnd) => {
        const url = (event as NavigationEnd).urlAfterRedirects;
        this.showMenus = !['/login', '/register'].includes(url);
      });
  }

  ngOnInit(): void {
    // initialize and react to auth changes to immediately show/hide logout
    this.isLoggedIn = this.auth.isLoggedIn();
    this.auth.authState$.pipe(takeUntil(this.destroy$)).subscribe(isIn => {
      this.isLoggedIn = isIn;
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  logout(): void {
    this.showLogoutConfirmation = true;
  }

  onLogoutConfirm(): void {
    this.showLogoutConfirmation = false;
    this.auth.logout();
    this.router.navigate(['/home']);
  }

  onLogoutCancel(): void {
    this.showLogoutConfirmation = false;
  }
}

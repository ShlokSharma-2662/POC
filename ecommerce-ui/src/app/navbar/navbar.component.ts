import { Component, OnInit, OnDestroy } from '@angular/core';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { Router, NavigationEnd } from '@angular/router';
import { filter, takeUntil } from 'rxjs/operators';
import { Subject } from 'rxjs';
import { AuthService } from '../auth.service';
import { CartService } from '../services/cart.service';
import { WishlistService } from '../services/wishlist.service';
import { LogoutConfirmationComponent } from '../shared/logout-confirmation/logout-confirmation.component';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [RouterModule, CommonModule, LogoutConfirmationComponent],
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.scss']
})
export class NavbarComponent implements OnInit, OnDestroy {
  isLoggedIn = false;
  isAdmin = false;
  isProfileDropdownOpen = false;
  showLogoutConfirmation = false;
  private destroy$ = new Subject<void>();

  constructor(
    private router: Router, 
    private auth: AuthService, 
    private cartService: CartService, 
    private wishlistService: WishlistService
  ) {}

  ngOnInit(): void {
    // Subscribe to authentication state changes
    this.auth.authState$.pipe(
      takeUntil(this.destroy$)
    ).subscribe(isLoggedIn => {
      this.isLoggedIn = isLoggedIn;
      this.isAdmin = this.auth.getRole() === 'Admin';
    });

    // Listen to route changes for admin role updates
    this.router.events
      .pipe(
        filter(event => event instanceof NavigationEnd),
        takeUntil(this.destroy$)
      )
      .subscribe((event: NavigationEnd) => {
        this.isAdmin = this.auth.getRole() === 'Admin';
      });

    // Add click outside listener for dropdown
    document.addEventListener('click', this.handleClickOutside.bind(this));
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    
    // Remove click outside listener
    document.removeEventListener('click', this.handleClickOutside.bind(this));
  }

  logout(): void {
    this.closeProfileDropdown();
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

  toggleProfileDropdown(event: Event): void {
    event.preventDefault();
    event.stopPropagation();
    this.isProfileDropdownOpen = !this.isProfileDropdownOpen;
  }

  closeProfileDropdown(): void {
    this.isProfileDropdownOpen = false;
  }

  onProfileItemClick(event: Event): void {
    event.preventDefault();
    event.stopPropagation();
    this.closeProfileDropdown();
  }

  private handleClickOutside(event: Event): void {
    const target = event.target as HTMLElement;
    const dropdown = document.querySelector('.nav-item.dropdown');
    const dropdownMenu = document.querySelector('.dropdown-menu');
    
    // Check if click is outside both the dropdown toggle and the dropdown menu
    if (dropdown && dropdownMenu) {
      const isClickInsideDropdown = dropdown.contains(target);
      const isClickInsideMenu = dropdownMenu.contains(target);
      
      if (!isClickInsideDropdown && !isClickInsideMenu) {
        this.closeProfileDropdown();
      }
    }
  }

  get cartItemCount(): number {
    return this.cartService.getCartItemCount();
  }

  get wishlistCount(): number {
    return this.wishlistService.getCurrentWishlistCount();
  }
}

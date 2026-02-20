import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { WishlistService, WishlistItem, GuestWishlistItem } from '../services/wishlist.service';
import { CartService } from '../services/cart.service';
import { ProductService } from '../services/product.service';
import { ToastrService } from 'ngx-toastr';
import { LoaderComponent } from '../shared/loader/loader.component';
import { AuthService } from '../auth.service';

@Component({
  selector: 'app-wishlist',
  standalone: true,
  imports: [CommonModule, RouterModule, LoaderComponent],
  templateUrl: './wishlist.component.html',
  styleUrls: ['./wishlist.component.scss']
})
export class WishlistComponent implements OnInit, OnDestroy {
  wishlistItems: WishlistItem[] = [];
  guestWishlistItems: GuestWishlistItem[] = [];
  loading = false;
  private destroy$ = new Subject<void>();

  // Computed properties for template
  get inStockCount(): number {
    if (!this.authService.isLoggedIn()) {
      return this.guestWishlistItems.filter(item => item.product.stock > 0).length;
    }
    return this.wishlistItems.filter(item => item.isInStock).length;
  }

  get outOfStockCount(): number {
    if (!this.authService.isLoggedIn()) {
      return this.guestWishlistItems.filter(item => item.product.stock <= 0).length;
    }
    return this.wishlistItems.filter(item => !item.isInStock).length;
  }

  constructor(
    private wishlistService: WishlistService,
    private cartService: CartService,
    private productService: ProductService,
    private toastr: ToastrService,
    private router: Router,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.loadWishlist();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadWishlist(forceRefresh: boolean = false): void {
    this.loading = true;
    
    if (!this.authService.isLoggedIn()) {
      // For guest users, load from local storage
      this.loadGuestWishlist();
      this.loading = false;
    } else {
      // For authenticated users, load from API
      this.wishlistService.getUserWishlist(forceRefresh)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (items) => {
            this.wishlistItems = items;
            this.loading = false;
          },
          error: (error) => {
            this.loading = false;
            this.toastr.error('Failed to load wishlist', 'Error');
          }
        });
    }
  }

  private loadGuestWishlist(): void {
    const guestWishlistData = localStorage.getItem('guest_wishlist');
    this.guestWishlistItems = guestWishlistData ? JSON.parse(guestWishlistData) : [];
  }

  removeFromWishlist(productId: number): void {
    this.wishlistService.removeFromWishlist(productId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          if (!this.authService.isLoggedIn()) {
            this.guestWishlistItems = this.guestWishlistItems.filter(item => item.product.productId !== productId);
            this.loadGuestWishlist(); // Reload to update UI
          } else {
            this.wishlistItems = this.wishlistItems.filter(item => item.productId !== productId);
          }
          this.toastr.success('Product removed from wishlist', 'Success');
        },
        error: (error) => {
          this.toastr.error('Failed to remove from wishlist', 'Error');
        }
      });
  }

  addToCart(productId: number): void {
    if (!this.authService.isLoggedIn()) {
      // For guest users, get product from guest wishlist
      const guestItem = this.guestWishlistItems.find(item => item.product.productId === productId);
      if (guestItem) {
        this.cartService.addToCart(guestItem.product);
        // Cart service will handle success/error notifications
      }
    } else {
      // For authenticated users, get product from API
      this.productService.getProductById(productId)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (product) => {
            this.cartService.addToCart(product);
            // Cart service will handle success/error notifications
          },
          error: (error) => {
            this.toastr.error('Failed to add to cart', 'Error');
          }
        });
    }
  }

  moveToCart(productId: number): void {
    this.addToCart(productId);
    this.removeFromWishlist(productId);
  }

  viewProduct(productId: number): void {
    this.router.navigate(['/products', productId]);
  }

  getFormattedDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  }

  getDiscountedPrice(originalPrice: number): number {
    // Apply 10% discount for wishlist items (example)
    return originalPrice * 0.9;
  }

  hasDiscount(originalPrice: number): boolean {
    return true; // Always show discount for demo
  }

  getDiscountPercentage(): number {
    return 10; // 10% discount
  }

  getTotalWishlistCount(): number {
    if (!this.authService.isLoggedIn()) {
      return this.guestWishlistItems.length;
    }
    return this.wishlistItems.length;
  }

  clearWishlist(): void {
    this.wishlistService.clearWishlist()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          if (!this.authService.isLoggedIn()) {
            this.guestWishlistItems = [];
          } else {
            this.wishlistItems = [];
            // Add a small delay to ensure cache invalidation is complete
            // before refreshing the wishlist from server with force refresh
            setTimeout(() => {
              this.loadWishlist(true);
            }, 500);
          }
          this.toastr.success('Wishlist cleared successfully', 'Success');
        },
        error: (error) => {
          this.toastr.error('Failed to clear wishlist', 'Error');
        }
      });
  }
}

import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, BehaviorSubject } from 'rxjs';
import { tap } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { BaseApiService } from './base-api.service';
import { ApiResponse } from '../models/api-response.model';
import { Product } from '../models/product';

export interface WishlistItem {
  id: number;
  productId: number;
  productName: string;
  productDescription: string;
  productPrice: number;
  productImageUrl: string;
  categoryName: string;
  stock: number;
  addedAt: string;
  isInStock: boolean;
}

export interface GuestWishlistItem {
  product: Product;
  addedAt: string;
}

export interface AddToWishlistRequest {
  productId: number;
  userId: number;
}

export interface RemoveFromWishlistRequest {
  productId: number;
  userId: number;
}

@Injectable({
  providedIn: 'root'
})
export class WishlistService extends BaseApiService {
  private apiUrl = `${this.baseUrl}/wishlist`;
  private wishlistItemsSubject = new BehaviorSubject<WishlistItem[]>([]);
  private wishlistCountSubject = new BehaviorSubject<number>(0);
  private wishlistStatusMap = new Map<number, boolean>();
  private guestWishlistItems: GuestWishlistItem[] = [];

  public wishlistItems$ = this.wishlistItemsSubject.asObservable();
  public wishlistCount$ = this.wishlistCountSubject.asObservable();

  constructor(http: HttpClient) {
    super(http);
    this.loadGuestWishlist();
  }

  private getHeaders(): HttpHeaders {
    const token = localStorage.getItem('accessToken');
    return new HttpHeaders({
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    });
  }

  private isLoggedIn(): boolean {
    return !!localStorage.getItem('accessToken');
  }

  private getGuestWishlistKey(): string {
    return 'guest_wishlist';
  }

  private saveGuestWishlist(): void {
    localStorage.setItem(this.getGuestWishlistKey(), JSON.stringify(this.guestWishlistItems));
  }

  private loadGuestWishlist(): void {
    const guestWishlistData = localStorage.getItem(this.getGuestWishlistKey());
    this.guestWishlistItems = guestWishlistData ? JSON.parse(guestWishlistData) : [];
    this.updateGuestWishlistStatusMap();
  }

  private updateGuestWishlistStatusMap(): void {
    this.guestWishlistItems.forEach(item => {
      this.wishlistStatusMap.set(item.product.productId, true);
    });
  }

  // Get user's wishlist
  getUserWishlist(forceRefresh: boolean = false): Observable<WishlistItem[]> {
    let url = `${this.apiUrl}`;
    
    // Add cache-busting parameter if force refresh is requested
    if (forceRefresh) {
      url += `?t=${Date.now()}`;
    }
    
    return this.handleRequest(
      this.http.get<ApiResponse<WishlistItem[]>>(url, { headers: this.getHeaders() })
    ).pipe(
      tap(items => {
        this.wishlistItemsSubject.next(items);
        this.wishlistCountSubject.next(items.length);
        // Update wishlist status map
        this.wishlistStatusMap.clear();
        items.forEach(item => {
          this.wishlistStatusMap.set(item.productId, true);
        });
      })
    );
  }

  // Add product to wishlist
  addToWishlist(productId: number, product?: Product): Observable<any> {
    if (!this.isLoggedIn()) {
      // Guest user - add to local storage
      if (product) {
        const existingItem = this.guestWishlistItems.find(item => item.product.productId === productId);
        if (!existingItem) {
          this.guestWishlistItems.push({
            product: product,
            addedAt: new Date().toISOString()
          });
          this.saveGuestWishlist();
          this.wishlistStatusMap.set(productId, true);
          this.updateWishlistCount();
        }
      }
      // Return a mock observable for guest users
      return new Observable(observer => {
        observer.next({ success: true });
        observer.complete();
      });
    }

    // Authenticated user - use API
    const request: AddToWishlistRequest = {
      productId: productId,
      userId: 0 // Will be set by backend from token
    };

    return this.handleRequest(
      this.http.post<ApiResponse<any>>(`${this.apiUrl}/add`, request, { headers: this.getHeaders() })
    ).pipe(
      tap(() => {
        this.wishlistStatusMap.set(productId, true);
        this.updateWishlistCount();
      })
    );
  }

  // Remove product from wishlist
  removeFromWishlist(productId: number): Observable<any> {
    if (!this.isLoggedIn()) {
      // Guest user - remove from local storage
      this.guestWishlistItems = this.guestWishlistItems.filter(item => item.product.productId !== productId);
      this.saveGuestWishlist();
      this.wishlistStatusMap.set(productId, false);
      this.updateWishlistCount();
      
      // Return a mock observable for guest users
      return new Observable(observer => {
        observer.next({ success: true });
        observer.complete();
      });
    }

    // Authenticated user - use API
    const request: RemoveFromWishlistRequest = {
      productId: productId,
      userId: 0 // Will be set by backend from token
    };

    return this.handleRequest(
      this.http.delete<ApiResponse<any>>(`${this.apiUrl}/remove`, { 
        headers: this.getHeaders(),
        body: request
      })
    ).pipe(
      tap(() => {
        this.wishlistStatusMap.set(productId, false);
        this.updateWishlistCount();
      })
    );
  }

  // Check if product is in wishlist
  checkWishlistStatus(productId: number): Observable<boolean> {
    if (!this.isLoggedIn()) {
      // Guest user - check local storage
      const isInWishlist = this.guestWishlistItems.some(item => item.product.productId === productId);
      this.wishlistStatusMap.set(productId, isInWishlist);
      
      return new Observable(observer => {
        observer.next(isInWishlist);
        observer.complete();
      });
    }

    // Authenticated user - use API
    return this.handleRequest(
      this.http.get<ApiResponse<boolean>>(`${this.apiUrl}/check/${productId}`, { headers: this.getHeaders() })
    ).pipe(
      tap(isInWishlist => {
        this.wishlistStatusMap.set(productId, isInWishlist);
      })
    );
  }

  // Get wishlist count
  getWishlistCount(): Observable<number> {
    if (!this.isLoggedIn()) {
      // Guest user - return local count
      const count = this.guestWishlistItems.length;
      this.wishlistCountSubject.next(count);
      
      return new Observable(observer => {
        observer.next(count);
        observer.complete();
      });
    }

    // Authenticated user - use API
    return this.handleRequest(
      this.http.get<ApiResponse<number>>(`${this.apiUrl}/count`, { headers: this.getHeaders() })
    ).pipe(
      tap(count => {
        this.wishlistCountSubject.next(count);
      })
    );
  }

  // Toggle wishlist status (add if not in wishlist, remove if in wishlist)
  toggleWishlist(productId: number, product?: Product): Observable<any> {
    const isInWishlist = this.wishlistStatusMap.get(productId);
    
    if (isInWishlist) {
      return this.removeFromWishlist(productId);
    } else {
      return this.addToWishlist(productId, product);
    }
  }

  // Get current wishlist status for a product (from cache)
  getWishlistStatus(productId: number): boolean {
    return this.wishlistStatusMap.get(productId) || false;
  }

  // Get current wishlist items (from cache)
  getCurrentWishlistItems(): WishlistItem[] {
    return this.wishlistItemsSubject.value;
  }

  // Get current wishlist count (from cache)
  getCurrentWishlistCount(): number {
    if (!this.isLoggedIn()) {
      return this.guestWishlistItems.length;
    }
    return this.wishlistCountSubject.value;
  }

  // Clear wishlist cache
  clearWishlistCache(): void {
    this.wishlistItemsSubject.next([]);
    this.wishlistCountSubject.next(0);
    this.wishlistStatusMap.clear();
    
    // Clear guest wishlist if not logged in
    if (!this.isLoggedIn()) {
      this.guestWishlistItems = [];
      this.saveGuestWishlist();
    }
  }

  // Update wishlist count
  private updateWishlistCount(): void {
    if (!this.isLoggedIn()) {
      const count = this.guestWishlistItems.length;
      this.wishlistCountSubject.next(count);
    } else {
      const currentItems = this.wishlistItemsSubject.value;
      const count = currentItems.length;
      this.wishlistCountSubject.next(count);
    }
  }

  // Refresh wishlist data
  refreshWishlist(): void {
    if (!this.isLoggedIn()) {
      // For guest users, just reload from localStorage
      this.loadGuestWishlist();
      this.updateWishlistCount();
    } else {
      // Force refresh from server to bypass any cache
      this.getUserWishlist(true).subscribe();
    }
  }

  // Clear all items from wishlist
  clearWishlist(): Observable<any> {
    if (!this.isLoggedIn()) {
      // Guest user - clear local storage
      this.guestWishlistItems = [];
      this.saveGuestWishlist();
      this.wishlistItemsSubject.next([]);
      this.wishlistCountSubject.next(0);
      this.wishlistStatusMap.clear();
      
      return new Observable(observer => {
        observer.next({ success: true });
        observer.complete();
      });
    }

    // Authenticated user - use API
    return this.handleRequest(
      this.http.delete<ApiResponse<any>>(`${this.apiUrl}/clear`, { headers: this.getHeaders() })
    ).pipe(
      tap(() => {
        // Clear local cache immediately
        this.wishlistItemsSubject.next([]);
        this.wishlistCountSubject.next(0);
        this.wishlistStatusMap.clear();
        
        // Force refresh from server by clearing any cached data
        this.refreshWishlist();
      })
    );
  }
}

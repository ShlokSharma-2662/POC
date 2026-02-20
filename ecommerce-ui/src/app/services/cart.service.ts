import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { AuthService } from '../auth.service';
import { ToastrService } from 'ngx-toastr';
import { Product } from '../models/product';
import { BaseApiService } from './base-api.service';
import { ApiResponse } from '../models/api-response.model';

export interface CartItem {
  product: Product;
  quantity: number;
  price: number;
}

@Injectable({
  providedIn: 'root',
})
export class CartService extends BaseApiService {
  cartItems: CartItem[] = [];
  private readonly apiUrl = `${this.baseUrl}/cart`;

  constructor(http: HttpClient, private auth: AuthService, private toastr: ToastrService) {
    super(http);
    this.loadCart();
    // Reload cart when auth state changes (e.g., login/logout) so counts reflect immediately
    this.auth.authState$.subscribe(() => this.loadCart());
  }

  // Add product to the cart (server-first to respect stock)
  addToCart(product: Product, quantity?: number): void {
    const addQty = typeof quantity === 'number' && !isNaN(quantity) && quantity > 0
      ? quantity
      : (product.quantity && product.quantity > 0 ? product.quantity : 1);

    // Call server first; on success reflect locally. If not logged in, fallback to local clamp.
    const headers = this.getAuthHeaders();
    if (headers.get('Authorization')) {
      this.handleRequest(
        this.http.post<ApiResponse<any>>(`${this.apiUrl}`, { productId: product.productId, quantity: addQty }, { headers })
      ).subscribe({
        next: (response) => {
          const existingItem = this.cartItems.find(i => i.product.productId === product.productId);
          if (existingItem) {
            existingItem.quantity += addQty;
          } else {
            this.cartItems.push({ product, quantity: addQty, price: product.price });
          }
          this.saveCart();
          
          // Show success message
          this.toastr.success(
            `${addQty} × ${product.name} added to cart!`,
            'Item Added 🛒',
            {
              progressBar: true,
              closeButton: true,
            }
          );
        },
        error: (err) => {
          // Handle the new structured response format
          const errorData = err.error?.data;
          if (errorData && typeof errorData === 'object') {
            // New structured response with detailed stock information
            const maxAddable = errorData.maxAddableQuantity || 0;
            const currentStock = errorData.currentStock || 0;
            const currentCartQty = errorData.currentCartQuantity || 0;
            
            if (maxAddable > 0) {
              // Add the maximum allowed quantity
              const existingItem = this.cartItems.find(i => i.product.productId === product.productId);
              if (existingItem) {
                existingItem.quantity += maxAddable;
              } else {
                this.cartItems.push({ product, quantity: maxAddable, price: product.price });
              }
              this.saveCart();
              this.toastr.warning(`Only ${maxAddable} units available to add. Added ${maxAddable} to cart.`, 'Stock Limit');
            } else {
              // Don't add anything to cart when maxAddable is 0
              this.toastr.warning(errorData.message || 'Item not available right now', 'Stock Limit');
            }
          } else {
            // Fallback to old behavior for backward compatibility
            const existingItem = this.cartItems.find(i => i.product.productId === product.productId);
            const currentQty = existingItem?.quantity ?? 0;
            const available = Math.max(0, (product.stock ?? 0) - currentQty);
            if (available > 0) {
              if (existingItem) existingItem.quantity += Math.min(addQty, available);
              else this.cartItems.push({ product, quantity: Math.min(addQty, available), price: product.price });
              this.saveCart();
              this.toastr.warning(`Only ${available} available. Added ${Math.min(addQty, available)}.`, 'Stock Limit');
            } else {
              this.toastr.warning(err.message || 'Item not available right now', 'Stock Limit');
            }
          }
        }
      });
    } else {
      // Guest/local: clamp by product.stock if known
      const existingItem = this.cartItems.find(i => i.product.productId === product.productId);
      const currentQty = existingItem?.quantity ?? 0;
      const available = Math.max(0, (product.stock ?? Infinity) - currentQty);
      const toAdd = Math.min(addQty, available);
      if (existingItem) {
        existingItem.quantity += toAdd;
      } else {
        this.cartItems.push({ product, quantity: toAdd, price: product.price });
      }
      this.saveCart();
      if (toAdd < addQty) {
        this.toastr.warning(`Only ${available} available. Added ${toAdd}.`, 'Stock Limit');
      }
    }
  }
 updateCart(item: CartItem): void {
    const existingItem = this.cartItems.find(
      (cartItem) => cartItem.product.productId === item.product.productId
    );
    if (existingItem) {
      existingItem.quantity = item.quantity;  // Update the quantity of the existing item
    }
    this.saveCart();
  }
  getTotalAmount(): number {
    return this.cartItems.reduce(
      (sum, item) => sum + item.price * item.quantity,
      0
    );
  }
  // Update product quantity in the cart (optimistic updates)
  updateQuantity(productId: number, quantity: number): void {
    const item = this.cartItems.find((i) => i.product.productId === productId);
    if (item) {
      // Store original quantity for rollback
      const originalQuantity = item.quantity;
      
      // Optimistic update - update UI immediately
      item.quantity = quantity;
      if (item.quantity <= 0) {
        this.cartItems = this.cartItems.filter(i => i.product.productId !== productId);
      }
      this.saveCart();
      
      // Server sync if logged in
      const headers = this.getAuthHeaders();
      if (headers.get('Authorization')) {
        this.handleRequest(
          this.http.put<ApiResponse<any>>(`${this.apiUrl}`, { productId, quantity }, { headers })
        ).subscribe({
          next: () => {
            // Success - keep optimistic update
          },
          error: (err) => {
            // Rollback on error
            if (originalQuantity > 0) {
              const existingItem = this.cartItems.find(i => i.product.productId === productId);
              if (existingItem) {
                existingItem.quantity = originalQuantity;
              } else {
                this.cartItems.push({
                  product: item.product,
                  quantity: originalQuantity,
                  price: item.price
                });
              }
            }
            this.saveCart();
            
            // Show error message
            const maxQty = item.product.stock ?? originalQuantity;
            const clamped = Math.min(quantity, maxQty);
            if (clamped < quantity) {
              this.toastr.warning(`Only ${maxQty} available. Updated to ${clamped}.`, 'Stock Limit');
            } else {
              this.toastr.warning(err.message || 'Unable to update quantity', 'Cart');
            }
          }
        });
      } else {
        // Guest user - local validation only
        const maxQty = item.product.stock ?? quantity;
        const clamped = Math.min(quantity, maxQty);
        item.quantity = clamped;
        if (item.quantity <= 0) this.removeItem(productId);
        this.saveCart();
        if (clamped < quantity) {
          this.toastr.warning(`Only ${maxQty} available. Updated to ${clamped}.`, 'Stock Limit');
        }
      }
    }
  }

  // Remove product from cart (optimistic updates)
  removeItem(productId: number): void {
    // Store item for potential rollback
    const itemToRemove = this.cartItems.find(i => i.product.productId === productId);
    
    // Optimistic update - remove immediately
    this.cartItems = this.cartItems.filter(
      (i) => i.product.productId !== productId
    );
    this.saveCart();
    
    // Server sync if logged in
    const headers = this.getAuthHeaders();
    if (headers.get('Authorization')) {
      this.handleRequest(
        this.http.delete<ApiResponse<any>>(`${this.apiUrl}/${productId}`, { headers })
      ).subscribe({
        next: () => {
          // Success - keep optimistic update
        },
        error: (err) => {
          // Rollback on error
          if (itemToRemove) {
            this.cartItems.push(itemToRemove);
            this.saveCart();
            this.toastr.error('Failed to remove item from cart', 'Error');
          }
        }
      });
    }
  }

  // Clear the cart
  clearCart(): void {
    this.cartItems = [];
    this.saveCart();
    this.handleRequest(
      this.http.delete<ApiResponse<any>>(`${this.apiUrl}/clear`, { headers: this.getAuthHeaders() })
    ).subscribe({ error: () => {} });
  }

  // Get all cart items
  getCartItems(): CartItem[] {
    return this.cartItems;
  }

  // Save cart to localStorage
  private saveCart(): void {
    const key = this.getCartKey();
    localStorage.setItem(key, JSON.stringify(this.cartItems));
  }

  // Load cart from localStorage
  loadCart(): CartItem[] {
    const key = this.getCartKey();
    const cartData = localStorage.getItem(key);
    this.cartItems = cartData ? JSON.parse(cartData) : [];

    // try load from API and reconcile
    this.handleRequest(
      this.http.get<ApiResponse<any[]>>(`${this.apiUrl}`, { headers: this.getAuthHeaders() })
    ).subscribe({
      next: (serverItems) => {
        // Map server items to local shape
        const mapped: CartItem[] = serverItems.map(si => ({
          product: {
            productId: si.product.productId,
            name: si.product.name,
            description: si.product.description,
            price: si.product.price,
            imageUrl: si.product.imageUrl,
            stock: si.product.stock,
            categoryId: si.product.categoryId,
            categoryName: si.product.categoryName ?? '',
            quantity: si.quantity ?? 1
          },
          quantity: si.quantity,
          price: si.product.price
        }));
        this.cartItems = mapped;
        this.saveCart();
      },
      error: () => {}
    });
    return this.cartItems;
  }

  // Get the cart key (based on user ID or guest)
  private getCartKey(): string {
    const userId = this.getUserId();
    return userId ? `cart_${userId}` : 'cart_guest';
  }

  // Get user ID from local storage (JWT token)
  private getUserId(): string | null {
    const token = localStorage.getItem('accessToken');
    if (!token) return null;

    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return payload['sub'] || null; // 'sub' holds user ID
    } catch {
      return null;
    }
  }

  // Get cart item count
  getCartItemCount(): number {
    return this.cartItems.reduce((total, item) => total + item.quantity, 0);
  }

  private getAuthHeaders(): HttpHeaders {
    const token = this.auth.getToken();
    return token ? new HttpHeaders({ 'Authorization': `Bearer ${token}` }) : new HttpHeaders();
  }
}
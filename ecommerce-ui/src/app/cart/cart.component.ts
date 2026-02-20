import { Component, OnInit, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CartService, CartItem } from '../services/cart.service';
import { OrderService, CheckoutRequest, CheckoutResponse } from '../services/order.service';
import { PaymentService, PaymentIntentRequest, PaymentIntentResponse } from '../services/payment.service';
import { ToastrService } from 'ngx-toastr';
import { Router, RouterModule } from '@angular/router';
import { environment } from '../../environments/environment';
// Stripe imports removed - using regular input fields instead
import { AuthService } from '../auth.service';

@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './cart.component.html',
  styleUrls: ['./cart.component.scss'],
})
export class CartComponent implements OnInit {
  cartItems: CartItem[] = [];
  // Checkout form fields (ngModel)
  fullName: string = '';
  address: string = '';
  phoneNumber: string = '';
  cardNumber: string = '';
  cardExpiry: string = '';
  cardCvc: string = '';

  // UI/payment state
  submitting = false;
  error = '';
  isPaymentProcessing = false;

  // Modal states
  isCheckoutModalVisible: boolean = false;
  isClearCartModalVisible: boolean = false;

  // Payment fields
  postalCode: string = '';

  constructor(
    private cartService: CartService,
    private orderService: OrderService,
    private paymentService: PaymentService,
    private toastr: ToastrService,
    private router: Router,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.loadCart();
  }

  // Load the cart items from the CartService
  loadCart(): void {
    this.cartItems = this.cartService.getCartItems();
  }

  // Update product quantity with better UX
  updateQuantity(item: CartItem, newQuantity: number): void {
    if (newQuantity < 1) {
      this.removeFromCart(item.product.productId);
      return;
    }

    // Check stock availability
    if (item.product.stock > 0 && newQuantity > item.product.stock) {
      this.toastr.warning(`Only ${item.product.stock} items available in stock`, 'Stock Limit');
      return;
    }

    item.quantity = newQuantity;
    this.cartService.updateQuantity(item.product.productId, newQuantity);
    this.toastr.success(`Quantity updated for ${item.product.name}`, 'Cart Updated');
  }

  // Update product quantity and handle removal if quantity is less than 1
  updateCart(item: CartItem): void {
    if (item.quantity < 1) {
      this.removeFromCart(item.product.productId);
    } else {
      this.cartService.updateQuantity(item.product.productId, item.quantity);
      this.toastr.info(
        `Quantity updated for ${item.product.name}`,
        'Cart Updated'
      );
    }
  }

  // Remove item from cart and display a notification
  removeFromCart(productId: number): void {
    const productName = this.cartItems.find(
      (i) => i.product.productId === productId
    )?.product.name;
    this.cartService.removeItem(productId);
    this.loadCart();
    this.toastr.warning(`${productName} removed from cart`, 'Item Removed');
  }

  // Display the confirmation modal to clear the cart
  clearCart(): void {
    this.isClearCartModalVisible = true;
    document.body.style.overflow = 'hidden';
  }

  // Calculate the subtotal amount (before tax and shipping)
  getSubtotal(): string {
    const subtotal = this.cartItems.reduce(
      (sum, item) => sum + item.product.price * item.quantity,
      0
    );
    return subtotal.toFixed(2);
  }

  // Calculate tax (assuming 8.5% tax rate)
  getTax(): string {
    const subtotal = parseFloat(this.getSubtotal());
    const tax = subtotal * 0.085; // 8.5% tax rate
    return tax.toFixed(2);
  }

  // Calculate the total amount of all items in the cart (including tax)
  getTotal(): string {
    const subtotal = parseFloat(this.getSubtotal());
    const tax = parseFloat(this.getTax());
    const total = subtotal + tax;
    return total.toFixed(2);
  }

  // Clear all items from the cart and update the UI
  confirmClearCart(): void {
    this.cartService.clearCart();
    this.loadCart();
    this.toastr.error(
      'All items have been removed from your cart.',
      'Cart Cleared 🗑️',
      {
        progressBar: true,
        closeButton: true,
      }
    );
    this.closeClearCartModal();
  }

  // --- Checkout Modal Flow ---
  async openCheckoutModal(): Promise<void> {
    console.log('openCheckoutModal called'); // Debug log
    this.error = '';
    
    if (this.cartItems.length === 0) {
      this.toastr.info('Your cart is empty.');
      return;
    }

    // Check if user is logged in, if not redirect to login
    if (!this.authService.isLoggedIn()) {
      this.toastr.info('Please sign in to proceed with checkout', 'Sign In Required');
      this.router.navigate(['/login']);
      return;
    }

    // Reset form fields when opening modal
    this.fullName = '';
    this.address = '';
    this.phoneNumber = '';
    this.postalCode = '';
    this.cardNumber = '';
    this.cardExpiry = '';
    this.cardCvc = '';

    // Check if user has valid JWT token (required for API calls)
    const token = localStorage.getItem('authToken') || localStorage.getItem('accessToken');
    const isOAuthUser = this.authService.isOAuthAuthenticated();
    
    if (!token && isOAuthUser) {
      this.error = 'OAuth authentication completed but JWT token not received. Please try logging in again.';
      this.toastr.error('OAuth authentication issue. Please try logging in again.', 'Authentication Required');
      this.router.navigate(['/login']);
      return;
    }
    
    if (!token) {
      this.error = 'Please log in to proceed with checkout.';
      this.toastr.error('Please log in to proceed with checkout', 'Authentication Required');
      this.router.navigate(['/login']);
      return;
    }

    // Show modal directly - no need for Stripe elements
    this.isCheckoutModalVisible = true;
    document.body.style.overflow = 'hidden';
  }

  // Stripe-related methods removed - using regular input fields instead

  canProceed(): boolean {
    return !!this.fullName && 
           !!this.address && 
           this.isValidPhone() && 
           !!this.postalCode && 
           this.isValidPostalCode() &&
           !!this.cardNumber && 
           this.isValidCardNumber() &&
           !!this.cardExpiry && 
           this.isValidExpiryDate() &&
           !!this.cardCvc && 
           this.isValidCvc() &&
           this.cartItems.length > 0;
  }

  isValidPhone(): boolean {
    const digitsOnly = (this.phoneNumber || '').replace(/\D/g, '');
    return /^\d{10}$/.test(digitsOnly);
  }

  // Card validation methods
  isValidCardNumber(): boolean {
    const digitsOnly = (this.cardNumber || '').replace(/\D/g, '');
    return /^\d{16}$/.test(digitsOnly);
  }

  isValidExpiryDate(): boolean {
    const expiry = this.cardExpiry || '';
    const match = expiry.match(/^(\d{2})\/(\d{2})$/);
    if (!match) return false;
    
    const month = parseInt(match[1], 10);
    const year = parseInt(match[2], 10);
    const currentYear = new Date().getFullYear() % 100;
    const currentMonth = new Date().getMonth() + 1;
    
    return month >= 1 && month <= 12 && 
           (year > currentYear || (year === currentYear && month >= currentMonth));
  }

  isValidCvc(): boolean {
    const digitsOnly = (this.cardCvc || '').replace(/\D/g, '');
    return /^\d{3,4}$/.test(digitsOnly);
  }

  isValidPostalCode(): boolean {
    const digitsOnly = (this.postalCode || '').replace(/\D/g, '');
    return /^\d{5,10}$/.test(digitsOnly);
  }

  // Formatting methods
  formatCardNumber(event: any): void {
    let value = event.target.value.replace(/\D/g, '');
    if (value.length > 16) {
      value = value.substring(0, 16);
    }
    // Add spaces every 4 digits
    value = value.replace(/(\d{4})(?=\d)/g, '$1 ');
    this.cardNumber = value;
  }

  formatExpiryDate(event: any): void {
    let value = event.target.value.replace(/\D/g, '');
    if (value.length > 4) {
      value = value.substring(0, 4);
    }
    // Add slash after 2 digits
    if (value.length >= 2) {
      value = value.substring(0, 2) + '/' + value.substring(2);
    }
    this.cardExpiry = value;
  }

  formatCvc(event: any): void {
    let value = event.target.value.replace(/\D/g, '');
    if (value.length > 4) {
      value = value.substring(0, 4);
    }
    this.cardCvc = value;
  }

  formatPostalCode(event: any): void {
    let value = event.target.value.replace(/\D/g, '');
    if (value.length > 10) {
      value = value.substring(0, 10);
    }
    this.postalCode = value;
  }

  async proceedToPayment(): Promise<void> {
    if (!this.isValidPhone()) {
      this.error = 'Enter a valid 10-digit mobile number.';
      return;
    }
    
    if (!this.isValidCardNumber()) {
      this.error = 'Please enter a valid 16-digit card number.';
      return;
    }
    
    if (!this.isValidExpiryDate()) {
      this.error = 'Please enter a valid expiry date (MM/YY).';
      return;
    }
    
    if (!this.isValidCvc()) {
      this.error = 'Please enter a valid CVC (3-4 digits).';
      return;
    }
    
    if (!this.isValidPostalCode()) {
      this.error = 'Please enter a valid postal code (5-10 digits).';
      return;
    }
    
    this.submitting = true;
    this.isPaymentProcessing = true;
    this.error = '';

    // For demo purposes, we'll simulate a successful payment
    // In a real application, you would integrate with a payment processor here
    try {
      // Simulate payment processing delay
      await new Promise(resolve => setTimeout(resolve, 3000));
      
      // Simulate successful payment
      this.placeOrder();
      this.closeCheckoutModal();
      
    } catch (error: any) {
      this.submitting = false;
      this.isPaymentProcessing = false;
      this.error = error.message || 'Payment failed. Please try again.';
    }
  }

  private placeOrder(): void {
    const payload: CheckoutRequest = {
      fullName: this.fullName,
      address: this.address,
      phoneNumber: this.phoneNumber,
      items: this.cartItems.map((item) => ({
        productId: (item as any).product?.productId ?? (item as any).product?.id,
        quantity: item.quantity,
      })),
    };

    const start = performance.now();
    this.orderService.checkout(payload).subscribe({
      next: (res: CheckoutResponse) => {
        
        // Persist a lightweight summary for the success page
        try {
          const itemsSnapshot = this.cartItems.map((i) => ({
            name: (i as any).product?.name,
            price: (i as any).product?.price ?? (i as any).price,
            quantity: i.quantity,
            imageUrl: (i as any).product?.imageUrl
          }));
          const summary = {
            orderId: res?.orderId,
            fullName: this.fullName,
            address: this.address,
            phoneNumber: this.phoneNumber,
            items: itemsSnapshot,
            total: parseFloat(this.getTotal())
          };
          localStorage.setItem('lastOrderSummary', JSON.stringify(summary));
          localStorage.setItem('lastOrderItems', JSON.stringify(itemsSnapshot));
        } catch (error) {
          // Silent fail for localStorage operations
        }

        this.cartService.clearCart();
        this.submitting = false;
        this.isPaymentProcessing = false;
        this.router.navigate(['/success'], { queryParams: { orderId: res?.orderId } });
        const responseTime = performance.now() - start;
      },
      error: (err: any) => {
        this.submitting = false;
        this.isPaymentProcessing = false;
        this.error = 'Failed to place order.';
        const responseTime = performance.now() - start;
      },
    });
  }

  // Modal management methods
  closeCheckoutModal(): void {
    this.isCheckoutModalVisible = false;
    document.body.style.overflow = 'auto';
  }

  closeClearCartModal(): void {
    this.isClearCartModalVisible = false;
    document.body.style.overflow = 'auto';
  }

  // Handle backdrop clicks
  onCheckoutBackdropClick(event: Event): void {
    if (event.target === event.currentTarget) {
      this.closeCheckoutModal();
    }
  }

  onClearCartBackdropClick(event: Event): void {
    if (event.target === event.currentTarget) {
      this.closeClearCartModal();
    }
  }

  // Handle escape key
  @HostListener('document:keydown.escape', ['$event'])
  onEscapeKey(event: KeyboardEvent): void {
    if (this.isCheckoutModalVisible) {
      this.closeCheckoutModal();
    } else if (this.isClearCartModalVisible) {
      this.closeClearCartModal();
    }
  }
}

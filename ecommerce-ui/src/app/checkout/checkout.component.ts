import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { CartService } from '../services/cart.service';
import { ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { MonitoringService } from '../services/monitoring.service';
import { LoaderComponent } from '../shared/loader/loader.component';
import { environment } from '../../environments/environment';
import { Stripe, StripeElements, StripeCardElement, loadStripe } from '@stripe/stripe-js';
import { AuthService } from '../auth.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-checkout',
  standalone: true,
  templateUrl: './checkout.component.html',
  styleUrls: ['./checkout.component.scss'],
  imports: [ReactiveFormsModule, CommonModule, LoaderComponent],
})
export class CheckoutComponent implements OnInit {
  checkoutForm: FormGroup;
  cartItems: any[] = [];
  total: number = 0;
  submitting = false;
  error = '';

  // Stripe
  private stripe: Stripe | null = null;
  private elements: StripeElements | null = null;
  private cardElement: StripeCardElement | null = null;
  private clientSecret: string | null = null;

  constructor(
    private fb: FormBuilder,
    private cartService: CartService,
    private http: HttpClient,
    private router: Router,
    private monitoring: MonitoringService,
    private authService: AuthService,
    private toastr: ToastrService
  ) {
    this.checkoutForm = this.fb.group({
      fullName: ['', Validators.required],
      address: ['', Validators.required],
      phoneNumber: ['', Validators.required],
    });
  }

  ngOnInit(): void {
    // Check if user is logged in, if not redirect to login
    if (!this.authService.isLoggedIn()) {
      this.toastr.info('Please sign in to proceed with checkout', 'Sign In Required');
      this.router.navigate(['/login']);
      return;
    }

    this.cartItems = this.cartService.getCartItems();
    this.total = this.cartService.getTotalAmount();
  }

  getTotal(): number {
    return this.cartItems.reduce((sum, item) => {
      const price = item.product?.price ?? item.price ?? 0;
      return sum + price * item.quantity;
    }, 0);
  }

  async openPaymentModal() {
    if (this.checkoutForm.invalid || this.cartItems.length === 0) return;
    this.error = '';

    try {
      if (!this.stripe) {
        this.stripe = await loadStripe(environment.stripePublishableKey);
      }
      if (!this.stripe) {
        this.error = 'Payment initialization failed.';
        return;
      }

      if (!this.elements) {
        this.elements = this.stripe.elements();
      }

      if (!this.cardElement) {
        this.cardElement = this.elements.create('card');
        const target = document.getElementById('card-element');
        if (target) {
          this.cardElement.mount('#card-element');
        }
      }

      // Create PaymentIntent
      const headers = { Authorization: `Bearer ${localStorage.getItem('accessToken')}` };
      const amountCents = Math.round(this.getTotal() * 100);
      const res: any = await this.http
        .post(`${environment.apiUrl}/payments/create-payment-intent`, { amount: amountCents, currency: 'usd' }, { headers })
        .toPromise();
      this.clientSecret = res?.clientSecret;

      // Open modal
      // @ts-ignore bootstrap global
      const modal = new (window as any).bootstrap.Modal(document.getElementById('paymentModal'));
      modal.show();
    } catch (e: any) {
      this.error = e?.error?.message || 'Failed to initialize payment.';
    }
  }

  async confirmPayment() {
    if (!this.stripe || !this.clientSecret) return;
    this.submitting = true;
    this.error = '';

    const { error, paymentIntent } = await this.stripe.confirmCardPayment(this.clientSecret, {
      payment_method: { card: this.cardElement! }
    });

    if (error) {
      this.submitting = false;
      this.error = error.message || 'Payment failed.';
      return;
    }

    if (paymentIntent && paymentIntent.status === 'succeeded') {
      this.placeOrder();
      // Close modal
      try {
        // @ts-ignore
        const modalEl = document.getElementById('paymentModal');
        // @ts-ignore
        const modal = (window as any).bootstrap.Modal.getInstance(modalEl);
        modal?.hide();
      } catch {}
    } else {
      this.submitting = false;
      this.error = 'Payment not completed.';
    }
  }

  placeOrder() {
    if (this.checkoutForm.invalid || this.cartItems.length === 0) return;

    this.submitting = true;

    const payload = {
      ...this.checkoutForm.value,
      items: this.cartItems.map((item) => ({
        productId: item.product?.productId ?? item.product?.id, // ✅ updated here
        quantity: item.quantity,
      })),
    };

    const headers = {
      Authorization: `Bearer ${localStorage.getItem('accessToken')}`,
    };

    const start = performance.now();
    this.http
      .post<any>('https://localhost:7273/api/orders/checkout', payload, {
        headers,
      })
      .subscribe({
        next: (res) => {
          this.cartService.clearCart();
          this.router.navigate(['/success'], {
            queryParams: { orderId: res.orderId },
          });

          const responseTime = performance.now() - start;
          //this.monitoring.logMetric('/api/orders/checkout', responseTime);
        },
        error: (err) => {
          this.error = 'Failed to place order.';
          this.submitting = false;

          const responseTime = performance.now() - start;
          //this.monitoring.logMetric('/api/orders/checkout', responseTime);
        },
      });
  }
}

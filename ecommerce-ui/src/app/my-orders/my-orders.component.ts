import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { OrderDto } from '../models/order.model';
import { MonitoringService } from '../services/monitoring.service';
import { LoaderComponent } from '../shared/loader/loader.component';
declare var bootstrap: any;

interface ApiResponse<T> {
  isSuccessful: boolean;
  status: string;
  statusReason: string;
  data: T;
}

@Component({
  selector: 'app-my-orders',
  templateUrl: './my-orders.component.html',
  styleUrls: ['./my-orders.component.scss'],
  standalone: true,
  imports: [CommonModule, RouterModule, LoaderComponent],
})
export class MyOrdersComponent implements OnInit {
  orders: OrderDto[] = [];
  selectedOrder!: OrderDto;
  loading = false;
  error: string | null = null;
  imageFailed = false;

  constructor(
    private http: HttpClient,
    private monitoring: MonitoringService
  ) {}

  ngOnInit(): void {
    const start = performance.now();
    const headers = {
      Authorization: `Bearer ${localStorage.getItem('accessToken')}`,
    };
    this.loading = true;
    this.error = null;

    this.http
      .get<ApiResponse<OrderDto[]>>('https://localhost:7273/api/orders/my-orders', { headers })
      .subscribe({
        next: (response) => {
          this.orders = response.data ?? [];
          this.loading = false;
          const responseTime = performance.now() - start;
          //this.monitoring.logMetric('/api/orders/my-orders', responseTime);
        },
        error: (err) => {
          this.error = 'Unable to load your orders right now. Please try again later.';
          this.loading = false;
          const responseTime = performance.now() - start;
          //this.monitoring.logMetric('/api/orders/my-orders', responseTime);
        },
      });
  }

  getTotalAmount(order: OrderDto): number {
    return order.items.reduce(
      (total, item) => total + item.price * item.quantity,
      0
    );
  }

  openOrderPopup(order: OrderDto): void {
    this.selectedOrder = order;
    this.imageFailed = false;

    const modalElement = document.getElementById('orderModal');
    if (modalElement) {
      const modal = new bootstrap.Modal(modalElement);
      modal.show();
    }
  }

  getExpectedDate(order: OrderDto): string {
    // Fallback expected date: createdAt + 7 days
    const base = new Date(order.createdAt);
    const expected = new Date(base.getTime() + 7 * 24 * 60 * 60 * 1000);
    return expected.toLocaleDateString(undefined, { year: 'numeric', month: 'long', day: 'numeric' });
  }

  getOrderStatus(order: OrderDto): string {
    // Use the actual status from the API
    return order.status || 'Pending';
  }

  getStatusClass(order: OrderDto): string {
    const status = this.getOrderStatus(order).toLowerCase();
    if (status.includes('delivered')) return 'status-delivered';
    if (status.includes('transit') || status.includes('shipped')) return 'status-transit';
    if (status.includes('pending')) return 'status-pending';
    if (status.includes('cancelled')) return 'status-cancelled';
    return 'status-transit';
  }

  getStatusIcon(order: OrderDto): string {
    const status = this.getOrderStatus(order).toLowerCase();
    if (status.includes('delivered')) return 'fas fa-check-circle';
    if (status.includes('transit') || status.includes('shipped')) return 'fas fa-truck';
    if (status.includes('pending')) return 'fas fa-clock';
    if (status.includes('cancelled')) return 'fas fa-times-circle';
    return 'fas fa-truck';
  }

  getItemTotal(price: number, quantity: number): number {
    return (price ?? 0) * (quantity ?? 0);
  }

  getPreviewImage(): string | null {
    const item: any = this.selectedOrder?.items?.[0];
    if (!item) return null;
    return item.imageUrl || item.ImageUrl || null;
  }

  onPreviewError(): void {
    this.imageFailed = true;
  }

  onImageError(event: any): void {
    event.target.style.display = 'none';
    const placeholder = event.target.nextElementSibling;
    if (placeholder) {
      placeholder.style.display = 'flex';
    }
  }
}

import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CommonModule, DatePipe } from '@angular/common';
import { AdminFooterComponent } from '../admin-footer/admin-footer.component';
import { AdminOrder } from '../models/adminorder';
import { PagedResult } from '../models/adminorder';
import { FormsModule } from '@angular/forms';
import { MonitoringService } from '../services/monitoring.service';
import { LoaderComponent } from '../shared/loader/loader.component';
import { ApiResponse } from '../models/api-response.model';
import { CsvExportService } from '../services/csv-export.service';
import { ToastrService } from 'ngx-toastr';

declare var bootstrap: any;
const token = localStorage.getItem('accessToken');

@Component({
  selector: 'app-admin-orders',
  templateUrl: './admin-orders.component.html',
  styleUrls: ['./admin-orders.component.scss'],
  standalone: true,
  imports: [CommonModule, AdminFooterComponent, FormsModule, LoaderComponent],
})
export class AdminOrdersComponent implements OnInit {
  selectedOrder: any = null;
  orders: AdminOrder[] = [];
  totalCount = 0;
  pageNumber = 1;
  pageSize = 10;
  searchTerm = '';
  statusFilter = '';
  copiedId: string = '';
  loading = false;
  error = '';
  Math = Math; // Make Math available in template

  constructor(
    private http: HttpClient,
    private monitoring: MonitoringService,
    private csvExportService: CsvExportService,
    private toastr: ToastrService
  ) {}


  ngOnInit(): void {
    const savedSize = localStorage.getItem('pageSize');
    if (savedSize) this.pageSize = +savedSize;
    this.loadOrders();
  }

  loadOrders(): void {
    this.loading = true;
    this.error = '';
    
    const params = {
      PageNumber: this.pageNumber,
      PageSize: this.pageSize,
      SearchTerm: this.searchTerm,
      Status: this.statusFilter,
    };

    const start = performance.now();
    this.http
      .get<ApiResponse<PagedResult<AdminOrder>>>(
        'https://localhost:7273/api/admin/all-orders',
        { headers: { Authorization: `Bearer ${token}` }, params }
      )
      .subscribe({
        next: (res) => {
          if (res.isSuccessful && res.data) {
            this.orders = res.data.items;
            this.totalCount = res.data.totalCount;
            this.loading = false;

            const responseTime = performance.now() - start;
          } else {
            this.error = res.statusReason || 'Failed to load orders';
            this.loading = false;
          }
        },
        error: (err) => {
          this.error = 'Failed to load orders. Please try again.';
          this.loading = false;
        },
      });
  }

  viewDetails(order: any): void {
    this.selectedOrder = order;
  }

  closeModal(): void {
    this.selectedOrder = null;
  }

  getTotalAmount(order: any): number {
    return order.items.reduce((total: number, item: any) => {
      return total + item.price * item.quantity;
    }, 0);
  }
  getFormattedDate(date: string): string {
    const datePipe = new DatePipe('en-US');
    return datePipe.transform(date, 'mediumDate') || '';
  }

  updateStatus(orderId: string, newStatus: string): void {
    const order = this.orders.find((o) => o.id === orderId);
    if (!order || order.status === newStatus) return;

    this.http
      .put<ApiResponse<any>>(
        `https://localhost:7273/api/admin/orders/${orderId}/status`,
        { status: newStatus },
        {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        }
      )
      .subscribe({
        next: (res) => {
          console.log('Status update response:', res); // Debug log
          // The API returns ActionResult<object> structure: {result: {}, value: null}
          // For successful updates, we expect a 200 status code
          if (res) {
            // Update local order status
            order.status = newStatus;
            // Refresh the orders list to ensure UI is updated
            this.loadOrders();
          } else {
            console.error('Status update failed: No response received');
            this.toastr.error('Failed to update order status', 'Error');
          }
        },
        error: (err) => {
          console.error('Status update error:', err);
          this.toastr.error('Failed to update order status', 'Error');
        },
      });
  }

  pendingStatus: string = '';
  pendingOrderId: string = '';
  statusSuccessMessage: string = '';
  selectedOrderForStatus: AdminOrder | null = null;

  confirmStatusChange(): void {
    const order = this.orders.find((o) => o.id === this.pendingOrderId);
    if (!order || order.status === this.pendingStatus) return;

    this.http
      .put<ApiResponse<any>>(
        `https://localhost:7273/api/admin/orders/${this.pendingOrderId}/status`,
        { status: this.pendingStatus },
        {
          headers: {
            'Content-Type': 'application/json',
            Authorization: `Bearer ${token}`,
          },
        }
      )
      .subscribe({
        next: (res) => {
          console.log('Status update response:', res); // Debug log
          // The API returns ActionResult<object> structure: {result: {}, value: null}
          // For successful updates, we expect a 200 status code
          if (res) {
            // ✅ Success message (toast)
            this.statusSuccessMessage = `Order status updated to "${this.pendingStatus}" successfully.`;

            // 🧼 Cleanup
            this.pendingStatus = '';
            this.pendingOrderId = '';

            // Auto-dismiss toast after 3s
            setTimeout(() => (this.statusSuccessMessage = ''), 3000);
            
            // 🔄 Refresh the orders list to show updated data
            this.loadOrders();
          } else {
            console.error('Status update failed: No response received');
            this.toastr.error('Failed to update order status', 'Error');
          }
        },
        error: (err) => {
          console.error('Status update error:', err);
          this.toastr.error('Failed to update order status', 'Error');
        },
      });
  }

  quickChangeStatus(order: AdminOrder, newStatus: string): void {
    if (order.status === newStatus) return;
    this.pendingOrderId = order.id;
    this.pendingStatus = newStatus;
    this.confirmStatusChange();
  }

  cancelStatusChange(): void {
    this.pendingStatus = '';
    this.pendingOrderId = '';
    const modalEl = document.getElementById('changeStatusModal');
    if (modalEl) bootstrap.Modal.getInstance(modalEl)?.hide();
  }

  getMinValue(a: number, b: number): number {
    return Math.min(a, b);
  }
  decrementPage(): void {
    if (this.pageNumber > 1) {
      this.pageNumber--;
      this.loadOrders();
    }
  }
  nextPage() {
    this.pageNumber++;
    this.loadOrders();
  }
  onPageSizeChange(): void {
    localStorage.setItem('pageSize', this.pageSize.toString());
    this.pageNumber = 1;
    this.loadOrders();
  }

  copyOrderId(orderId: string): void {
    if (navigator && navigator.clipboard && navigator.clipboard.writeText) {
      navigator.clipboard.writeText(orderId).then(() => {
        this.copiedId = orderId;
        setTimeout(() => {
          if (this.copiedId === orderId) this.copiedId = '';
        }, 1500);
      });
    }
  }

  getTotalPages(): number {
    return Math.max(1, Math.ceil(this.totalCount / this.pageSize));
  }

  // CSV Export functionality
  exportToCsv(): void {
    if (!this.orders || this.orders.length === 0) {
      this.toastr.warning('No orders to export', 'Export Warning');
      return;
    }

    try {
      const timestamp = new Date().toISOString().split('T')[0];
      const filename = `orders_export_${timestamp}.csv`;
      
      // Transform orders to include calculated total amount and order number
      const ordersWithTotals = this.orders.map(order => ({
        ...order,
        orderNumber: (order as any).orderNumber || `ORD-${order.id}`,
        totalAmount: this.getTotalAmount(order)
      }));

      this.csvExportService.exportOrders(ordersWithTotals, filename);
      this.toastr.success(`Exported ${this.orders.length} orders successfully!`, 'Export Complete');
    } catch (error) {
      console.error('Export error:', error);
      this.toastr.error('Failed to export orders', 'Export Error');
    }
  }
}

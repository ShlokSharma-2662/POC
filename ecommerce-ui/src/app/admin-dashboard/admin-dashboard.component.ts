import { Component, OnInit } from '@angular/core';
import { CommonModule, CurrencyPipe, DecimalPipe } from '@angular/common';
import { RouterModule } from '@angular/router';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AdminFooterComponent } from "../admin-footer/admin-footer.component";

interface PagedResult<T> { items: T[]; totalCount: number; }
interface AdminOrderItem { price: number; quantity: number; }
interface AdminOrder { id: string; createdAt: string; items: AdminOrderItem[]; }
interface AdminUser { id: number; }
interface ErrorLog { id: number; timestamp: string; }
interface ApiResponse<T> { isSuccessful: boolean; data: T; status: string; statusReason: string; }

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, AdminFooterComponent, CurrencyPipe, DecimalPipe],
  templateUrl: './admin-dashboard.component.html',
  styleUrls: ['./admin-dashboard.component.scss']
})
export class AdminDashboardComponent implements OnInit {
  ordersCount = 0;
  revenue = 0;
  usersCount = 0;
  errors24h = 0;

  loadingOrders = false;
  loadingRevenue = false;
  loadingUsers = false;
  loadingErrors = false;

  private apiBase = 'https://localhost:7273/api';

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    const headers = this.buildAuthHeaders();
    this.fetchOrders(headers);
    this.fetchUsers(headers);
    this.fetchErrors(headers);
  }

  private buildAuthHeaders(): { headers: HttpHeaders } {
    const token = localStorage.getItem('accessToken') || '';
    return { headers: new HttpHeaders({ Authorization: `Bearer ${token}` }) };
  }

  private fetchOrders(options: { headers: HttpHeaders }): void {
    this.loadingOrders = this.loadingRevenue = true;
    // Request a large page to aggregate revenue on the client (temporary until dedicated stats API)
    this.http.get<ApiResponse<PagedResult<AdminOrder>>>(`${this.apiBase}/admin/all-orders?pageNumber=1&pageSize=1000`, options)
      .subscribe({
        next: (response) => {
          if (response.isSuccessful && response.data) {
            this.ordersCount = response.data.totalCount ?? 0;
            this.revenue = (response.data.items || []).reduce((sum, o) => sum + (o.items || []).reduce((s, i) => s + (i.price || 0) * (i.quantity || 0), 0), 0);
          } else {
            this.ordersCount = 0;
            this.revenue = 0;
          }
          this.loadingOrders = false;
          this.loadingRevenue = false;
        },
        error: (err) => {
          this.ordersCount = 0;
          this.revenue = 0;
          this.loadingOrders = false;
          this.loadingRevenue = false;
        }
      });
  }

  private fetchUsers(options: { headers: HttpHeaders }): void {
    this.loadingUsers = true;
    this.http.get<ApiResponse<PagedResult<AdminUser>>>(`${this.apiBase}/admin/users?pageNumber=1&pageSize=1`, options)
      .subscribe({
        next: (response) => {
          if (response.isSuccessful && response.data) {
            this.usersCount = response.data.totalCount ?? 0;
          } else {
            this.usersCount = 0;
          }
          this.loadingUsers = false;
        },
        error: (err) => {
          this.usersCount = 0;
          this.loadingUsers = false;
        }
      });
  }

  private fetchErrors(options: { headers: HttpHeaders }): void {
    this.loadingErrors = true;
    this.http.get<ApiResponse<PagedResult<ErrorLog>>>(`${this.apiBase}/admin/errors`, options)
      .subscribe({
        next: (response) => {
          if (response.isSuccessful && response.data) {
            const since = Date.now() - 24 * 60 * 60 * 1000;
            this.errors24h = (response.data.items || []).filter(l => new Date(l.timestamp).getTime() >= since).length;
          } else {
            this.errors24h = 0;
          }
          this.loadingErrors = false;
        },
        error: (err) => {
          this.errors24h = 0;
          this.loadingErrors = false;
        }
      });
  }
}


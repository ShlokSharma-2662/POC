import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { BaseApiService } from './base-api.service';
import { ApiResponse } from '../models/api-response.model';

export interface OrderDto {
  id: number;
  orderNumber: string;
  fullName: string;
  address: string;
  phoneNumber: string;
  totalAmount: number;
  status: string;
  createdAt: string;
  items: OrderItemDto[];
}

export interface OrderItemDto {
  id: number;
  productId: number;
  productName: string;
  productImageUrl: string;
  quantity: number;
  price: number;
}

export interface CheckoutRequest {
  fullName: string;
  address: string;
  phoneNumber: string;
  items: {
    productId: number;
    quantity: number;
  }[];
}

export interface CheckoutResponse {
  orderId: number;
  orderNumber: string;
  totalAmount: number;
}

@Injectable({
  providedIn: 'root'
})
export class OrderService extends BaseApiService {
  private apiUrl = `${this.baseUrl}/orders`;

  constructor(http: HttpClient) {
    super(http);
  }

  private getHeaders(): HttpHeaders {
    const token = localStorage.getItem('accessToken');
    return new HttpHeaders({
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    });
  }

  checkout(request: CheckoutRequest): Observable<CheckoutResponse> {
    return this.handleRequest(
      this.http.post<ApiResponse<CheckoutResponse>>(`${this.apiUrl}/checkout`, request, { 
        headers: this.getHeaders() 
      })
    );
  }

  getMyOrders(): Observable<OrderDto[]> {
    return this.handleRequest(
      this.http.get<ApiResponse<OrderDto[]>>(`${this.apiUrl}/my-orders`, { 
        headers: this.getHeaders() 
      })
    );
  }

  getOrderById(orderId: number): Observable<OrderDto> {
    return this.handleRequest(
      this.http.get<ApiResponse<OrderDto>>(`${this.apiUrl}/${orderId}`, { 
        headers: this.getHeaders() 
      })
    );
  }
}


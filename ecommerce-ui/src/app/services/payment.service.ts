import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { BaseApiService } from './base-api.service';
import { ApiResponse } from '../models/api-response.model';

export interface PaymentIntentRequest {
  amount: number;
  currency: string;
}

export interface PaymentIntentResponse {
  clientSecret: string;
}

@Injectable({
  providedIn: 'root'
})
export class PaymentService extends BaseApiService {
  private apiUrl = `${this.baseUrl}/payments`;

  constructor(http: HttpClient) {
    super(http);
  }

  createPaymentIntent(request: PaymentIntentRequest): Observable<PaymentIntentResponse> {
    return this.handleRequest(
      this.http.post<ApiResponse<PaymentIntentResponse>>(`${this.apiUrl}/create-payment-intent`, request)
    );
  }
}


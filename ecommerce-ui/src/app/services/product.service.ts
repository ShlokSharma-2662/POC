import { Injectable } from '@angular/core';
import { HttpClient, HttpParams, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Product } from '../models/product';
import { tap } from 'rxjs/operators';
import { MonitoringService } from './monitoring.service';
import { BaseApiService } from './base-api.service';
import { ApiResponse } from '../models/api-response.model';

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
}

@Injectable({
  providedIn: 'root',
})
export class ProductService extends BaseApiService {
  private apiUrl = `${this.baseUrl}/products`;

  constructor(
    http: HttpClient,
    private monitoring: MonitoringService
  ) {
    super(http);
  }

  getProducts(
    pageNumber: number,
    pageSize: number,
    categoryId?: number,
    searchTerm?: string,
    minPrice?: number,
    maxPrice?: number,
    categoryName?: string
  ): Observable<PagedResult<Product>> {
    const start = performance.now();
    let params = new HttpParams()
      .set('PageNumber', pageNumber.toString())
      .set('PageSize', pageSize.toString());

    if (categoryId && categoryId !== 0) {
      params = params.set('CategoryId', categoryId.toString());
    }

    if (searchTerm && searchTerm.trim()) {
      params = params.set('SearchTerm', searchTerm.trim());
    }

    if (minPrice !== undefined && minPrice !== null) {
      params = params.set('MinPrice', minPrice.toString());
    }

    if (maxPrice !== undefined && maxPrice !== null) {
      params = params.set('MaxPrice', maxPrice.toString());
    }

    if (categoryName && categoryName.trim()) {
      params = params.set('CategoryName', categoryName.trim());
    }

    return this.handleRequest(
      this.http.get<ApiResponse<PagedResult<Product>>>(this.apiUrl, { params })
    ).pipe(
      tap(() => {
        const responseTime = performance.now() - start;
        //this.monitoring.logMetric(this.apiUrl, responseTime);
      })
    );
  }

  // Admin endpoints
  getAllProductsAdmin(pageNumber: number, pageSize: number, opts?: { isDeleted?: boolean; categoryId?: number; search?: string }) {
    let params = new HttpParams()
      .set('PageNumber', String(pageNumber))
      .set('PageSize', String(pageSize));
    if (opts?.isDeleted !== undefined) params = params.set('IsDeleted', String(opts.isDeleted));
    if (opts?.categoryId) params = params.set('CategoryId', String(opts.categoryId));
    if (opts?.search) params = params.set('Search', opts.search);
    return this.handleRequest(
      this.http.get<ApiResponse<PagedResult<Product>>>(`${this.apiUrl}/admin`, { 
        params,
        headers: this.getAuthHeaders()
      })
    );
  }

  createProduct(payload: Partial<Product>) {
    return this.handleRequest(
      this.http.post<ApiResponse<{ productId: number }>>(this.apiUrl, payload, {
        headers: this.getAuthHeaders()
      })
    );
  }

  updateProduct(product: Product | any) {
    console.log('Updating product:', product);
    console.log('Auth headers:', this.getAuthHeaders());
    return this.http.put<any>(`${this.apiUrl}/${product.productId}`, product, {
      headers: this.getAuthHeaders()
    });
  }

  // multipart/form-data variants
  createProductWithImage(formData: FormData) {
    return this.handleRequest(
      this.http.post<ApiResponse<{ productId: number }>>(`${this.apiUrl}/with-image`, formData, {
        headers: this.getAuthHeaders()
      })
    );
  }

  updateProductWithImage(productId: number, formData: FormData) {
    return this.http.put<any>(`${this.apiUrl}/${productId}/with-image`, formData, {
      headers: this.getAuthHeaders()
    });
  }

  softDeleteProduct(productId: number) {
    return this.http.delete<any>(`${this.apiUrl}/${productId}`, {
      headers: this.getAuthHeaders()
    });
  }

  restoreProduct(productId: number) {
    return this.http.post<any>(`${this.apiUrl}/${productId}/restore`, {}, {
      headers: this.getAuthHeaders()
    });
  }

  getProductById(productId: number): Observable<Product> {
    return this.handleRequest(
      this.http.get<ApiResponse<Product>>(`${this.apiUrl}/${productId}`)
    );
  }

  private getAuthHeaders(): HttpHeaders {
    const token = localStorage.getItem('accessToken');
    return token ? new HttpHeaders({ 'Authorization': `Bearer ${token}` }) : new HttpHeaders();
  }
}

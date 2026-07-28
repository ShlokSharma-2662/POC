export interface AdminPagedResult<T> {
  items: T[];
  totalCount: number;
}

export interface AdminOrderItem {
  productId: number;
  productName: string;
  description: string;
  price: number;
  quantity: number;
}

export interface AdminOrder {
  id: string;
  customerName: string;
  phone: string;
  shippingAddress: string;
  createdAt: string;
  status: string;
  items: AdminOrderItem[];
}

export interface AdminUser {
  id: number;
  username: string;
  email: string;
  role: string;
  status: string;
  createdAt: string;
  updatedAt: string;
}

export interface AdminCategory {
  id: number;
  name: string;
}

export interface AdminProduct {
  productId: number;
  name: string;
  description: string;
  price: number;
  imageUrl?: string;
  categoryName?: string;
  stock: number;
  categoryId: number;
  isDeleted?: boolean;
  createdAt?: string;
  updatedAt?: string;
}

export interface SystemMetric {
  id: number;
  endpoint: string;
  method: string;
  statusCode: number;
  responseTimeMs: number;
  timestamp: string;
  userAgent?: string;
  ipAddress?: string;
  isThresholdExceeded?: boolean;
}

export interface ErrorLog {
  id: number;
  message: string;
  stackTrace?: string;
  path: string;
  timestamp: string;
  severity: string;
  userAgent?: string;
  ipAddress?: string;
}

export interface RevenuePeriod {
  startDate: string;
  endDate: string;
  revenue: number;
  orderCount: number;
  averageOrderValue: number;
  periodLabel: string;
}

export interface RevenueReport {
  totalRevenue: number;
  totalOrders: number;
  periods: RevenuePeriod[];
  reportStartDate: string;
  reportEndDate: string;
  reportType: string;
}

export interface RevenueSummary {
  totalRevenue: number;
  totalOrders: number;
  averageOrderValue: number;
  revenueGrowth: number;
  orderGrowth: number;
  lastUpdated: string;
}

export interface EndpointPerformance {
  name: string;
  avgResponseTime: number;
  requestCount: number;
  errorRate: number;
  successRate: number;
  minTime: number;
  maxTime: number;
  healthStatus: 'Healthy' | 'Warning' | 'Critical';
  lastCalled: string;
}

export interface ProductDraft {
  name: string;
  description: string;
  price: number;
  stock: number;
  imageUrl: string;
  categoryId: number;
}

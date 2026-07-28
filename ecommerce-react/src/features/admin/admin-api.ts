import { api } from '../../lib/api-client';
import type {
  AdminCategory,
  AdminOrder,
  AdminPagedResult,
  AdminProduct,
  AdminUser,
  ErrorLog,
  ProductDraft,
  RevenueReport,
  RevenueSummary,
  SystemMetric,
} from './types';
import { buildQuery } from './utils';

interface PageOptions {
  page: number;
  pageSize: number;
}

interface OrderOptions extends PageOptions {
  search?: string;
  status?: string;
}

interface UserOptions extends PageOptions {
  search?: string;
  status?: string;
  role?: string;
}

interface ProductOptions extends PageOptions {
  search?: string;
  isDeleted?: boolean;
  categoryId?: number;
}

interface ErrorOptions extends PageOptions {
  search?: string;
  severity?: string;
}

interface MetricOptions extends PageOptions {
  endpoint?: string;
  statusCode?: number;
  fromDate?: string;
  toDate?: string;
}

export interface RevenueReportOptions {
  reportType: string;
  startDate?: string;
  endDate?: string;
  month?: number;
  year?: number;
}

function normalizePage<T>(result: AdminPagedResult<T> | null | undefined) {
  return {
    items: Array.isArray(result?.items) ? result.items : [],
    totalCount: Number(result?.totalCount ?? 0),
  } satisfies AdminPagedResult<T>;
}

export const adminApi = {
  async getOrders(options: OrderOptions) {
    const result = await api.get<AdminPagedResult<AdminOrder>>(
      `/admin/all-orders${buildQuery({
        PageNumber: options.page,
        PageSize: options.pageSize,
        SearchTerm: options.search,
        Status: options.status,
      })}`,
    );
    return normalizePage(result);
  },

  updateOrderStatus(orderId: string, status: string) {
    return api.put<{ success: boolean }>(`/admin/orders/${orderId}/status`, {
      status,
    });
  },

  async getUsers(options: UserOptions) {
    const result = await api.get<AdminPagedResult<AdminUser>>(
      `/admin/users${buildQuery({
        PageNumber: options.page,
        PageSize: options.pageSize,
        SearchTerm: options.search,
        Status: options.status,
        Role: options.role,
      })}`,
    );
    return normalizePage(result);
  },

  assignRole(userId: number, role: string) {
    return api.post<{ success: boolean }>('/admin/users/assign-role', {
      userId,
      role,
    });
  },

  resetPassword(userId: number, newPassword: string) {
    return api.post<{ success: boolean }>('/admin/users/reset-password', {
      userId,
      newPassword,
    });
  },

  deactivateUser(userId: number) {
    return api.post<{ success: boolean }>('/admin/users/deactivate', {
      userId,
    });
  },

  activateUser(userId: number) {
    return api.post<{ success: boolean }>('/admin/users/activate', { userId });
  },

  async getProducts(options: ProductOptions) {
    const result = await api.get<AdminPagedResult<AdminProduct>>(
      `/products/admin${buildQuery({
        PageNumber: options.page,
        PageSize: options.pageSize,
        Search: options.search,
        IsDeleted: options.isDeleted,
        CategoryId: options.categoryId,
      })}`,
    );
    return normalizePage(result);
  },

  async getCategories() {
    const categories = await api.get<AdminCategory[]>('/categories');
    return Array.isArray(categories) ? categories : [];
  },

  createProduct(draft: ProductDraft, image?: File) {
    if (!image) return api.post<number>('/products', draft);
    const data = productFormData(draft, image);
    return api.post<number>('/products/with-image', data);
  },

  updateProduct(productId: number, draft: ProductDraft, image?: File) {
    if (!image) {
      return api.put<boolean>(`/products/${productId}`, {
        productId,
        ...draft,
      });
    }
    const data = productFormData(draft, image);
    data.append('productId', String(productId));
    return api.put<boolean>(`/products/${productId}/with-image`, data);
  },

  deleteProduct(productId: number) {
    return api.delete<boolean>(`/products/${productId}`);
  },

  restoreProduct(productId: number) {
    return api.post<boolean>(`/products/${productId}/restore`);
  },

  async getErrors(options: ErrorOptions) {
    const result = await api.get<AdminPagedResult<ErrorLog>>(
      `/admin/errors${buildQuery({
        PageNumber: options.page,
        PageSize: options.pageSize,
        SeverityFilter: options.severity,
        SearchTerm: options.search,
      })}`,
    );
    return normalizePage(result);
  },

  async getMetrics(options: MetricOptions) {
    const result = await api.get<AdminPagedResult<SystemMetric>>(
      `/admin/metrics${buildQuery({
        PageNumber: options.page,
        PageSize: options.pageSize,
        EndpointFilter: options.endpoint,
        StatusCodeFilter: options.statusCode,
        FromDate: options.fromDate,
        ToDate: options.toDate,
      })}`,
    );
    return normalizePage(result);
  },

  seedMetrics() {
    return api.post<{ count?: number }>('/admin/metrics/seed');
  },

  getRevenueReport(options: RevenueReportOptions) {
    return api.get<RevenueReport>(
      `/admin/revenue/report${buildQuery({
        reportType: options.reportType,
        startDate: options.startDate,
        endDate: options.endDate,
        month: options.month,
        year: options.year,
      })}`,
    );
  },

  getRevenueSummary(period: string) {
    return api.get<RevenueSummary>(
      `/admin/revenue/summary${buildQuery({ period })}`,
    );
  },
};

function productFormData(draft: ProductDraft, image: File) {
  const data = new FormData();
  data.append('name', draft.name);
  data.append('description', draft.description);
  data.append('price', String(draft.price));
  data.append('stock', String(draft.stock));
  data.append('categoryId', String(draft.categoryId));
  if (draft.imageUrl) data.append('imageUrl', draft.imageUrl);
  data.append('image', image);
  return data;
}

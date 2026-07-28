import type { Product } from '../../types/domain';

export interface StorefrontCategory {
  id: number;
  name: string;
  description?: string;
}

export interface StorefrontProduct extends Product {
  stockQuantity?: number;
  stockStatus?: string;
}

export interface ProductPageResult {
  items: StorefrontProduct[];
  totalCount: number;
  pageNumber?: number;
  pageSize?: number;
  totalPages?: number;
}

export interface StorefrontOrderItem {
  productId: number;
  productName: string;
  price: number;
  quantity: number;
  imageUrl?: string;
}

export interface StorefrontOrder {
  id: string;
  userId?: number;
  customerName: string;
  shippingAddress: string;
  phone: string;
  createdAt: string;
  status: string;
  items: StorefrontOrderItem[];
}

export interface StorefrontProfile {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  role: string;
  status: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface LastOrderSummary {
  ownerSubject: string;
  orderId: string;
  fullName: string;
  address: string;
  phoneNumber: string;
  items: Array<{
    name: string;
    price: number;
    quantity: number;
    imageUrl?: string;
  }>;
  total: number;
}

const currencyFormatter = new Intl.NumberFormat('en-US', {
  style: 'currency',
  currency: 'USD',
});

export function formatCurrency(value: number): string {
  return currencyFormatter.format(Number.isFinite(value) ? value : 0);
}

export function formatDate(
  value: string | Date | undefined,
  options: Intl.DateTimeFormatOptions = {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  },
): string {
  if (!value) return 'Not available';
  const date = value instanceof Date ? value : new Date(value);
  return Number.isNaN(date.getTime())
    ? 'Not available'
    : date.toLocaleDateString('en-US', options);
}

export function getErrorMessage(
  error: unknown,
  fallback = 'Something went wrong. Please try again.',
): string {
  return error instanceof Error && error.message.trim()
    ? error.message
    : fallback;
}

export function normalizedStock(product: StorefrontProduct): number {
  const value = product.stock ?? product.stockQuantity ?? 0;
  return Number.isFinite(Number(value)) ? Math.max(Number(value), 0) : 0;
}

export function normalizeProduct(product: StorefrontProduct): Product {
  return {
    ...product,
    productId: Number(product.productId ?? product.id ?? 0),
    categoryId: Number(product.categoryId ?? 0),
    price: Number(product.price ?? 0),
    stock: normalizedStock(product),
    quantity: product.quantity ?? 1,
  };
}

export function orderTotal(order: StorefrontOrder): number {
  return order.items.reduce(
    (total, item) => total + Number(item.price || 0) * Number(item.quantity || 0),
    0,
  );
}

export function expectedDelivery(createdAt: string): string {
  const created = new Date(createdAt);
  if (Number.isNaN(created.getTime())) return 'To be confirmed';
  const expected = new Date(created.getTime() + 7 * 24 * 60 * 60 * 1000);
  return formatDate(expected);
}

export function statusTone(
  status: string,
): 'success' | 'warning' | 'danger' | 'info' {
  const normalized = status.toLowerCase();
  if (normalized.includes('delivered')) return 'success';
  if (normalized.includes('cancel')) return 'danger';
  if (normalized.includes('pending')) return 'warning';
  return 'info';
}

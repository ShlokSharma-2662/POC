export interface Category {
  categoryId: number;
  name: string;
  description?: string;
}

export interface Product {
  productId: number;
  id?: number;
  name: string;
  description: string;
  price: number;
  stock: number;
  quantity?: number;
  imageUrl?: string;
  categoryId: number;
  categoryName?: string;
  isDeleted?: boolean;
}

export interface CartItem {
  product: Product;
  quantity: number;
  price: number;
}

export interface WishlistItem {
  id: number;
  productId: number;
  productName: string;
  productDescription: string;
  productPrice: number;
  productImageUrl?: string;
  categoryName?: string;
  stock: number;
  addedAt: string;
  isInStock: boolean;
}

export interface AuthUser {
  userId?: number;
  firstName?: string;
  lastName?: string;
  name?: string;
  email: string;
  role?: string;
  picture?: string;
}

export interface AuthResponse extends AuthUser {
  token: string;
  role: string;
}

export interface UserProfile {
  userId: number;
  firstName: string;
  lastName: string;
  email: string;
  role: string;
  createdAt?: string;
}

export interface OrderItem {
  productId: number;
  productName?: string;
  productImageUrl?: string;
  price: number;
  quantity: number;
}

export interface Order {
  orderId: string;
  id?: string;
  userId?: number;
  customerName?: string;
  customerEmail?: string;
  fullName?: string;
  address?: string;
  phoneNumber?: string;
  totalAmount: number;
  status: string;
  createdAt: string;
  orderDate?: string;
  items: OrderItem[];
}

export interface CheckoutRequest {
  fullName: string;
  address: string;
  phoneNumber: string;
  paymentIntentId: string;
  items: Array<{ productId: number; quantity: number }>;
}

export interface CheckoutResponse {
  orderId: string;
  totalAmount?: number;
  status?: string;
}

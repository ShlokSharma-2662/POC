export interface OrderItemDto {
  productId: number;
  productName: string;
  price: number;
  quantity: number;
  imageUrl?: string;
}

export interface OrderDto {
  id: string; // Backend returns Guid as string
  customerName: string;
  shippingAddress: string;
  phone: string;
  createdAt: Date;
  status: string; // Add missing status property
  items: OrderItemDto[];
}
  
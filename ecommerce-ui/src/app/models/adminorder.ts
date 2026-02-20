export interface AdminOrder {
    id: string;
    customerName: string;
    phone: string;
    shippingAddress: string;
    createdAt: string;
    status: string;
    items: {
      productId: number;
      productName: string;
      description: string;
      price: number;
      quantity: number;
    }[];
  }
  
  export interface PagedResult<T> {
    items: T[];
    totalCount: number;
  }
  
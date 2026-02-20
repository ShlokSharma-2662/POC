export interface Product {
    productId: number;
    name: string;
    description: string;
    price: number;
    imageUrl: string;
    categoryName: string;
    stock: number;
    quantity: number;
    categoryId: number;
    isDeleted?: boolean;
    isInWishlist?: boolean;
  }  
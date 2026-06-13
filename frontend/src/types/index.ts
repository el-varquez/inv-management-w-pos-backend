export interface Item {
  id: string;
  name: string;
  description?: string;
  sku?: string;
  costPrice: number;
  sellingPrice: number;
  stock: number;
  lowStockThreshold: number;
  isLowStock: boolean;
  isActive: boolean;
  categoryId: string;
  categoryName: string;
  createdAt: string;
}

export interface Category {
  id: string;
  name: string;
  description?: string;
  itemCount: number;
}

export interface LoginResult {
  token: string;
  name: string;
  email: string;
  role: string;
}
export type StockStatusFilter = 'All' | 'InStock' | 'OutOfStock';

export interface Product {
  id: number;
  productCode: string;
  productName: string;
  categoryId: number;
  categoryCode: string;
  categoryName: string;
  categoryIsActive: boolean;
  unit: string;
  price: number;
  quantity: number;
  stockStatus: string;
  description: string | null;
  isActive: boolean;
  createdByUserId: string | null;
  createdByUsername: string | null;
  createdByRole: string | null;
  lastModifiedByUserId: string | null;
  lastModifiedByUsername: string | null;
  lastModifiedByRole: string | null;
  isAdminProtected: boolean;
  canModify?: boolean;
  canEdit?: boolean;
  canDelete?: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export interface ProductCreatePayload {
  productName: string;
  categoryId: number;
  unit: string;
  price: number;
  quantity: number;
  description: string | null;
  isActive: boolean;
}

export interface ProductUpdatePayload {
  productName: string;
  unit: string;
  price: number;
  quantity: number;
  description: string | null;
  isActive: boolean;
}

export interface ProductSearchParams {
  keyword?: string;
  categoryId?: number;
  minPrice?: number;
  maxPrice?: number;
  stockStatus?: StockStatusFilter;
  isActive?: boolean;
  page: number;
  pageSize: number;
  sortBy: string;
  sortDirection: 'asc' | 'desc';
}

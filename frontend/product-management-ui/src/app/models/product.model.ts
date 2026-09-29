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
  stockQuantity: number;
  stockStatus: string;
  description: string | null;
  isActive: boolean;
  createdByUserId: string | null;
  createdByUsername: string | null;
  lastModifiedByUserId: string | null;
  lastModifiedByUsername: string | null;
  isAdminProtected: boolean;
  canModify?: boolean;
  canEdit?: boolean;
  canDelete?: boolean;
  isDeleted: boolean;
  deletedAt: string | null;
  deletedByUserId: string | null;
  deletedByUsername: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface ProductCreatePayload {
  productName: string;
  categoryId: number;
  unit: string;
  price: number;
  description: string | null;
  isActive: boolean;
}

export interface ProductUpdatePayload {
  productName: string;
  unit: string;
  price: number;
  description: string | null;
  isActive: boolean;
}

export interface StockInPayload {
  productId: number;
  quantity: number;
  referenceCode: string | null;
  note: string | null;
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

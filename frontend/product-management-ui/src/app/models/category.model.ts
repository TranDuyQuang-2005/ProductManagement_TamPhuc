export interface Category {
  id: number;
  categoryCode: string;
  categoryName: string;
  codePrefix: string;
  nextProductNumber: number;
  description: string | null;
  isActive: boolean;
  productCount: number;
  hasProducts: boolean;
  canEditPrefix: boolean;
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

export interface CategoryOption {
  id: number;
  categoryCode: string;
  categoryName: string;
  codePrefix: string;
  isActive: boolean;
}

export interface ProductCodePreview {
  categoryId: number;
  codePrefix: string;
  nextProductNumber: number;
  productCodePreview: string;
}

export interface CategoryPayload {
  categoryCode: string;
  categoryName: string;
  codePrefix: string;
  description: string | null;
  isActive: boolean;
}

export interface CategorySearchParams {
  keyword?: string;
  isActive?: boolean;
  page: number;
  pageSize: number;
  sortBy: string;
  sortDirection: 'asc' | 'desc';
}

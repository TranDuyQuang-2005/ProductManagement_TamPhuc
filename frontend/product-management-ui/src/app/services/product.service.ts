import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse, PagedResult } from '../models/api.model';
import { AuditLog } from '../models/audit-log.model';
import {
  Product,
  ProductCreatePayload,
  ProductSearchParams,
  ProductUpdatePayload,
  StockInPayload
} from '../models/product.model';

export interface InventoryTransaction {
  id: number;
  productId: number;
  movementType: string;
  quantityChange: number;
  quantityBefore: number;
  quantityAfter: number;
  referenceCode: string | null;
  note: string | null;
  createdByUserId: string | null;
  username: string | null;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class ProductService {
  private readonly baseUrl = `${environment.apiBaseUrl}/products`;
  private readonly inventoryUrl = `${environment.apiBaseUrl}/inventory`;

  constructor(private readonly http: HttpClient) {}

  search(params: ProductSearchParams): Observable<ApiResponse<PagedResult<Product>>> {
    return this.http.post<ApiResponse<PagedResult<Product>>>(`${this.baseUrl}/search`, params);
  }

  searchTrash(params: ProductSearchParams): Observable<ApiResponse<PagedResult<Product>>> {
    return this.http.post<ApiResponse<PagedResult<Product>>>(`${this.baseUrl}/trash/search`, params);
  }

  getById(id: number): Observable<ApiResponse<Product>> {
    return this.http.post<ApiResponse<Product>>(`${this.baseUrl}/get`, { id });
  }

  create(payload: ProductCreatePayload): Observable<ApiResponse<Product>> {
    return this.http.post<ApiResponse<Product>>(`${this.baseUrl}/create`, payload);
  }

  update(id: number, payload: ProductUpdatePayload): Observable<ApiResponse<Product>> {
    return this.http.post<ApiResponse<Product>>(`${this.baseUrl}/update`, { id, ...payload });
  }

  delete(id: number): Observable<ApiResponse<object>> {
    return this.http.post<ApiResponse<object>>(`${this.baseUrl}/delete`, { id });
  }

  restore(id: number): Observable<ApiResponse<Product>> {
    return this.http.post<ApiResponse<Product>>(`${this.baseUrl}/restore`, { id });
  }

  deletePermanent(id: number): Observable<ApiResponse<object>> {
    return this.http.post<ApiResponse<object>>(`${this.baseUrl}/delete-permanent`, { id });
  }

  history(id: number, page = 1, pageSize = 10): Observable<ApiResponse<PagedResult<AuditLog>>> {
    return this.http.post<ApiResponse<PagedResult<AuditLog>>>(`${this.baseUrl}/history`, { id, page, pageSize });
  }

  stockIn(payload: StockInPayload): Observable<ApiResponse<InventoryTransaction>> {
    return this.http.post<ApiResponse<InventoryTransaction>>(`${this.inventoryUrl}/stock-in`, payload);
  }

  inventoryHistory(productId: number, page = 1, pageSize = 10): Observable<ApiResponse<PagedResult<InventoryTransaction>>> {
    return this.http.post<ApiResponse<PagedResult<InventoryTransaction>>>(`${this.inventoryUrl}/history`, {
      productId,
      page,
      pageSize
    });
  }
}

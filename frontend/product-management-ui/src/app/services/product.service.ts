import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse, PagedResult } from '../models/api.model';
import { AuditLog } from '../models/audit-log.model';
import { Product, ProductCreatePayload, ProductSearchParams, ProductUpdatePayload } from '../models/product.model';

@Injectable({ providedIn: 'root' })
export class ProductService {
  private readonly baseUrl = `${environment.apiBaseUrl}/products`;

  constructor(private readonly http: HttpClient) {}

  search(params: ProductSearchParams): Observable<ApiResponse<PagedResult<Product>>> {
    let httpParams = new HttpParams()
      .set('page', params.page)
      .set('pageSize', params.pageSize)
      .set('sortBy', params.sortBy)
      .set('sortDirection', params.sortDirection)
      .set('stockStatus', params.stockStatus ?? 'All');

    if (params.keyword?.trim()) httpParams = httpParams.set('keyword', params.keyword.trim());
    if (params.categoryId) httpParams = httpParams.set('categoryId', params.categoryId);
    if (params.minPrice !== undefined) httpParams = httpParams.set('minPrice', params.minPrice);
    if (params.maxPrice !== undefined) httpParams = httpParams.set('maxPrice', params.maxPrice);
    if (params.isActive !== undefined) httpParams = httpParams.set('isActive', params.isActive);

    return this.http.get<ApiResponse<PagedResult<Product>>>(this.baseUrl, { params: httpParams });
  }

  getById(id: number): Observable<ApiResponse<Product>> {
    return this.http.get<ApiResponse<Product>>(`${this.baseUrl}/${id}`);
  }

  create(payload: ProductCreatePayload): Observable<ApiResponse<Product>> {
    return this.http.post<ApiResponse<Product>>(this.baseUrl, payload);
  }

  update(id: number, payload: ProductUpdatePayload): Observable<ApiResponse<Product>> {
    return this.http.put<ApiResponse<Product>>(`${this.baseUrl}/${id}`, payload);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  history(id: number, page = 1, pageSize = 10): Observable<ApiResponse<PagedResult<AuditLog>>> {
    return this.http.get<ApiResponse<PagedResult<AuditLog>>>(`${this.baseUrl}/${id}/history`, {
      params: new HttpParams().set('page', page).set('pageSize', pageSize)
    });
  }
}

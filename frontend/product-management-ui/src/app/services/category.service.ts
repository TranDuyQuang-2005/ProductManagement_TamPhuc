import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse, PagedResult } from '../models/api.model';
import { Category, CategoryOption, CategoryPayload, CategorySearchParams, ProductCodePreview } from '../models/category.model';

@Injectable({ providedIn: 'root' })
export class CategoryService {
  private readonly baseUrl = `${environment.apiBaseUrl}/categories`;

  constructor(private readonly http: HttpClient) {}

  search(params: CategorySearchParams): Observable<ApiResponse<PagedResult<Category>>> {
    let httpParams = new HttpParams()
      .set('page', params.page)
      .set('pageSize', params.pageSize)
      .set('sortBy', params.sortBy)
      .set('sortDirection', params.sortDirection);

    if (params.keyword?.trim()) httpParams = httpParams.set('keyword', params.keyword.trim());
    if (params.isActive !== undefined) httpParams = httpParams.set('isActive', params.isActive);

    return this.http.get<ApiResponse<PagedResult<Category>>>(this.baseUrl, { params: httpParams });
  }

  getById(id: number): Observable<ApiResponse<Category>> {
    return this.http.get<ApiResponse<Category>>(`${this.baseUrl}/${id}`);
  }

  getOptions(activeOnly = true): Observable<ApiResponse<CategoryOption[]>> {
    return this.http.get<ApiResponse<CategoryOption[]>>(`${this.baseUrl}/options`, {
      params: new HttpParams().set('activeOnly', activeOnly)
    });
  }

  getProductCodePreview(id: number): Observable<ApiResponse<ProductCodePreview>> {
    return this.http.get<ApiResponse<ProductCodePreview>>(`${this.baseUrl}/${id}/product-code-preview`);
  }

  create(payload: CategoryPayload): Observable<ApiResponse<Category>> {
    return this.http.post<ApiResponse<Category>>(this.baseUrl, payload);
  }

  update(id: number, payload: CategoryPayload): Observable<ApiResponse<Category>> {
    return this.http.put<ApiResponse<Category>>(`${this.baseUrl}/${id}`, payload);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}

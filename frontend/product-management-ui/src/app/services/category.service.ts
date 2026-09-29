import { HttpClient } from '@angular/common/http';
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
    return this.http.post<ApiResponse<PagedResult<Category>>>(`${this.baseUrl}/search`, params);
  }

  getById(id: number): Observable<ApiResponse<Category>> {
    return this.http.post<ApiResponse<Category>>(`${this.baseUrl}/get`, { id });
  }

  getOptions(activeOnly = true): Observable<ApiResponse<CategoryOption[]>> {
    return this.http.post<ApiResponse<CategoryOption[]>>(`${this.baseUrl}/options`, { activeOnly });
  }

  getProductCodePreview(id: number): Observable<ApiResponse<ProductCodePreview>> {
    return this.http.post<ApiResponse<ProductCodePreview>>(`${this.baseUrl}/product-code-preview`, { id });
  }

  create(payload: CategoryPayload): Observable<ApiResponse<Category>> {
    return this.http.post<ApiResponse<Category>>(`${this.baseUrl}/create`, payload);
  }

  update(id: number, payload: CategoryPayload): Observable<ApiResponse<Category>> {
    return this.http.post<ApiResponse<Category>>(`${this.baseUrl}/update`, { id, ...payload });
  }

  delete(id: number): Observable<ApiResponse<object>> {
    return this.http.post<ApiResponse<object>>(`${this.baseUrl}/delete`, { id });
  }
}

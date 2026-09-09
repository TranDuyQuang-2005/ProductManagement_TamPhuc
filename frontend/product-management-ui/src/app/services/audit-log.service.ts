import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse, PagedResult } from '../models/api.model';
import { AuditLog, AuditLogSearchParams } from '../models/audit-log.model';

@Injectable({ providedIn: 'root' })
export class AuditLogService {
  private readonly baseUrl = `${environment.apiBaseUrl}/audit-logs`;

  constructor(private readonly http: HttpClient) {}

  search(params: AuditLogSearchParams): Observable<ApiResponse<PagedResult<AuditLog>>> {
    let httpParams = new HttpParams()
      .set('page', params.page)
      .set('pageSize', params.pageSize)
      .set('sortBy', params.sortBy)
      .set('sortDirection', params.sortDirection);

    if (params.userId?.trim()) httpParams = httpParams.set('userId', params.userId.trim());
    if (params.username?.trim()) httpParams = httpParams.set('username', params.username.trim());
    if (params.action?.trim()) httpParams = httpParams.set('action', params.action.trim());
    if (params.entityType?.trim()) httpParams = httpParams.set('entityType', params.entityType.trim());
    if (params.entityId?.trim()) httpParams = httpParams.set('entityId', params.entityId.trim());
    if (params.entityCode?.trim()) httpParams = httpParams.set('entityCode', params.entityCode.trim());
    if (params.fromDate) httpParams = httpParams.set('fromDate', params.fromDate);
    if (params.toDate) httpParams = httpParams.set('toDate', params.toDate);

    return this.http.get<ApiResponse<PagedResult<AuditLog>>>(this.baseUrl, { params: httpParams });
  }

  getById(id: number): Observable<ApiResponse<AuditLog>> {
    return this.http.get<ApiResponse<AuditLog>>(`${this.baseUrl}/${id}`);
  }
}

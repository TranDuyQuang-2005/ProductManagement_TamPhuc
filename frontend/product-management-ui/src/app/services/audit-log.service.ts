import { HttpClient } from '@angular/common/http';
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
    return this.http.post<ApiResponse<PagedResult<AuditLog>>>(`${this.baseUrl}/search`, params);
  }

  getById(id: number): Observable<ApiResponse<AuditLog>> {
    return this.http.post<ApiResponse<AuditLog>>(`${this.baseUrl}/get`, { id });
  }
}

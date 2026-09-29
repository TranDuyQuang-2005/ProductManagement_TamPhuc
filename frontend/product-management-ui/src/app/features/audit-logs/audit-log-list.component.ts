import { CommonModule } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { getApiError } from '../../core/api-error.util';
import { AuditLog, AuditLogSearchParams } from '../../models/audit-log.model';
import { AuditLogService } from '../../services/audit-log.service';
import {
  AuditDetailView,
  getActionLabel as mapActionLabel,
  getAuditCodeLabel as mapAuditCodeLabel,
  getAuditDetailView,
  getEntityLabel as mapEntityLabel
} from '../../shared/audit-log-presenter';

@Component({
  selector: 'app-audit-log-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './audit-log-list.component.html'
})
export class AuditLogListComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  readonly pageSize = 20;
  readonly actions = [
    { value: '', label: 'Tất cả' },
    { value: 'CREATE', label: mapActionLabel('CREATE') },
    { value: 'UPDATE', label: mapActionLabel('UPDATE') },
    { value: 'DELETE', label: mapActionLabel('DELETE') },
    { value: 'RESTORE', label: mapActionLabel('RESTORE') },
    { value: 'PERMANENT_DELETE', label: mapActionLabel('PERMANENT_DELETE') },
    { value: 'LOGIN', label: mapActionLabel('LOGIN') },
    { value: 'LOGIN_FAILED', label: mapActionLabel('LOGIN_FAILED') }
  ];
  readonly entityTypes = [
    { value: '', label: 'Tất cả' },
    { value: 'PRODUCT', label: mapEntityLabel('PRODUCT') },
    { value: 'CATEGORY', label: mapEntityLabel('CATEGORY') },
    { value: 'AUTH', label: mapEntityLabel('AUTH') }
  ];
  logs: AuditLog[] = [];
  selectedLog: AuditLog | null = null;
  page = 1;
  totalPages = 0;
  totalItems = 0;
  loading = false;
  errorMessage = '';

  readonly filterForm = this.fb.group({
    fromDate: this.fb.control('', { nonNullable: true }),
    toDate: this.fb.control('', { nonNullable: true }),
    username: this.fb.control('', { nonNullable: true }),
    action: this.fb.control('', { nonNullable: true }),
    entityType: this.fb.control('', { nonNullable: true }),
    entityCode: this.fb.control('', { nonNullable: true }),
    sortDirection: this.fb.control<'asc' | 'desc'>('desc', { nonNullable: true })
  });

  constructor(private readonly auditLogService: AuditLogService) {}

  ngOnInit(): void {
    this.loadLogs();
  }

  search(): void {
    this.page = 1;
    this.loadLogs();
  }

  reset(): void {
    this.filterForm.reset({
      fromDate: '',
      toDate: '',
      username: '',
      action: '',
      entityType: '',
      entityCode: '',
      sortDirection: 'desc'
    });
    this.page = 1;
    this.loadLogs();
  }

  previousPage(): void {
    if (this.page > 1) {
      this.page--;
      this.loadLogs();
    }
  }

  nextPage(): void {
    if (this.page < this.totalPages) {
      this.page++;
      this.loadLogs();
    }
  }

  openDetail(log: AuditLog): void {
    this.selectedLog = log;
  }

  closeDetail(): void {
    this.selectedLog = null;
  }

  getActionLabel(action: string): string {
    return mapActionLabel(action);
  }

  getEntityLabel(entityType: string): string {
    return mapEntityLabel(entityType);
  }

  getAuditCodeLabel(entityType: string): string {
    return mapAuditCodeLabel(entityType);
  }

  detailView(log: AuditLog): AuditDetailView {
    return getAuditDetailView(log);
  }

  retryLoad(): void {
    this.loadLogs();
  }

  private loadLogs(): void {
    this.errorMessage = '';
    this.loading = true;
    const value = this.filterForm.getRawValue();
    const params: AuditLogSearchParams = {
      page: this.page,
      pageSize: this.pageSize,
      sortBy: 'createdAt',
      sortDirection: value.sortDirection
    };

    if (value.fromDate) params.fromDate = value.fromDate;
    if (value.toDate) params.toDate = value.toDate;
    if (value.username.trim()) params.username = value.username.trim();
    if (value.action) params.action = value.action;
    if (value.entityType) params.entityType = value.entityType;
    if (value.entityCode.trim()) params.entityCode = value.entityCode.trim();

    this.auditLogService.search(params)
      .pipe(finalize(() => this.loading = false))
      .subscribe({
        next: response => {
          this.logs = response.data.items;
          this.totalItems = response.data.totalItems;
          this.totalPages = response.data.totalPages;
          this.page = response.data.page;
        },
        error: error => this.errorMessage = getApiError(error).message || 'Không thể tải nhật ký hoạt động. Vui lòng thử lại.'
      });
  }
}

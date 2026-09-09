export interface AuditLog {
  id: number;
  userId: string | null;
  username: string;
  action: string;
  entityType: string;
  entityId: string | null;
  entityCode: string | null;
  oldValues: string | null;
  newValues: string | null;
  changedFields: string | null;
  ipAddress: string | null;
  description: string | null;
  createdAt: string;
}

export interface AuditLogSearchParams {
  page: number;
  pageSize: number;
  userId?: string;
  username?: string;
  action?: string;
  entityType?: string;
  entityId?: string;
  entityCode?: string;
  fromDate?: string;
  toDate?: string;
  sortBy: string;
  sortDirection: 'asc' | 'desc';
}

export interface AuditLogLike {
  username: string;
  action: string;
  entityType: string;
  entityCode: string | null;
  oldValues: string | null;
  newValues: string | null;
  changedFields: string | null;
  ipAddress: string | null;
  description: string | null;
}

export interface AuditChangeItem {
  label: string;
  oldValue?: string;
  newValue?: string;
  value?: string;
}

export interface AuditDetailView {
  title: string;
  mode: 'changes' | 'snapshot' | 'auth' | 'empty';
  items: AuditChangeItem[];
}

const ACTION_LABELS: Record<string, string> = {
  CREATE: 'Thêm mới',
  UPDATE: 'Cập nhật',
  DELETE: 'Xóa',
  LOGIN: 'Đăng nhập',
  LOGIN_FAILED: 'Đăng nhập thất bại'
};

const ENTITY_LABELS: Record<string, string> = {
  PRODUCT: 'Hàng hóa',
  CATEGORY: 'Danh mục',
  AUTH: 'Tài khoản'
};

const FIELD_LABELS: Record<string, string> = {
  productCode: 'Mã hàng hóa',
  productName: 'Tên hàng hóa',
  categoryId: 'Danh mục',
  unit: 'Đơn vị tính',
  price: 'Giá bán',
  quantity: 'Số lượng tồn',
  description: 'Mô tả',
  isActive: 'Trạng thái',
  categoryCode: 'Mã danh mục',
  categoryName: 'Tên danh mục',
  codePrefix: 'Ký hiệu',
  username: 'Tài khoản',
  ipAddress: 'Địa chỉ IP'
};

const HIDDEN_FIELDS = new Set([
  'id',
  'entityId',
  'userId',
  'createdAt',
  'updatedAt',
  'createdByUserId',
  'lastModifiedByUserId',
  'createdByUsername',
  'lastModifiedByUsername',
  'createdByRole',
  'lastModifiedByRole',
  'isAdminProtected',
  'canModify',
  'canEdit',
  'canDelete',
  'canEditPrefix',
  'hasProducts',
  'productCount',
  'nextProductNumber',
  'stockStatus',
  'password',
  'token',
  'accessToken',
  'refreshToken'
].map(normalizeField));

export function getActionLabel(action: string | null | undefined): string {
  if (!action) return '—';
  return ACTION_LABELS[action.toUpperCase()] ?? action;
}

export function getEntityLabel(entityType: string | null | undefined): string {
  if (!entityType) return '—';
  return ENTITY_LABELS[entityType.toUpperCase()] ?? entityType;
}

export function getFieldLabel(field: string, entityType?: string | null): string {
  const normalized = normalizeField(field);
  if (normalized === 'categoryName' && entityType?.toUpperCase() === 'PRODUCT') return 'Danh mục';
  return FIELD_LABELS[normalized] ?? field;
}

export function getAuditCodeLabel(entityType: string | null | undefined): string {
  const normalized = entityType?.toUpperCase();
  if (normalized === 'PRODUCT') return 'Mã hàng hóa';
  if (normalized === 'CATEGORY') return 'Mã danh mục';
  return 'Mã đối tượng';
}

export function getAuditDetailView(log: AuditLogLike): AuditDetailView {
  const action = log.action.toUpperCase();
  const entity = log.entityType.toUpperCase();

  if (entity === 'AUTH' || action === 'LOGIN' || action === 'LOGIN_FAILED') {
    const items = [
      { label: 'Tài khoản', value: formatEmpty(log.username) },
      { label: 'Địa chỉ IP', value: formatEmpty(log.ipAddress) }
    ];
    return {
      title: action === 'LOGIN_FAILED' ? 'Đăng nhập thất bại' : 'Đăng nhập hệ thống',
      mode: 'auth',
      items
    };
  }

  const oldSnapshot = parseRecord(log.oldValues);
  const newSnapshot = parseRecord(log.newValues);

  if (action === 'UPDATE') {
    return {
      title: 'Chi tiết thay đổi',
      mode: 'changes',
      items: getChangedFieldKeys(log, oldSnapshot, newSnapshot)
        .filter(field => !isHiddenField(field))
        .map(field => ({
          label: getFieldLabel(field, entity),
          oldValue: formatAuditValue(field, oldSnapshot?.[field] ?? oldSnapshot?.[toPascalCase(field)]),
          newValue: formatAuditValue(field, newSnapshot?.[field] ?? newSnapshot?.[toPascalCase(field)])
        }))
        .filter(item => item.oldValue !== item.newValue)
    };
  }

  if (action === 'CREATE') {
    return {
      title: `Thêm mới ${getEntityLabel(entity).toLowerCase()}`,
      mode: 'snapshot',
      items: snapshotItems(newSnapshot, entity)
    };
  }

  if (action === 'DELETE') {
    return {
      title: `Đã xóa ${getEntityLabel(entity).toLowerCase()}`,
      mode: 'snapshot',
      items: snapshotItems(oldSnapshot, entity)
    };
  }

  return {
    title: getActionLabel(action),
    mode: 'empty',
    items: []
  };
}

export function formatAuditValue(field: string, value: unknown): string {
  if (value === null || value === undefined || value === '') return '—';
  const normalized = normalizeField(field);

  if (normalized.toLowerCase().includes('password') || normalized.toLowerCase().includes('token')) {
    return '••••••';
  }

  if (normalized === 'isActive') {
    if (value === true || value === 'true' || value === 1 || value === '1') return 'Hoạt động';
    if (value === false || value === 'false' || value === 0 || value === '0') return 'Ngừng hoạt động';
  }

  if (normalized === 'price') {
    const numberValue = Number(value);
    if (Number.isFinite(numberValue)) return `${new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 2 }).format(numberValue)} VND`;
  }

  if (Array.isArray(value)) return value.map(item => formatEmpty(item)).join(', ');
  if (typeof value === 'object') return Object.entries(value as Record<string, unknown>)
    .filter(([key]) => !isHiddenField(key))
    .map(([key, itemValue]) => `${getFieldLabel(key)}: ${formatAuditValue(key, itemValue)}`)
    .join(', ') || '—';

  return String(value);
}

function snapshotItems(snapshot: Record<string, unknown> | null, entityType?: string | null): AuditChangeItem[] {
  if (!snapshot) return [];
  return Object.keys(snapshot)
    .filter(field => !isHiddenField(field))
    .map(field => ({
      label: getFieldLabel(field, entityType),
      value: formatAuditValue(field, snapshot[field])
    }));
}

function getChangedFieldKeys(
  log: AuditLogLike,
  oldSnapshot: Record<string, unknown> | null,
  newSnapshot: Record<string, unknown> | null
): string[] {
  const parsed = parseJson(log.changedFields);
  if (Array.isArray(parsed)) return parsed.map(field => normalizeField(String(field)));

  const keys = new Set<string>();
  Object.keys(oldSnapshot ?? {}).forEach(key => keys.add(normalizeField(key)));
  Object.keys(newSnapshot ?? {}).forEach(key => keys.add(normalizeField(key)));
  return Array.from(keys);
}

function parseRecord(value: string | null): Record<string, unknown> | null {
  const parsed = parseJson(value);
  return parsed && typeof parsed === 'object' && !Array.isArray(parsed)
    ? normalizeRecord(parsed as Record<string, unknown>)
    : null;
}

function normalizeRecord(record: Record<string, unknown>): Record<string, unknown> {
  return Object.fromEntries(Object.entries(record).map(([key, value]) => [normalizeField(key), value]));
}

function parseJson(value: string | null): unknown {
  if (!value) return null;
  try {
    return JSON.parse(value);
  } catch {
    return null;
  }
}

function normalizeField(field: string): string {
  return field ? field.charAt(0).toLowerCase() + field.slice(1) : field;
}

function toPascalCase(field: string): string {
  return field ? field.charAt(0).toUpperCase() + field.slice(1) : field;
}

function isHiddenField(field: string): boolean {
  const normalized = normalizeField(field);
  return HIDDEN_FIELDS.has(normalized)
    || normalized.toLowerCase().includes('password')
    || normalized.toLowerCase().includes('token');
}

function formatEmpty(value: unknown): string {
  return value === null || value === undefined || value === '' ? '—' : String(value);
}

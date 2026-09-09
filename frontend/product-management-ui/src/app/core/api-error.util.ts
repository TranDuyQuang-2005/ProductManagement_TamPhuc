import { HttpErrorResponse } from '@angular/common/http';
import { ApiErrorResponse } from '../models/api.model';

export function getApiError(error: unknown): ApiErrorResponse {
  if (error instanceof HttpErrorResponse && error.error && typeof error.error === 'object') {
    return error.error as ApiErrorResponse;
  }

  return {
    success: false,
    message: 'Không thể kết nối tới máy chủ. Vui lòng thử lại sau.'
  };
}

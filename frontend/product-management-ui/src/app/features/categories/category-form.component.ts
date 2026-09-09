import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, OnInit } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { getApiError } from '../../core/api-error.util';
import { CategoryPayload } from '../../models/category.model';
import { AuthService } from '../../services/auth.service';
import { CategoryService } from '../../services/category.service';
import { ToastService } from '../../shared/toast.service';

function notBlank(control: AbstractControl): ValidationErrors | null {
  const value = typeof control.value === 'string' ? control.value.trim() : control.value;
  return value === '' ? { blank: true } : null;
}

function codePrefix(control: AbstractControl): ValidationErrors | null {
  const value = typeof control.value === 'string' ? control.value.trim().toUpperCase() : '';
  return /^[A-Z0-9]{2,10}$/.test(value) ? null : { prefix: true };
}

@Component({
  selector: 'app-category-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './category-form.component.html'
})
export class CategoryFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  id: number | null = null;
  loading = false;
  saving = false;
  loadFailed = false;
  errorMessage = '';
  serverErrors: Record<string, string[]> = {};
  productCount = 0;
  canEditPrefix = true;

  readonly form = this.fb.group({
    categoryCode: this.fb.control('', [Validators.required, Validators.maxLength(50), notBlank]),
    categoryName: this.fb.control('', [Validators.required, Validators.maxLength(200), notBlank]),
    codePrefix: this.fb.control('', [Validators.required, Validators.minLength(2), Validators.maxLength(10), codePrefix]),
    description: this.fb.control('', [Validators.maxLength(500)]),
    isActive: this.fb.control(true, { nonNullable: true })
  });

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly categoryService: CategoryService,
    private readonly authService: AuthService,
    private readonly toastService: ToastService
  ) {}

  get isEdit(): boolean { return this.id !== null; }

  ngOnInit(): void {
    const rawId = this.route.snapshot.paramMap.get('id');
    this.id = rawId ? Number(rawId) : null;
    if (this.id !== null && (!Number.isInteger(this.id) || this.id <= 0)) {
      this.errorMessage = 'Id danh mục không hợp lệ.';
      this.loadFailed = true;
      return;
    }

    if (this.id === null) return;

    this.loading = true;
    this.categoryService.getById(this.id)
      .pipe(finalize(() => this.loading = false))
      .subscribe({
        next: response => {
          if (this.authService.isStaff && response.data.isAdminProtected) {
            this.errorMessage = 'Bạn không có quyền chỉnh sửa danh mục này.';
            this.toastService.error(this.errorMessage);
            this.loadFailed = true;
            return;
          }

          this.productCount = response.data.productCount;
          this.canEditPrefix = response.data.canEditPrefix;
          this.form.patchValue({
            categoryCode: response.data.categoryCode,
            categoryName: response.data.categoryName,
            codePrefix: response.data.codePrefix,
            description: response.data.description ?? '',
            isActive: response.data.isActive
          });
          if (!response.data.canEditPrefix) this.form.controls.codePrefix.disable();
        },
        error: () => {
          this.errorMessage = 'Không thể tải thông tin danh mục. Vui lòng thử lại.';
          this.loadFailed = true;
        }
      });
  }

  submit(): void {
    this.serverErrors = {};
    this.errorMessage = '';
    this.form.markAllAsTouched();
    if (this.form.invalid) return;

    const value = this.form.getRawValue();
    const payload: CategoryPayload = {
      categoryCode: value.categoryCode!.trim(),
      categoryName: value.categoryName!.trim(),
      codePrefix: value.codePrefix!.trim().toUpperCase(),
      description: value.description?.trim() || null,
      isActive: value.isActive
    };

    this.saving = true;
    const request = this.id === null
      ? this.categoryService.create(payload)
      : this.categoryService.update(this.id, payload);

    request.pipe(finalize(() => this.saving = false)).subscribe({
      next: () => void this.router.navigate(['/categories'], {
        state: { message: this.id === null ? 'Thêm danh mục thành công.' : 'Cập nhật danh mục thành công.' }
      }),
      error: error => {
        if (this.isForbidden(error)) {
          this.errorMessage = 'Bạn không có quyền chỉnh sửa danh mục này.';
          this.toastService.error(this.errorMessage);
          return;
        }

        const apiError = getApiError(error);
        this.errorMessage = apiError.message || 'Không thể lưu danh mục. Vui lòng thử lại.';
        this.serverErrors = apiError.errors ?? {};
      }
    });
  }

  fieldError(field: string): string {
    const server = this.serverErrors[field];
    if (server?.length) return server[0];
    const control = this.form.get(field);
    if (!control || !control.touched || !control.errors) return '';
    if (field === 'categoryCode' && (control.errors['required'] || control.errors['blank'])) return 'Vui lòng nhập mã danh mục.';
    if (field === 'categoryName' && (control.errors['required'] || control.errors['blank'])) return 'Vui lòng nhập tên danh mục.';
    if (field === 'codePrefix' && control.errors['required']) return 'Vui lòng nhập ký hiệu mã hàng.';
    if (control.errors['required'] || control.errors['blank']) return 'Trường này là bắt buộc.';
    if (control.errors['maxlength']) return `Tối đa ${control.errors['maxlength'].requiredLength} ký tự.`;
    if (control.errors['minlength']) return `Tối thiểu ${control.errors['minlength'].requiredLength} ký tự.`;
    if (control.errors['prefix']) return 'Chỉ chấp nhận chữ cái/số, từ 2 đến 10 ký tự.';
    return 'Giá trị không hợp lệ.';
  }

  private isForbidden(error: unknown): boolean {
    return error instanceof HttpErrorResponse && error.status === 403;
  }
}

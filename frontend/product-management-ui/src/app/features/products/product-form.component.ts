import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, OnInit } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';
import { getApiError } from '../../core/api-error.util';
import { CategoryOption } from '../../models/category.model';
import { ProductCreatePayload, ProductUpdatePayload } from '../../models/product.model';
import { AuthService } from '../../services/auth.service';
import { CategoryService } from '../../services/category.service';
import { ProductService } from '../../services/product.service';
import { ToastService } from '../../shared/toast.service';

function notBlank(control: AbstractControl): ValidationErrors | null {
  const value = typeof control.value === 'string' ? control.value.trim() : control.value;
  return value === '' ? { blank: true } : null;
}

const UI_MAX_AMOUNT = 90_000_000_000_000;

function maxTwoDecimals(control: AbstractControl): ValidationErrors | null {
  if (control.value === null || control.value === undefined || control.value === '') return null;
  const value = Number(control.value);
  if (!Number.isFinite(value)) return { number: true };
  return Math.abs(value * 100 - Math.round(value * 100)) > 1e-8 ? { decimals: true } : null;
}

@Component({
  selector: 'app-product-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './product-form.component.html'
})
export class ProductFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  readonly units = ['Kg', 'Hộp', 'Chai', 'Gói', 'Cái', 'Thùng', 'Túi'];
  categories: CategoryOption[] = [];
  serverErrors: Record<string, string[]> = {};
  errorMessage = '';
  loading = false;
  saving = false;
  loadFailed = false;
  previewLoading = false;
  id: number | null = null;
  originalCategoryId: number | null = null;
  private previewSequence = 0;

  readonly form = this.fb.group({
    productCode: this.fb.control({ value: 'Chọn danh mục', disabled: true }),
    productName: this.fb.control('', [Validators.required, Validators.maxLength(250), notBlank]),
    categoryId: this.fb.control<number | null>(null, [Validators.required, Validators.min(1)]),
    unit: this.fb.control('', [Validators.required, Validators.maxLength(50), notBlank]),
    price: this.fb.control<number | null>(null, [Validators.required, Validators.min(0), Validators.max(UI_MAX_AMOUNT), maxTwoDecimals]),
    description: this.fb.control('', [Validators.maxLength(1000)]),
    isActive: this.fb.control(true, { nonNullable: true })
  });

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly productService: ProductService,
    private readonly categoryService: CategoryService,
    private readonly authService: AuthService,
    private readonly toastService: ToastService
  ) {}

  get isEdit(): boolean { return this.id !== null; }

  ngOnInit(): void {
    const rawId = this.route.snapshot.paramMap.get('id');
    this.id = rawId ? Number(rawId) : null;
    if (this.id !== null && (!Number.isInteger(this.id) || this.id <= 0)) {
      this.errorMessage = 'Id hàng hóa không hợp lệ.';
      this.loadFailed = true;
      return;
    }

    this.loading = true;
    if (this.id !== null) {
      this.loadForEdit();
      return;
    }

    this.categoryService.getOptions(true)
      .pipe(finalize(() => this.loading = false))
      .subscribe({
        next: response => {
          this.categories = response.data;
          this.form.controls.categoryId.valueChanges.subscribe(categoryId => this.loadPreview(categoryId));
        },
        error: () => {
          this.errorMessage = 'Không thể tải danh mục. Vui lòng thử lại.';
          this.loadFailed = true;
        }
      });
  }

  isCategoryDisabled(item: CategoryOption): boolean {
    return !item.isActive && item.id !== this.originalCategoryId;
  }

  submit(): void {
    this.serverErrors = {};
    this.errorMessage = '';
    this.form.markAllAsTouched();
    if (this.form.invalid) return;

    const value = this.form.getRawValue();
    const createPayload: ProductCreatePayload = {
      productName: value.productName!.trim(),
      categoryId: value.categoryId!,
      unit: value.unit!.trim(),
      price: value.price!,
      description: value.description?.trim() || null,
      isActive: value.isActive
    };
    const updatePayload: ProductUpdatePayload = {
      productName: value.productName!.trim(),
      unit: value.unit!.trim(),
      price: value.price!,
      description: value.description?.trim() || null,
      isActive: value.isActive
    };

    this.saving = true;
    const request = this.id === null
      ? this.productService.create(createPayload)
      : this.productService.update(this.id, updatePayload);

    request.pipe(finalize(() => this.saving = false)).subscribe({
      next: response => void this.router.navigate(['/products'], {
        state: {
          message: this.id === null
            ? `Thêm hàng hóa thành công. Mã hàng hóa: ${response.data.productCode}.`
            : 'Cập nhật hàng hóa thành công.'
        }
      }),
      error: error => {
        if (this.isForbidden(error)) {
          this.errorMessage = 'Bạn không có quyền chỉnh sửa hàng hóa này.';
          this.toastService.error(this.errorMessage);
          return;
        }

        const apiError = getApiError(error);
        this.errorMessage = apiError.message || 'Không thể lưu hàng hóa. Vui lòng thử lại.';
        this.serverErrors = apiError.errors ?? {};
      }
    });
  }

  fieldError(field: string): string {
    const server = this.serverErrors[field];
    if (server?.length) return server[0];

    const control = this.form.get(field);
    if (!control || !control.touched || !control.errors) return '';
    if (field === 'productName' && (control.errors['required'] || control.errors['blank'])) return 'Vui lòng nhập tên hàng hóa.';
    if (field === 'categoryId' && control.errors['required']) return 'Vui lòng chọn danh mục.';
    if (field === 'unit' && (control.errors['required'] || control.errors['blank'])) return 'Đơn vị tính không được để trống.';
    if (field === 'price' && control.errors['min']) return 'Giá bán không được nhỏ hơn 0.';
    if (control.errors['required'] || control.errors['blank']) return 'Trường này là bắt buộc.';
    if (control.errors['maxlength']) return `Tối đa ${control.errors['maxlength'].requiredLength} ký tự.`;
    if (control.errors['max']) return 'Giá trị quá lớn.';
    if (control.errors['decimals']) return 'Chỉ được nhập tối đa 2 chữ số thập phân.';
    return 'Giá trị không hợp lệ.';
  }

  retryLoad(): void {
    this.loadFailed = false;
    this.ngOnInit();
  }

  private loadForEdit(): void {
    forkJoin({
      categories: this.categoryService.getOptions(false),
      product: this.productService.getById(this.id!)
    }).pipe(finalize(() => this.loading = false)).subscribe({
      next: ({ categories, product }) => {
        if (this.authService.isStaff && product.data.isAdminProtected) {
          this.errorMessage = 'Bạn không có quyền chỉnh sửa hàng hóa này.';
          this.toastService.error(this.errorMessage);
          this.loadFailed = true;
          return;
        }

        this.categories = categories.data;
        this.originalCategoryId = product.data.categoryId;
        this.form.patchValue({
          productCode: product.data.productCode,
          productName: product.data.productName,
          categoryId: product.data.categoryId,
          unit: product.data.unit,
          price: product.data.price,
          description: product.data.description ?? '',
          isActive: product.data.isActive
        });
        this.form.controls.categoryId.disable();
      },
      error: () => {
        this.errorMessage = 'Không thể tải thông tin hàng hóa. Vui lòng thử lại.';
        this.loadFailed = true;
      }
    });
  }

  private isForbidden(error: unknown): boolean {
    return error instanceof HttpErrorResponse && error.status === 403;
  }

  private loadPreview(categoryId: number | null): void {
    const sequence = ++this.previewSequence;
    if (!categoryId) {
      this.form.controls.productCode.setValue('Chọn danh mục');
      return;
    }

    this.previewLoading = true;
    this.form.controls.productCode.setValue('Đang tạo mã dự kiến...');
    this.categoryService.getProductCodePreview(categoryId)
      .pipe(finalize(() => {
        if (sequence === this.previewSequence) this.previewLoading = false;
      }))
      .subscribe({
        next: response => {
          if (sequence !== this.previewSequence) return;
          this.form.controls.productCode.setValue(response.data.productCodePreview);
        },
        error: () => {
          if (sequence !== this.previewSequence) return;
          this.form.controls.productCode.setValue('Không thể tạo mã dự kiến');
        }
      });
  }
}

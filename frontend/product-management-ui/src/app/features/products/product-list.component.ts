import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, OnInit } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { getApiError } from '../../core/api-error.util';
import { AuditLog } from '../../models/audit-log.model';
import { CategoryOption } from '../../models/category.model';
import { Product, ProductSearchParams, StockStatusFilter } from '../../models/product.model';
import { AuthService } from '../../services/auth.service';
import { CategoryService } from '../../services/category.service';
import { ProductService } from '../../services/product.service';
import { ConfirmDialogComponent } from '../../shared/confirm-dialog.component';
import {
  AuditDetailView,
  getActionLabel as mapActionLabel,
  getAuditDetailView
} from '../../shared/audit-log-presenter';
import { ToastService } from '../../shared/toast.service';

const UI_MAX_AMOUNT = 90_000_000_000_000;

function maxTwoDecimals(control: AbstractControl): ValidationErrors | null {
  if (control.value === null || control.value === undefined || control.value === '') return null;
  const value = Number(control.value);
  if (!Number.isFinite(value)) return { number: true };
  return Math.abs(value * 100 - Math.round(value * 100)) > 1e-8 ? { decimals: true } : null;
}

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, ConfirmDialogComponent],
  templateUrl: './product-list.component.html'
})
export class ProductListComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  readonly pageSize = 10;
  products: Product[] = [];
  categories: CategoryOption[] = [];
  page = 1;
  totalPages = 0;
  totalItems = 0;
  loading = false;
  deletingId: number | null = null;
  restoringId: number | null = null;
  permanentDeletingId: number | null = null;
  pendingDeleteProduct: Product | null = null;
  pendingPermanentDeleteProduct: Product | null = null;
  trashOpen = false;
  trashProducts: Product[] = [];
  trashPage = 1;
  trashTotalPages = 0;
  trashTotalItems = 0;
  trashLoading = false;
  historyProduct: Product | null = null;
  historyLogs: AuditLog[] = [];
  historyPage = 1;
  historyTotalPages = 0;
  historyLoading = false;
  stockInProduct: Product | null = null;
  stockInSaving = false;
  stockInServerErrors: Record<string, string[]> = {};
  message = '';
  errorMessage = '';

  readonly filterForm = this.fb.group({
    keyword: this.fb.control('', { nonNullable: true }),
    categoryId: this.fb.control<number | null>(null),
    stockStatus: this.fb.control<StockStatusFilter>('All', { nonNullable: true }),
    minPrice: this.fb.control<number | null>(null),
    maxPrice: this.fb.control<number | null>(null),
    isActive: this.fb.control<boolean | null>(null),
    sortBy: this.fb.control<string>('createdAt', { nonNullable: true }),
    sortDirection: this.fb.control<'asc' | 'desc'>('desc', { nonNullable: true })
  });

  readonly stockInForm = this.fb.group({
    quantity: this.fb.control<number | null>(null, [
      Validators.required,
      Validators.min(0.01),
      Validators.max(UI_MAX_AMOUNT),
      maxTwoDecimals
    ]),
    referenceCode: this.fb.control('', [Validators.maxLength(50)]),
    note: this.fb.control('', [Validators.maxLength(500)])
  });

  constructor(
    private readonly productService: ProductService,
    private readonly categoryService: CategoryService,
    public readonly authService: AuthService,
    private readonly toastService: ToastService
  ) {}

  ngOnInit(): void {
    this.categoryService.getOptions(false).subscribe({
      next: response => this.categories = response.data,
      error: () => this.errorMessage = 'Không thể tải danh mục. Vui lòng thử lại.'
    });
    this.loadProducts();
    const navigationMessage = window.history.state?.['message'];
    if (typeof navigationMessage === 'string') this.message = navigationMessage;
  }

  search(): void {
    const { minPrice, maxPrice } = this.filterForm.getRawValue();
    if ((minPrice !== null && minPrice < 0) || (maxPrice !== null && maxPrice < 0)) {
      this.errorMessage = 'Giá tìm kiếm không được âm.';
      return;
    }
    if (minPrice !== null && maxPrice !== null && minPrice > maxPrice) {
      this.errorMessage = 'Giá từ không được lớn hơn giá đến.';
      return;
    }

    this.page = 1;
    this.loadProducts();
  }

  reset(): void {
    this.filterForm.reset({
      keyword: '', categoryId: null, stockStatus: 'All', minPrice: null, maxPrice: null,
      isActive: null, sortBy: 'createdAt', sortDirection: 'desc'
    });
    this.page = 1;
    this.loadProducts();
  }

  previousPage(): void {
    if (this.page > 1) {
      this.page--;
      this.loadProducts();
    }
  }

  nextPage(): void {
    if (this.page < this.totalPages) {
      this.page++;
      this.loadProducts();
    }
  }

  canEditProduct(product: Product): boolean {
    if (typeof product.canEdit === 'boolean') return product.canEdit;
    if (typeof product.canModify === 'boolean') return product.canModify;
    return this.authService.isAdmin || !product.isAdminProtected;
  }

  canDeleteProduct(product: Product): boolean {
    if (typeof product.canDelete === 'boolean') return product.canDelete;
    if (typeof product.canModify === 'boolean') return product.canModify;
    return this.canEditProduct(product);
  }

  editTitle(product: Product): string {
    return this.canEditProduct(product) ? '' : 'Bạn không có quyền chỉnh sửa hàng hóa này.';
  }

  deleteTitle(product: Product): string {
    if (!this.canDeleteProduct(product)) return 'Bạn không có quyền xóa hàng hóa này.';
    if (product.stockQuantity > 0) return 'Không thể xóa hàng hóa khi vẫn còn tồn kho.';
    return '';
  }

  deleteProduct(product: Product): void {
    if (!this.canDeleteProduct(product)) {
      this.errorMessage = 'Bạn không có quyền xóa hàng hóa này.';
      this.toastService.error(this.errorMessage);
      return;
    }
    this.pendingDeleteProduct = product;
  }

  cancelDeleteProduct(): void {
    if (this.deletingId === null) this.pendingDeleteProduct = null;
  }

  openStockIn(product: Product): void {
    if (!this.canEditProduct(product)) {
      this.errorMessage = 'Bạn không có quyền nhập hàng cho hàng hóa này.';
      this.toastService.error(this.errorMessage);
      return;
    }

    this.stockInProduct = product;
    this.stockInServerErrors = {};
    this.stockInForm.reset({ quantity: null, referenceCode: '', note: '' });
  }

  closeStockIn(): void {
    if (this.stockInSaving) return;
    this.stockInProduct = null;
    this.stockInServerErrors = {};
  }

  submitStockIn(): void {
    if (!this.stockInProduct || this.stockInSaving) return;

    this.stockInServerErrors = {};
    this.stockInForm.markAllAsTouched();
    if (this.stockInForm.invalid) return;

    const value = this.stockInForm.getRawValue();
    this.stockInSaving = true;
    this.productService.stockIn({
      productId: this.stockInProduct.id,
      quantity: value.quantity!,
      referenceCode: value.referenceCode?.trim() || null,
      note: value.note?.trim() || null
    }).pipe(finalize(() => this.stockInSaving = false)).subscribe({
      next: () => {
        this.toastService.success('Nhập hàng thành công.');
        this.stockInProduct = null;
        this.loadProducts(false);
      },
      error: error => {
        const apiError = getApiError(error);
        this.stockInServerErrors = apiError.errors ?? {};
        this.errorMessage = apiError.message || 'Không thể nhập hàng. Vui lòng thử lại.';
        this.toastService.error(this.errorMessage);
      }
    });
  }

  stockInFieldError(field: string): string {
    const server = this.stockInServerErrors[field];
    if (server?.length) return server[0];

    const control = this.stockInForm.get(field);
    if (!control || !control.touched || !control.errors) return '';
    if (field === 'quantity' && (control.errors['required'] || control.errors['min'])) return 'Số lượng nhập phải lớn hơn 0.';
    if (control.errors['maxlength']) return `Tối đa ${control.errors['maxlength'].requiredLength} ký tự.`;
    if (control.errors['max']) return 'Giá trị quá lớn.';
    if (control.errors['decimals']) return 'Chỉ được nhập tối đa 2 chữ số thập phân.';
    return 'Giá trị không hợp lệ.';
  }

  confirmDeleteProduct(): void {
    const product = this.pendingDeleteProduct;
    if (!product || this.deletingId !== null) return;

    this.deletingId = product.id;
    this.message = '';
    this.errorMessage = '';
    this.productService.delete(product.id)
      .pipe(finalize(() => this.deletingId = null))
      .subscribe({
        next: () => {
          this.toastService.success('Đã chuyển hàng hóa vào danh sách đã xóa.');
          this.pendingDeleteProduct = null;
          if (this.products.length === 1 && this.page > 1) this.page--;
          this.loadProducts(false);
          if (this.trashOpen) this.loadTrash(false);
        },
        error: error => {
          const fallbackMessage = 'Không thể xóa hàng hóa. Vui lòng thử lại.';
          this.errorMessage = this.isForbidden(error)
            ? 'Bạn không có quyền xóa hàng hóa này.'
            : getApiError(error).message || fallbackMessage;
          this.toastService.error(this.errorMessage);
        }
      });
  }

  openHistory(product: Product): void {
    this.historyProduct = product;
    this.historyPage = 1;
    this.loadHistory();
  }

  openTrash(): void {
    if (!this.authService.isAdmin) return;
    this.trashOpen = true;
    this.trashPage = 1;
    this.loadTrash();
  }

  closeTrash(): void {
    if (this.trashLoading || this.restoringId !== null || this.permanentDeletingId !== null) return;
    this.trashOpen = false;
    this.trashProducts = [];
    this.pendingPermanentDeleteProduct = null;
  }

  previousTrashPage(): void {
    if (this.trashPage > 1) {
      this.trashPage--;
      this.loadTrash();
    }
  }

  nextTrashPage(): void {
    if (this.trashPage < this.trashTotalPages) {
      this.trashPage++;
      this.loadTrash();
    }
  }

  restoreProduct(product: Product): void {
    if (!this.authService.isAdmin || this.restoringId !== null || this.permanentDeletingId !== null) return;

    this.restoringId = product.id;
    this.errorMessage = '';
    this.productService.restore(product.id)
      .pipe(finalize(() => this.restoringId = null))
      .subscribe({
        next: () => {
          this.toastService.success('Khôi phục hàng hóa thành công.');
          if (this.trashProducts.length === 1 && this.trashPage > 1) this.trashPage--;
          this.loadTrash(false);
          this.loadProducts(false);
        },
        error: error => {
          this.errorMessage = this.isForbidden(error)
            ? 'Bạn không có quyền khôi phục hàng hóa này.'
            : getApiError(error).message || 'Không thể khôi phục hàng hóa. Vui lòng thử lại.';
          this.toastService.error(this.errorMessage);
        }
      });
  }

  deletePermanentProduct(product: Product): void {
    if (!this.authService.isAdmin) {
      this.errorMessage = 'Bạn không có quyền xóa vĩnh viễn hàng hóa này.';
      this.toastService.error(this.errorMessage);
      return;
    }

    this.pendingPermanentDeleteProduct = product;
  }

  cancelPermanentDeleteProduct(): void {
    if (this.permanentDeletingId === null) this.pendingPermanentDeleteProduct = null;
  }

  confirmPermanentDeleteProduct(): void {
    const product = this.pendingPermanentDeleteProduct;
    if (!product || this.permanentDeletingId !== null) return;

    this.permanentDeletingId = product.id;
    this.errorMessage = '';
    this.productService.deletePermanent(product.id)
      .pipe(finalize(() => this.permanentDeletingId = null))
      .subscribe({
        next: () => {
          this.toastService.success('Xóa vĩnh viễn hàng hóa thành công.');
          this.pendingPermanentDeleteProduct = null;
          if (this.trashProducts.length === 1 && this.trashPage > 1) this.trashPage--;
          this.loadTrash(false);
        },
        error: error => {
          this.errorMessage = this.isForbidden(error)
            ? 'Bạn không có quyền xóa vĩnh viễn hàng hóa này.'
            : getApiError(error).message || 'Không thể xóa vĩnh viễn hàng hóa. Vui lòng thử lại.';
          this.toastService.error(this.errorMessage);
        }
      });
  }

  closeHistory(): void {
    this.historyProduct = null;
    this.historyLogs = [];
  }

  previousHistoryPage(): void {
    if (this.historyPage > 1) {
      this.historyPage--;
      this.loadHistory();
    }
  }

  nextHistoryPage(): void {
    if (this.historyPage < this.historyTotalPages) {
      this.historyPage++;
      this.loadHistory();
    }
  }

  getActionLabel(action: string): string {
    return mapActionLabel(action);
  }

  detailView(log: AuditLog): AuditDetailView {
    return getAuditDetailView(log);
  }

  retryLoad(): void {
    this.loadProducts();
  }

  private loadProducts(clearMessage = true): void {
    if (clearMessage) this.message = '';
    this.errorMessage = '';
    this.loading = true;

    const params = this.buildSearchParams(this.page);

    this.productService.search(params)
      .pipe(finalize(() => this.loading = false))
      .subscribe({
        next: response => {
          this.products = response.data.items;
          this.totalItems = response.data.totalItems;
          this.totalPages = response.data.totalPages;
          this.page = response.data.page;
        },
        error: () => this.errorMessage = 'Không thể tải danh sách hàng hóa. Vui lòng thử lại.'
      });
  }

  private loadTrash(clearMessage = true): void {
    if (clearMessage) this.message = '';
    this.errorMessage = '';
    this.trashLoading = true;

    this.productService.searchTrash(this.buildSearchParams(this.trashPage))
      .pipe(finalize(() => this.trashLoading = false))
      .subscribe({
        next: response => {
          this.trashProducts = response.data.items;
          this.trashTotalItems = response.data.totalItems;
          this.trashTotalPages = response.data.totalPages;
          this.trashPage = response.data.page;
        },
        error: error => {
          this.errorMessage = this.isForbidden(error)
            ? 'Bạn không có quyền xem danh sách hàng hóa đã xóa.'
            : getApiError(error).message || 'Không thể tải danh sách hàng hóa đã xóa. Vui lòng thử lại.';
          this.toastService.error(this.errorMessage);
        }
      });
  }

  private buildSearchParams(page: number): ProductSearchParams {
    const value = this.filterForm.getRawValue();
    const params: ProductSearchParams = {
      page,
      pageSize: this.pageSize,
      sortBy: value.sortBy,
      sortDirection: value.sortDirection,
      stockStatus: value.stockStatus
    };

    if (value.keyword.trim()) params.keyword = value.keyword.trim();
    if (value.categoryId !== null) params.categoryId = value.categoryId;
    if (value.minPrice !== null) params.minPrice = value.minPrice;
    if (value.maxPrice !== null) params.maxPrice = value.maxPrice;
    if (value.isActive !== null) params.isActive = value.isActive;
    return params;
  }

  private isForbidden(error: unknown): boolean {
    return error instanceof HttpErrorResponse && error.status === 403;
  }

  private loadHistory(): void {
    if (!this.historyProduct) return;

    this.historyLoading = true;
    this.productService.history(this.historyProduct.id, this.historyPage, 10)
      .pipe(finalize(() => this.historyLoading = false))
      .subscribe({
        next: response => {
          this.historyLogs = response.data.items;
          this.historyTotalPages = response.data.totalPages;
          this.historyPage = response.data.page;
        },
        error: () => this.errorMessage = 'Không thể tải lịch sử hàng hóa. Vui lòng thử lại.'
      });
  }
}

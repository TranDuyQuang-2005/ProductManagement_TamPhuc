import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
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
  pendingDeleteProduct: Product | null = null;
  historyProduct: Product | null = null;
  historyLogs: AuditLog[] = [];
  historyPage = 1;
  historyTotalPages = 0;
  historyLoading = false;
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
          this.toastService.success('✓ Xóa hàng hóa thành công.');
          this.pendingDeleteProduct = null;
          if (this.products.length === 1 && this.page > 1) this.page--;
          this.loadProducts(false);
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

    const value = this.filterForm.getRawValue();
    const params: ProductSearchParams = {
      page: this.page,
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

import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { getApiError } from '../../core/api-error.util';
import { Category, CategorySearchParams } from '../../models/category.model';
import { AuthService } from '../../services/auth.service';
import { CategoryService } from '../../services/category.service';
import { ConfirmDialogComponent } from '../../shared/confirm-dialog.component';
import { ToastService } from '../../shared/toast.service';

@Component({
  selector: 'app-category-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, ConfirmDialogComponent],
  templateUrl: './category-list.component.html'
})
export class CategoryListComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  readonly pageSize = 10;
  categories: Category[] = [];
  page = 1;
  totalPages = 0;
  totalItems = 0;
  loading = false;
  deletingId: number | null = null;
  pendingDeleteCategory: Category | null = null;
  message = '';
  errorMessage = '';

  readonly filterForm = this.fb.group({
    keyword: this.fb.control('', { nonNullable: true }),
    isActive: this.fb.control<boolean | null>(null),
    sortBy: this.fb.control<string>('createdAt', { nonNullable: true }),
    sortDirection: this.fb.control<'asc' | 'desc'>('desc', { nonNullable: true })
  });

  constructor(
    private readonly categoryService: CategoryService,
    public readonly authService: AuthService,
    private readonly toastService: ToastService
  ) {}

  ngOnInit(): void {
    this.loadCategories();
    const navigationMessage = window.history.state?.['message'];
    if (typeof navigationMessage === 'string') this.message = navigationMessage;
  }

  search(): void {
    this.page = 1;
    this.loadCategories();
  }

  reset(): void {
    this.filterForm.reset({ keyword: '', isActive: null, sortBy: 'createdAt', sortDirection: 'desc' });
    this.page = 1;
    this.loadCategories();
  }

  previousPage(): void {
    if (this.page > 1) {
      this.page--;
      this.loadCategories();
    }
  }

  nextPage(): void {
    if (this.page < this.totalPages) {
      this.page++;
      this.loadCategories();
    }
  }

  canModifyCategory(category: Category): boolean {
    if (typeof category.canModify === 'boolean') return category.canModify;
    if (typeof category.canEdit === 'boolean') return category.canEdit;
    return this.authService.isAdmin || !category.isAdminProtected;
  }

  canDeleteCategory(category: Category): boolean {
    if (typeof category.canDelete === 'boolean') return category.canDelete;
    if (typeof category.canModify === 'boolean') return category.canModify;
    return this.canModifyCategory(category);
  }

  editTitle(category: Category): string {
    return this.canModifyCategory(category) ? '' : 'Bạn không có quyền chỉnh sửa danh mục này.';
  }

  deleteTitle(category: Category): string {
    if (!this.canDeleteCategory(category)) return 'Bạn không có quyền xóa danh mục này.';
    if (category.productCount > 0) return 'Danh mục đang có hàng hóa sẽ không được xóa.';
    return '';
  }

  deleteCategory(category: Category): void {
    if (!this.canDeleteCategory(category)) {
      this.errorMessage = 'Bạn không có quyền xóa danh mục này.';
      this.toastService.error(this.errorMessage);
      return;
    }

    if (category.productCount > 0) {
      this.errorMessage = `Không thể xóa "${category.categoryName}" vì đang có ${category.productCount} hàng hóa sử dụng.`;
      return;
    }

    this.pendingDeleteCategory = category;
  }

  cancelDeleteCategory(): void {
    if (this.deletingId === null) this.pendingDeleteCategory = null;
  }

  confirmDeleteCategory(): void {
    const category = this.pendingDeleteCategory;
    if (!category || this.deletingId !== null) return;

    this.deletingId = category.id;
    this.message = '';
    this.errorMessage = '';
    this.categoryService.delete(category.id)
      .pipe(finalize(() => this.deletingId = null))
      .subscribe({
        next: () => {
          this.toastService.success('✓ Xóa danh mục thành công.');
          this.pendingDeleteCategory = null;
          if (this.categories.length === 1 && this.page > 1) this.page--;
          this.loadCategories(false);
        },
        error: error => {
          const fallbackMessage = 'Không thể xóa danh mục. Vui lòng thử lại.';
          this.errorMessage = this.isForbidden(error)
            ? 'Bạn không có quyền xóa danh mục này.'
            : getApiError(error).message || fallbackMessage;
          this.toastService.error(this.errorMessage);
        }
      });
  }

  retryLoad(): void {
    this.loadCategories();
  }

  private loadCategories(clearMessage = true): void {
    if (clearMessage) this.message = '';
    this.errorMessage = '';
    this.loading = true;
    const value = this.filterForm.getRawValue();
    const params: CategorySearchParams = {
      page: this.page,
      pageSize: this.pageSize,
      sortBy: value.sortBy,
      sortDirection: value.sortDirection
    };
    if (value.keyword.trim()) params.keyword = value.keyword.trim();
    if (value.isActive !== null) params.isActive = value.isActive;

    this.categoryService.search(params)
      .pipe(finalize(() => this.loading = false))
      .subscribe({
        next: response => {
          this.categories = response.data.items;
          this.totalItems = response.data.totalItems;
          this.totalPages = response.data.totalPages;
          this.page = response.data.page;
        },
        error: () => this.errorMessage = 'Không thể tải danh sách danh mục. Vui lòng thử lại.'
      });
  }

  private isForbidden(error: unknown): boolean {
    return error instanceof HttpErrorResponse && error.status === 403;
  }
}

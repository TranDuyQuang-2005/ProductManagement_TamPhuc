import { CommonModule } from '@angular/common';
import { Component, EventEmitter, HostListener, Input, Output } from '@angular/core';

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="modal-backdrop" *ngIf="open" (click)="closeFromBackdrop()">
      <section class="card modal confirm-dialog" role="dialog" aria-modal="true" (click)="$event.stopPropagation()">
        <div class="modal-header">
          <h2>{{ title }}</h2>
        </div>
        <p class="confirm-message">{{ message }}</p>
        <p class="confirm-target" *ngIf="target">{{ target }}</p>
        <p class="confirm-warning" *ngIf="warning">{{ warning }}</p>
        <div class="form-actions">
          <button data-testid="confirm-cancel" class="btn" type="button" [disabled]="loading" (click)="cancel.emit()">{{ cancelLabel }}</button>
          <button data-testid="confirm-accept" class="btn danger solid" type="button" [disabled]="loading" (click)="confirm.emit()">
            {{ loading ? loadingLabel : confirmLabel }}
          </button>
        </div>
      </section>
    </div>
  `
})
export class ConfirmDialogComponent {
  @Input() open = false;
  @Input() title = 'Xác nhận';
  @Input() message = '';
  @Input() target = '';
  @Input() warning = '';
  @Input() confirmLabel = 'Đồng ý';
  @Input() cancelLabel = 'Hủy';
  @Input() loadingLabel = 'Đang xử lý...';
  @Input() loading = false;
  @Input() closeOnBackdrop = false;
  @Output() cancel = new EventEmitter<void>();
  @Output() confirm = new EventEmitter<void>();

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.open && !this.loading) this.cancel.emit();
  }

  closeFromBackdrop(): void {
    if (this.closeOnBackdrop && !this.loading) this.cancel.emit();
  }
}

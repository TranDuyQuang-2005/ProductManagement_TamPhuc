import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { ToastService } from './toast.service';

@Component({
  selector: 'app-toast-container',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="toast-stack" aria-live="polite">
      <button
        class="toast"
        *ngFor="let toast of toastService.messages$ | async"
        [class.success]="toast.type === 'success'"
        [class.error]="toast.type === 'error'"
        [class.info]="toast.type === 'info'"
        type="button"
        (click)="toastService.dismiss(toast.id)">
        {{ toast.message }}
      </button>
    </div>
  `
})
export class ToastContainerComponent {
  constructor(public readonly toastService: ToastService) {}
}

import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from './services/auth.service';
import { ToastContainerComponent } from './shared/toast-container.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive, ToastContainerComponent],
  template: `
    <header class="topbar" *ngIf="authService.authState$ | async as auth">
      <div class="brand">Product Management</div>
      <nav>
        <a data-testid="nav-products" routerLink="/products" routerLinkActive="active">Hàng hóa</a>
        <a data-testid="nav-categories" routerLink="/categories" routerLinkActive="active">Danh mục</a>
        <a data-testid="nav-audit" routerLink="/audit-logs" routerLinkActive="active">Nhật ký</a>
      </nav>
      <div class="user-box">
        <div>
          <strong>{{ auth.user.fullName }}</strong>
          <span>{{ auth.user.role }}</span>
        </div>
        <button class="btn small" type="button" (click)="logout()">Đăng xuất</button>
      </div>
    </header>
    <main class="container">
      <router-outlet />
    </main>
    <app-toast-container />
  `
})
export class AppComponent {
  constructor(
    public readonly authService: AuthService,
    private readonly router: Router
  ) {}

  logout(): void {
    this.authService.logout();
    void this.router.navigate(['/login']);
  }
}

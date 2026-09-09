import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { getApiError } from '../../core/api-error.util';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './login.component.html'
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  loading = false;
  errorMessage = '';

  readonly form = this.fb.group({
    username: this.fb.control('', { nonNullable: true, validators: [Validators.required] }),
    password: this.fb.control('', { nonNullable: true, validators: [Validators.required] })
  });

  constructor(
    private readonly authService: AuthService,
    private readonly router: Router
  ) {}

  submit(): void {
    this.errorMessage = '';
    this.form.markAllAsTouched();
    if (this.form.invalid) return;

    this.loading = true;
    const value = this.form.getRawValue();
    this.authService.login({
      username: value.username.trim(),
      password: value.password
    }).pipe(finalize(() => this.loading = false)).subscribe({
      next: () => void this.router.navigate(['/products']),
      error: error => this.errorMessage = getApiError(error).message || 'Không thể đăng nhập. Vui lòng thử lại.'
    });
  }

  fieldError(field: 'username' | 'password'): string {
    const control = this.form.controls[field];
    if (!control.touched || !control.errors) return '';
    if (field === 'username' && control.errors['required']) return 'Vui lòng nhập tên đăng nhập.';
    if (field === 'password' && control.errors['required']) return 'Vui lòng nhập mật khẩu.';
    return 'Giá trị không hợp lệ.';
  }
}

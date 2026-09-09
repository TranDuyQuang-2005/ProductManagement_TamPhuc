import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/api.model';
import { AuthState, LoginRequest, LoginResponse } from '../models/auth.model';

const AUTH_STORAGE_KEY = 'product_management_auth';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly baseUrl = `${environment.apiBaseUrl}/auth`;
  private readonly stateSubject = new BehaviorSubject<AuthState | null>(this.readState());
  readonly authState$ = this.stateSubject.asObservable();

  constructor(private readonly http: HttpClient) {}

  get state(): AuthState | null {
    const current = this.stateSubject.value;
    if (current && new Date(current.expiresAt).getTime() <= Date.now()) {
      this.logout();
      return null;
    }
    return current;
  }

  get token(): string | null {
    return this.state?.accessToken ?? null;
  }

  get isAuthenticated(): boolean {
    return this.state !== null;
  }

  get role(): string {
    return this.state?.user.role?.toUpperCase() ?? '';
  }

  get isAdmin(): boolean {
    return this.role === 'ADMIN';
  }

  get isStaff(): boolean {
    return this.role === 'STAFF';
  }

  login(payload: LoginRequest): Observable<ApiResponse<LoginResponse>> {
    return this.http.post<ApiResponse<LoginResponse>>(`${this.baseUrl}/login`, payload).pipe(
      tap(response => {
        if (!response.data) return;
        const state: AuthState = {
          accessToken: response.data.accessToken,
          expiresAt: response.data.expiresAt,
          user: response.data.user
        };
        localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(state));
        this.stateSubject.next(state);
      })
    );
  }

  logout(): void {
    localStorage.removeItem(AUTH_STORAGE_KEY);
    this.stateSubject.next(null);
  }

  private readState(): AuthState | null {
    const raw = localStorage.getItem(AUTH_STORAGE_KEY);
    if (!raw) return null;

    try {
      const parsed = JSON.parse(raw) as AuthState;
      if (!parsed.accessToken || !parsed.expiresAt || new Date(parsed.expiresAt).getTime() <= Date.now()) {
        localStorage.removeItem(AUTH_STORAGE_KEY);
        return null;
      }
      return parsed;
    } catch {
      localStorage.removeItem(AUTH_STORAGE_KEY);
      return null;
    }
  }
}

export interface AuthUser {
  id: string;
  username: string;
  fullName: string;
  role: string;
}

export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  expiresAt: string;
  user: AuthUser;
}

export interface AuthState {
  accessToken: string;
  expiresAt: string;
  user: AuthUser;
}

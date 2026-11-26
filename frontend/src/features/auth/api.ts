import { http } from '@/lib/http'
import type {
  AuthResponse,
  ChangePasswordRequest,
  LoginRequest,
  RegisterRequest,
  ResetPasswordRequest,
  User,
  VerifyCodeRequest,
} from './types'

export const authApi = {
  register: (request: RegisterRequest) => http.post<void>('/auth/register', request),
  verifyEmail: (request: VerifyCodeRequest) => http.post<AuthResponse>('/auth/verify-email', request),
  resendVerification: (email: string) => http.post<void>('/auth/resend-verification', { email }),
  login: (request: LoginRequest) => http.post<AuthResponse>('/auth/login', request),
  refresh: () => http.post<AuthResponse>('/auth/refresh'),
  logout: () => http.post<void>('/auth/logout'),
  forgotPassword: (email: string) => http.post<void>('/auth/forgot-password', { email }),
  verifyResetCode: (request: VerifyCodeRequest) =>
    http.post<{ resetToken: string }>('/auth/verify-reset-code', request),
  resetPassword: (request: ResetPasswordRequest) => http.post<void>('/auth/reset-password', request),
  changePassword: (request: ChangePasswordRequest) => http.post<void>('/auth/change-password', request),
  me: () => http.get<User>('/auth/me'),
}

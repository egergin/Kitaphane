export type User = {
  id: string
  name: string
  email: string
  profileImage: string | null
}

export type AuthResponse = {
  accessToken: string
  expiresAt: string
  user: User
}

export type RegisterRequest = { name: string; email: string; password: string }
export type LoginRequest = { email: string; password: string; rememberMe: boolean }
export type VerifyCodeRequest = { email: string; code: string }
export type ResetPasswordRequest = { email: string; resetToken: string; newPassword: string }
export type ChangePasswordRequest = { currentPassword: string; newPassword: string }

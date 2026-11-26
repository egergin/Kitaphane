import type { User } from '@/features/auth/types'
import { http } from '@/lib/http'

export type UpdateProfileRequest = { name: string; profileImage: string | null }

export const profileApi = {
  update: (request: UpdateProfileRequest) => http.put<User>('/profile', request),
}

import { z } from 'zod'
import { passwordSchema } from '@/features/auth/schemas'

function isHttpUrl(value: string) {
  try {
    const url = new URL(value)
    return url.protocol === 'http:' || url.protocol === 'https:'
  } catch {
    return false
  }
}

/** Mirrors the API rules: name required (max 100), image empty or an http(s) URL (max 2048). */
export const profileSchema = z.object({
  name: z.string().trim().min(1, 'auth.validation.required').max(100, 'auth.validation.nameMax'),
  profileImage: z
    .string()
    .trim()
    .max(2048, 'profile.validation.imageUrl')
    .refine((value) => value === '' || isHttpUrl(value), 'profile.validation.imageUrl'),
})

export const changePasswordSchema = z
  .object({
    currentPassword: z.string().min(1, 'auth.validation.required'),
    newPassword: passwordSchema,
    confirmPassword: z.string().min(1, 'auth.validation.required'),
  })
  .refine((values) => values.newPassword === values.confirmPassword, {
    path: ['confirmPassword'],
    message: 'auth.validation.passwordMismatch',
  })

export type ProfileValues = z.infer<typeof profileSchema>
export type ChangePasswordValues = z.infer<typeof changePasswordSchema>

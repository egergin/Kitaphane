import { z } from 'zod'

/**
 * Client-side validation mirrors the API rules (docs/api/api-design.md).
 * Messages are i18n keys, translated when rendered.
 */
export const emailSchema = z
  .string()
  .trim()
  .min(1, 'auth.validation.required')
  .max(256, 'auth.validation.email')
  .pipe(z.email('auth.validation.email'))

export const passwordSchema = z
  .string()
  .min(8, 'auth.validation.passwordMin')
  .max(128, 'auth.validation.passwordMin')
  .regex(/\p{L}/u, 'auth.validation.passwordLetter')
  .regex(/\d/, 'auth.validation.passwordDigit')

export const codeSchema = z
  .string()
  .trim()
  .regex(/^\d{6}$/, 'auth.validation.code')

export const loginSchema = z.object({
  email: emailSchema,
  password: z.string().min(1, 'auth.validation.required'),
  rememberMe: z.boolean(),
})

export const registerSchema = z
  .object({
    name: z.string().trim().min(1, 'auth.validation.required').max(100, 'auth.validation.nameMax'),
    email: emailSchema,
    password: passwordSchema,
    confirmPassword: z.string().min(1, 'auth.validation.required'),
  })
  .refine((values) => values.password === values.confirmPassword, {
    path: ['confirmPassword'],
    message: 'auth.validation.passwordMismatch',
  })

export const forgotPasswordSchema = z.object({ email: emailSchema })

export const codeFormSchema = z.object({ code: codeSchema })

export const resetPasswordSchema = z
  .object({
    password: passwordSchema,
    confirmPassword: z.string().min(1, 'auth.validation.required'),
  })
  .refine((values) => values.password === values.confirmPassword, {
    path: ['confirmPassword'],
    message: 'auth.validation.passwordMismatch',
  })

export type LoginValues = z.infer<typeof loginSchema>
export type RegisterValues = z.infer<typeof registerSchema>
export type ForgotPasswordValues = z.infer<typeof forgotPasswordSchema>
export type CodeFormValues = z.infer<typeof codeFormSchema>
export type ResetPasswordValues = z.infer<typeof resetPasswordSchema>

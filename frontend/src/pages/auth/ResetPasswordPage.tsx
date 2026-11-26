import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { Navigate, useLocation, useNavigate } from 'react-router'
import { toast } from 'sonner'
import { FormAlert } from '@/components/form/FormAlert'
import { PasswordField } from '@/components/form/PasswordField'
import { SubmitButton } from '@/components/form/SubmitButton'
import { useFormErrors } from '@/components/form/useFormErrors'
import { authApi } from '@/features/auth/api'
import { resetPasswordSchema, type ResetPasswordValues } from '@/features/auth/schemas'
import { AuthHeading } from './AuthHeading'

type ResetState = { email?: string; resetToken?: string } | null

export function ResetPasswordPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const state = useLocation().state as ResetState

  const form = useForm<ResetPasswordValues>({
    resolver: zodResolver(resetPasswordSchema),
    defaultValues: { password: '', confirmPassword: '' },
  })
  const { formError, fieldError, handleError, setFormError } = useFormErrors(form.setError, ['password'])
  const { errors, isSubmitting } = form.formState

  // The reset token comes from the code verification step; opened directly, start over.
  if (!state?.email || !state.resetToken) return <Navigate to="/forgot-password" replace />
  const { email, resetToken } = state

  const onSubmit = form.handleSubmit(async ({ password }) => {
    setFormError(null)
    try {
      await authApi.resetPassword({ email, resetToken, newPassword: password })
      toast.success(t('auth.reset.success'))
      navigate('/login', { replace: true, state: { email } })
    } catch (error) {
      handleError(error)
    }
  })

  return (
    <form onSubmit={onSubmit} noValidate className="flex flex-col gap-5">
      <AuthHeading title={t('auth.reset.title')} subtitle={t('auth.reset.subtitle')} />
      <FormAlert message={formError} />

      <PasswordField
        label={t('auth.password')}
        autoComplete="new-password"
        error={fieldError(errors.password?.message)}
        {...form.register('password')}
      />
      <PasswordField
        label={t('auth.confirmPassword')}
        autoComplete="new-password"
        error={fieldError(errors.confirmPassword?.message)}
        {...form.register('confirmPassword')}
      />

      <SubmitButton loading={isSubmitting}>{t('auth.reset.submit')}</SubmitButton>
    </form>
  )
}

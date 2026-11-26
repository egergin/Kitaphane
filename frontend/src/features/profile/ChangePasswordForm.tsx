import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { FormAlert } from '@/components/form/FormAlert'
import { PasswordField } from '@/components/form/PasswordField'
import { SubmitButton } from '@/components/form/SubmitButton'
import { useFormErrors } from '@/components/form/useFormErrors'
import { authApi } from '@/features/auth/api'
import { ApiError } from '@/lib/http'
import { changePasswordSchema, type ChangePasswordValues } from './schemas'
import { SettingsCard } from './SettingsCard'

const EMPTY: ChangePasswordValues = { currentPassword: '', newPassword: '', confirmPassword: '' }

/** Password change with the current password. Other sessions are signed out by the API. */
export function ChangePasswordForm() {
  const { t } = useTranslation()
  const form = useForm<ChangePasswordValues>({ resolver: zodResolver(changePasswordSchema), defaultValues: EMPTY })
  const { formError, fieldError, handleError, setFormError } = useFormErrors(form.setError, ['newPassword'])
  const { errors, isSubmitting } = form.formState

  const onSubmit = form.handleSubmit(async ({ currentPassword, newPassword }) => {
    setFormError(null)
    try {
      await authApi.changePassword({ currentPassword, newPassword })
      form.reset(EMPTY)
      toast.success(t('profile.password.changed'))
    } catch (error) {
      if (error instanceof ApiError && error.code === 'INVALID_CURRENT_PASSWORD') {
        form.setError('currentPassword', { message: t('errors.INVALID_CURRENT_PASSWORD') })
        return
      }
      if (error instanceof ApiError && error.code === 'PASSWORD_SAME_AS_CURRENT') {
        form.setError('newPassword', { message: t('errors.PASSWORD_SAME_AS_CURRENT') })
        return
      }
      handleError(error)
    }
  })

  return (
    <SettingsCard title={t('profile.password.title')} description={t('profile.password.description')}>
      <form onSubmit={onSubmit} noValidate className="flex flex-col gap-5">
        <FormAlert message={formError} />
        <PasswordField
          label={t('profile.password.current')}
          autoComplete="current-password"
          error={fieldError(errors.currentPassword?.message)}
          {...form.register('currentPassword')}
        />
        <PasswordField
          label={t('profile.password.new')}
          autoComplete="new-password"
          error={fieldError(errors.newPassword?.message)}
          {...form.register('newPassword')}
        />
        <PasswordField
          label={t('profile.password.confirm')}
          autoComplete="new-password"
          error={fieldError(errors.confirmPassword?.message)}
          {...form.register('confirmPassword')}
        />
        <div className="sm:ml-auto sm:w-44">
          <SubmitButton size="md" loading={isSubmitting}>
            {t('profile.password.submit')}
          </SubmitButton>
        </div>
      </form>
    </SettingsCard>
  )
}

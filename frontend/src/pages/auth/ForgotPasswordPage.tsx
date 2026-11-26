import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate } from 'react-router'
import { FormAlert } from '@/components/form/FormAlert'
import { OutlinedField } from '@/components/form/OutlinedField'
import { SubmitButton } from '@/components/form/SubmitButton'
import { useFormErrors } from '@/components/form/useFormErrors'
import { authApi } from '@/features/auth/api'
import { forgotPasswordSchema, type ForgotPasswordValues } from '@/features/auth/schemas'
import { AuthHeading } from './AuthHeading'

export function ForgotPasswordPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()

  const form = useForm<ForgotPasswordValues>({
    resolver: zodResolver(forgotPasswordSchema),
    defaultValues: { email: '' },
  })
  const { formError, fieldError, handleError, setFormError } = useFormErrors(form.setError, ['email'])
  const { errors, isSubmitting } = form.formState

  const onSubmit = form.handleSubmit(async ({ email }) => {
    setFormError(null)
    try {
      await authApi.forgotPassword(email)
      navigate('/forgot-password/verify', { state: { email } })
    } catch (error) {
      handleError(error)
    }
  })

  return (
    <form onSubmit={onSubmit} noValidate className="flex flex-col gap-5">
      <AuthHeading title={t('auth.forgot.title')} subtitle={t('auth.forgot.subtitle')} />
      <FormAlert message={formError} />

      <OutlinedField
        label={t('auth.email')}
        type="email"
        autoComplete="email"
        error={fieldError(errors.email?.message)}
        {...form.register('email')}
      />

      <SubmitButton loading={isSubmitting}>{t('auth.forgot.submit')}</SubmitButton>

      <Link to="/login" className="self-center text-base text-primary underline">
        {t('auth.forgot.backToLogin')}
      </Link>
    </form>
  )
}

import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate } from 'react-router'
import { FormAlert } from '@/components/form/FormAlert'
import { OutlinedField } from '@/components/form/OutlinedField'
import { PasswordField } from '@/components/form/PasswordField'
import { SubmitButton } from '@/components/form/SubmitButton'
import { useFormErrors } from '@/components/form/useFormErrors'
import { authApi } from '@/features/auth/api'
import { registerSchema, type RegisterValues } from '@/features/auth/schemas'
import { AuthHeading } from './AuthHeading'

export function RegisterPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()

  const form = useForm<RegisterValues>({
    resolver: zodResolver(registerSchema),
    defaultValues: { name: '', email: '', password: '', confirmPassword: '' },
  })
  const { formError, fieldError, handleError, setFormError } = useFormErrors(form.setError, [
    'name',
    'email',
    'password',
  ])
  const { errors, isSubmitting } = form.formState

  const onSubmit = form.handleSubmit(async ({ name, email, password }) => {
    setFormError(null)
    try {
      await authApi.register({ name, email, password })
      navigate('/verify-email', { state: { email } })
    } catch (error) {
      handleError(error)
    }
  })

  return (
    <form onSubmit={onSubmit} noValidate className="flex flex-col gap-5">
      <AuthHeading title={t('auth.register.title')} subtitle={t('auth.register.subtitle')} />
      <FormAlert message={formError} />

      <OutlinedField
        label={t('auth.name')}
        autoComplete="name"
        error={fieldError(errors.name?.message)}
        {...form.register('name')}
      />
      <OutlinedField
        label={t('auth.email')}
        type="email"
        autoComplete="email"
        error={fieldError(errors.email?.message)}
        {...form.register('email')}
      />
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

      <SubmitButton loading={isSubmitting}>{t('auth.register.submit')}</SubmitButton>

      <p className="text-center text-base">
        {t('auth.register.haveAccount')}{' '}
        <Link to="/login" className="text-primary underline">
          {t('auth.register.loginLink')}
        </Link>
      </p>
    </form>
  )
}

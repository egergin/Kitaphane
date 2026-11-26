import { zodResolver } from '@hookform/resolvers/zod'
import { Controller, useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { Link, type Location, useLocation, useNavigate } from 'react-router'
import { FormAlert } from '@/components/form/FormAlert'
import { OutlinedField } from '@/components/form/OutlinedField'
import { PasswordField } from '@/components/form/PasswordField'
import { SubmitButton } from '@/components/form/SubmitButton'
import { useFormErrors } from '@/components/form/useFormErrors'
import { Checkbox } from '@/components/ui/checkbox'
import { authApi } from '@/features/auth/api'
import { useAuth } from '@/features/auth/AuthContext'
import { loginSchema, type LoginValues } from '@/features/auth/schemas'
import { AuthHeading } from './AuthHeading'

type LoginLocationState = { from?: Location; email?: string } | null

export function LoginPage() {
  const { t } = useTranslation()
  const { signIn } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const state = location.state as LoginLocationState

  const form = useForm<LoginValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: state?.email ?? '', password: '', rememberMe: true },
  })
  const { formError, fieldError, handleError, setFormError } = useFormErrors(form.setError, ['email', 'password'])
  const { errors, isSubmitting } = form.formState

  const onSubmit = form.handleSubmit(async (values) => {
    setFormError(null)
    try {
      signIn(await authApi.login(values))
      const from = state?.from
      navigate(from ? `${from.pathname}${from.search}` : '/', { replace: true })
    } catch (error) {
      handleError(error)
    }
  })

  return (
    <form onSubmit={onSubmit} noValidate className="flex flex-col gap-5">
      <AuthHeading title={t('auth.login.title')} showBack={false} />
      <FormAlert message={formError} />

      <OutlinedField
        label={t('auth.email')}
        type="email"
        autoComplete="email"
        error={fieldError(errors.email?.message)}
        {...form.register('email')}
      />
      <PasswordField
        label={t('auth.password')}
        autoComplete="current-password"
        error={fieldError(errors.password?.message)}
        {...form.register('password')}
      />

      <div className="flex items-center justify-between gap-4 text-sm">
        <Controller
          control={form.control}
          name="rememberMe"
          render={({ field }) => (
            <label htmlFor="remember-me" className="flex cursor-pointer items-center gap-1.5">
              <Checkbox
                id="remember-me"
                checked={field.value}
                onCheckedChange={(checked) => field.onChange(checked === true)}
                className="size-5 rounded-[5px] border-2 border-foreground"
              />
              {t('auth.login.rememberMe')}
            </label>
          )}
        />
        <Link to="/forgot-password" className="font-semibold text-primary hover:underline">
          {t('auth.login.forgotPassword')}
        </Link>
      </div>

      <SubmitButton loading={isSubmitting}>{t('auth.login.submit')}</SubmitButton>

      <p className="text-center text-base">
        {t('auth.login.noAccount')}{' '}
        <Link to="/register" className="text-primary underline">
          {t('auth.login.registerLink')}
        </Link>
      </p>
    </form>
  )
}

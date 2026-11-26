import { zodResolver } from '@hookform/resolvers/zod'
import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { FormAlert } from '@/components/form/FormAlert'
import { OutlinedField } from '@/components/form/OutlinedField'
import { SubmitButton } from '@/components/form/SubmitButton'
import { useFormErrors } from '@/components/form/useFormErrors'
import { codeFormSchema, type CodeFormValues } from '@/features/auth/schemas'
import { AuthHeading } from './AuthHeading'

const RESEND_COOLDOWN_SECONDS = 60

type CodeVerificationFormProps = {
  email: string
  onVerify: (code: string) => Promise<void>
  onResend: () => Promise<void>
}

/** "Kodu Doğrula" screen, shared by sign-up and password reset. */
export function CodeVerificationForm({ email, onVerify, onResend }: CodeVerificationFormProps) {
  const { t } = useTranslation()
  // A code was just sent when this screen opens.
  const [secondsLeft, setSecondsLeft] = useState(RESEND_COOLDOWN_SECONDS)
  const [resending, setResending] = useState(false)

  const form = useForm<CodeFormValues>({ resolver: zodResolver(codeFormSchema), defaultValues: { code: '' } })
  const { formError, fieldError, handleError, setFormError } = useFormErrors(form.setError, ['code'])
  const { errors, isSubmitting } = form.formState

  useEffect(() => {
    if (secondsLeft <= 0) return
    const timer = window.setTimeout(() => setSecondsLeft((seconds) => seconds - 1), 1000)
    return () => window.clearTimeout(timer)
  }, [secondsLeft])

  const onSubmit = form.handleSubmit(async ({ code }) => {
    setFormError(null)
    try {
      await onVerify(code)
    } catch (error) {
      handleError(error)
    }
  })

  const resend = async () => {
    setResending(true)
    setFormError(null)
    try {
      await onResend()
      form.reset({ code: '' })
      setSecondsLeft(RESEND_COOLDOWN_SECONDS)
      toast.success(t('auth.verify.resent'))
    } catch (error) {
      handleError(error)
    } finally {
      setResending(false)
    }
  }

  return (
    <form onSubmit={onSubmit} noValidate className="flex flex-col gap-5">
      <AuthHeading title={t('auth.verify.title')} subtitle={t('auth.verify.subtitle')} />
      <p className="-mt-3 text-sm text-muted-foreground">{t('auth.verify.sentTo', { email })}</p>
      <FormAlert message={formError} />

      <OutlinedField
        label={t('auth.code')}
        inputMode="numeric"
        autoComplete="one-time-code"
        maxLength={6}
        className="[&_input]:tracking-[0.4em]"
        error={fieldError(errors.code?.message)}
        {...form.register('code')}
      />

      <SubmitButton loading={isSubmitting}>{t('auth.verify.submit')}</SubmitButton>

      <button
        type="button"
        onClick={() => void resend()}
        disabled={secondsLeft > 0 || resending}
        className="self-center text-base font-semibold text-primary underline disabled:cursor-not-allowed disabled:text-muted-foreground disabled:no-underline"
      >
        {secondsLeft > 0 ? t('auth.verify.resendIn', { seconds: secondsLeft }) : t('auth.verify.resend')}
      </button>
    </form>
  )
}

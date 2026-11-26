import { zodResolver } from '@hookform/resolvers/zod'
import { useForm, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { FormAlert } from '@/components/form/FormAlert'
import { OutlinedField } from '@/components/form/OutlinedField'
import { SubmitButton } from '@/components/form/SubmitButton'
import { useFormErrors } from '@/components/form/useFormErrors'
import { UserAvatar } from '@/components/UserAvatar'
import { useAuth } from '@/features/auth/AuthContext'
import type { User } from '@/features/auth/types'
import { profileApi } from './api'
import { profileSchema, type ProfileValues } from './schemas'
import { SettingsCard } from './SettingsCard'

/** Name and profile image URL. The email is shown but cannot be changed. */
export function ProfileForm({ user, onSaved }: { user: User; onSaved?: () => void }) {
  const { t } = useTranslation()
  const { updateUser } = useAuth()
  const form = useForm<ProfileValues>({
    resolver: zodResolver(profileSchema),
    defaultValues: { name: user.name, profileImage: user.profileImage ?? '' },
  })
  const { formError, fieldError, handleError, setFormError } = useFormErrors(form.setError, ['name', 'profileImage'])
  const { errors, isSubmitting } = form.formState
  const [name, profileImage] = useWatch({ control: form.control, name: ['name', 'profileImage'] })

  const onSubmit = form.handleSubmit(async (values) => {
    setFormError(null)
    try {
      const updated = await profileApi.update({ name: values.name, profileImage: values.profileImage || null })
      updateUser(updated)
      form.reset({ name: updated.name, profileImage: updated.profileImage ?? '' })
      toast.success(t('profile.account.saved'))
      onSaved?.()
    } catch (error) {
      handleError(error)
    }
  })

  return (
    <SettingsCard title={t('profile.account.profileTitle')}>
      <form onSubmit={onSubmit} noValidate className="flex flex-col gap-5">
        <FormAlert message={formError} />
        <div className="flex items-center gap-4">
          <UserAvatar
            name={name.trim() || user.name}
            image={profileImage.trim() || null}
            className="size-14 text-lg"
            aria-hidden
          />
          <p className="text-sm text-muted-foreground">{t('profile.account.imageHint')}</p>
        </div>
        <OutlinedField
          label={t('auth.name')}
          autoComplete="name"
          error={fieldError(errors.name?.message)}
          {...form.register('name')}
        />
        <OutlinedField label={t('auth.email')} value={user.email} readOnly />
        <OutlinedField
          label={t('profile.account.imageUrl')}
          type="url"
          placeholder="https://"
          error={fieldError(errors.profileImage?.message)}
          {...form.register('profileImage')}
        />
        <div className="sm:ml-auto sm:w-44">
          <SubmitButton size="md" loading={isSubmitting}>
            {t('profile.account.save')}
          </SubmitButton>
        </div>
      </form>
    </SettingsCard>
  )
}

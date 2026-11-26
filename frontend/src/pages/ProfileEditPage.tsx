import { type CSSProperties, useRef } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import { PenpotBackButton } from '@/components/PenpotBackButton'
import { useAuth } from '@/features/auth/AuthContext'
import { ChangePasswordForm } from '@/features/profile/ChangePasswordForm'
import { ProfileForm } from '@/features/profile/ProfileForm'
import { useOffsetFromRight } from '@/lib/useAlignWithElement'

/**
 * Account settings, opened from the profile's "Düzenle" button: name / profile image and password.
 * Saving the profile returns to the profile screen; a password change stays here.
 */
export function ProfileEditPage() {
  const { t } = useTranslation()
  const { user } = useAuth()
  const navigate = useNavigate()
  const contentRef = useRef<HTMLDivElement>(null)
  const avatarGap = useOffsetFromRight(contentRef, '[data-user-avatar]', 'left')

  if (!user) return null

  return (
    <section className="flex flex-col gap-6">
      <PenpotBackButton fallback="/profile" />
      <div
        ref={contentRef}
        className="lg:pr-[var(--avatar-gap,0px)]"
        style={avatarGap !== null && avatarGap > 0 ? ({ '--avatar-gap': `${avatarGap}px` } as CSSProperties) : undefined}
      >
        <div className="flex flex-col gap-6 [zoom:0.9]">
          <h1 className="text-2xl leading-8 font-semibold">{t('profile.editTitle')}</h1>
          <div className="grid items-start gap-4 lg:grid-cols-2">
            <ProfileForm
              key={`${user.name}|${user.profileImage}`}
              user={user}
              onSaved={() => navigate('/profile', { replace: true })}
            />
            <ChangePasswordForm />
          </div>
        </div>
      </div>
    </section>
  )
}

import { ChevronDownIcon, LogOutIcon, UserIcon } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { UserAvatar } from '@/components/UserAvatar'
import { useAuth } from '@/features/auth/AuthContext'

/** Avatar (60px circle) + name from the Penpot header; opens profile and logout actions. */
export function UserMenu() {
  const { t } = useTranslation()
  const { user, signOut } = useAuth()
  const navigate = useNavigate()

  if (!user) return null

  return (
    <DropdownMenu>
      <DropdownMenuTrigger
        className="group flex items-center gap-2 rounded-full text-left outline-none focus-visible:ring-3 focus-visible:ring-ring/50"
        aria-label={t('nav.userMenu')}
      >
        <UserAvatar data-user-avatar name={user.name} image={user.profileImage} className="size-9 text-sm" />
        <span className="hidden max-w-48 truncate text-sm font-semibold lg:inline">{user.name}</span>
        {/* Shows that the name opens a menu; turns upside down while the menu is open. */}
        <ChevronDownIcon
          className="-ml-1 size-4 shrink-0 text-foreground/70 transition-transform duration-200 group-data-[state=open]:rotate-180"
          aria-hidden
        />
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="min-w-48">
        <DropdownMenuItem onSelect={() => navigate('/profile')}>
          <UserIcon aria-hidden />
          {t('nav.profile')}
        </DropdownMenuItem>
        <DropdownMenuSeparator />
        <DropdownMenuItem onSelect={() => void signOut()}>
          <LogOutIcon aria-hidden />
          {t('common.logout')}
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}

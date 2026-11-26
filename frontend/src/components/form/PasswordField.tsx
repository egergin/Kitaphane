import { EyeIcon, EyeOffIcon } from 'lucide-react'
import { type ComponentProps, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { OutlinedField } from './OutlinedField'

type PasswordFieldProps = Omit<ComponentProps<typeof OutlinedField>, 'type' | 'endAdornment'>

export function PasswordField(props: PasswordFieldProps) {
  const { t } = useTranslation()
  const [visible, setVisible] = useState(false)

  return (
    <OutlinedField
      {...props}
      type={visible ? 'text' : 'password'}
      endAdornment={
        <button
          type="button"
          onClick={() => setVisible((value) => !value)}
          className="mb-1.5 ml-2 shrink-0 rounded-md p-1 text-foreground hover:bg-muted"
          aria-label={visible ? t('common.hidePassword') : t('common.showPassword')}
        >
          {visible ? <EyeIcon className="size-5" /> : <EyeOffIcon className="size-5" />}
        </button>
      }
    />
  )
}

import { Navigate, useLocation, useNavigate } from 'react-router'
import { authApi } from '@/features/auth/api'
import { CodeVerificationForm } from './CodeVerificationForm'

export function VerifyResetCodePage() {
  const navigate = useNavigate()
  const email = (useLocation().state as { email?: string } | null)?.email

  if (!email) return <Navigate to="/forgot-password" replace />

  return (
    <CodeVerificationForm
      email={email}
      onVerify={async (code) => {
        const { resetToken } = await authApi.verifyResetCode({ email, code })
        navigate('/reset-password', { replace: true, state: { email, resetToken } })
      }}
      onResend={() => authApi.forgotPassword(email)}
    />
  )
}

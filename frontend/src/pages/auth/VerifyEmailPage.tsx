import { Navigate, useLocation, useNavigate } from 'react-router'
import { authApi } from '@/features/auth/api'
import { useAuth } from '@/features/auth/AuthContext'
import { CodeVerificationForm } from './CodeVerificationForm'

export function VerifyEmailPage() {
  const { signIn } = useAuth()
  const navigate = useNavigate()
  const email = (useLocation().state as { email?: string } | null)?.email

  // The email comes from the sign-up form; opened directly, start over.
  if (!email) return <Navigate to="/register" replace />

  return (
    <CodeVerificationForm
      email={email}
      onVerify={async (code) => {
        signIn(await authApi.verifyEmail({ email, code }))
        navigate('/', { replace: true })
      }}
      onResend={() => authApi.resendVerification(email)}
    />
  )
}

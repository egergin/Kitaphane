import { createBrowserRouter } from 'react-router'
import { GuestRoute, ProtectedRoute } from '@/features/auth/routes'
import { AppLayout } from '@/layouts/AppLayout'
import { AuthLayout } from '@/layouts/AuthLayout'
import { ForgotPasswordPage } from '@/pages/auth/ForgotPasswordPage'
import { LoginPage } from '@/pages/auth/LoginPage'
import { RegisterPage } from '@/pages/auth/RegisterPage'
import { ResetPasswordPage } from '@/pages/auth/ResetPasswordPage'
import { VerifyEmailPage } from '@/pages/auth/VerifyEmailPage'
import { VerifyResetCodePage } from '@/pages/auth/VerifyResetCodePage'
import { AddBookPage } from '@/pages/books/AddBookPage'
import { BookDetailPage } from '@/pages/books/BookDetailPage'
import { EditBookPage } from '@/pages/books/EditBookPage'
import { ReadBooksPage } from '@/pages/books/ReadBooksPage'
import { HomePage } from '@/pages/HomePage'
import { NotFoundPage } from '@/pages/NotFoundPage'
import { ProfileEditPage } from '@/pages/ProfileEditPage'
import { ProfilePage } from '@/pages/ProfilePage'

export const router = createBrowserRouter([
  {
    element: <GuestRoute />,
    children: [
      {
        element: <AuthLayout />,
        children: [
          { path: 'login', element: <LoginPage /> },
          { path: 'register', element: <RegisterPage /> },
          { path: 'verify-email', element: <VerifyEmailPage /> },
          { path: 'forgot-password', element: <ForgotPasswordPage /> },
          { path: 'forgot-password/verify', element: <VerifyResetCodePage /> },
          { path: 'reset-password', element: <ResetPasswordPage /> },
        ],
      },
    ],
  },
  {
    element: <ProtectedRoute />,
    children: [
      {
        element: <AppLayout />,
        children: [
          { index: true, element: <HomePage /> },
          { path: 'books/new', element: <AddBookPage /> },
          { path: 'books/:id', element: <BookDetailPage /> },
          { path: 'books/:id/edit', element: <EditBookPage /> },
          { path: 'read-books', element: <ReadBooksPage /> },
          { path: 'profile', element: <ProfilePage /> },
          { path: 'profile/edit', element: <ProfileEditPage /> },
        ],
      },
    ],
  },
  { path: '*', element: <NotFoundPage /> },
])

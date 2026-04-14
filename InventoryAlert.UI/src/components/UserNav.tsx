'use client'

import { useEffect, useState } from 'react'
import Link from 'next/link'
import { useRouter } from 'next/navigation'
import { useTheme } from './ThemeProvider'
import NotificationBell from './NotificationBell'

export default function UserNav() {
  const [token, setToken] = useState<string | null>(null)
  const [mounted, setMounted] = useState(false)
  const router = useRouter()
  const { theme, toggleTheme, mounted: themeMounted } = useTheme()

  useEffect(() => {
    setMounted(true)
    setToken(localStorage.getItem('auth_token'))
  }, [])

  const handleLogout = async () => {
    try {
      if (token) {
        await fetch(`${process.env.NEXT_PUBLIC_API_URL || "http://localhost:8080"}/api/v1/auth/logout`, {
          method: 'POST',
          headers: {
            'Authorization': `Bearer ${token}`
          },
          credentials: 'include'
        });
      }
    } catch (e) {
      console.error('Logout failed', e);
    } finally {
      localStorage.removeItem('auth_token');
      setToken(null);
      router.push('/login');
    }
  }

  if (!mounted || !themeMounted) return <div className="w-20"></div>

  return (
    <div className="flex items-center gap-6">
      <button 
        onClick={toggleTheme}
        className="p-2 rounded-xl bg-zinc-100 dark:bg-zinc-900 border border-zinc-200 dark:border-white/5 text-zinc-600 dark:text-zinc-400 hover:text-blue-500 transition-all shadow-sm"
        title={`Switch to ${theme === 'dark' ? 'light' : 'dark'} mode`}
      >
        {theme === 'dark' ? '☀️' : '🌙'}
      </button>

      {!token ? (
        <Link 
          href="/login" 
          className="text-sm font-bold bg-zinc-900 dark:bg-white text-white dark:text-black px-4 py-2 rounded-xl hover:opacity-90 transition-all shadow-lg"
        >
          Sign In
        </Link>
      ) : (
        <div className="flex items-center gap-4">
          <NotificationBell />
          <div className="w-8 h-8 rounded-full bg-blue-500 flex items-center justify-center font-bold text-xs ring-2 ring-zinc-200 dark:ring-white/10 shadow-lg text-white">
            A
          </div>
          <button 
            onClick={handleLogout}
            className="text-xs font-bold text-zinc-500 hover:text-zinc-900 dark:hover:text-white transition-colors"
          >
            Sign Out
          </button>
        </div>
      )}
    </div>
  )
}

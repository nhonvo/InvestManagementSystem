'use client'

import { useEffect, useState } from 'react'
import Link from 'next/link'
import { useRouter } from 'next/navigation'

export default function UserNav() {
  const [token, setToken] = useState<string | null>(null)
  const [mounted, setMounted] = useState(false)
  const router = useRouter()

  useEffect(() => {
    setMounted(true)
    const storedToken = localStorage.getItem('auth_token')
    setToken(storedToken)
  }, [])

  const handleLogout = () => {
    localStorage.removeItem('auth_token')
    setToken(null)
    router.push('/login')
  }

  if (!mounted) return <div className="w-20"></div>

  if (!token) {
    return (
      <Link 
        href="/login" 
        className="text-sm font-bold bg-white text-black px-4 py-2 rounded-xl hover:bg-zinc-200 transition-colors"
      >
        Sign In
      </Link>
    )
  }

  return (
    <div className="flex items-center gap-4">
      <div className="w-8 h-8 rounded-full bg-blue-500 flex items-center justify-center font-bold text-xs ring-2 ring-white/10 shadow-lg">
        A
      </div>
      <button 
        onClick={handleLogout}
        className="text-xs font-bold text-zinc-500 hover:text-white transition-colors"
      >
        Sign Out
      </button>
    </div>
  )
}

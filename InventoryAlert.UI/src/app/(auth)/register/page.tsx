'use client'

import { useState } from 'react'
import { useRouter } from 'next/navigation'
import Link from 'next/link'
import { fetchApi } from '@/lib/api'

export default function RegisterPage() {
  const [username, setUsername] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const router = useRouter()

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    
    if (password !== confirmPassword) {
      setError('Passwords do not match')
      return
    }

    setLoading(true)

    try {
      await fetchApi('/api/auth/register', {
        method: 'POST',
        body: JSON.stringify({ username, email, password, confirmPassword }),
      })
      router.push('/login?registered=true')
    } catch (err: any) {
      setError(err.message || 'Registration failed')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="flex flex-col items-center justify-center min-h-[60vh]">
      <div className="w-full max-w-md p-8 space-y-8 bg-zinc-900 border border-white/10 rounded-2xl shadow-xl">
        <div className="text-center">
          <h2 className="text-3xl font-extrabold text-white tracking-tight">Create Account</h2>
          <p className="mt-2 text-zinc-400">Join InventoryAlert today</p>
        </div>
        
        <form className="mt-8 space-y-6" onSubmit={handleSubmit}>
          <div className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-zinc-400 mb-1">Username</label>
              <input
                type="text"
                required
                className="w-full px-4 py-3 bg-zinc-800 border border-white/5 rounded-xl text-white focus:outline-none focus:ring-2 focus:ring-blue-500 transition-all"
                placeholder="johndoe"
                value={username}
                onChange={(e) => setUsername(e.target.value)}
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-zinc-400 mb-1">Email Address</label>
              <input
                type="email"
                required
                className="w-full px-4 py-3 bg-zinc-800 border border-white/5 rounded-xl text-white focus:outline-none focus:ring-2 focus:ring-blue-500 transition-all"
                placeholder="john@example.com"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-zinc-400 mb-1">Password</label>
              <input
                type="password"
                required
                className="w-full px-4 py-3 bg-zinc-800 border border-white/5 rounded-xl text-white focus:outline-none focus:ring-2 focus:ring-blue-500 transition-all"
                placeholder="••••••••"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-zinc-400 mb-1">Confirm Password</label>
              <input
                type="password"
                required
                className="w-full px-4 py-3 bg-zinc-800 border border-white/5 rounded-xl text-white focus:outline-none focus:ring-2 focus:ring-blue-500 transition-all"
                placeholder="••••••••"
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
              />
            </div>
          </div>

          {error && (
            <div className="p-3 text-sm bg-rose-500/10 border border-rose-500/20 text-rose-400 rounded-lg">
              {error}
            </div>
          )}

          <button
            type="submit"
            disabled={loading}
            className="w-full py-4 bg-blue-600 hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed text-white font-bold rounded-xl shadow-lg shadow-blue-500/20 transition-all transform hover:scale-[1.02] active:scale-[0.98]"
          >
            {loading ? 'Creating Account...' : 'Sign Up'}
          </button>

          <div className="text-center text-sm">
            <span className="text-zinc-500">Already have an account? </span>
            <Link href="/login" className="text-blue-400 hover:text-blue-300 font-medium transition-colors">
              Log In
            </Link>
          </div>
        </form>
      </div>
    </div>
  )
}

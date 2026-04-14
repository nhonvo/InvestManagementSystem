'use client'

import { useEffect, useState } from 'react'

export type ToastType = 'success' | 'error' | 'info'

interface ToastProps {
  message: string
  type: ToastType
  duration?: number
  onClose: () => void
}

export function Toast({ message, type, duration = 5000, onClose }: ToastProps) {
  const [isVisible, setIsVisible] = useState(true)

  useEffect(() => {
    const timer = setTimeout(() => {
      setIsVisible(false)
      setTimeout(onClose, 300) // Wait for fade out animation
    }, duration)

    return () => clearTimeout(timer)
  }, [duration, onClose])

  const bgColors = {
    success: 'bg-emerald-500/10 border-emerald-500/20 text-emerald-400',
    error: 'bg-rose-500/10 border-rose-500/20 text-rose-400',
    info: 'bg-blue-500/10 border-blue-500/20 text-blue-400'
  }

  const icons = {
    success: '✓',
    error: '✕',
    info: 'ℹ'
  }

  return (
    <div className={`fixed bottom-8 right-8 z-100 transition-all duration-300 transform ${isVisible ? 'translate-y-0 opacity-100' : 'translate-y-4 opacity-0'}`}>
      <div className={`flex items-center gap-3 px-6 py-4 rounded-2xl border backdrop-blur-xl shadow-2xl ${bgColors[type]}`}>
        <span className="flex items-center justify-center w-6 h-6 rounded-full bg-black/20 text-xs font-bold leading-none">
          {icons[type]}
        </span>
        <p className="text-sm font-bold uppercase tracking-tight">{message}</p>
        <button 
          onClick={() => setIsVisible(false)}
          className="ml-4 text-white/40 hover:text-white transition-colors"
        >
          ✕
        </button>
      </div>
    </div>
  )
}

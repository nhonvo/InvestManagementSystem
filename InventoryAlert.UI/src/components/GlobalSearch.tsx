'use client'

import { useState, useEffect, useRef } from 'react'
import { fetchApi } from '@/lib/api'
import { useRouter } from 'next/navigation'

interface GlobalSearchProps {
  isOpen: boolean
  onClose: () => void
}

export function GlobalSearch({ isOpen, onClose }: GlobalSearchProps) {
  const [query, setQuery] = useState('')
  const [results, setResults] = useState<any[]>([])
  const [loading, setLoading] = useState(false)
  const [activeIndex, setActiveIndex] = useState(0)
  const inputRef = useRef<HTMLInputElement>(null)
  const router = useRouter()

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (!isOpen) return;
      if (e.key === 'Escape') onClose()
      
      if (e.key === 'ArrowDown') {
          e.preventDefault()
          setActiveIndex(prev => Math.min(prev + 1, results.length - 1))
      }
      if (e.key === 'ArrowUp') {
          e.preventDefault()
          setActiveIndex(prev => Math.max(prev - 1, 0))
      }
      if (e.key === 'Enter' && results.length > 0) {
          e.preventDefault()
          router.push(`/stocks/${results[activeIndex].symbol.toLowerCase()}`)
          onClose()
      }
    }
    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [onClose, isOpen, results, activeIndex, router])

  useEffect(() => {
    if (isOpen) {
      setTimeout(() => inputRef.current?.focus(), 50)
    } else {
      setQuery('')
      setResults([])
      setActiveIndex(0)
    }
  }, [isOpen])

  useEffect(() => {
    if (!query.trim()) {
      setResults([])
      return
    }
    const timer = setTimeout(async () => {
      setLoading(true)
      try {
        const data = await fetchApi(`/api/v1/stocks/search?q=${encodeURIComponent(query)}`)
        setResults(data || [])
        setActiveIndex(0)
      } catch {
        // silent
      } finally {
        setLoading(false)
      }
    }, 300)
    return () => clearTimeout(timer)
  }, [query])

  if (!isOpen) return null

  return (
    <div
      className="fixed inset-0 z-[200] flex items-start justify-center pt-[10vh] px-4 animate-in fade-in duration-300"
      style={{ backdropFilter: 'blur(20px)', background: 'rgba(0,0,0,0.6)' }}
      role="dialog"
      aria-modal="true"
      aria-label="Global search"
    >
      <div className="absolute inset-0" onClick={onClose} aria-hidden />

      <div className="relative w-full max-w-3xl bg-white/90 dark:bg-[#0a0a0a]/90 backdrop-blur-3xl rounded-[2.5rem] shadow-2xl shadow-black/40 overflow-hidden border border-white/20 dark:border-white/10 animate-in slide-in-from-top-8 duration-300 transform scale-100">
        {/* Glow effect */}
        <div className="absolute top-0 right-0 w-64 h-64 bg-blue-500/10 rounded-full blur-3xl pointer-events-none"></div>
        <div className="absolute bottom-0 left-0 w-64 h-64 bg-emerald-500/10 rounded-full blur-3xl pointer-events-none"></div>

        {/* Input row */}
        <div className="px-8 py-6 border-b border-zinc-200/50 dark:border-white/10 flex items-center gap-5 relative z-10">
          <svg className="w-7 h-7 text-blue-500 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2.5}>
            <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-4.35-4.35M17 11A6 6 0 1 1 5 11a6 6 0 0 1 12 0z" />
          </svg>
          <input
            ref={inputRef}
            id="global-search-input"
            type="text"
            placeholder="Search symbols or companies..."
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            className="flex-1 bg-transparent border-none outline-none text-2xl font-black text-zinc-900 dark:text-white placeholder:text-zinc-300 dark:placeholder:text-zinc-600 tracking-tight"
          />
          <button
            onClick={onClose}
            className="shrink-0 text-[10px] font-black uppercase tracking-widest text-zinc-500 hover:text-zinc-900 dark:hover:text-white bg-zinc-100 dark:bg-zinc-800 border border-zinc-200 dark:border-white/10 rounded-xl px-3 py-1.5 transition-all shadow-sm active:scale-95"
          >
            ESC
          </button>
        </div>

        {/* Results */}
        <div className="max-h-[50vh] overflow-y-auto p-4 relative z-10 scrollbar-thin scrollbar-thumb-zinc-200 dark:scrollbar-thumb-zinc-800">
          {loading ? (
            <div className="p-4 space-y-3">
              {[1, 2, 3].map((i) => (
                <div key={i} className="h-16 bg-zinc-100/50 dark:bg-zinc-800/50 rounded-2xl animate-pulse" />
              ))}
            </div>
          ) : results.length === 0 ? (
            <div className="py-20 flex flex-col items-center justify-center text-center">
              <div className="w-16 h-16 bg-zinc-50 dark:bg-zinc-900 rounded-full flex items-center justify-center mb-4 border border-zinc-100 dark:border-white/5">
                <svg className="w-6 h-6 text-zinc-300 dark:text-zinc-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19.428 15.428a2 2 0 00-1.022-.547l-2.387-.477a6 6 0 00-3.86.517l-.318.158a6 6 0 01-3.86.517L6.05 15.21a2 2 0 00-1.806.547M8 4h8l-1 1v5.172a2 2 0 00.586 1.414l5 5c1.26 1.26.367 3.414-1.415 3.414H4.828c-1.782 0-2.674-2.154-1.414-3.414l5-5A2 2 0 009 10.172V5L8 4z" />
                </svg>
              </div>
              <p className="text-zinc-500 dark:text-zinc-400 font-bold uppercase tracking-widest text-xs">
                {query.trim() ? 'No securities found' : 'Type to discover securities'}
              </p>
            </div>
          ) : (
            <div className="space-y-2">
              {results.map((r, idx) => (
                <button
                  key={r.symbol}
                  onClick={() => {
                    router.push(`/stocks/${r.symbol.toLowerCase()}`)
                    onClose()
                  }}
                  onMouseEnter={() => setActiveIndex(idx)}
                  className={`w-full flex items-center justify-between p-4 rounded-[1.5rem] transition-all group text-left ${idx === activeIndex ? 'bg-blue-500/10 dark:bg-blue-500/20 border border-blue-500/20 dark:border-blue-500/30' : 'bg-transparent border border-transparent hover:bg-zinc-50 dark:hover:bg-zinc-800/50 hover:border-zinc-200 dark:hover:border-white/5'}`}
                >
                  <div className="flex items-center gap-5">
                    <div className={`w-14 h-14 rounded-2xl flex items-center justify-center text-sm font-black border transition-colors shadow-sm ${idx === activeIndex ? 'bg-blue-600 text-white border-blue-500/50' : 'bg-white dark:bg-zinc-900 border-zinc-200 dark:border-white/10 group-hover:border-blue-500/30 text-zinc-800 dark:text-zinc-200'}`}>
                      {r.symbol}
                    </div>
                    <div>
                      <p className={`font-bold text-lg mb-0.5 tracking-tight ${idx === activeIndex ? 'text-blue-600 dark:text-blue-400' : 'text-zinc-900 dark:text-white group-hover:text-blue-500'}`}>
                        {r.description}
                      </p>
                      <p className="text-[11px] font-black text-zinc-400 dark:text-zinc-500 uppercase tracking-widest">
                        {r.exchange} · {r.type}
                      </p>
                    </div>
                  </div>
                  <div className={`w-8 h-8 rounded-full flex items-center justify-center transition-all ${idx === activeIndex ? 'bg-blue-500 text-white' : 'bg-transparent text-zinc-300 group-hover:bg-blue-500/10 group-hover:text-blue-500'}`}>
                      <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={3}>
                        <path strokeLinecap="round" strokeLinejoin="round" d="M9 5l7 7-7 7" />
                      </svg>
                  </div>
                </button>
              ))}
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="px-6 py-4 border-t border-zinc-200/50 dark:border-white/10 bg-zinc-50/80 dark:bg-black/40 flex items-center justify-between relative z-10 backdrop-blur-xl">
          <div className="flex gap-4 text-[10px] font-black uppercase tracking-widest text-zinc-400">
            <span className="flex items-center gap-1"><kbd className="bg-zinc-200 dark:bg-zinc-800 px-1.5 py-0.5 rounded text-zinc-500">↑↓</kbd> Navigate</span>
            <span className="flex items-center gap-1"><kbd className="bg-zinc-200 dark:bg-zinc-800 px-1.5 py-0.5 rounded text-zinc-500">↵</kbd> Select</span>
          </div>
          <div className="flex items-center gap-2">
            <div className="w-1.5 h-1.5 bg-blue-500 rounded-full animate-pulse"></div>
            <p className="text-[10px] font-black uppercase tracking-widest text-blue-500">Market Search</p>
          </div>
        </div>
      </div>
    </div>
  )
}

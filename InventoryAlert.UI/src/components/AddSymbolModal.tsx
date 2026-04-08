'use client'

import { useState } from 'react'
import { fetchApi } from '@/lib/api'

interface AddSymbolModalProps {
  isOpen: boolean
  onClose: () => void
  onSuccess: () => void
}

export function AddSymbolModal({ isOpen, onClose, onSuccess }: AddSymbolModalProps) {
  const [symbol, setSymbol] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  if (!isOpen) return null

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!symbol.trim()) return

    setLoading(true)
    setError('')

    try {
      await fetchApi(`/api/v1/watchlist/${symbol.trim()}`, {
        method: 'POST'
      })
      setSymbol('')
      onSuccess()
      onClose()
    } catch (err: any) {
      setError(err.message || 'Failed to add symbol')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div 
        className="absolute inset-0 bg-black/60 backdrop-blur-sm" 
        onClick={onClose}
      />
      
      <div className="relative w-full max-w-md bg-zinc-900 border border-white/10 rounded-3xl shadow-2xl overflow-hidden p-8">
        <div className="mb-6 text-center">
          <h2 className="text-2xl font-bold mb-2">Add New Symbol</h2>
          <p className="text-zinc-500 text-sm">Enter a stock ticker to start tracking it.</p>
        </div>

        <form onSubmit={handleSubmit} className="space-y-6">
          <div>
            <label htmlFor="symbol" className="block text-xs font-bold text-zinc-500 uppercase tracking-widest mb-2 px-1">
              Stock Ticker
            </label>
            <input
              id="symbol"
              type="text"
              autoFocus
              value={symbol}
              onChange={(e) => setSymbol(e.target.value.toUpperCase())}
              placeholder="e.g. TSLA, AAPL, BTC-USD"
              className="w-full bg-black border border-white/10 rounded-2xl p-4 text-white placeholder:text-zinc-700 outline-hidden focus:border-blue-500/50 transition-all font-mono uppercase"
            />
          </div>

          {error && (
            <div className="bg-rose-500/10 border border-rose-500/20 text-rose-400 p-4 rounded-xl text-xs">
              {error}
            </div>
          )}

          <div className="flex gap-3">
            <button
              type="button"
              onClick={onClose}
              className="flex-1 bg-zinc-800 hover:bg-zinc-700 text-white rounded-2xl py-4 font-bold text-sm transition-all active:scale-95 px-4"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={loading || !symbol.trim()}
              className="flex-3 bg-blue-600 hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed text-white rounded-2xl py-4 font-bold text-sm shadow-xl shadow-blue-500/20 transition-all active:scale-95 px-4"
            >
              {loading ? 'Adding...' : 'Add to Watchlist'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

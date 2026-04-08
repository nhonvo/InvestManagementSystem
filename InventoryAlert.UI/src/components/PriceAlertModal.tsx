'use client'

import { useState } from 'react'
import { fetchApi } from '@/lib/api'

interface PriceAlertModalProps {
  isOpen: boolean
  onClose: () => void
  symbol: string
  currentPrice: number
}

export function PriceAlertModal({ isOpen, onClose, symbol, currentPrice }: PriceAlertModalProps) {
  const [threshold, setThreshold] = useState(currentPrice.toString())
  const [operator, setOperator] = useState('gt')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  if (!isOpen) return null

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setLoading(true)
    setError('')

    try {
      await fetchApi('/api/v1/alerts', {
        method: 'POST',
        body: JSON.stringify({
          symbol: symbol.toUpperCase(),
          field: 'price',
          operator,
          threshold: parseFloat(threshold),
          notifyChannel: 'telegram'
        })
      })
      alert('Alert set successfully!')
      onClose()
    } catch (err: any) {
      setError(err.message || 'Failed to set alert')
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
          <h2 className="text-2xl font-bold mb-2">Set Price Alert</h2>
          <p className="text-zinc-400 text-sm">Notify me when <span className="text-white font-bold">{symbol}</span> price is...</p>
        </div>

        <form onSubmit={handleSubmit} className="space-y-6">
          <div className="flex gap-2 p-1 bg-black rounded-2xl border border-white/5">
            <button
              type="button"
              onClick={() => setOperator('gt')}
              className={`flex-1 py-3 rounded-xl font-bold text-sm transition-all ${operator === 'gt' ? "bg-zinc-800 text-white shadow-lg" : "text-zinc-500 hover:text-white"}`}
            >
              Above
            </button>
            <button
              type="button"
              onClick={() => setOperator('lt')}
              className={`flex-1 py-3 rounded-xl font-bold text-sm transition-all ${operator === 'lt' ? "bg-zinc-800 text-white shadow-lg" : "text-zinc-500 hover:text-white"}`}
            >
              Below
            </button>
          </div>

          <div>
            <label className="block text-xs font-bold text-zinc-500 uppercase tracking-widest mb-2 px-1">
              Target Price ($)
            </label>
            <input
              type="number"
              step="0.01"
              value={threshold}
              onChange={(e) => setThreshold(e.target.value)}
              className="w-full bg-black border border-white/10 rounded-2xl p-4 text-white outline-hidden focus:border-blue-500/50 transition-all font-mono text-xl"
            />
            <p className="text-[10px] text-zinc-500 mt-2 px-1 italic">Current Price: ${currentPrice.toFixed(2)}</p>
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
              className="flex-1 bg-zinc-800 hover:bg-zinc-700 text-white rounded-2xl py-4 font-bold text-sm transition-all active:scale-95"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={loading || !threshold}
              className="flex-2 bg-blue-600 hover:bg-blue-700 disabled:opacity-50 text-white rounded-2xl py-4 font-bold text-sm shadow-xl shadow-blue-500/20 transition-all active:scale-95"
            >
              {loading ? 'Setting...' : 'Set Alert'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

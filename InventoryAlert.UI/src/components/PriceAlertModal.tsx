'use client'

import { useState } from 'react'
import { fetchApi } from '@/lib/api'

interface PriceAlertModalProps {
  isOpen: boolean
  onClose: () => void
  symbol: string
  currentPrice: number
}

export function PriceAlertModal({ isOpen, onClose, symbol = "SYMBOL", currentPrice = 0 }: PriceAlertModalProps) {
  const [threshold, setThreshold] = useState(currentPrice?.toString() ?? "0")
  const [condition, setCondition] = useState('PriceAbove')
  const [triggerOnce, setTriggerOnce] = useState(true)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState(false)

  if (!isOpen) return null

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setLoading(true)
    setError('')

    try {
      await fetchApi('/api/v1/alertrules', {
        method: 'POST',
        body: JSON.stringify({
          tickerSymbol: symbol.toUpperCase(),
          condition: condition,
          targetValue: parseFloat(threshold),
          triggerOnce: triggerOnce
        })
      })
      setSuccess(true)
      setTimeout(() => {
        onClose()
        setSuccess(false)
      }, 2000)
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
              onClick={() => setCondition('PriceAbove')}
              className={`flex-1 py-3 rounded-xl font-bold text-sm transition-all ${condition === 'PriceAbove' ? "bg-zinc-800 text-white shadow-lg" : "text-zinc-500 hover:text-white"}`}
            >
              Above
            </button>
            <button
              type="button"
              onClick={() => setCondition('PriceBelow')}
              className={`flex-1 py-3 rounded-xl font-bold text-sm transition-all ${condition === 'PriceBelow' ? "bg-zinc-800 text-white shadow-lg" : "text-zinc-500 hover:text-white"}`}
            >
              Below
            </button>
          </div>

          <div className="flex items-center justify-between p-4 bg-black rounded-2xl border border-white/5">
            <div className="flex flex-col">
              <span className="text-xs font-bold text-white uppercase tracking-wider">Trigger Once</span>
              <span className="text-xs text-zinc-500">Deactivate after firing</span>
            </div>
            <button
              type="button"
              onClick={() => setTriggerOnce(!triggerOnce)}
              className={`w-12 h-6 rounded-full transition-all relative ${triggerOnce ? 'bg-blue-600' : 'bg-zinc-800'}`}
            >
              <div className={`absolute top-1 w-4 h-4 rounded-full bg-white transition-all ${triggerOnce ? 'right-1' : 'left-1'}`}></div>
            </button>
          </div>

          <div>
            <label className="block text-xs font-bold text-zinc-500 uppercase tracking-wider mb-2 px-1">
              Target Price ($)
            </label>
            <input
              type="number"
              step="0.01"
              value={threshold}
              onChange={(e) => setThreshold(e.target.value)}
              className="w-full bg-black border border-white/10 rounded-2xl p-4 text-white outline-hidden focus:border-blue-500/50 transition-all font-mono text-xl"
            />
            <p className="text-xs text-zinc-500 mt-2 px-1 italic">Current Price: ${currentPrice?.toFixed(2) ?? "0.00"}</p>
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
              disabled={loading || !threshold || success}
              className={`flex-2 rounded-2xl py-4 font-bold text-sm shadow-xl transition-all active:scale-95 ${success ? 'bg-emerald-600' : 'bg-blue-600 hover:bg-blue-700 shadow-blue-500/20'} text-white disabled:opacity-50`}
            >
              {loading ? 'Setting...' : success ? '✓ Alert Live' : 'Set Alert'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

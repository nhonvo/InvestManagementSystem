'use client'

import { useState, useEffect } from "react";
import { fetchApi } from "@/lib/api";

interface TradeModalProps {
  isOpen: boolean;
  onClose: () => void;
  symbol: string;
  onSuccess: () => void;
}

export function TradeModal({ isOpen, onClose, symbol, onSuccess }: TradeModalProps) {
  const [formData, setFormData] = useState({
    tickerSymbol: symbol,
    type: 'Buy',
    quantity: 1,
    unitPrice: 0,
    notes: '',
    tradedAt: new Date().toISOString().split('T')[0]
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [isExisting, setIsExisting] = useState(false);

  useEffect(() => {
    if (isOpen) {
      setFormData(prev => ({ ...prev, tickerSymbol: symbol, type: 'Buy' }));
      if (symbol) {
        checkExistingPosition(symbol);
        fetchCurrentPrice(symbol);
      }
    }
  }, [isOpen, symbol]);

  const checkExistingPosition = async (sym: string) => {
    try {
      const data = await fetchApi(`/api/v1/portfolio/positions/${sym}`);
      setIsExisting(!!data);
    } catch {
      setIsExisting(false);
    }
  };

  const fetchCurrentPrice = async (sym: string) => {
    try {
      const data = await fetchApi(`/api/v1/stocks/${sym}/quote`);
      if (data && data.price) {
        setFormData(prev => ({ ...prev, unitPrice: data.price }));
      }
    } catch (err) {
       console.error("Failed to fetch price", err);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError("");

    try {
      if (isExisting) {
        // Record a trade to adjust position
        await fetchApi(`/api/v1/portfolio/${formData.tickerSymbol}/trades`, {
          method: 'POST',
          body: JSON.stringify({
            type: formData.type,
            quantity: formData.quantity,
            unitPrice: formData.unitPrice,
            notes: formData.notes
          })
        });
      } else {
        // Create new position
        await fetchApi("/api/v1/portfolio/positions", {
          method: 'POST',
          body: JSON.stringify({
            tickerSymbol: formData.tickerSymbol,
            quantity: formData.quantity,
            unitPrice: formData.unitPrice,
            tradedAt: new Date(formData.tradedAt).toISOString()
          })
        });
      }
      onSuccess();
      onClose();
    } catch (err: any) {
      setError(err.message || "Failed to record trade");
    } finally {
      setLoading(false);
    }
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-[100] flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm animate-in fade-in duration-200">
      <div className="bg-white dark:bg-zinc-900 w-full max-w-md rounded-3xl overflow-hidden shadow-2xl border border-zinc-200 dark:border-white/5">
        <div className="p-8 border-b border-zinc-100 dark:border-white/5 bg-zinc-50/50 dark:bg-white/5 flex items-center justify-between">
          <h2 className="text-2xl font-semibold tracking-tight text-zinc-900 dark:text-white uppercase">
            {isExisting ? "Record Trade" : "Open Position"}
          </h2>
          <div className="bg-blue-500/10 text-blue-500 px-3 py-1 rounded-lg text-xs font-semibold uppercase tracking-wider">
            {formData.tickerSymbol || "NEW"}
          </div>
        </div>
        
        <form onSubmit={handleSubmit} className="p-8 space-y-6">
          {error && (
            <div className="p-4 bg-rose-500/10 border border-rose-500/20 text-rose-400 text-sm font-bold rounded-2xl text-center">
              {error}
            </div>
          )}

          {!isExisting && (
            <div className="space-y-2">
              <label className="text-xs font-semibold uppercase tracking-wider text-zinc-400 dark:text-zinc-500 ml-1">Symbol</label>
              <input 
                type="text"
                required
                placeholder="e.g. AAPL"
                value={formData.tickerSymbol}
                onChange={(e) => setFormData({ ...formData, tickerSymbol: e.target.value.toUpperCase() })}
                onBlur={() => formData.tickerSymbol && fetchCurrentPrice(formData.tickerSymbol)}
                className="w-full bg-zinc-100 dark:bg-zinc-800 border-none rounded-2xl px-5 py-4 font-bold text-zinc-900 dark:text-white placeholder:text-zinc-400 dark:placeholder:text-zinc-600 focus:ring-2 focus:ring-blue-500 transition-all uppercase"
              />
            </div>
          )}

          {isExisting && (
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <label className="text-xs font-semibold uppercase tracking-wider text-zinc-400 dark:text-zinc-500 ml-1">Trade Type</label>
                <select 
                  value={formData.type}
                  onChange={(e) => setFormData({ ...formData, type: e.target.value })}
                  className="w-full bg-zinc-100 dark:bg-zinc-800 border-none rounded-2xl px-5 py-4 font-bold text-zinc-900 dark:text-white focus:ring-2 focus:ring-blue-500 appearance-none bg-[url('data:image/svg+xml;charset=utf-8,%3Csvg%20xmlns%3D%22http%3A%2F%2Fwww.w3.org%2F2000%2Fsvg%22%20fill%3D%22none%22%20viewBox%3D%220%200%2020%2020%22%3E%3Cpath%20stroke%3D%22%236b7280%22%20stroke-linecap%3D%22round%22%20stroke-linejoin%3D%22round%22%20stroke-width%3D%221.5%22%20d%3D%22m6%208%204%204%204-4%22%2F%3E%3C%2Fsvg%3E')] bg-[length:1.25em_1.25em] bg-[right_1rem_center] bg-no-repeat"
                >
                  <option value="Buy">Buy</option>
                  <option value="Sell">Sell</option>
                  <option value="Dividend">Dividend</option>
                  <option value="Split">Split</option>
                </select>
              </div>
              <div className="space-y-2">
                <label className="text-xs font-semibold uppercase tracking-wider text-zinc-400 dark:text-zinc-500 ml-1">Quantity</label>
                <input 
                  type="number"
                  step="any"
                  required
                  min="0.00000001"
                  value={formData.quantity}
                  onChange={(e) => setFormData({ ...formData, quantity: parseFloat(e.target.value) })}
                  className="w-full bg-zinc-100 dark:bg-zinc-800 border-none rounded-2xl px-5 py-4 font-bold text-zinc-900 dark:text-white focus:ring-2 focus:ring-blue-500 transition-all"
                />
              </div>
            </div>
          )}

          {!isExisting && (
             <div className="space-y-2">
                <label className="text-xs font-semibold uppercase tracking-wider text-zinc-400 dark:text-zinc-500 ml-1">Quantity</label>
                <input 
                  type="number"
                  step="any"
                  required
                  min="0.00000001"
                  value={formData.quantity}
                  onChange={(e) => setFormData({ ...formData, quantity: parseFloat(e.target.value) })}
                  className="w-full bg-zinc-100 dark:bg-zinc-800 border-none rounded-2xl px-5 py-4 font-bold text-zinc-900 dark:text-white focus:ring-2 focus:ring-blue-500 transition-all"
                />
              </div>
          )}

          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <label className="text-xs font-semibold uppercase tracking-wider text-zinc-400 dark:text-zinc-500 ml-1">Unit Price ($)</label>
              <input 
                type="number"
                step="0.01"
                required
                value={formData.unitPrice}
                onChange={(e) => setFormData({ ...formData, unitPrice: parseFloat(e.target.value) })}
                className="w-full bg-zinc-100 dark:bg-zinc-800 border-none rounded-2xl px-5 py-4 font-bold text-zinc-900 dark:text-white focus:ring-2 focus:ring-blue-500 transition-all"
              />
            </div>
            <div className="space-y-2">
              <label className="text-xs font-semibold uppercase tracking-wider text-zinc-400 dark:text-zinc-500 ml-1">Trade Date</label>
              <input 
                type="date"
                required
                value={formData.tradedAt}
                onChange={(e) => setFormData({ ...formData, tradedAt: e.target.value })}
                className="w-full bg-zinc-100 dark:bg-zinc-800 border-none rounded-2xl px-5 py-4 font-bold text-zinc-900 dark:text-white focus:ring-2 focus:ring-blue-500 transition-all appearance-none"
              />
            </div>
          </div>

          <div className="space-y-2">
            <label className="text-xs font-semibold uppercase tracking-wider text-zinc-400 dark:text-zinc-500 ml-1">Notes (Optional)</label>
            <textarea 
              value={formData.notes}
              onChange={(e) => setFormData({ ...formData, notes: e.target.value })}
              className="w-full bg-zinc-100 dark:bg-zinc-800 border-none rounded-2xl px-5 py-4 font-bold text-zinc-900 dark:text-white focus:ring-2 focus:ring-blue-500 transition-all h-24 resize-none"
              placeholder="Record details about this trade..."
            />
          </div>

          <div className="flex gap-4 pt-4">
            <button 
              type="button"
              onClick={onClose}
              className="flex-1 px-6 py-4 bg-zinc-100 dark:bg-zinc-800 text-zinc-500 hover:text-zinc-900 dark:hover:text-white font-semibold rounded-2xl transition-all uppercase tracking-wider text-xs"
            >
              Cancel
            </button>
            <button 
              type="submit"
              disabled={loading}
              className="flex-[2] px-6 py-4 bg-blue-600 hover:bg-blue-700 text-white font-semibold rounded-2xl shadow-lg shadow-blue-500/20 transition-all active:scale-95 disabled:opacity-50 uppercase tracking-wider text-xs"
            >
              {loading ? "SAVING..." : (isExisting ? "RECORD TRADE" : "OPEN POSITION")}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

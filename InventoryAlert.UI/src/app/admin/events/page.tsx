'use client'

import { useState, useEffect } from "react";
import { fetchApi } from "@/lib/api";

interface Product {
  id: number;
  name: string;
  tickerSymbol: string;
}

export default function AdminEvents() {
  const [products, setProducts] = useState<Product[]>([]);
  const [marketSymbol, setMarketSymbol] = useState("");
  const [marketProductId, setMarketProductId] = useState<number | "">("");
  const [newsSymbol, setNewsSymbol] = useState("");
  const [loading, setLoading] = useState(false);
  const [triggerStatus, setTriggerStatus] = useState<{ type: 'success' | 'error', message: string } | null>(null);

  useEffect(() => {
    async function loadProducts() {
      try {
        const data = await fetchApi("/api/v1/products");
        setProducts(data.items || data.Items || []); // Handle different casing from API
      } catch (err) {
        console.error("Failed to load products for triggers", err);
      }
    }
    loadProducts();
  }, []);

  const handleTriggerMarketAlert = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setTriggerStatus(null);
    try {
      await fetchApi("/api/v1/events/market-alert", {
        method: "POST",
        body: JSON.stringify({ productId: marketProductId, symbol: marketSymbol })
      });
      setTriggerStatus({ type: 'success', message: `MarketPriceAlert triggered for ${marketSymbol}` });
      setMarketSymbol("");
      setMarketProductId("");
    } catch (err: any) {
      setTriggerStatus({ type: 'error', message: err.message });
    } finally {
      setLoading(false);
    }
  };

  const handleTriggerNewsAlert = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setTriggerStatus(null);
    try {
      await fetchApi("/api/v1/events/news-alert", {
        method: "POST",
        body: JSON.stringify({ symbol: newsSymbol })
      });
      setTriggerStatus({ type: 'success', message: `CompanyNewsAlert triggered for ${newsSymbol}` });
      setNewsSymbol("");
    } catch (err: any) {
      setTriggerStatus({ type: 'error', message: err.message });
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="max-w-7xl mx-auto flex flex-col lg:grid lg:grid-cols-[1fr_350px] gap-8 h-full">
      <div className="flex flex-col gap-8 min-h-0">
        <div className="flex flex-col md:flex-row md:items-end justify-between gap-4">
          <div>
            <h1 className="text-5xl font-black tracking-tighter uppercase">Service Event Registry</h1>
            <p className="text-zinc-500 font-medium mt-2 text-lg">System-wide event bus monitoring and manual trigger control.</p>
          </div>
        </div>

        <div className="bg-zinc-900 border border-white/5 rounded-3xl flex-1 flex flex-col overflow-hidden shadow-2xl relative">
          <div className="p-8 border-b border-white/5 bg-black/20 flex items-center justify-between sticky top-0 z-10 backdrop-blur-md">
            <h3 className="font-black text-xl tracking-tight uppercase">Recent Logs</h3>
            <div className="flex gap-4">
              <span className="text-[10px] font-black uppercase tracking-widest text-zinc-500 bg-zinc-800/50 px-2 py-1 rounded">Filtering: ALL</span>
            </div>
          </div>
          
          <div className="overflow-auto flex-1">
            <table className="w-full text-left border-collapse">
              <thead>
                <tr className="bg-zinc-900/50 text-[10px] font-black uppercase tracking-[0.2em] text-zinc-500 border-b border-white/5">
                  <th className="p-6 font-bold">Timestamp</th>
                  <th className="p-6 font-bold text-center">Status</th>
                  <th className="p-6 font-bold">Component</th>
                  <th className="p-6 font-bold">Payload Overview</th>
                  <th className="p-6 font-bold text-right pr-8">Details</th>
                </tr>
              </thead>
              <tbody className="text-sm font-mono">
                {[
                  { time: '2026-04-08 17:01:45', status: 'SUCCESS', component: 'SNS.PUBLISHER', payload: 'Published MarketPriceAlert for AAPL' },
                  { time: '2026-04-08 16:45:10', status: 'INFO', component: 'AUTH.SERVICE', payload: 'System-admin session initiated' },
                  { time: '2026-04-08 16:32:01', status: 'SUCCESS', component: 'PRICE.WORKER', payload: 'Batch update success (254 items)' },
                  { time: '2026-04-08 16:31:55', status: 'ERROR', component: 'FINNHUB.API', payload: 'Connection reset by peer' },
                ].map((row, i) => (
                  <tr key={i} className="border-b border-white/5 hover:bg-white/2 transition-colors group">
                    <td className="p-6 text-zinc-500 text-xs">{row.time}</td>
                    <td className="p-6">
                      <div className="flex justify-center">
                        <span className={`px-2 py-0.5 rounded text-[10px] font-black tracking-widest ${row.status === 'ERROR' ? 'bg-rose-500/10 text-rose-400' : row.status === 'INFO' ? 'bg-blue-500/10 text-blue-400' : 'bg-emerald-500/10 text-emerald-400'}`}>
                          {row.status}
                        </span>
                      </div>
                    </td>
                    <td className="p-6">
                      <div className="font-bold text-zinc-300 group-hover:text-blue-400 transition-colors uppercase py-0.5 px-2 bg-zinc-800/50 rounded w-fit text-[10px]">
                        {row.component}
                      </div>
                    </td>
                    <td className="p-6">
                        <p className="text-zinc-400 line-clamp-1">{row.payload}</p>
                    </td>
                    <td className="p-6 text-right pr-8">
                       <button className="text-zinc-600 hover:text-white transition-colors text-xs font-black uppercase tracking-widest">View Raw</button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      </div>

      <div className="flex flex-col gap-6 sticky top-0 h-fit">
        <div className="bg-zinc-900 border border-white/5 rounded-3xl p-8 shadow-2xl">
          <h3 className="font-black text-xl mb-6 tracking-tight uppercase">Manual Triggers</h3>
          
          {triggerStatus && (
            <div className={`mb-6 p-4 rounded-2xl text-xs font-bold ${triggerStatus.type === 'success' ? 'bg-emerald-500/10 text-emerald-400 border border-emerald-500/20' : 'bg-rose-500/10 text-rose-400 border border-rose-500/20'}`}>
              {triggerStatus.message}
            </div>
          )}

          <div className="space-y-10">
            {/* Market Alert Trigger */}
            <form onSubmit={handleTriggerMarketAlert} className="space-y-4">
              <div className="flex items-center justify-between mb-2">
                <span className="text-[10px] font-black uppercase tracking-[0.2em] text-zinc-500">Market Price Alert</span>
                <span className="w-1.5 h-1.5 rounded-full bg-blue-500"></span>
              </div>
              <div className="space-y-3">
                <select 
                  required
                  className="w-full bg-zinc-800 border border-white/5 rounded-2xl px-4 py-3 text-sm text-white focus:outline-none focus:ring-2 focus:ring-blue-500 transition-all font-bold"
                  value={marketProductId}
                  onChange={(e) => setMarketProductId(Number(e.target.value))}
                >
                  <option value="">Select Product...</option>
                  {products.map(p => (
                    <option key={p.id} value={p.id}>{p.name} ({p.tickerSymbol})</option>
                  ))}
                </select>
                <input 
                  required
                  type="text" 
                  placeholder="Ticker Symbol (e.g. AAPL)" 
                  className="w-full bg-zinc-800 border border-white/5 rounded-2xl px-4 py-3 text-sm text-white focus:outline-none focus:ring-2 focus:ring-blue-500 transition-all font-bold uppercase"
                  value={marketSymbol}
                  onChange={(e) => setMarketSymbol(e.target.value)}
                />
              </div>
              <button 
                type="submit"
                disabled={loading}
                className="w-full py-4 bg-white text-black font-black rounded-2xl text-xs uppercase tracking-[0.2em] hover:bg-zinc-200 transition-all active:scale-[0.98] shadow-xl disabled:opacity-50"
              >
                {loading ? "PROcessing..." : "Publish Market-Alert"}
              </button>
            </form>

            <div className="h-px bg-white/5"></div>

            {/* News Alert Trigger */}
            <form onSubmit={handleTriggerNewsAlert} className="space-y-4">
               <div className="flex items-center justify-between mb-2">
                <span className="text-[10px] font-black uppercase tracking-[0.2em] text-zinc-500">Company News Sync</span>
                <span className="w-1.5 h-1.5 rounded-full bg-indigo-500"></span>
              </div>
              <input 
                required
                type="text" 
                placeholder="Ticker Symbol (e.g. MSFT)" 
                className="w-full bg-zinc-800 border border-white/5 rounded-2xl px-4 py-3 text-sm text-white focus:outline-none focus:ring-2 focus:ring-indigo-500 transition-all font-bold uppercase"
                value={newsSymbol}
                onChange={(e) => setNewsSymbol(e.target.value)}
              />
              <button 
                type="submit"
                disabled={loading}
                className="w-full py-4 bg-indigo-600 text-white font-black rounded-2xl text-xs uppercase tracking-[0.2em] hover:bg-indigo-500 transition-all active:scale-[0.98] shadow-xl shadow-indigo-900/20 disabled:opacity-50"
              >
                {loading ? "Syncing..." : "Trigger News-Sync"}
              </button>
            </form>
          </div>
        </div>

        <div className="bg-zinc-900 border border-white/5 rounded-3xl p-8 flex items-center gap-4">
           <div className="w-12 h-12 rounded-2xl bg-zinc-800 flex items-center justify-center text-xl shadow-inner">
             🔌
           </div>
           <div>
             <p className="text-white font-black uppercase text-xs tracking-widest">SNS Connection</p>
             <p className="text-zinc-500 text-[10px] font-bold uppercase tracking-tight">Active & Scalable</p>
           </div>
        </div>
      </div>
    </div>
  );
}

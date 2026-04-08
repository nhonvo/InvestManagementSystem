'use client'

import { useState, useEffect } from "react";
import { fetchApi } from "@/lib/api";

interface MarketNews {
  headline: string;
  source: string;
  datetime: number;
  summary: string;
  category: string;
  url: string;
  image: string;
}

interface MarketStatus {
  exchange: string;
  holiday: string | null;
  isOpen: boolean;
  session: string;
  timezone: string;
}

export default function MarketOverview() {
  const [news, setNews] = useState<MarketNews[]>([]);
  const [status, setStatus] = useState<MarketStatus | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    async function loadMarketData() {
      try {
        const [newsData, statusData] = await Promise.all([
          fetchApi("/api/v1/market/news?limit=10"),
          fetchApi("/api/v1/market/status?exchange=US")
        ]);
        setNews(newsData);
        setStatus(statusData);
      } catch (err) {
        console.error("Failed to load market data", err);
      } finally {
        setLoading(false);
      }
    }
    loadMarketData();
  }, []);

  return (
    <div className="flex flex-col gap-10 max-w-7xl mx-auto h-full">
      <div className="flex flex-col md:flex-row md:items-end justify-between gap-4 py-2">
        <div>
          <h1 className="text-5xl font-black tracking-tighter uppercase">Market Pulse</h1>
          <p className="text-zinc-500 font-medium mt-2 text-lg">Global financial overview and real-time news stream.</p>
        </div>
        <div className="flex gap-4">
          <div className={`flex items-center gap-3 px-6 py-3 rounded-2xl border font-bold text-sm tracking-tight transition-all shadow-lg ${status?.isOpen ? 'bg-emerald-500/10 border-emerald-500/20 text-emerald-400' : 'bg-rose-500/10 border-rose-500/20 text-rose-400'}`}>
            <span className={`w-2 h-2 rounded-full animate-pulse ${status?.isOpen ? 'bg-emerald-500 shadow-[0_0_8px_rgba(16,185,129,0.5)]' : 'bg-rose-500 shadow-[0_0_8px_rgba(244,63,94,0.5)]'}`}></span>
            {status?.isOpen ? 'US MARKET OPEN' : 'US MARKET CLOSED'}
          </div>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {[
          { name: "S&P 500", value: "5,137.08", change: "+0.80%", up: true },
          { name: "NASDAQ 100", value: "18,302.91", change: "+1.14%", up: true },
          { name: "DOW JONES", value: "38,989.83", change: "-0.06%", up: false },
        ].map((index) => (
          <div key={index.name} className="bg-zinc-900 border border-white/5 rounded-3xl p-8 relative overflow-hidden group hover:border-white/10 transition-colors">
            <div className={`absolute top-0 right-0 w-32 h-32 opacity-[0.03] group-hover:opacity-[0.05] transition-opacity ${index.up ? 'text-emerald-500' : 'text-rose-500'}`}>
              {index.up ? '▲' : '▼'}
            </div>
            <p className="text-zinc-400 text-xs font-black uppercase tracking-widest mb-2">{index.name}</p>
            <p className="text-3xl font-black text-white mb-2">{index.value}</p>
            <p className={`text-sm font-bold ${index.up ? 'text-emerald-400' : 'text-rose-400'}`}>{index.change}</p>
          </div>
        ))}
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8 flex-1 min-h-0">
        <div className="lg:col-span-2 bg-zinc-900 border border-white/5 rounded-3xl flex flex-col overflow-hidden shadow-2xl">
          <div className="p-8 border-b border-white/5 bg-black/20 flex items-center justify-between">
            <h3 className="font-black text-xl tracking-tight uppercase">Headlines</h3>
            <span className="text-[10px] bg-zinc-800 text-zinc-400 px-2 py-1 rounded font-bold tracking-widest uppercase">Live Stream</span>
          </div>
          <div className="p-2 flex-1 overflow-auto">
             {loading ? (
               <div className="p-6 space-y-6">
                 {[1, 2, 3].map(i => <div key={i} className="h-24 bg-zinc-800/50 animate-pulse rounded-2xl"></div>)}
               </div>
             ) : (
               news.map((item, i) => (
                 <a 
                   key={i} 
                   href={item.url} 
                   target="_blank" 
                   rel="noopener noreferrer"
                   className="flex gap-6 p-6 rounded-2xl hover:bg-white/2 transition-all group"
                 >
                   <div className="w-24 h-24 bg-zinc-800 rounded-2xl shrink-0 overflow-hidden relative shadow-lg">
                     {item.image ? (
                       <img src={item.image} alt="" className="w-full h-full object-cover group-hover:scale-110 transition-transform duration-500" />
                     ) : (
                       <div className="w-full h-full bg-linear-to-br from-zinc-700 to-zinc-800 flex items-center justify-center text-zinc-600 text-xs font-bold">NEWS</div>
                     )}
                   </div>
                   <div className="flex flex-col justify-center">
                     <p className="font-black text-lg leading-tight mb-2 group-hover:text-blue-400 transition-colors line-clamp-2">{item.headline}</p>
                     <p className="text-sm text-zinc-500 line-clamp-2 mb-3 font-medium">{item.summary}</p>
                     <div className="flex gap-3 text-[10px] items-center text-zinc-500 font-black uppercase tracking-widest">
                       <span className="text-blue-500">{item.source}</span>
                       <span className="w-1 h-1 bg-zinc-700 rounded-full"></span>
                       <span>{new Date(item.datetime * 1000).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</span>
                     </div>
                   </div>
                 </a>
               ))
             )}
          </div>
        </div>

        <div className="bg-zinc-900 border border-white/5 rounded-3xl flex flex-col overflow-hidden shadow-2xl">
          <div className="p-8 border-b border-white/5 bg-black/20">
            <h3 className="font-black text-xl tracking-tight uppercase">Market Events</h3>
          </div>
          <div className="p-8 space-y-6">
             <div className="space-y-4">
                <p className="text-zinc-500 text-[10px] font-black uppercase tracking-[0.2em] mb-4">Earnings Calendar</p>
                <div className="group cursor-pointer">
                  <div className="flex justify-between items-center mb-1">
                    <span className="font-black text-lg text-white group-hover:text-blue-400 transition-colors">NVDA</span>
                    <span className="text-[10px] font-black bg-blue-500/10 text-blue-400 px-2 py-0.5 rounded tracking-widest uppercase">Today</span>
                  </div>
                  <p className="text-xs text-zinc-500 font-medium uppercase tracking-tight">After Market Close</p>
                </div>
                <div className="h-px bg-white/5"></div>
                <div className="group cursor-pointer">
                   <div className="flex justify-between items-center mb-1">
                    <span className="font-black text-lg text-white group-hover:text-blue-400 transition-colors">CRWD</span>
                    <span className="text-[10px] font-black bg-zinc-800 text-zinc-500 px-2 py-0.5 rounded tracking-widest uppercase">Aug 24</span>
                  </div>
                  <p className="text-xs text-zinc-500 font-medium uppercase tracking-tight">Pre-Market Open</p>
                </div>
             </div>

             <div className="pt-8 space-y-4">
                <p className="text-zinc-500 text-[10px] font-black uppercase tracking-[0.2em] mb-4">Exchange Status</p>
                <div className="flex items-center justify-between">
                  <span className="text-sm font-bold text-zinc-400 uppercase tracking-tight">Timezone</span>
                  <span className="text-sm font-black text-white">{status?.timezone}</span>
                </div>
                <div className="flex items-center justify-between">
                  <span className="text-sm font-bold text-zinc-400 uppercase tracking-tight">Session</span>
                  <span className="text-sm font-black text-white uppercase">{status?.session}</span>
                </div>
             </div>
          </div>
        </div>
      </div>
    </div>
  );
}

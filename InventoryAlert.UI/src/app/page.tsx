'use client'

import { useState, useEffect } from "react";
import Link from "next/link";
import { fetchApi } from "@/lib/api";
import { AddSymbolModal } from "@/components/AddSymbolModal";

interface WatchlistItem {
  symbol: string;
  name: string;
  currentPrice: number;
  change: number;
}

interface NewsItem {
  headline: string;
  source: string;
  dateTime: number;
  url: string;
  image: string;
}

export default function Dashboard() {
  const [watchlist, setWatchlist] = useState<WatchlistItem[]>([]);
  const [news, setNews] = useState<NewsItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [isModalOpen, setIsModalOpen] = useState(false);

  const loadDashboardData = async () => {
    try {
      const [watchlistData, newsData] = await Promise.all([
        fetchApi("/api/v1/watchlist"),
        fetchApi("/api/v1/market/news?limit=5")
      ]);
      setWatchlist(watchlistData);
      setNews(newsData);
    } catch (err: any) {
      setError(err.message || "Failed to load dashboard data");
    } finally {
      setLoading(false);
    }
  };

  const removeFromWatchlist = async (e: React.MouseEvent, symbol: string) => {
    e.preventDefault();
    e.stopPropagation();
    
    if (!confirm(`Are you sure you want to remove ${symbol} from your watchlist?`)) return;

    try {
      await fetchApi(`/api/v1/watchlist/${symbol}`, {
        method: 'DELETE'
      });
      loadDashboardData();
    } catch (err: any) {
      alert(err.message || "Failed to remove symbol");
    }
  };

  useEffect(() => {
    loadDashboardData();
  }, []);

  return (
    <>
      <AddSymbolModal 
        isOpen={isModalOpen} 
        onClose={() => setIsModalOpen(false)} 
        onSuccess={loadDashboardData} 
      />
      <div className="flex flex-col lg:flex-row gap-6 h-full">
        <div className="flex-1 flex flex-col gap-6">
          <div className="flex items-center justify-between">
            <div>
              <h2 className="text-2xl font-bold tracking-tight">Your Watchlist</h2>
              <p className="text-zinc-500 text-sm mt-1">Real-time performance of your saved symbols.</p>
            </div>
            <button 
              onClick={() => setIsModalOpen(true)}
              className="bg-blue-600 hover:bg-blue-700 text-sm px-5 py-2.5 rounded-xl font-bold transition-all shadow-lg shadow-blue-500/20 active:scale-95"
            >
              + Add Symbol
            </button>
          </div>
          
          {loading ? (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              {[1, 2, 3].map((i) => (
                <div key={i} className="bg-zinc-900/50 animate-pulse border border-white/5 rounded-2xl p-6 h-32"></div>
              ))}
            </div>
          ) : error ? (
            <div className="bg-rose-500/10 border border-rose-500/20 text-rose-400 p-6 rounded-2xl text-center">
              {error}
            </div>
          ) : watchlist.length === 0 ? (
            <div className="bg-zinc-900 border border-white/5 p-12 rounded-2xl text-center">
              <p className="text-zinc-400 mb-4">Your watchlist is empty.</p>
              <Link href="/market" className="text-blue-400 hover:underline">Explore market to add symbols</Link>
            </div>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              {watchlist.map((item) => (
                <Link key={item.symbol} href={`/stocks/${item.symbol.toLowerCase()}`}>
                  <div className="bg-zinc-900 border border-white/5 rounded-2xl p-6 hover:border-blue-500/50 hover:bg-zinc-800/50 transition-all cursor-pointer group relative overflow-hidden">
                    <div className="absolute top-2 right-2 flex gap-1 opacity-0 group-hover:opacity-100 transition-opacity z-10">
                      <button 
                        onClick={(e) => removeFromWatchlist(e, item.symbol)}
                        className="w-8 h-8 rounded-full bg-rose-500/10 flex items-center justify-center text-rose-400 hover:bg-rose-500 hover:text-white transition-all"
                        title="Remove from watchlist"
                      >
                        ×
                      </button>
                      <div className="w-8 h-8 rounded-full bg-blue-500/10 flex items-center justify-center text-blue-400">
                        →
                      </div>
                    </div>
                    <div className="flex justify-between items-start mb-6">
                      <div>
                        <h3 className="font-bold text-xl group-hover:text-blue-400 transition-colors uppercase">{item.symbol}</h3>
                        <p className="text-sm text-zinc-400 line-clamp-1">{item.name}</p>
                      </div>
                      <div className="text-right">
                        <p className="font-bold text-lg">${(item.currentPrice || 0).toFixed(2)}</p>
                        <p className={`text-sm font-semibold ${(item.change || 0) >= 0 ? "text-emerald-400" : "text-rose-400"}`}>
                          {(item.change || 0) >= 0 ? "+" : ""}{(item.change || 0).toFixed(2)}%
                        </p>
                      </div>
                    </div>
                    <div className="h-2 w-full bg-zinc-800/50 rounded-full overflow-hidden">
                      <div 
                        className={`h-full ${(item.change || 0) >= 0 ? "bg-emerald-500" : "bg-rose-500"}`}
                        style={{ width: `${Math.min(Math.abs(item.change || 0) * 10, 100)}%` }}
                      />
                    </div>
                  </div>
                </Link>
              ))}
            </div>
          )}
        </div>

        <div className="w-full lg:w-80 flex flex-col gap-6 border-t lg:border-t-0 lg:border-l border-white/10 pt-6 lg:pt-0 lg:pl-6">
          <div className="flex items-center justify-between">
            <h3 className="font-bold text-lg tracking-tight">Market News</h3>
            <Link href="/market" className="text-xs text-blue-400 hover:underline">View All</Link>
          </div>
          <div className="flex flex-col gap-5">
            {loading ? (
              [1, 2, 3].map((i) => (
                <div key={i} className="h-20 bg-zinc-900/50 animate-pulse rounded-xl border border-white/5"></div>
              ))
            ) : (
              news.slice(0, 5).map((item, i) => (
                <a 
                  key={i} 
                  href={item.url} 
                  target="_blank" 
                  rel="noopener noreferrer" 
                  className="flex gap-4 group cursor-pointer pb-2 border-b border-white/5 last:border-0 hover:border-white/10 transition-colors"
                >
                  <div className="w-20 h-20 bg-zinc-800 rounded-xl shrink-0 overflow-hidden relative">
                    {item.image ? (
                      <img src={item.image} alt="" className="w-full h-full object-cover" />
                    ) : (
                      <div className="absolute inset-0 bg-blue-500/10 flex items-center justify-center text-blue-500/30 text-[10px] font-bold">NEWS</div>
                    )}
                  </div>
                  <div className="flex flex-col justify-between py-0.5">
                    <p className="text-sm font-bold leading-snug line-clamp-2 group-hover:text-blue-400 transition-colors uppercase tracking-tight">{item.headline}</p>
                    <div className="flex gap-2 text-[10px] items-center text-zinc-500 mt-2 font-black uppercase tracking-widest">
                      <span className="text-blue-500">{item.source}</span>
                      <span>•</span>
                      <span>{new Date(item.dateTime * 1000).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</span>
                    </div>
                  </div>
                </a>
              ))
            )}
          </div>
          
          <div className="mt-4 p-5 rounded-2xl bg-linear-to-br from-blue-600 to-indigo-700 shadow-xl shadow-blue-900/20">
            <h4 className="font-bold text-white mb-1">Upgrade to Pro</h4>
            <p className="text-blue-100 text-xs leading-relaxed mb-4">Get detailed financial deep dives and unlimited alerts.</p>
            <button className="w-full py-2.5 bg-white text-blue-600 rounded-xl text-xs font-bold hover:bg-blue-50 transition-colors">Learn More</button>
          </div>
        </div>
      </div>
    </>
  );
}

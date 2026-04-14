'use client'

import { useState, useEffect } from "react";
import Link from "next/link";
import { fetchApi } from "@/lib/api";
import { AddSymbolModal } from "@/components/AddSymbolModal";
import { Toast } from "@/components/Toast";
import { ConfirmDialog } from "@/components/ConfirmDialog";

type ToastType = 'success' | 'error' | 'info';

interface WatchlistItem {
  symbol: string;
  name: string;
  currentPrice: number;
  change: number;
  logo?: string;
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
  const [portfolioSummary, setPortfolioSummary] = useState<{totalValue: number, totalReturn: number, returnPercent: number} | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [syncingNews, setSyncingNews] = useState(false);
  
  // New UI states
  const [toast, setToast] = useState<{message: string, type: ToastType} | null>(null);
  const [confirmDelete, setConfirmDelete] = useState<string | null>(null);

  const loadDashboardData = async () => {
    try {
      const [watchlistData, newsData, portfolioData] = await Promise.all([
        fetchApi("/api/v1/watchlist"),
        fetchApi("/api/v1/market/news?category=general&page=1"),
        fetchApi("/api/v1/portfolio/positions")
      ]);
      setWatchlist(watchlistData || []);
      setNews(newsData || []);
      
      if (portfolioData && portfolioData.items) {
          const items = portfolioData.items;
          const totalValue = items.reduce((acc: number, p: any) => acc + p.marketValue, 0);
          const totalCost = items.reduce((acc: number, p: any) => acc + p.totalCost, 0);
          const totalReturn = totalValue - totalCost;
          const returnPercent = totalCost > 0 ? (totalReturn / totalCost) * 100 : 0;
          setPortfolioSummary({ totalValue, totalReturn, returnPercent });
      }
    } catch (err: any) {
      setError(err.message || "Failed to load dashboard data");
    } finally {
      setLoading(false);
    }
  };

  const syncNews = async () => {
    setSyncingNews(true);
    try {
        await fetchApi("/api/v1/events", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ 
              eventType: "inventoryalert.news.sync-requested.v1", 
              payload: {} 
          })
        });
        
        setToast({ message: "Sync job enqueued. News will update shortly.", type: 'success' });
    } catch (err: any) {
        setToast({ message: err.message || "Failed to enqueue sync job", type: 'error' });
    } finally {
        setTimeout(() => setSyncingNews(false), 2000);
    }
  };

  const removeFromWatchlist = async (symbol: string) => {
    try {
      await fetchApi(`/api/v1/watchlist/${symbol}`, {
        method: 'DELETE'
      });
      loadDashboardData();
      setToast({ message: `${symbol} removed from watchlist`, type: 'success' });
    } catch (err: any) {
      setToast({ message: err.message || "Failed to remove symbol", type: 'error' });
    } finally {
      setConfirmDelete(null);
    }
  };

  const onAddSuccess = () => {
    loadDashboardData();
    setToast({ message: "Symbol added successfully", type: 'success' });
  };

  useEffect(() => {
    loadDashboardData();
  }, []);

  return (
    <>
      <AddSymbolModal 
        isOpen={isModalOpen} 
        onClose={() => setIsModalOpen(false)} 
        onSuccess={onAddSuccess} 
      />
      
      {toast && (
        <Toast 
          message={toast.message} 
          type={toast.type} 
          onClose={() => setToast(null)} 
        />
      )}

      <ConfirmDialog
        isOpen={!!confirmDelete}
        title="Remove Symbol"
        message={`Are you sure you want to remove ${confirmDelete} from your watchlist? This action cannot be undone.`}
        confirmText="Remove"
        type="danger"
        onConfirm={() => confirmDelete && removeFromWatchlist(confirmDelete)}
        onCancel={() => setConfirmDelete(null)}
      />

      <div className="flex flex-col gap-10">
        {portfolioSummary && (
          <div className="bg-white/60 dark:bg-zinc-900/40 backdrop-blur-3xl border border-white/40 dark:border-white/10 rounded-[2.5rem] p-8 md:p-12 flex flex-col md:flex-row items-center justify-between gap-8 relative overflow-hidden shadow-2xl dark:shadow-black/50 transition-all hover:border-blue-500/30 group">
            <div className="absolute top-0 right-0 w-[400px] h-[400px] bg-blue-500/10 blur-[120px] rounded-full -mr-20 -mt-20 group-hover:bg-blue-500/15 transition-all duration-700 pointer-events-none"></div>
            <div className="flex-1 space-y-2 relative z-10 w-full text-center md:text-left">
                <p className="text-zinc-500 text-xs font-sm uppercase tracking-widest text-blue-500/80">Net Portfolio Value</p>
                <h1 className="text-4xl md:text-6xl font-black text-zinc-900 dark:text-white tracking-tighter">
                    ${portfolioSummary.totalValue.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                </h1>
            </div>
            <div className="flex flex-col items-center md:items-end gap-1 relative z-10 w-full md:w-auto">
                <p className="text-zinc-500 text-xs font-semibold uppercase tracking-wide">Total Returns</p>
                <div className="flex items-center justify-center md:justify-end gap-3 w-full">
                    <span className={`text-2xl font-bold ${portfolioSummary.totalReturn >= 0 ? 'text-emerald-500' : 'text-rose-500'}`}>
                        {portfolioSummary.totalReturn >= 0 ? '+' : ''}${Math.abs(portfolioSummary.totalReturn).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                    </span>
                    <span className={`px-4 py-1.5 rounded-xl text-sm font-bold shadow-sm ${portfolioSummary.totalReturn >= 0 ? 'bg-emerald-500/10 text-emerald-600 dark:text-emerald-400' : 'bg-rose-500/10 text-rose-600 dark:text-rose-400'}`}>
                        {portfolioSummary.returnPercent.toFixed(2)}%
                    </span>
                </div>
            </div>
            <div className="h-24 w-px bg-zinc-200 dark:bg-white/10 hidden md:block relative z-10"></div>
            <Link href="/portfolio" className="relative z-10 w-full md:w-auto">
                <button className="w-full md:w-auto bg-gradient-to-r from-zinc-900 to-zinc-700 dark:from-white dark:to-zinc-200 text-white dark:text-black px-10 py-5 rounded-2xl text-sm font-bold uppercase tracking-widest hover:scale-[1.02] active:scale-[0.98] hover:shadow-2xl dark:hover:shadow-white/20 transition-all duration-300">
                    Holdings →
                </button>
            </Link>
          </div>
        )}

      <div className="flex flex-col xl:flex-row gap-10 h-full">
        {/* Watchlist Section */}
        <div className="flex-1 flex flex-col gap-6">
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
            <div>
              <h2 className="text-3xl font-black tracking-tight text-zinc-900 dark:text-white">Your Watchlist</h2>
              <p className="text-zinc-500 dark:text-zinc-400 text-sm mt-1 font-medium">Real-time performance of your saved symbols.</p>
            </div>
            <button 
              onClick={() => setIsModalOpen(true)}
              className="bg-blue-600 hover:bg-blue-500 text-sm px-6 py-3 rounded-2xl font-bold text-white transition-all shadow-xl shadow-blue-500/20 hover:shadow-blue-500/40 active:scale-95 flex items-center justify-center gap-2"
            >
              <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={3}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M12 4v16m8-8H4" />
              </svg>
              Add Symbol
            </button>
          </div>
          
          {loading ? (
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              {[1, 2, 3, 4].map((i) => (
                <div key={i} className="bg-zinc-100 dark:bg-zinc-800/50 animate-pulse border border-zinc-200 dark:border-white/5 rounded-[2rem] p-6 h-36"></div>
              ))}
            </div>
          ) : error ? (
            <div className="bg-rose-500/10 border border-rose-500/20 text-rose-500 p-8 rounded-3xl text-center font-bold">
              {error}
            </div>
          ) : watchlist.length === 0 ? (
            <div className="bg-zinc-50/50 dark:bg-zinc-900/50 border-2 border-dashed border-zinc-200 dark:border-white/10 p-12 rounded-[2rem] text-center flex flex-col items-center justify-center">
              <div className="w-16 h-16 bg-zinc-200 dark:bg-zinc-800 rounded-full flex items-center justify-center mb-4">
                <svg className="w-8 h-8 text-zinc-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                </svg>
              </div>
              <p className="text-zinc-500 dark:text-zinc-400 mb-4 font-bold uppercase tracking-widest text-xs">Your watchlist is empty</p>
              <Link href="/market" className="text-blue-500 hover:text-blue-600 font-bold px-6 py-2 bg-blue-500/10 rounded-xl transition-colors">Explore Market</Link>
            </div>
          ) : (
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-2 xl:grid-cols-2 gap-4">
              {watchlist.map((item) => (
                <Link key={item.symbol} href={`/stocks/${item.symbol.toLowerCase()}`}>
                  <div className="bg-white dark:bg-[#111] border border-zinc-200 dark:border-white/5 rounded-[2rem] p-6 hover:border-blue-500/50 hover:bg-zinc-50 dark:hover:bg-zinc-900/80 transition-all duration-300 transform hover:-translate-y-1 hover:shadow-2xl dark:hover:shadow-blue-900/20 cursor-pointer group relative overflow-hidden h-full flex flex-col justify-between">
                    <div className="absolute top-3 right-3 flex gap-2 opacity-0 group-hover:opacity-100 transition-opacity z-10">
                      <button 
                        onClick={(e) => {
                          e.preventDefault();
                          e.stopPropagation();
                          setConfirmDelete(item.symbol);
                        }}
                        className="w-10 h-10 rounded-full bg-white dark:bg-zinc-800 shadow-lg flex items-center justify-center text-zinc-400 hover:bg-rose-500 hover:text-white transition-all border border-zinc-100 dark:border-white/10"
                        title="Remove from watchlist"
                      >
                        <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2.5}><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
                      </button>
                    </div>
                    <div className="flex justify-between items-start mb-6">
                      <div className="flex gap-4 items-center">
                        {item.logo ? (
                          <div className="w-12 h-12 bg-white rounded-2xl flex items-center justify-center overflow-hidden p-2 shrink-0 border border-zinc-200 dark:border-white/10 shadow-sm">
                            <img src={item.logo} alt="" className="max-w-full max-h-full object-contain" />
                          </div>
                        ) : (
                          <div className="w-12 h-12 bg-zinc-100 dark:bg-zinc-800 rounded-2xl flex items-center justify-center shrink-0 border border-zinc-200 dark:border-white/10 shadow-sm text-zinc-500 font-bold text-xs uppercase">
                            {item.symbol.slice(0,2)}
                          </div>
                        )}
                        <div>
                          <h3 className="font-title font-black text-2xl group-hover:text-blue-500 dark:group-hover:text-blue-400 transition-colors uppercase text-zinc-900 dark:text-white tracking-tighter">{item.symbol}</h3>
                          <p className="text-xs font-semibold uppercase tracking-wider text-zinc-400 dark:text-zinc-500 line-clamp-1">{item.name}</p>
                        </div>
                      </div>
                      <div className="text-right">
                        <p className="font-bold text-xl text-zinc-900 dark:text-white tracking-tight leading-none">${(item.currentPrice || 0).toFixed(2)}</p>
                        <p className={`text-sm font-bold mt-1 ${(item.change || 0) >= 0 ? "text-emerald-500" : "text-rose-500"}`}>
                          {(item.change || 0) >= 0 ? "+" : ""}{(item.change || 0).toFixed(2)}%
                        </p>
                      </div>
                    </div>
                    <div className="h-2 w-full bg-zinc-100 dark:bg-zinc-800 rounded-full overflow-hidden">
                      <div 
                        className={`h-full transition-all duration-1000 ${(item.change || 0) >= 0 ? "bg-emerald-500" : "bg-rose-500"}`}
                        style={{ width: `${Math.min(Math.abs(item.change || 0) * 10, 100)}%` }}
                      />
                    </div>
                  </div>
                </Link>
              ))}
            </div>
          )}
        </div>

        {/* News Section */}
        <div className="w-full xl:w-96 flex flex-col gap-6 border-t xl:border-t-0 xl:border-l border-zinc-200 dark:border-white/10 pt-10 xl:pt-0 xl:pl-10 shrink-0">
          <div className="flex items-center justify-between">
            <h3 className="font-black text-sm uppercase tracking-widest text-zinc-900 dark:text-white flex items-center gap-2">
              Market News
              {syncingNews && (
                <svg className="w-4 h-4 animate-spin text-blue-500" fill="none" viewBox="0 0 24 24">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                </svg>
              )}
            </h3>
            <div className="flex gap-4 items-center">
                <button 
                  onClick={syncNews}
                  disabled={syncingNews}
                  className="text-xs font-bold uppercase tracking-wider text-zinc-500 hover:text-blue-500 transition-colors disabled:opacity-50 flex items-center gap-1"
                >
                  <svg className="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2.5}>
                    <path strokeLinecap="round" strokeLinejoin="round" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                  </svg>
                  Sync
                </button>
                <Link href="/market" className="text-xs font-bold uppercase tracking-wider text-blue-500 hover:text-blue-600 transition-colors">View All</Link>
            </div>
          </div>
          <div className="flex flex-col gap-4">
            {loading ? (
              [1, 2, 3, 4].map((i) => (
                <div key={i} className="h-24 bg-zinc-100 dark:bg-zinc-800/50 animate-pulse rounded-2xl border border-zinc-200 dark:border-white/5"></div>
              ))
            ) : news.length === 0 ? (
                <div className="text-center py-8 text-zinc-500 font-semibold text-sm">No news available</div>
            ) : (
              news.slice(0, 5).map((item, i) => (
                <a 
                  key={i} 
                  href={item.url} 
                  target="_blank" 
                  rel="noopener noreferrer" 
                  className="flex gap-4 group cursor-pointer p-3 rounded-2xl hover:bg-white dark:hover:bg-zinc-800/80 transition-all border border-transparent hover:border-zinc-200 dark:hover:border-white/10 hover:shadow-lg dark:hover:shadow-black/50"
                >
                  <div className="w-20 h-20 bg-zinc-100 dark:bg-zinc-900 rounded-xl shrink-0 overflow-hidden relative border border-zinc-200 dark:border-white/10">
                    {item.image ? (
                      <img src={item.image} alt="" className="w-full h-full object-cover group-hover:scale-110 transition-transform duration-500" />
                    ) : (
                      <div className="absolute inset-0 bg-blue-500/10 flex items-center justify-center text-blue-500/30 text-[10px] font-black uppercase">NEWS</div>
                    )}
                  </div>
                  <div className="flex flex-col justify-between py-0.5">
                    <p className="text-sm font-bold leading-tight line-clamp-2 group-hover:text-blue-500 dark:group-hover:text-blue-400 transition-colors text-zinc-900 dark:text-zinc-100 tracking-tight">{item.headline}</p>
                    <div className="flex gap-2 text-[10px] items-center text-zinc-400 dark:text-zinc-500 font-black uppercase tracking-widest mt-2">
                      <span className="text-blue-500 bg-blue-500/10 px-2 py-0.5 rounded-md">{item.source}</span>
                      <span>{new Date(item.dateTime * 1000).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</span>
                    </div>
                  </div>
                </a>
              ))
            )}
          </div>
        </div>
      </div>
    </div>
    </>
  );
}

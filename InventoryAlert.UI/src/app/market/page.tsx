'use client'

import { useState, useEffect } from "react";
import { fetchApi } from "@/lib/api";

// Spec §4.4 DTOs
interface MarketNews {
  id: number;
  headline: string;
  source: string;
  dateTime: number;
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

interface EarningsCalendar {
  symbol: string;
  date: string;
  epsEstimate: number | null;
  epsActual: number | null;
  revenueEstimate: number | null;
  revenueActual: number | null;
}

function formatDateParam(d: Date): string {
  return d.toISOString().split("T")[0];
}

export default function MarketOverview() {
  const [news, setNews] = useState<MarketNews[]>([]);
  const [statuses, setStatuses] = useState<MarketStatus[]>([]);
  const [earningsCalendar, setEarningsCalendar] = useState<EarningsCalendar[]>([]);
  const [loading, setLoading] = useState(true);
  const [newsCategory, setNewsCategory] = useState<string>("general");
  const [newsPage, setNewsPage] = useState<number>(1);
  const [syncingNews, setSyncingNews] = useState(false);
  const [toast, setToast] = useState<{ message: string; type: 'success' | 'error' } | null>(null);

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
        
        setToast({ message: "Sync job enqueued. Market news will update shortly.", type: 'success' });
    } catch (err: any) {
        setToast({ message: err.message || "Failed to enqueue sync job", type: 'error' });
    } finally {
        setTimeout(() => setSyncingNews(false), 2000);
    }
  };

  useEffect(() => {
    if (toast) {
      const timer = setTimeout(() => setToast(null), 3000);
      return () => clearTimeout(timer);
    }
  }, [toast]);

  const loadMarketData = async (category = "general") => {
    try {
      const now = new Date();
      const monthAhead = new Date(now);
      monthAhead.setDate(now.getDate() + 30);

      const [newsData, statusData, earningsData] = await Promise.all([
        fetchApi(`/api/v1/market/news?category=${category}&page=${newsPage}`),
        fetchApi("/api/v1/market/status"),
        // Spec §5.4: GET /market/calendar/earnings — free tier limited to 1-month
        fetchApi(
          `/api/v1/market/calendar/earnings?from=${formatDateParam(now)}&to=${formatDateParam(monthAhead)}`
        ).catch(() => []),
      ]);

      setNews(newsData?.items ?? newsData ?? []);
      setStatuses(statusData ?? []);
      setEarningsCalendar(earningsData ?? []);
    } catch (err) {
      console.error("Failed to load market data", err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadMarketData(newsCategory);
  }, [newsCategory, newsPage]);

  // Reset page when category changes
  useEffect(() => {
    setNewsPage(1);
  }, [newsCategory]);

  // Primary US market status
  const usStatus = statuses.find(
    (s) => s.exchange === "US" || s.exchange === "NYSE" || s.exchange.includes("US")
  ) ?? statuses[0] ?? null;

  const NEWS_CATEGORIES = ["general", "business", "forex", "crypto", "merger"];

  return (
    <div className="flex flex-col gap-10 max-w-7xl mx-auto h-full">
      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-end justify-between gap-4 py-2">
        <div>
          <h1 className="text-5xl font-semibold tracking-tight uppercase">Market Pulse</h1>
          <p className="text-zinc-500 font-medium mt-2 text-lg">Global financial overview and real-time news stream.</p>
        </div>
        <div className="flex gap-4">
          <button
            onClick={() => loadMarketData(newsCategory)}
            className="p-3 bg-zinc-900 border border-white/5 rounded-2xl text-zinc-400 hover:text-white hover:border-white/10 transition-all active:scale-95"
            title="Refresh View"
          >
            <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
            </svg>
          </button>
          <div
            className={`flex items-center gap-3 px-6 py-3 rounded-2xl border font-bold text-sm tracking-tight transition-all shadow-lg ${
              usStatus?.isOpen
                ? "bg-emerald-500/10 border-emerald-500/20 text-emerald-400"
                : "bg-rose-500/10 border-rose-500/20 text-rose-400"
            }`}
          >
            <span
              className={`w-2 h-2 rounded-full animate-pulse ${
                usStatus?.isOpen
                  ? "bg-emerald-500 shadow-[0_0_8px_rgba(16,185,129,0.5)]"
                  : "bg-rose-500 shadow-[0_0_8px_rgba(244,63,94,0.5)]"
              }`}
            />
            {usStatus?.isOpen ? "US MARKET OPEN" : "US MARKET CLOSED"}
          </div>
        </div>
      </div>

      {/* Exchange Status Grid */}
      {statuses.length > 0 && (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          {statuses.slice(0, 4).map((s) => (
            <div
              key={s.exchange}
              className={`rounded-3xl p-6 border relative overflow-hidden group transition-colors ${
                s.isOpen
                  ? "bg-emerald-500/5 border-emerald-500/20 hover:border-emerald-500/40"
                  : "bg-zinc-900 border-white/5 hover:border-white/10"
              }`}
            >
              <p className="text-zinc-400 text-xs font-semibold uppercase tracking-wider mb-1">{s.exchange}</p>
              <p className={`text-xl font-semibold mb-1 ${s.isOpen ? "text-emerald-400" : "text-zinc-300"}`}>
                {s.isOpen ? "OPEN" : "CLOSED"}
              </p>
              <p className="text-xs text-zinc-500 font-medium uppercase tracking-wider">
                {s.session} · {s.timezone}
              </p>
              {s.holiday && (
                <p className="text-xs text-amber-400 font-semibold mt-1 uppercase tracking-wider">
                  🎌 {s.holiday}
                </p>
              )}
            </div>
          ))}
        </div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8 flex-1 min-h-0 relative">
        {/* Toast Notification */}
        {toast && (
            <div className={`fixed bottom-8 left-1/2 -translate-x-1/2 z-50 px-6 py-3 rounded-2xl shadow-2xl animate-in fade-in slide-in-from-bottom-4 duration-300 border backdrop-blur-xl ${
                toast.type === 'success' ? 'bg-emerald-500/10 border-emerald-500/20 text-emerald-400' : 'bg-rose-500/10 border-rose-500/20 text-rose-400'
            }`}>
                <div className="flex items-center gap-3">
                    {toast.type === 'success' ? (
                        <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={3}><path strokeLinecap="round" strokeLinejoin="round" d="M5 13l4 4L19 7" /></svg>
                    ) : (
                        <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={3}><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
                    )}
                    <span className="text-sm font-bold uppercase tracking-wider">{toast.message}</span>
                </div>
            </div>
        )}

        {/* News Feed */}
        <div className="lg:col-span-2 bg-zinc-900 border border-white/5 rounded-3xl flex flex-col overflow-hidden shadow-2xl">
          <div className="p-6 border-b border-white/5 bg-black/20 flex items-center justify-between flex-wrap gap-3">
            <div className="flex items-center gap-4">
                <h3 className="font-semibold text-xl tracking-tight uppercase">Headlines</h3>
                <button
                    onClick={syncNews}
                    disabled={syncingNews}
                    className={`flex items-center gap-2 px-4 py-1.5 rounded-xl border text-[10px] font-black uppercase tracking-widest transition-all ${
                        syncingNews 
                        ? 'bg-blue-600/10 border-blue-600/20 text-blue-400 cursor-not-allowed' 
                        : 'bg-zinc-800 border-white/5 text-zinc-400 hover:text-white hover:border-white/10 hover:bg-zinc-700 active:scale-95'
                    }`}
                >
                    <svg className={`w-3.5 h-3.5 ${syncingNews ? 'animate-spin' : ''}`} fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={3}>
                        <path strokeLinecap="round" strokeLinejoin="round" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                    </svg>
                    {syncingNews ? 'Syncing...' : 'Sync News'}
                </button>
            </div>
            {/* Spec §5.4: category filter */}
            <div className="flex gap-1 p-1 bg-zinc-800 rounded-xl">
              {NEWS_CATEGORIES.map((cat) => (
                <button
                  key={cat}
                  onClick={() => setNewsCategory(cat)}
                  className={`px-3 py-1.5 rounded-lg text-xs font-semibold uppercase tracking-wider transition-all ${
                    newsCategory === cat ? "bg-blue-600 text-white" : "text-zinc-400 hover:text-white"
                  }`}
                >
                  {cat}
                </button>
              ))}
            </div>
          </div>
          <div className="p-2 flex-1 overflow-auto">
            {loading ? (
              <div className="p-6 space-y-6">
                {[1, 2, 3].map((i) => (
                  <div key={i} className="h-24 bg-zinc-800/50 animate-pulse rounded-2xl" />
                ))}
              </div>
            ) : news.length === 0 ? (
              <div className="p-12 text-center">
                <p className="text-zinc-500 font-bold uppercase text-xs tracking-wider">No news available</p>
              </div>
            ) : (
              news.map((item) => (
                <a
                  key={item.id}
                  href={item.url}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="flex gap-6 p-6 rounded-2xl hover:bg-white/[0.02] transition-all group"
                >
                  <div className="w-24 h-24 bg-zinc-800 rounded-2xl shrink-0 overflow-hidden relative shadow-lg">
                    {item.image ? (
                      <img
                        src={item.image}
                        alt=""
                        className="w-full h-full object-cover group-hover:scale-110 transition-transform duration-500"
                      />
                    ) : (
                      <div className="w-full h-full bg-linear-to-br from-zinc-700 to-zinc-800 flex items-center justify-center text-zinc-600 text-xs font-bold">
                        NEWS
                      </div>
                    )}
                  </div>
                  <div className="flex flex-col justify-center">
                    <p className="font-semibold text-lg leading-tight mb-2 group-hover:text-blue-400 transition-colors line-clamp-2">
                      {item.headline}
                    </p>
                    <p className="text-sm text-zinc-500 line-clamp-2 mb-3 font-medium">{item.summary}</p>
                    <div className="flex gap-3 text-xs items-center text-zinc-500 font-semibold uppercase tracking-wider">
                      <span className="text-blue-500">{item.source}</span>
                      <span className="w-1 h-1 bg-zinc-700 rounded-full" />
                      <span>{new Date(item.dateTime * 1000).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })}</span>
                    </div>
                  </div>
                </a>
              ))
            )}
          </div>
          
          {/* Pagination */}
          {news.length > 0 && (
            <div className="p-6 border-t border-white/5 bg-black/10 flex items-center justify-between">
              <button
                onClick={() => setNewsPage(p => Math.max(1, p - 1))}
                disabled={newsPage === 1}
                className="px-4 py-2 bg-zinc-800 border border-white/5 rounded-xl text-xs font-bold uppercase tracking-widest text-zinc-400 hover:text-white disabled:opacity-30 disabled:cursor-not-allowed transition-all"
              >
                Previous
              </button>
              <span className="text-xs font-black text-zinc-500 uppercase tracking-widest">Page {newsPage}</span>
              <button
                onClick={() => setNewsPage(p => p + 1)}
                disabled={news.length < 20}
                className="px-4 py-2 bg-zinc-800 border border-white/5 rounded-xl text-xs font-bold uppercase tracking-widest text-zinc-400 hover:text-white disabled:opacity-30 disabled:cursor-not-allowed transition-all"
              >
                Next
              </button>
            </div>
          )}
        </div>

        {/* Right panel: Earnings Calendar + Exchange Status sidebar */}
        <div className="bg-zinc-900 border border-white/5 rounded-3xl flex flex-col overflow-hidden shadow-2xl">
          <div className="p-6 border-b border-white/5 bg-black/20">
            <h3 className="font-semibold text-xl tracking-tight uppercase">Upcoming Earnings</h3>
            <p className="text-zinc-500 text-xs font-medium mt-1 uppercase tracking-wider">Next 30 days</p>
          </div>
          <div className="p-6 space-y-4 flex-1 overflow-auto">
            {loading ? (
              [1, 2, 3].map((i) => (
                <div key={i} className="h-14 bg-zinc-800/50 animate-pulse rounded-xl" />
              ))
            ) : earningsCalendar.length === 0 ? (
              <p className="text-zinc-500 text-sm font-medium text-center py-8">
                No earnings scheduled in the next 30 days.
              </p>
            ) : (
              earningsCalendar.slice(0, 15).map((e) => {
                const isReleased = e.epsActual != null;
                return (
                  <div key={`${e.symbol}-${e.date}`} className="group cursor-pointer">
                    <div className="flex justify-between items-center mb-1">
                      <span className="font-semibold text-lg text-white group-hover:text-blue-400 transition-colors uppercase tracking-tight">
                        {e.symbol}
                      </span>
                      <span
                        className={`text-xs font-semibold px-2 py-0.5 rounded tracking-wider uppercase ${
                          isReleased
                            ? "bg-emerald-500/10 text-emerald-400"
                            : "bg-blue-500/10 text-blue-400"
                        }`}
                      >
                        {isReleased ? "Released" : e.date}
                      </span>
                    </div>
                    {e.epsEstimate != null && (
                      <p className="text-xs text-zinc-500 font-medium">
                        EPS Est: ${e.epsEstimate.toFixed(2)}
                        {isReleased && (
                          <span className={`ml-2 font-semibold ${(e.epsActual ?? 0) >= (e.epsEstimate ?? 0) ? "text-emerald-400" : "text-rose-400"}`}>
                            → Actual: ${e.epsActual?.toFixed(2)}
                          </span>
                        )}
                      </p>
                    )}
                    <div className="h-px bg-white/5 mt-3" />
                  </div>
                );
              })
            )}
          </div>

          {/* Exchange status mini-section */}
          {usStatus && (
            <div className="p-6 border-t border-white/5 space-y-3">
              <p className="text-zinc-500 text-xs font-semibold uppercase tracking-wide mb-3">Primary Exchange</p>
              <div className="flex items-center justify-between">
                <span className="text-sm font-bold text-zinc-400 uppercase tracking-tight">Timezone</span>
                <span className="text-sm font-semibold text-white">{usStatus.timezone}</span>
              </div>
              <div className="flex items-center justify-between">
                <span className="text-sm font-bold text-zinc-400 uppercase tracking-tight">Session</span>
                <span className="text-sm font-semibold text-white uppercase">{usStatus.session}</span>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

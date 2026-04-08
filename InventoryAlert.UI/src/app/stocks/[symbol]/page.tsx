'use client'

import { useState, useEffect } from "react";
import { useParams } from "next/navigation";
import { fetchApi } from "@/lib/api";
import { PriceAlertModal } from "@/components/PriceAlertModal";

interface StockQuote {
  currentPrice: number;
  change: number;
  percentChange: number;
  highPrice: number;
  lowPrice: number;
  openPrice: number;
  previousClose: number;
}

interface StockProfile {
  name: string;
  ticker: string;
  logo: string;
  finnhubIndustry: string;
  weburl: string;
  marketCapitalization: number;
  shareOutstanding: number;
}

export default function StockDetailPage() {
  const { symbol } = useParams();
  const [quote, setQuote] = useState<StockQuote | null>(null);
  const [profile, setProfile] = useState<StockProfile | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [isInWatchlist, setIsInWatchlist] = useState(false);
  const [updatingWatchlist, setUpdatingWatchlist] = useState(false);
  const [isAlertModalOpen, setIsAlertModalOpen] = useState(false);

  const checkWatchlistStatus = async () => {
    try {
      const watchlist = await fetchApi("/api/v1/watchlist");
      const exists = (watchlist || []).some((item: any) => item.symbol.toUpperCase() === (symbol as string).toUpperCase());
      setIsInWatchlist(exists);
    } catch (err) {
      console.error("Failed to check watchlist status", err);
    }
  };

  const toggleWatchlist = async () => {
    if (!symbol) return;
    setUpdatingWatchlist(true);
    try {
      if (isInWatchlist) {
        await fetchApi(`/api/v1/watchlist/${symbol}`, { method: 'DELETE' });
        setIsInWatchlist(false);
      } else {
        await fetchApi(`/api/v1/watchlist/${symbol}`, { method: 'POST' });
        setIsInWatchlist(true);
      }
    } catch (err: any) {
      alert(err.message || "Failed to update watchlist");
    } finally {
      setUpdatingWatchlist(false);
    }
  };

  useEffect(() => {
    async function loadData() {
      if (!symbol) return;
      try {
        setLoading(true);
        const [quoteData, profileData] = await Promise.all([
          fetchApi(`/api/v1/stocks/${symbol}/quote`),
          fetchApi(`/api/v1/stocks/${symbol}/profile`),
          checkWatchlistStatus(),
        ]);
        setQuote(quoteData);
        setProfile(profileData);
      } catch (err: any) {
        setError(err.message || "Failed to load stock data");
      } finally {
        setLoading(false);
      }
    }
    loadData();
  }, [symbol]);

  if (loading) {
    return (
      <div className="max-w-6xl mx-auto space-y-8 animate-pulse">
        <div className="h-16 bg-zinc-900 rounded-2xl w-1/3"></div>
        <div className="h-[400px] bg-zinc-900 rounded-2xl"></div>
        <div className="grid grid-cols-3 gap-6">
          <div className="col-span-2 h-64 bg-zinc-900 rounded-2xl"></div>
          <div className="h-64 bg-zinc-900 rounded-2xl"></div>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="max-w-6xl mx-auto p-12 bg-rose-500/10 border border-rose-500/20 text-rose-400 rounded-2xl text-center">
        <h3 className="text-xl font-bold mb-2">Error Loading Stock</h3>
        <p>{error}</p>
      </div>
    );
  }

  return (
    <>
      <PriceAlertModal 
        isOpen={isAlertModalOpen} 
        onClose={() => setIsAlertModalOpen(false)} 
        symbol={symbol as string} 
        currentPrice={quote?.currentPrice || 0}
      />
      <div className="flex flex-col gap-8 max-w-6xl mx-auto">
        <div className="flex flex-col md:flex-row items-start justify-between gap-6 pb-8 border-b border-white/10">
          <div className="flex items-center gap-6">
            {profile?.logo && (
              <div className="w-16 h-16 bg-white rounded-2xl flex items-center justify-center overflow-hidden p-2">
                <img src={profile.logo} alt={profile.name} className="max-w-full max-h-full object-contain" />
              </div>
            )}
            <div>
              <div className="flex items-center gap-3">
                <h1 className="text-5xl font-black tracking-tighter uppercase">{symbol}</h1>
                <span className="px-2 py-0.5 bg-blue-500/10 text-blue-400 text-xs font-bold rounded uppercase tracking-wider">{profile?.finnhubIndustry}</span>
              </div>
              <p className="text-zinc-400 text-xl font-medium mt-1">{profile?.name}</p>
            </div>
          </div>
          <div className="text-left md:text-right">
            <p className="text-5xl font-black tracking-tighter">${quote?.currentPrice.toFixed(2)}</p>
            <p className={`text-xl font-bold mt-1 ${(quote?.change || 0) >= 0 ? "text-emerald-400" : "text-rose-400"}`}>
              {(quote?.change || 0) >= 0 ? "+" : ""}{quote?.change.toFixed(2)} ({(quote?.percentChange || 0).toFixed(2)}%) Today
            </p>
          </div>
        </div>

        <div className="flex gap-2 p-1 bg-zinc-900 rounded-2xl w-fit">
          {["Overview", "News", "Financials", "Peers"].map((tab, i) => (
            <button 
              key={tab} 
              className={`px-6 py-2.5 rounded-xl font-bold text-sm transition-all ${i === 0 ? "bg-zinc-800 text-white shadow-lg" : "text-zinc-500 hover:text-white"}`}
            >
              {tab}
            </button>
          ))}
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
          <div className="lg:col-span-2 space-y-8">
            <div className="bg-zinc-900 border border-white/5 rounded-3xl p-8 h-[450px] flex flex-col items-center justify-center relative overflow-hidden group">
              <div className="absolute inset-0 bg-linear-to-br from-blue-500/5 to-transparent"></div>
              <div className="text-center relative z-10">
                <div className="w-20 h-20 bg-blue-500/10 rounded-full flex items-center justify-center text-blue-400 mx-auto mb-6 group-hover:scale-110 transition-transform">
                  📈
                </div>
                <p className="text-xl font-bold text-white">Interactive Chart</p>
                <p className="text-zinc-500 text-sm mt-2 max-w-xs">Connecting to real-time market stream for dynamic price visualization.</p>
              </div>
            </div>

            <div className="bg-zinc-900 border border-white/5 rounded-3xl p-8">
              <h3 className="font-bold text-2xl mb-6 tracking-tight">About {profile?.name}</h3>
              <p className="text-zinc-400 leading-relaxed text-lg">
                {profile?.name} is a leading company in the {profile?.finnhubIndustry} sector. 
                The company currently has a market capitalization of {(profile?.marketCapitalization || 0).toLocaleString()}M 
                with approximately {(profile?.shareOutstanding || 0).toLocaleString()}M shares outstanding.
              </p>
              <div className="grid grid-cols-2 md:grid-cols-4 gap-8 mt-10">
                <div className="space-y-1">
                  <p className="text-zinc-500 text-xs font-bold uppercase tracking-widest">Open</p>
                  <p className="text-xl font-bold">${quote?.openPrice.toFixed(2)}</p>
                </div>
                <div className="space-y-1">
                  <p className="text-zinc-500 text-xs font-bold uppercase tracking-widest">Prev Close</p>
                  <p className="text-xl font-bold">${quote?.previousClose.toFixed(2)}</p>
                </div>
                <div className="space-y-1">
                  <p className="text-zinc-500 text-xs font-bold uppercase tracking-widest">Day High</p>
                  <p className="text-xl font-bold text-emerald-400">${quote?.highPrice.toFixed(2)}</p>
                </div>
                <div className="space-y-1">
                  <p className="text-zinc-500 text-xs font-bold uppercase tracking-widest">Day Low</p>
                  <p className="text-xl font-bold text-rose-400">${quote?.lowPrice.toFixed(2)}</p>
                </div>
              </div>
            </div>
          </div>
          
          <div className="space-y-8">
            <div className="bg-zinc-900 border border-white/5 rounded-3xl p-8">
              <h3 className="font-bold text-xl mb-6 tracking-tight">Quick Actions</h3>
              <div className="space-y-4">
                <button 
                  onClick={toggleWatchlist}
                  disabled={updatingWatchlist}
                  className={`w-full py-4 font-black rounded-2xl transition-all active:scale-[0.98] ${
                    isInWatchlist 
                      ? "bg-zinc-800 border border-rose-500/30 text-rose-400 hover:bg-rose-500 hover:text-white" 
                      : "bg-white text-black hover:bg-zinc-200"
                  }`}
                >
                  {updatingWatchlist ? "UPDATING..." : isInWatchlist ? "REMOVE FROM WATCHLIST" : "ADD TO WATCHLIST"}
                </button>
                <button 
                  onClick={() => setIsAlertModalOpen(true)}
                  className="w-full py-4 bg-zinc-800 border border-white/5 text-white font-black rounded-2xl hover:bg-zinc-700 transition-all active:scale-[0.98]"
                >
                  SET PRICE ALERT
                </button>
              </div>
            </div>

            <div className="bg-zinc-900 border border-white/5 rounded-3xl p-8">
              <h3 className="font-bold text-xl mb-6 tracking-tight">Company Links</h3>
              <div className="space-y-4">
                <a 
                  href={profile?.weburl} 
                  target="_blank" 
                  rel="noopener noreferrer"
                  className="flex items-center justify-between p-4 bg-zinc-800/50 rounded-2xl hover:bg-zinc-800 transition-colors group"
                >
                  <span className="font-bold text-zinc-300 group-hover:text-white">Official Website</span>
                  <span className="text-blue-500">↗</span>
                </a>
              </div>
            </div>
          </div>
        </div>
      </div>
    </>
  );
}

'use client'

import { useState, useEffect } from "react";
import { useParams } from "next/navigation";
import { fetchApi } from "@/lib/api";
import { PriceAlertModal } from "@/components/PriceAlertModal";
import Link from "next/link";

// Spec §4.4 DTOs

interface StockQuote {
  symbol: string;
  price: number;
  change: number;
  changePercent: number;
  high: number;
  low: number;
  open: number;
  prevClose: number;
  timestamp: string;
}

interface StockProfile {
  symbol: string;
  name: string;
  exchange: string;
  currency: string;
  country: string;
  industry: string;
  marketCap: number;
  ipo: string;
  webUrl: string;
  logo: string;
}

interface StockMetrics {
  peRatio: number;
  pbRatio: number;
  epsBasicTtm: number;
  dividendYield: number;
  week52High: number;
  week52Low: number;
  revenueGrowthTtm: number;
  marginNet: number;
  lastSyncedAt: string;
}

interface EarningsSurprise {
  period: string;
  actualEps: number | null;
  estimateEps: number | null;
  surprisePercent: number | null;
  reportDate: string | null;
}

interface RecommendationTrend {
  period: string;
  strongBuy: number;
  buy: number;
  hold: number;
  sell: number;
  strongSell: number;
}

interface InsiderTransaction {
  name: string | null;
  share: number | null;
  value: number | null;
  transactionDate: string | null;
  fillingDate: string | null;
  transactionCode: string | null;
}

interface NewsItem {
  id: number;
  headline: string;
  summary: string;
  source: string;
  url: string;
  dateTime: number;
  image: string;
  category: string;
}

interface PeersResponse {
  symbol: string;
  peers: string[];
}

const TABS = ["Overview", "Financials", "Earnings", "Recommendations", "Insiders", "News", "Peers"] as const;
type Tab = typeof TABS[number];

export default function StockDetailPage() {
  const { symbol: rawSymbol } = useParams();
  // L2 fix: always uppercase the symbol for API calls
  const symbol = ((rawSymbol as string) || "").toUpperCase();

  const [quote, setQuote] = useState<StockQuote | null>(null);
  const [profile, setProfile] = useState<StockProfile | null>(null);
  const [metrics, setMetrics] = useState<StockMetrics | null>(null);
  const [earnings, setEarnings] = useState<EarningsSurprise[]>([]);
  const [recommendations, setRecommendations] = useState<RecommendationTrend[]>([]);
  const [insiders, setInsiders] = useState<InsiderTransaction[]>([]);
  const [news, setNews] = useState<NewsItem[]>([]);
  const [peers, setPeers] = useState<string[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [activeTab, setActiveTab] = useState<Tab>("Overview");
  const [isInWatchlist, setIsInWatchlist] = useState(false);
  const [updatingWatchlist, setUpdatingWatchlist] = useState(false);
  const [isAlertModalOpen, setIsAlertModalOpen] = useState(false);

  const checkWatchlistStatus = async () => {
    try {
      const watchlist = await fetchApi("/api/v1/watchlist");
      const exists = (watchlist || []).some((item: any) => item.symbol?.toUpperCase() === symbol);
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
        // Core data loaded eagerly
        const [quoteData, profileData, metricsData] = await Promise.all([
          fetchApi(`/api/v1/stocks/${symbol}/quote`),
          fetchApi(`/api/v1/stocks/${symbol}/profile`),
          fetchApi(`/api/v1/stocks/${symbol}/financials`).catch(() => null),
          checkWatchlistStatus(),
        ]);
        setQuote(quoteData);
        setProfile(profileData);
        setMetrics(metricsData);
      } catch (err: any) {
        setError(err.message || "Failed to load stock data");
      } finally {
        setLoading(false);
      }
    }
    loadData();
  }, [symbol]);

  // Lazy-load tab data on first activation
  useEffect(() => {
    if (!symbol) return;

    if (activeTab === "Earnings" && earnings.length === 0) {
      fetchApi(`/api/v1/stocks/${symbol}/earnings`)
        .then((data) => setEarnings(data || []))
        .catch(console.error);
    }
    if (activeTab === "Recommendations" && recommendations.length === 0) {
      fetchApi(`/api/v1/stocks/${symbol}/recommendation`)
        .then((data) => setRecommendations(data || []))
        .catch(console.error);
    }
    if (activeTab === "Insiders" && insiders.length === 0) {
      fetchApi(`/api/v1/stocks/${symbol}/insiders`)
        .then((data) => setInsiders(data || []))
        .catch(console.error);
    }
    if (activeTab === "News" && news.length === 0) {
      fetchApi(`/api/v1/stocks/${symbol}/news`)
        .then((data) => setNews(data || []))
        .catch(console.error);
    }
    if (activeTab === "Peers" && peers.length === 0) {
      fetchApi(`/api/v1/stocks/${symbol}/peers`)
        .then((data: PeersResponse) => setPeers(data?.peers || []))
        .catch(console.error);
    }
  }, [activeTab, symbol]);

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
        symbol={symbol}
        currentPrice={quote?.price || 0}
      />
      <div className="flex flex-col gap-8 max-w-6xl mx-auto">
        {/* Header */}
        <div className="flex flex-col md:flex-row items-start justify-between gap-6 pb-8 border-b border-white/10">
          <div className="flex items-center gap-6">
            {profile?.logo && (
              <div className="w-16 h-16 bg-white rounded-2xl flex items-center justify-center overflow-hidden p-2">
                <img src={profile.logo} alt={profile.name} className="max-w-full max-h-full object-contain" />
              </div>
            )}
            <div>
              <div className="flex items-center gap-3">
                <h1 className="text-5xl font-semibold tracking-tight uppercase">{symbol}</h1>
                <span className="px-2 py-0.5 bg-blue-500/10 text-blue-400 text-xs font-bold rounded uppercase tracking-wider">
                  {profile?.industry}
                </span>
              </div>
              <p className="text-zinc-400 text-xl font-medium mt-1">{profile?.name}</p>
            </div>
          </div>
          <div className="text-left md:text-right">
            <p className="text-5xl font-semibold tracking-tight">${(quote?.price || 0).toFixed(2)}</p>
            <p className={`text-xl font-bold mt-1 ${(quote?.change || 0) >= 0 ? "text-emerald-400" : "text-rose-400"}`}>
              {(quote?.change || 0) >= 0 ? "+" : ""}{(quote?.change || 0).toFixed(2)} ({(quote?.changePercent || 0).toFixed(2)}%) Today
            </p>
          </div>
        </div>

        {/* Tab bar */}
        <div className="flex gap-1 p-1 bg-zinc-900 rounded-2xl w-fit flex-wrap">
          {TABS.map((tab) => (
            <button
              key={tab}
              onClick={() => setActiveTab(tab)}
              className={`px-5 py-2.5 rounded-xl font-bold text-sm transition-all ${
                activeTab === tab ? "bg-zinc-800 text-white shadow-lg" : "text-zinc-500 hover:text-white"
              }`}
            >
              {tab}
            </button>
          ))}
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
          {/* Main content area */}
          <div className="lg:col-span-2 space-y-8">
            {/* Chart placeholder */}
            <div className="bg-zinc-900 border border-white/5 rounded-3xl p-8 h-[300px] flex flex-col items-center justify-center relative overflow-hidden group">
              <div className="absolute inset-0 bg-linear-to-br from-blue-500/5 to-transparent"></div>
              <div className="text-center relative z-10">
                <div className="w-20 h-20 bg-blue-500/10 rounded-full flex items-center justify-center text-blue-400 mx-auto mb-6 group-hover:scale-110 transition-transform">
                  📈
                </div>
                <p className="text-xl font-bold text-white">Interactive Chart</p>
                <p className="text-zinc-500 text-sm mt-2 max-w-xs">Connecting to real-time market stream for dynamic price visualization.</p>
              </div>
            </div>

            {/* Tab content */}
            <div className="bg-zinc-900 border border-white/5 rounded-3xl p-8">
              <h3 className="font-bold text-2xl mb-6 tracking-tight">{activeTab}</h3>

              {/* Overview */}
              {activeTab === "Overview" && (
                <>
                  <p className="text-zinc-400 leading-relaxed text-lg">
                    {profile?.name} is a leading company in the {profile?.industry} sector.
                    Based in {profile?.country}, trading in {profile?.currency} on {profile?.exchange}.
                    Market cap: ${((profile?.marketCap || 0) / 1_000_000_000).toFixed(2)}B.
                  </p>
                  <div className="grid grid-cols-2 md:grid-cols-4 gap-8 mt-10">
                    {[
                      { label: "Open", value: `$${(quote?.open || 0).toFixed(2)}` },
                      { label: "Prev Close", value: `$${(quote?.prevClose || 0).toFixed(2)}` },
                      { label: "Day High", value: `$${(quote?.high || 0).toFixed(2)}`, color: "text-emerald-400" },
                      { label: "Day Low", value: `$${(quote?.low || 0).toFixed(2)}`, color: "text-rose-400" },
                    ].map(({ label, value, color }) => (
                      <div key={label} className="space-y-1">
                        <p className="text-zinc-500 text-xs font-bold uppercase tracking-wider">{label}</p>
                        <p className={`text-xl font-bold ${color ?? ""}`}>{value}</p>
                      </div>
                    ))}
                  </div>
                </>
              )}

              {/* Financials — spec §5.3 GET /financials */}
              {activeTab === "Financials" && (
                metrics ? (
                  <div className="grid grid-cols-2 md:grid-cols-3 gap-6">
                    {[
                      { label: "P/E Ratio", value: metrics.peRatio?.toFixed(2) || "N/A" },
                      { label: "P/B Ratio", value: metrics.pbRatio?.toFixed(2) || "N/A" },
                      { label: "EPS (TTM)", value: metrics.epsBasicTtm != null ? `$${metrics.epsBasicTtm.toFixed(2)}` : "N/A" },
                      { label: "Div Yield", value: `${metrics.dividendYield?.toFixed(2) || "0.00"}%`, color: "text-emerald-400" },
                      { label: "52w High", value: metrics.week52High != null ? `$${metrics.week52High.toFixed(2)}` : "N/A", color: "text-emerald-400" },
                      { label: "52w Low", value: metrics.week52Low != null ? `$${metrics.week52Low.toFixed(2)}` : "N/A", color: "text-rose-400" },
                      { label: "Net Margin", value: `${metrics.marginNet?.toFixed(2) || "N/A"}%`, color: "text-blue-400" },
                      { label: "Rev Growth", value: `${metrics.revenueGrowthTtm?.toFixed(2) || "N/A"}%` },
                    ].map(({ label, value, color }) => (
                      <div key={label} className="p-5 bg-zinc-800/20 rounded-2xl border border-white/5">
                        <p className="text-zinc-500 text-xs font-bold uppercase tracking-wider mb-2">{label}</p>
                        <p className={`text-2xl font-semibold ${color ?? "text-white"}`}>{value}</p>
                      </div>
                    ))}
                  </div>
                ) : (
                  <p className="text-zinc-500 italic">Financial metrics not yet available for this symbol.</p>
                )
              )}

              {/* Earnings — spec §5.3 GET /earnings */}
              {activeTab === "Earnings" && (
                earnings.length === 0 ? (
                  <p className="text-zinc-500 italic">No earnings data available.</p>
                ) : (
                  <div className="overflow-x-auto">
                    <table className="w-full text-left">
                      <thead>
                        <tr className="text-xs font-semibold uppercase tracking-wider text-zinc-500 border-b border-white/5">
                          <th className="py-3 pr-6">Period</th>
                          <th className="py-3 pr-6 text-right">Actual EPS</th>
                          <th className="py-3 pr-6 text-right">Estimate EPS</th>
                          <th className="py-3 text-right">Surprise %</th>
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-white/5">
                        {earnings.map((e) => {
                          const beat = (e.surprisePercent ?? 0) >= 0;
                          return (
                            <tr key={e.period} className="hover:bg-white/5 transition-colors">
                              <td className="py-4 pr-6 font-bold text-zinc-300">{e.period}</td>
                              <td className="py-4 pr-6 text-right font-semibold text-white">
                                {e.actualEps != null ? `$${e.actualEps.toFixed(2)}` : "—"}
                              </td>
                              <td className="py-4 pr-6 text-right font-medium text-zinc-400">
                                {e.estimateEps != null ? `$${e.estimateEps.toFixed(2)}` : "—"}
                              </td>
                              <td className="py-4 text-right">
                                {e.surprisePercent != null ? (
                                  <span className={`px-2 py-1 rounded-lg text-xs font-semibold ${beat ? "bg-emerald-500/10 text-emerald-400" : "bg-rose-500/10 text-rose-400"}`}>
                                    {beat ? "+" : ""}{e.surprisePercent.toFixed(2)}%
                                  </span>
                                ) : "—"}
                              </td>
                            </tr>
                          );
                        })}
                      </tbody>
                    </table>
                  </div>
                )
              )}

              {/* Recommendations — spec §5.3 GET /recommendation */}
              {activeTab === "Recommendations" && (
                recommendations.length === 0 ? (
                  <p className="text-zinc-500 italic">No analyst recommendation data available.</p>
                ) : (
                  <div className="space-y-4">
                    {recommendations.slice(0, 3).map((r) => {
                      const total = r.strongBuy + r.buy + r.hold + r.sell + r.strongSell || 1;
                      return (
                        <div key={r.period} className="p-5 bg-zinc-800/30 rounded-2xl border border-white/5">
                          <p className="text-xs font-semibold uppercase tracking-wider text-zinc-500 mb-4">{r.period}</p>
                          <div className="flex gap-1 h-8 rounded-lg overflow-hidden mb-4">
                            {r.strongBuy > 0 && <div className="bg-emerald-600" style={{ width: `${(r.strongBuy / total) * 100}%` }} title={`Strong Buy: ${r.strongBuy}`} />}
                            {r.buy > 0 && <div className="bg-emerald-400" style={{ width: `${(r.buy / total) * 100}%` }} title={`Buy: ${r.buy}`} />}
                            {r.hold > 0 && <div className="bg-zinc-400" style={{ width: `${(r.hold / total) * 100}%` }} title={`Hold: ${r.hold}`} />}
                            {r.sell > 0 && <div className="bg-rose-400" style={{ width: `${(r.sell / total) * 100}%` }} title={`Sell: ${r.sell}`} />}
                            {r.strongSell > 0 && <div className="bg-rose-600" style={{ width: `${(r.strongSell / total) * 100}%` }} title={`Strong Sell: ${r.strongSell}`} />}
                          </div>
                          <div className="flex gap-4 text-xs font-semibold uppercase tracking-wider">
                            <span className="text-emerald-600">SB: {r.strongBuy}</span>
                            <span className="text-emerald-400">B: {r.buy}</span>
                            <span className="text-zinc-400">H: {r.hold}</span>
                            <span className="text-rose-400">S: {r.sell}</span>
                            <span className="text-rose-600">SS: {r.strongSell}</span>
                          </div>
                        </div>
                      );
                    })}
                  </div>
                )
              )}

              {/* Insiders — spec §5.3 GET /insiders */}
              {activeTab === "Insiders" && (
                insiders.length === 0 ? (
                  <p className="text-zinc-500 italic">No insider transaction data available.</p>
                ) : (
                  <div className="overflow-x-auto">
                    <table className="w-full text-left">
                      <thead>
                        <tr className="text-xs font-semibold uppercase tracking-wider text-zinc-500 border-b border-white/5">
                          <th className="py-3 pr-6">Name</th>
                          <th className="py-3 pr-6 text-right">Shares</th>
                          <th className="py-3 pr-6 text-right">Value</th>
                          <th className="py-3 pr-4">Code</th>
                          <th className="py-3 text-right">Date</th>
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-white/5">
                        {insiders.slice(0, 20).map((t, i) => {
                          const isBuy = (t.share ?? 0) > 0;
                          return (
                            <tr key={i} className="hover:bg-white/5 transition-colors">
                              <td className="py-3 pr-6 font-bold text-zinc-300 text-sm">{t.name || "—"}</td>
                              <td className={`py-3 pr-6 text-right font-semibold text-sm ${isBuy ? "text-emerald-400" : "text-rose-400"}`}>
                                {t.share != null ? (isBuy ? "+" : "") + t.share.toLocaleString() : "—"}
                              </td>
                              <td className="py-3 pr-6 text-right font-medium text-zinc-400 text-sm">
                                {t.value != null ? `$${t.value.toLocaleString()}` : "—"}
                              </td>
                              <td className="py-3 pr-4">
                                <span className={`px-2 py-0.5 rounded text-xs font-semibold uppercase ${isBuy ? "bg-emerald-500/10 text-emerald-400" : "bg-rose-500/10 text-rose-400"}`}>
                                  {t.transactionCode || "—"}
                                </span>
                              </td>
                              <td className="py-3 text-right text-xs text-zinc-500 font-medium">{t.transactionDate || "—"}</td>
                            </tr>
                          );
                        })}
                      </tbody>
                    </table>
                  </div>
                )
              )}

              {/* News — spec §5.3 GET /stocks/{symbol}/news */}
              {activeTab === "News" && (
                <div className="space-y-6">
                  <div className="flex items-center justify-between border-b border-white/5 pb-4">
                     <h4 className="text-zinc-500 text-xs font-bold uppercase tracking-widest">Recent Activity</h4>
                     <button
                        onClick={async () => {
                            try {
                                await fetchApi("/api/v1/events", {
                                    method: "POST",
                                    headers: { "Content-Type": "application/json" },
                                    body: JSON.stringify({ 
                                        eventType: "inventoryalert.news.company-sync-requested.v1", 
                                        payload: { symbol: symbol } 
                                    })
                                });
                                alert("News sync requested for " + symbol);
                            } catch (err: any) {
                                alert("Failed to trigger sync: " + err.message);
                            }
                        }}
                        className="flex items-center gap-2 px-3 py-1 bg-zinc-800 border border-white/5 rounded-lg text-[10px] font-black uppercase tracking-widest text-zinc-400 hover:text-white hover:border-white/10 transition-all active:scale-95"
                    >
                        <svg className="w-3 h-3" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={3}>
                            <path strokeLinecap="round" strokeLinejoin="round" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                        </svg>
                        Sync
                    </button>
                  </div>
                  {news.length === 0 ? (
                    <p className="text-zinc-500 italic">No news available for {symbol}.</p>
                  ) : (
                  <div className="space-y-4">
                    {news.slice(0, 10).map((item) => (
                      <a
                        key={item.id}
                        href={item.url}
                        target="_blank"
                        rel="noopener noreferrer"
                        className="flex gap-4 group pb-4 border-b border-white/5 last:border-0 hover:border-blue-500/20 transition-colors"
                      >
                        <div className="w-20 h-20 bg-zinc-800 rounded-xl shrink-0 overflow-hidden">
                          {item.image ? (
                            <img src={item.image} alt="" className="w-full h-full object-cover group-hover:scale-105 transition-transform" />
                          ) : (
                            <div className="w-full h-full flex items-center justify-center text-zinc-600 text-xs font-bold">NEWS</div>
                          )}
                        </div>
                        <div className="flex flex-col justify-center gap-1">
                          <p className="font-semibold text-sm leading-snug line-clamp-2 group-hover:text-blue-400 transition-colors">{item.headline}</p>
                          <p className="text-xs text-zinc-500 line-clamp-1">{item.summary}</p>
                          <div className="flex gap-2 text-xs font-semibold uppercase tracking-wider text-zinc-500">
                            <span className="text-blue-500">{item.source}</span>
                            <span>•</span>
                            <span className="text-zinc-600 dark:text-zinc-400">{item.category}</span>
                            <span>•</span>
                            <span>{new Date(item.dateTime * 1000).toLocaleDateString()}</span>
                          </div>
                        </div>
                      </a>
                    ))}
                  </div>
                )}
              </div>
            )}

              {/* Peers — spec §5.3 GET /stocks/{symbol}/peers */}
              {activeTab === "Peers" && (
                peers.length === 0 ? (
                  <p className="text-zinc-500 italic">No peer data available.</p>
                ) : (
                  <div>
                    <p className="text-zinc-500 text-sm mb-6">Companies in the same sector and country as {symbol}.</p>
                    <div className="flex flex-wrap gap-3">
                      {peers.map((peer) => (
                        <Link
                          key={peer}
                          href={`/stocks/${peer.toLowerCase()}`}
                          className="px-5 py-3 bg-zinc-800 hover:bg-blue-600 border border-white/10 hover:border-blue-500 rounded-2xl font-semibold text-sm uppercase tracking-tight transition-all"
                        >
                          {peer}
                        </Link>
                      ))}
                    </div>
                  </div>
                )
              )}
            </div>
          </div>

          {/* Sidebar */}
          <div className="space-y-8">
            <div className="bg-zinc-900 border border-white/5 rounded-3xl p-8">
              <h3 className="font-bold text-xl mb-6 tracking-tight">Quick Actions</h3>
              <div className="space-y-4">
                <button
                  onClick={toggleWatchlist}
                  disabled={updatingWatchlist}
                  className={`w-full py-4 font-semibold rounded-2xl transition-all active:scale-[0.98] ${
                    isInWatchlist
                      ? "bg-zinc-800 border border-rose-500/30 text-rose-400 hover:bg-rose-500 hover:text-white"
                      : "bg-white text-black hover:bg-zinc-200"
                  }`}
                >
                  {updatingWatchlist ? "UPDATING..." : isInWatchlist ? "REMOVE FROM WATCHLIST" : "ADD TO WATCHLIST"}
                </button>
                <button
                  onClick={() => setIsAlertModalOpen(true)}
                  className="w-full py-4 bg-zinc-800 border border-white/5 text-white font-semibold rounded-2xl hover:bg-zinc-700 transition-all active:scale-[0.98]"
                >
                  SET PRICE ALERT
                </button>
              </div>
            </div>

            <div className="bg-zinc-900 border border-white/5 rounded-3xl p-8">
              <h3 className="font-bold text-xl mb-4 tracking-tight">Company Info</h3>
              <div className="space-y-3 text-sm">
                {[
                  { label: "Exchange", value: profile?.exchange },
                  { label: "Country", value: profile?.country },
                  { label: "Currency", value: profile?.currency },
                  { label: "IPO Date", value: profile?.ipo },
                ].map(({ label, value }) => (
                  <div key={label} className="flex justify-between">
                    <span className="text-zinc-500 font-medium">{label}</span>
                    <span className="font-bold text-zinc-200 uppercase text-sm tracking-wider">{value || "—"}</span>
                  </div>
                ))}
              </div>
              {profile?.webUrl && (
                <a
                  href={profile.webUrl}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="mt-6 flex items-center justify-between p-4 bg-zinc-800/50 rounded-2xl hover:bg-zinc-800 transition-colors group"
                >
                  <span className="font-bold text-zinc-300 group-hover:text-white text-sm">Official Website</span>
                  <span className="text-blue-500">↗</span>
                </a>
              )}
            </div>
          </div>
        </div>
      </div>
    </>
  );
}

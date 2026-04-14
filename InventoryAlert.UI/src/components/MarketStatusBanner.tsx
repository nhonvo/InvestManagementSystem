'use client'

import { useState, useEffect } from "react";
import { fetchApi } from "@/lib/api";

export function MarketStatusBanner() {
  const [status, setStatus] = useState<any>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const checkStatus = async () => {
      try {
        const data = await fetchApi("/api/v1/market/status");
        if (data && data.length > 0) {
          // Identify US market status (NYSE/NASDAQ)
          const usStatus = data.find((s: any) => s.exchange.includes("US") || s.exchange === "NYSE" || s.exchange === "NASDAQ");
          setStatus(usStatus || data[0]);
        }
      } catch (err) {
        console.error("Market status check failed", err);
      } finally {
        setLoading(false);
      }
    };
    checkStatus();
    const interval = setInterval(checkStatus, 60000); // 1 minute pool
    return () => clearInterval(interval);
  }, []);

  if (loading) return (
    <div className="w-32 h-6 bg-zinc-100 dark:bg-zinc-800 rounded-full animate-pulse"></div>
  );

  return (
    <div className={`hidden sm:flex items-center gap-2.5 text-xs font-semibold uppercase tracking-wider border border-zinc-200 dark:border-white/5 rounded-full px-4 py-1.5 bg-zinc-100 dark:bg-zinc-900/50 shadow-sm transition-all ${status?.isOpen ? 'text-emerald-500' : 'text-rose-500'}`}>
        <span className={`w-1.5 h-1.5 rounded-full ${status?.isOpen ? 'bg-emerald-500 animate-pulse' : 'bg-rose-500 shadow-[0_0_8px_rgba(244,63,94,0.5)]'}`}></span>
        {status?.exchange.replace(" EQUITIES", "") || "MARKET"}: {status?.isOpen ? 'OPEN' : 'CLOSED'}
    </div>
  );
}

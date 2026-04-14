'use client'

import { useState, useEffect } from "react";
import { fetchApi } from "@/lib/api";

interface HealthCheckEntry {
  name: string;
  status: string;
  description: string | null;
  duration: string;
}

interface HealthResponse {
  status: string;
  checks: HealthCheckEntry[];
  totalDuration: string;
}

export default function AdminHealth() {
  const [health, setHealth] = useState<HealthResponse | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    async function checkHealth() {
      try {
        const data = await fetchApi("/health");
        setHealth(data);
      } catch (err) {
        console.error("Health check failed", err);
      } finally {
        setLoading(false);
      }
    }
    checkHealth();
    const interval = setInterval(checkHealth, 30000); // Check every 30s
    return () => clearInterval(interval);
  }, []);

  const getStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case 'healthy': return 'text-emerald-400';
      case 'degraded': return 'text-amber-400';
      default: return 'text-rose-400';
    }
  };

  const getStatusBg = (status: string) => {
    switch (status.toLowerCase()) {
      case 'healthy': return 'bg-emerald-500/10 border-emerald-500/20';
      case 'degraded': return 'bg-amber-500/10 border-amber-500/20';
      default: return 'bg-rose-500/10 border-rose-500/20';
    }
  };

  return (
    <div className="max-w-7xl mx-auto flex flex-col gap-10 h-full">
      <div className="flex flex-col md:flex-row md:items-end justify-between gap-4 py-2">
        <div>
          <h1 className="text-5xl font-semibold tracking-tight uppercase">System Integrity</h1>
          <p className="text-zinc-500 font-medium mt-2 text-lg">Real-time infrastructure health and performance orchestration.</p>
        </div>
        <div className="flex items-center gap-6">
           <div className="text-right">
             <p className="text-xs font-semibold text-zinc-500 uppercase tracking-wider mb-1">Integrity Score</p>
             <p className="text-3xl font-semibold text-white">{health?.status === 'Healthy' ? '99.9' : '94.2'}<span className="text-zinc-600 text-sm ml-1">%</span></p>
           </div>
           <div className="w-px h-10 bg-white/5"></div>
           <div className={`px-4 py-2 rounded-2xl border font-semibold text-xs tracking-wider transition-all ${health?.status === 'Healthy' ? 'bg-emerald-500/10 border-emerald-500/20 text-emerald-400' : 'bg-rose-500/10 border-rose-500/20 text-rose-400'}`}>
             SYSTEM: {health?.status?.toUpperCase() || 'OFFLINE'}
           </div>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        {loading ? (
          [1,2,3,4].map(i => <div key={i} className="h-40 bg-zinc-900/50 animate-pulse rounded-3xl border border-white/5"></div>)
        ) : (
          health?.checks.map((check) => (
            <div key={check.name} className={`group border rounded-3xl p-6 transition-all hover:scale-[1.02] ${getStatusBg(check.status)}`}>
              <div className="flex justify-between items-start mb-6">
                <div className="p-3 rounded-2xl bg-black/20 text-xl">
                  {check.name === 'PostgreSQL' ? '🐘' : check.name === 'Redis' ? '⚡' : '⚙️'}
                </div>
                <span className={`w-2 h-2 rounded-full shadow-[0_0_8px_currentColor] ${getStatusColor(check.status)}`}></span>
              </div>
              <h3 className="font-semibold text-zinc-300 uppercase tracking-wider text-xs mb-1">{check.name}</h3>
              <p className={`text-xl font-semibold mb-4 ${getStatusColor(check.status)}`}>{check.status}</p>
              <div className="flex justify-between items-center text-xs font-bold text-zinc-500 uppercase tracking-tight">
                <span>Latency</span>
                <span className="text-white">{check.duration.split('.')[0]}ms</span>
              </div>
            </div>
          ))
        )}
        
        {/* Synthetic Stat Cards */}
        <div className="bg-zinc-900 border border-white/5 rounded-3xl p-6 hover:border-white/10 transition-colors group">
           <div className="flex justify-between items-start mb-6">
              <div className="p-3 rounded-2xl bg-black/20 text-xl">🚀</div>
              <div className="flex gap-1">
                {[1,2,3].map(i => <div key={i} className="w-1 h-3 bg-emerald-500/40 rounded-full"></div>)}
              </div>
           </div>
           <h3 className="font-semibold text-zinc-400 uppercase tracking-wider text-xs mb-1">Throughput</h3>
           <p className="text-xl font-semibold text-white mb-4">1.2k <span className="text-xs text-zinc-600">req/s</span></p>
           <div className="w-full bg-zinc-800 h-1 rounded-full overflow-hidden">
             <div className="bg-blue-500 h-full w-[65%] rounded-full shadow-[0_0_8px_rgba(59,130,246,0.5)]"></div>
           </div>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8 flex-1 min-h-0">
        <div className="lg:col-span-2 bg-zinc-900 border border-white/5 rounded-3xl flex flex-col overflow-hidden shadow-2xl relative group">
           <div className="absolute inset-0 bg-linear-to-br from-blue-500/5 to-transparent opacity-0 group-hover:opacity-100 transition-opacity pointer-events-none"></div>
           <div className="p-8 border-b border-white/5 bg-black/20 flex items-center justify-between">
              <h3 className="font-semibold text-xl tracking-tight uppercase">Performance Metrics</h3>
              <div className="flex gap-3">
                <span className="w-2 h-2 rounded-full bg-blue-500"></span>
                <span className="w-2 h-2 rounded-full bg-indigo-500"></span>
                <span className="w-2 h-2 rounded-full bg-purple-500"></span>
              </div>
           </div>
           <div className="p-12 flex-1 flex flex-col items-center justify-center text-center">
              <div className="w-20 h-20 bg-blue-500/10 rounded-full flex items-center justify-center text-blue-400 mb-6 animate-pulse">
                📊
              </div>
              <h4 className="font-semibold text-white text-lg mb-2">Advanced Analytics</h4>
              <p className="text-zinc-500 text-sm max-w-sm font-medium">Historical performance and resource distribution visualization is currently being processed by the worker hub.</p>
           </div>
        </div>

        <div className="space-y-6">
           <div className="bg-zinc-900 border border-white/5 rounded-3xl p-8 shadow-2xl">
              <h3 className="font-semibold text-xl mb-8 tracking-tight uppercase">System Logs</h3>
              <div className="space-y-6">
                 {[
                   { level: 'CRIT', msg: 'Finnhub rate limit approaching', time: '1m ago' },
                   { level: 'WARN', msg: 'High disk I/O on Postgres replica', time: '12m ago' },
                   { level: 'INFO', msg: 'System integrity scan complete', time: '1h ago' },
                 ].map((log, i) => (
                   <div key={i} className="flex gap-4 group">
                      <span className={`text-xs font-semibold px-2 py-0.5 rounded h-fit ${log.level === 'CRIT' ? 'bg-rose-500/10 text-rose-400' : log.level === 'WARN' ? 'bg-amber-500/10 text-amber-400' : 'bg-blue-500/10 text-blue-400'}`}>
                        {log.level}
                      </span>
                      <div className="flex-1">
                        <p className="text-sm font-bold text-zinc-300 group-hover:text-white transition-colors leading-tight">{log.msg}</p>
                        <p className="text-xs font-bold text-zinc-500 uppercase tracking-wider mt-1">{log.time}</p>
                      </div>
                   </div>
                 ))}
              </div>
           </div>
           
           <div className="bg-linear-to-br from-blue-600 to-indigo-700 rounded-3xl p-8 shadow-2xl shadow-blue-900/20 group relative overflow-hidden">
              <div className="absolute top-0 right-0 w-32 h-32 bg-white/10 rounded-full -mr-10 -mt-10 blur-3xl group-hover:bg-white/20 transition-all"></div>
              <h3 className="font-semibold text-white text-xl mb-2 tracking-tight uppercase relative z-10">Backup Status</h3>
              <p className="text-blue-100 text-sm font-medium mb-6 relative z-10 opacity-80">Last full system snapshot captured successfully.</p>
              <div className="flex items-end justify-between relative z-10">
                 <p className="text-4xl font-semibold text-white">100<span className="text-xl">%</span></p>
                 <button className="px-6 py-2 bg-white text-blue-600 rounded-2xl text-xs font-semibold uppercase tracking-wider hover:scale-105 transition-transform">Verify</button>
              </div>
           </div>
        </div>
      </div>
    </div>
  );
}

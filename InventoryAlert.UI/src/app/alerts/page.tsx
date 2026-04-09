'use client'

import { useState, useEffect } from "react";
import { fetchApi } from "@/lib/api";
import { PriceAlertModal } from "@/components/PriceAlertModal";

interface AlertRule {
  id: string;
  symbol: string;
  field: string;
  operator: string;
  threshold: number;
  notifyChannel: string;
  isActive: boolean;
  lastTriggeredAt?: string;
}

export default function AlertsManager() {
  const [alerts, setAlerts] = useState<AlertRule[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [isModalOpen, setIsModalOpen] = useState(false);

  const loadAlerts = async () => {
    setLoading(true);
    try {
      const data = await fetchApi("/api/v1/alerts");
      setAlerts(data);
    } catch (err: any) {
      setError(err.message || "Failed to load alerts");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadAlerts();
  }, []);

  const getConditionText = (alert: AlertRule) => {
    const opMap: Record<string, string> = {
      'gt': '>',
      'lt': '<',
      'gte': '≥',
      'lte': '≤',
      'eq': '='
    };
    return `${alert.field.toUpperCase()} ${opMap[alert.operator] || alert.operator} ${alert.threshold}`;
  };

  return (
    <div className="max-w-6xl mx-auto flex flex-col gap-8">
      <div className="flex flex-col md:flex-row items-start md:items-center justify-between gap-4">
        <div>
          <h1 className="text-4xl font-black tracking-tighter uppercase">Alert Center</h1>
          <p className="text-zinc-500 font-medium mt-1 text-lg">Manage your automated market triggers and notifications.</p>
        </div>
        <button 
          onClick={() => setIsModalOpen(true)}
          className="bg-blue-600 hover:bg-blue-700 text-white font-black px-6 py-3 rounded-2xl shadow-xl shadow-blue-500/20 transition-all active:scale-[0.98] tracking-tight text-sm"
        >
          + CREATE NEW RULE
        </button>
      </div>

      <PriceAlertModal 
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        onSuccess={loadAlerts}
      />

      {loading ? (
        <div className="space-y-4 animate-pulse">
          {[1, 2, 3].map((i) => (
            <div key={i} className="h-20 bg-zinc-900 rounded-2xl border border-white/5"></div>
          ))}
        </div>
      ) : error ? (
        <div className="bg-rose-500/10 border border-rose-500/20 text-rose-400 p-8 rounded-3xl text-center">
          <p className="font-bold text-lg">{error}</p>
        </div>
      ) : alerts.length === 0 ? (
        <div className="bg-zinc-900 border border-white/5 p-16 rounded-3xl text-center">
          <div className="w-16 h-16 bg-zinc-800 rounded-full flex items-center justify-center mx-auto mb-6 text-2xl">🔔</div>
          <p className="text-zinc-400 font-bold mb-2 text-xl">No alerts configured yet.</p>
          <p className="text-zinc-500 max-w-sm mx-auto">Create rules to stay updated on price movements and market events.</p>
        </div>
      ) : (
        <div className="bg-zinc-900 border border-white/5 rounded-3xl overflow-hidden shadow-2xl">
          <div className="overflow-x-auto">
            <table className="w-full text-left border-collapse">
              <thead>
                <tr className="bg-zinc-800/50 text-[10px] font-black uppercase tracking-[0.2em] text-zinc-500">
                  <th className="p-6 font-bold">Status</th>
                  <th className="p-6 font-bold">Symbol</th>
                  <th className="p-6 font-bold">Condition</th>
                  <th className="p-6 font-bold">Channel</th>
                  <th className="p-6 font-bold">Last Triggered</th>
                  <th className="p-6 font-bold text-right">Settings</th>
                </tr>
              </thead>
              <tbody className="text-sm">
                {alerts.map((alert) => (
                  <tr key={alert.id} className="border-b border-white/5 last:border-0 hover:bg-white/2 transition-colors group">
                    <td className="p-6">
                      <div className="flex items-center gap-2">
                        <span className={`w-2 h-2 rounded-full ${alert.isActive ? 'bg-emerald-500 shadow-[0_0_8px_rgba(16,185,129,0.5)]' : 'bg-zinc-600'}`}></span>
                        <span className={`font-bold ${alert.isActive ? 'text-emerald-400' : 'text-zinc-500'}`}>
                          {alert.isActive ? 'ACTIVE' : 'PAUSED'}
                        </span>
                      </div>
                    </td>
                    <td className="p-6">
                      <div className="font-black text-lg text-white group-hover:text-blue-400 transition-colors uppercase tracking-tight">{alert.symbol}</div>
                    </td>
                    <td className="p-6">
                      <div className="font-bold text-zinc-300 bg-zinc-800/50 px-3 py-1.5 rounded-lg border border-white/5 inline-block">
                        {getConditionText(alert)}
                      </div>
                    </td>
                    <td className="p-6">
                      <span className="text-zinc-400 font-medium uppercase text-xs tracking-widest bg-zinc-800/80 px-2 py-1 rounded shadow-inner">
                        {alert.notifyChannel}
                      </span>
                    </td>
                    <td className="p-6">
                      <span className="text-zinc-500 text-xs font-medium">
                        {alert.lastTriggeredAt ? new Date(alert.lastTriggeredAt).toLocaleString() : 'Never'}
                      </span>
                    </td>
                    <td className="p-6 text-right">
                      <div className="flex items-center justify-end gap-3">
                        <button className="w-9 h-9 flex items-center justify-center rounded-xl bg-zinc-800 text-blue-400 hover:bg-blue-500 hover:text-white transition-all shadow-sm">
                          ✎
                        </button>
                        <button className="w-9 h-9 flex items-center justify-center rounded-xl bg-zinc-800 text-rose-400 hover:bg-rose-500 hover:text-white transition-all shadow-sm">
                          ✕
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
}

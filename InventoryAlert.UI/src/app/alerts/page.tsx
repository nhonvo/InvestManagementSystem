'use client'

import { useState, useEffect } from "react";
import { fetchApi } from "@/lib/api";

// Spec §4.3 DTOs
interface AlertRule {
  id: string;
  tickerSymbol: string;
  condition: string;
  targetValue: number;
  isActive: boolean;
  triggerOnce: boolean;
  lastTriggeredAt?: string;
}

type AlertCondition = "PriceAbove" | "PriceBelow" | "PriceTargetReached" | "PercentDropFromCost" | "LowHoldingsCount";

const CONDITIONS: AlertCondition[] = [
  "PriceAbove",
  "PriceBelow",
  "PriceTargetReached",
  "PercentDropFromCost",
  "LowHoldingsCount",
];

const conditionLabel: Record<string, string> = {
  PriceAbove: "is above",
  PriceBelow: "is below",
  PriceTargetReached: "hits target",
  PercentDropFromCost: "% drop from cost",
  LowHoldingsCount: "holdings below",
};

interface AlertFormState {
  tickerSymbol: string;
  condition: AlertCondition;
  targetValue: string;
  triggerOnce: boolean;
}

const defaultForm: AlertFormState = {
  tickerSymbol: "",
  condition: "PriceAbove",
  targetValue: "",
  triggerOnce: true,
};

export default function AlertsManager() {
  const [alerts, setAlerts] = useState<AlertRule[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  // Form / modal state
  const [modalOpen, setModalOpen] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState<AlertFormState>(defaultForm);
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState("");

  const loadAlerts = async () => {
    setLoading(true);
    try {
      const data = await fetchApi("/api/v1/alertrules");
      setAlerts(Array.isArray(data) ? data : []);
    } catch (err: any) {
      setError(err.message || "Failed to load alerts");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadAlerts();
  }, []);

  const openCreate = () => {
    setEditingId(null);
    setForm(defaultForm);
    setFormError("");
    setModalOpen(true);
  };

  const openEdit = (alert: AlertRule) => {
    setEditingId(alert.id);
    setForm({
      tickerSymbol: alert.tickerSymbol,
      condition: alert.condition as AlertCondition,
      targetValue: String(alert.targetValue),
      triggerOnce: alert.triggerOnce,
    });
    setFormError("");
    setModalOpen(true);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    setFormError("");

    const payload = {
      tickerSymbol: form.tickerSymbol.toUpperCase(),
      condition: form.condition,
      targetValue: parseFloat(form.targetValue),
      triggerOnce: form.triggerOnce,
    };

    try {
      if (editingId) {
        // Spec §5.6: PUT /api/v1/alerts/{ruleId}
        await fetchApi(`/api/v1/alertrules/${editingId}`, {
          method: "PUT",
          body: JSON.stringify(payload),
        });
      } else {
        // Spec §5.6: POST /api/v1/alerts/
        await fetchApi("/api/v1/alertrules", {
          method: "POST",
          body: JSON.stringify(payload),
        });
      }
      setModalOpen(false);
      loadAlerts();
    } catch (err: any) {
      setFormError(err.message || "Failed to save alert");
    } finally {
      setSaving(false);
    }
  };

  const toggleAlert = async (id: string, currentStatus: boolean) => {
    try {
      // Spec §5.6: PATCH /api/v1/alerts/{ruleId}/toggle
      await fetchApi(`/api/v1/alertrules/${id}/toggle`, {
        method: "PATCH",
        body: JSON.stringify({ isActive: !currentStatus }),
      });
      setAlerts((prev) => prev.map((a) => (a.id === id ? { ...a, isActive: !currentStatus } : a)));
    } catch (err: any) {
      alert(err.message || "Failed to toggle alert");
    }
  };

  const deleteAlert = async (alert: AlertRule) => {
    if (!confirm(`Delete alert for ${alert.tickerSymbol}?`)) return;
    try {
      // Spec §5.6: DELETE /api/v1/alerts/{ruleId}
      await fetchApi(`/api/v1/alertrules/${alert.id}`, { method: "DELETE" });
      loadAlerts();
    } catch (err: any) {
      alert(err.message || "Failed to delete alert");
    }
  };

  return (
    <div className="max-w-6xl mx-auto flex flex-col gap-8">
      <div className="flex flex-col md:flex-row items-start md:items-center justify-between gap-4">
        <div>
          <h1 className="text-4xl font-semibold tracking-tight uppercase">Alert Center</h1>
          <p className="text-zinc-500 font-medium mt-1 text-lg">Manage your automated market triggers and notifications.</p>
        </div>
        <button
          onClick={openCreate}
          className="bg-blue-600 hover:bg-blue-700 text-white font-semibold px-6 py-3 rounded-2xl shadow-xl shadow-blue-500/20 transition-all active:scale-[0.98] tracking-tight text-sm"
        >
          + CREATE NEW RULE
        </button>
      </div>

      {/* Create / Edit Modal */}
      {modalOpen && (
        <div className="fixed inset-0 z-[100] flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm">
          <div className="bg-zinc-900 w-full max-w-md rounded-3xl overflow-hidden shadow-2xl border border-white/10">
            <div className="p-8 border-b border-white/5 flex items-center justify-between">
              <h2 className="text-xl font-semibold uppercase tracking-tight">
                {editingId ? "Edit Alert Rule" : "New Alert Rule"}
              </h2>
              <button onClick={() => setModalOpen(false)} className="text-zinc-500 hover:text-white transition-colors text-lg">✕</button>
            </div>

            <form onSubmit={handleSubmit} className="p-8 space-y-5">
              {formError && (
                <div className="p-4 bg-rose-500/10 border border-rose-500/20 text-rose-400 text-sm font-bold rounded-2xl text-center">
                  {formError}
                </div>
              )}

              <div className="space-y-2">
                <label className="text-xs font-semibold uppercase tracking-wider text-zinc-400 ml-1">Ticker Symbol</label>
                <input
                  type="text"
                  required
                  placeholder="e.g. AAPL"
                  value={form.tickerSymbol}
                  onChange={(e) => setForm({ ...form, tickerSymbol: e.target.value.toUpperCase() })}
                  disabled={!!editingId}
                  className="w-full bg-zinc-800 border-none rounded-2xl px-5 py-4 font-semibold text-white placeholder:text-zinc-600 focus:ring-2 focus:ring-blue-500 transition-all uppercase disabled:opacity-50"
                />
              </div>

              <div className="space-y-2">
                <label className="text-xs font-semibold uppercase tracking-wider text-zinc-400 ml-1">Condition</label>
                <select
                  value={form.condition}
                  onChange={(e) => setForm({ ...form, condition: e.target.value as AlertCondition })}
                  className="w-full bg-zinc-800 border-none rounded-2xl px-5 py-4 font-bold text-white focus:ring-2 focus:ring-blue-500"
                >
                  {CONDITIONS.map((c) => (
                    <option key={c} value={c}>{conditionLabel[c]} ({c})</option>
                  ))}
                </select>
              </div>

              <div className="space-y-2">
                <label className="text-xs font-semibold uppercase tracking-wider text-zinc-400 ml-1">Target Value</label>
                <input
                  type="number"
                  step="0.01"
                  min="0.01"
                  required
                  placeholder="0.00"
                  value={form.targetValue}
                  onChange={(e) => setForm({ ...form, targetValue: e.target.value })}
                  className="w-full bg-zinc-800 border-none rounded-2xl px-5 py-4 font-bold text-white placeholder:text-zinc-600 focus:ring-2 focus:ring-blue-500 transition-all"
                />
                <p className="text-xs text-zinc-600 ml-1">
                  {form.condition === "PercentDropFromCost" ? "Enter a percentage (0.01–100)" : "Enter a monetary value in USD"}
                </p>
              </div>

              <div className="flex items-center justify-between p-4 bg-zinc-800/50 rounded-2xl">
                <div>
                  <p className="text-xs font-semibold uppercase tracking-wider text-white">Trigger Once</p>
                  <p className="text-xs text-zinc-500">Deactivate automatically after first trigger</p>
                </div>
                <button
                  type="button"
                  onClick={() => setForm({ ...form, triggerOnce: !form.triggerOnce })}
                  className={`w-12 h-6 rounded-full relative transition-all ${form.triggerOnce ? "bg-blue-600" : "bg-zinc-700"}`}
                >
                  <div className={`absolute top-1 w-4 h-4 rounded-full bg-white transition-all ${form.triggerOnce ? "right-1" : "left-1"}`} />
                </button>
              </div>

              <div className="flex gap-4 pt-2">
                <button
                  type="button"
                  onClick={() => setModalOpen(false)}
                  className="flex-1 py-4 bg-zinc-800 text-zinc-400 hover:text-white font-semibold rounded-2xl transition-all uppercase tracking-wider text-xs"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={saving}
                  className="flex-[2] py-4 bg-blue-600 hover:bg-blue-700 text-white font-semibold rounded-2xl shadow-lg shadow-blue-500/20 transition-all active:scale-95 disabled:opacity-50 uppercase tracking-wider text-xs"
                >
                  {saving ? "SAVING..." : editingId ? "UPDATE RULE" : "CREATE RULE"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {loading ? (
        <div className="space-y-4 animate-pulse">
          {[1, 2, 3].map((i) => (
            <div key={i} className="h-20 bg-zinc-900 rounded-2xl border border-white/5" />
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
                <tr className="bg-zinc-800/50 text-xs font-semibold uppercase tracking-wide text-zinc-500">
                  <th className="p-6 font-bold">Status</th>
                  <th className="p-6 font-bold">Symbol</th>
                  <th className="p-6 font-bold">Condition</th>
                  <th className="p-6 font-bold">Mode</th>
                  <th className="p-6 font-bold">Last Triggered</th>
                  <th className="p-6 font-bold text-right">Actions</th>
                </tr>
              </thead>
              <tbody className="text-sm">
                {alerts.map((alert) => (
                  <tr key={alert.id} className="border-b border-white/5 last:border-0 hover:bg-white/[0.02] transition-colors group">
                    <td className="p-6">
                      <button onClick={() => toggleAlert(alert.id, alert.isActive)} className="flex items-center gap-2 group/toggle">
                        <span className={`w-2 h-2 rounded-full transition-all ${alert.isActive ? "bg-emerald-500 shadow-[0_0_8px_rgba(16,185,129,0.5)]" : "bg-zinc-600"}`} />
                        <span className={`font-bold transition-all ${alert.isActive ? "text-emerald-400" : "text-zinc-500 group-hover/toggle:text-zinc-300"}`}>
                          {alert.isActive ? "ACTIVE" : "PAUSED"}
                        </span>
                      </button>
                    </td>
                    <td className="p-6">
                      <div className="font-semibold text-lg text-white group-hover:text-blue-400 transition-colors uppercase tracking-tight">
                        {alert.tickerSymbol}
                      </div>
                    </td>
                    <td className="p-6">
                      <div className="font-bold text-zinc-300 bg-zinc-800/50 px-3 py-1.5 rounded-lg border border-white/5 inline-block text-xs">
                        {conditionLabel[alert.condition] || alert.condition} ${alert.targetValue}
                      </div>
                    </td>
                    <td className="p-6">
                      <span className="text-zinc-400 font-medium uppercase text-xs tracking-wider bg-zinc-800/80 px-2 py-1 rounded shadow-inner">
                        {alert.triggerOnce ? "ONCE" : "RECURRING"}
                      </span>
                    </td>
                    <td className="p-6">
                      <span className="text-zinc-500 text-xs font-medium">
                        {alert.lastTriggeredAt ? new Date(alert.lastTriggeredAt).toLocaleString() : "Never"}
                      </span>
                    </td>
                    <td className="p-6 text-right">
                      <div className="flex items-center justify-end gap-3">
                        {/* L3 fix: Edit wired to PUT /api/v1/alerts/{ruleId} */}
                        <button
                          onClick={() => openEdit(alert)}
                          className="w-9 h-9 flex items-center justify-center rounded-xl bg-zinc-800 text-blue-400 hover:bg-blue-500 hover:text-white transition-all shadow-sm"
                          title="Edit rule"
                        >
                          ✎
                        </button>
                        <button
                          onClick={() => deleteAlert(alert)}
                          className="w-9 h-9 flex items-center justify-center rounded-xl bg-zinc-800 text-rose-400 hover:bg-rose-500 hover:text-white transition-all shadow-sm"
                          title="Delete rule"
                        >
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

'use client'

import { useState, useEffect } from "react";
import { fetchApi } from "@/lib/api";
import Link from "next/link";
import { Toast } from "@/components/Toast";
import { useNotifications } from "@/components/NotificationProvider";

interface Notification {
  id: string;
  message: string;
  tickerSymbol: string | null;
  isRead: boolean;
  createdAt: string;
}

export default function NotificationsPage() {
  const { decrementCount, markAllAsRead: markAllInBadge } = useNotifications();
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [toast, setToast] = useState<{message: string, type: 'success' | 'error'} | null>(null);

  const loadNotifications = async () => {
    try {
      setLoading(true);
      const data = await fetchApi("/api/v1/notifications");
      setNotifications(data.items || []);
    } catch (err: any) {
      setError(err.message || "Failed to load notifications");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadNotifications();
  }, []);

  const markAsRead = async (id: string) => {
    try {
      await fetchApi(`/api/v1/notifications/${id}/read`, { method: 'PATCH' });
      setNotifications(prev => prev.map(n => n.id === id ? { ...n, isRead: true } : n));
      decrementCount();
    } catch (err: any) {
      setToast({ message: err.message || "Failed to mark as read", type: 'error' });
    }
  };

  const markAllAsRead = async () => {
    try {
      await fetchApi("/api/v1/notifications/read-all", { method: 'PATCH' });
      setNotifications(prev => prev.map(n => ({ ...n, isRead: true })));
      markAllInBadge();
      setToast({ message: "All notifications marked as read", type: 'success' });
    } catch (err: any) {
      setToast({ message: err.message || "Failed to mark all as read", type: 'error' });
    }
  };

  const deleteNotification = async (id: string) => {
    try {
      const isUnread = !notifications.find(n => n.id === id)?.isRead;
      await fetchApi(`/api/v1/notifications/${id}`, { method: 'DELETE' });
      setNotifications(prev => prev.filter(n => n.id !== id));
      if (isUnread) decrementCount();
      setToast({ message: "Notification dismissed", type: 'success' });
    } catch (err: any) {
      setToast({ message: err.message || "Failed to dismiss notification", type: 'error' });
    }
  };

  return (
    <div className="max-w-4xl mx-auto space-y-10">
      {toast && <Toast message={toast.message} type={toast.type} onClose={() => setToast(null)} />}
      
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-4xl font-semibold tracking-tight text-zinc-900 dark:text-white uppercase">Notifications</h1>
          <p className="text-zinc-500 dark:text-zinc-400 mt-2 font-medium">History of your triggered alerts and system messages.</p>
        </div>
        {notifications.some(n => !n.isRead) && (
          <button 
            onClick={markAllAsRead}
            className="text-xs font-semibold uppercase tracking-wider text-blue-500 hover:text-blue-600 transition-colors"
          >
            Mark all as read
          </button>
        )}
      </div>

      <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-white/5 rounded-3xl overflow-hidden shadow-sm dark:shadow-none">
        {loading ? (
          <div className="p-8 space-y-4">
            {[1, 2, 3].map(i => (
              <div key={i} className="h-20 bg-zinc-100 dark:bg-zinc-800 rounded-2xl animate-pulse"></div>
            ))}
          </div>
        ) : notifications.length === 0 ? (
          <div className="p-20 text-center">
            <p className="text-zinc-500 font-bold uppercase tracking-wider text-sm">You have no notifications</p>
          </div>
        ) : (
          <div className="divide-y divide-zinc-200 dark:divide-white/5">
            {notifications.map((n) => (
              <div 
                key={n.id} 
                className={`p-6 flex items-start gap-4 hover:bg-zinc-50 dark:hover:bg-white/[0.02] transition-colors group ${!n.isRead ? "bg-blue-500/[0.02] dark:bg-blue-500/[0.01]" : ""}`}
              >
                {!n.isRead && <div className="w-2 h-2 rounded-full bg-blue-500 mt-2.5 shrink-0 shadow-[0_0_8px_rgba(59,130,246,0.5)]"></div>}
                
                <div className="flex-1 space-y-1">
                  <div className="flex justify-between items-start">
                    <p className={`font-bold leading-snug ${!n.isRead ? "text-zinc-900 dark:text-white" : "text-zinc-500"}`}>
                      {n.message}
                    </p>
                    <button 
                      onClick={() => deleteNotification(n.id)}
                      className="opacity-0 group-hover:opacity-100 p-2 text-zinc-400 hover:text-rose-500 transition-all"
                      title="Dismiss"
                    >
                      ×
                    </button>
                  </div>
                  
                  <div className="flex items-center gap-3">
                    {n.tickerSymbol && (
                      <Link 
                        href={`/stocks/${n.tickerSymbol.toLowerCase()}`}
                        className="text-xs font-semibold uppercase tracking-wider text-blue-500 hover:underline"
                      >
                        {n.tickerSymbol}
                      </Link>
                    )}
                    <span className="text-xs font-bold text-zinc-400 uppercase tracking-wider">
                      {new Date(n.createdAt).toLocaleString([], { dateStyle: 'medium', timeStyle: 'short' })}
                    </span>
                    {!n.isRead && (
                      <button 
                        onClick={() => markAsRead(n.id)}
                        className="text-xs font-semibold uppercase tracking-wider text-emerald-500 hover:underline"
                      >
                        Mark as read
                      </button>
                    )}
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

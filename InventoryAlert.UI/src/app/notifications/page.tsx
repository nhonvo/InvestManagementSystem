'use client'

import { useState, useEffect } from "react";
import { fetchApi } from "@/lib/api";
import Link from "next/link";
import { Toast } from "@/components/Toast";
import { useNotifications } from "@/components/NotificationProvider";
import Pagination from "@/components/ui/Pagination";
import { getErrorMessage } from "@/lib/error-utils";

// Unified notification categories and severities (Matching Domain enums)
enum NotificationType {
  Price = 0,
  Holdings = 1,
  System = 2,
  News = 3
}

enum NotificationSeverity {
  Info = 0,
  Warning = 1,
  Critical = 2
}

interface Notification {
  id: string;
  message: string;
  tickerSymbol: string | null;
  type: NotificationType;
  severity: NotificationSeverity;
  isRead: boolean;
  createdAt: string;
}

export default function NotificationsPage() {
  const { decrementCount, markAllAsRead: markAllInBadge, refreshUnreadCount } = useNotifications();
  
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState("");
  const [toast, setToast] = useState<{message: string, type: 'success' | 'error'} | null>(null);
  
  // Paging state
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const pageSize = 15;

  const loadNotifications = async (targetPage: number, showRefreshing = false) => {
    try {
      if (showRefreshing) setRefreshing(true);
      else setLoading(true);
      setError("");

      const data = await fetchApi(`/api/v1/notifications?page=${targetPage}&pageSize=${pageSize}`);
      setNotifications(data.items || []);
      setTotalPages(data.totalPages || 1);
      setPage(data.page || targetPage);
      
      await refreshUnreadCount(); // Sync badge
    } catch (err: any) {
      const msg = getErrorMessage(err);
      setError(msg);
      setToast({ message: msg, type: 'error' });
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  useEffect(() => {
    loadNotifications(1);
  }, []);

  const handlePageChange = (newPage: number) => {
    loadNotifications(newPage);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  const markAsRead = async (id: string) => {
    try {
      await fetchApi(`/api/v1/notifications/${id}/read`, { method: 'PATCH' });
      setNotifications(prev => prev.map(n => n.id === id ? { ...n, isRead: true } : n));
      decrementCount();
    } catch (err: any) {
      setToast({ message: getErrorMessage(err), type: 'error' });
    }
  };

  const markAllAsRead = async () => {
    try {
      await fetchApi("/api/v1/notifications/read-all", { method: 'PATCH' });
      setNotifications(prev => prev.map(n => ({ ...n, isRead: true })));
      markAllInBadge();
      setToast({ message: "All notifications marked as read", type: 'success' });
    } catch (err: any) {
      setToast({ message: getErrorMessage(err), type: 'error' });
    }
  };

  const deleteNotification = async (id: string) => {
    try {
      const isUnread = !notifications.find(n => n.id === id)?.isRead;
      await fetchApi(`/api/v1/notifications/${id}`, { method: 'DELETE' });
      setNotifications(prev => prev.filter(n => n.id !== id));
      if (isUnread) decrementCount();
      setToast({ message: "Notification dismissed", type: 'success' });
      
      // If we deleted the last item on a page, go back one page
      if (notifications.length === 1 && page > 1) {
          handlePageChange(page - 1);
      }
    } catch (err: any) {
      setToast({ message: getErrorMessage(err), type: 'error' });
    }
  };

  const getSeverityStyles = (severity: NotificationSeverity) => {
    switch (severity) {
      case NotificationSeverity.Critical: return "bg-rose-500/10 text-rose-500 border-rose-500/20";
      case NotificationSeverity.Warning: return "bg-amber-500/10 text-amber-500 border-amber-500/20";
      default: return "bg-blue-500/10 text-blue-500 border-blue-500/20";
    }
  };

  const getTypeIcon = (type: NotificationType) => {
    switch (type) {
      case NotificationType.Price: return "📈";
      case NotificationType.Holdings: return "📦";
      case NotificationType.News: return "📰";
      default: return "⚙️";
    }
  };

  return (
    <div className="max-w-4xl mx-auto space-y-8">
      {toast && <Toast message={toast.message} type={toast.type} onClose={() => setToast(null)} />}
      
      <div className="flex justify-between items-end">
        <div>
          <h1 className="text-4xl font-semibold tracking-tight text-zinc-900 dark:text-white uppercase flex items-center gap-4">
            Notifications
            <button 
              onClick={() => loadNotifications(page, true)}
              disabled={loading || refreshing}
              className={`p-2 rounded-full hover:bg-zinc-100 dark:hover:bg-white/5 transition-all ${refreshing ? "animate-spin" : ""}`}
              title="Refresh"
            >
              <svg className="w-5 h-5 text-zinc-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={3}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
              </svg>
            </button>
          </h1>
          <p className="text-zinc-500 dark:text-zinc-400 mt-2 font-medium">History of your triggered alerts and system messages.</p>
        </div>
        <div className="flex items-center gap-4 mb-1">
          {notifications.some(n => !n.isRead) && (
            <button 
              onClick={markAllAsRead}
              className="text-xs font-semibold uppercase tracking-wider text-blue-500 hover:text-blue-600 transition-colors"
            >
              Mark all as read
            </button>
          )}
        </div>
      </div>

      {error && !loading && (
          <div className="p-4 bg-rose-500/10 border border-rose-500/20 rounded-2xl text-rose-500 text-sm font-medium">
              {error}
          </div>
      )}

      <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-white/5 rounded-3xl overflow-hidden shadow-sm dark:shadow-none">
        {loading && !refreshing ? (
          <div className="p-8 space-y-4">
            {[1, 2, 3].map(i => (
              <div key={i} className="h-24 bg-zinc-100 dark:bg-zinc-800 rounded-2xl animate-pulse"></div>
            ))}
          </div>
        ) : notifications.length === 0 ? (
          <div className="p-20 text-center">
            <p className="text-zinc-500 font-bold uppercase tracking-wider text-sm">You have no notifications</p>
          </div>
        ) : (
          <>
            <div className="divide-y divide-zinc-200 dark:divide-white/5">
              {notifications.map((n) => (
                <div 
                  key={n.id} 
                  className={`p-6 flex items-start gap-4 hover:bg-zinc-50 dark:hover:bg-white/[0.02] transition-colors group ${!n.isRead ? "bg-blue-500/[0.02] dark:bg-blue-500/[0.01]" : ""}`}
                >
                  <div className="mt-1 flex flex-col items-center gap-2">
                      <div className={`w-10 h-10 rounded-xl flex items-center justify-center text-lg border ${getSeverityStyles(n.severity)} shadow-sm`}>
                          {getTypeIcon(n.type)}
                      </div>
                      {!n.isRead && <div className="w-1.5 h-1.5 rounded-full bg-blue-500 shadow-[0_0_8px_rgba(59,130,246,0.5)]"></div>}
                  </div>
                  
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
                        <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                          <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
                        </svg>
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
            
            <div className="border-t border-zinc-200 dark:border-white/5">
                <Pagination 
                    currentPage={page} 
                    totalPages={totalPages} 
                    onPageChange={handlePageChange} 
                    isLoading={loading || refreshing} 
                />
            </div>
          </>
        )}
      </div>
    </div>
  );
}

'use client'

import { useState, useEffect } from "react";
import { fetchApi } from "@/lib/api";
import Link from "next/link";

export default function NotificationBell() {
  const [unreadCount, setUnreadCount] = useState(0);
  const [mounted, setMounted] = useState(false);

  useEffect(() => {
    setMounted(true);
    const checkNotifications = async () => {
      try {
        const data = await fetchApi("/api/v1/notifications/unread-count");
        if (data && typeof data.count === 'number') {
          setUnreadCount(data.count);
        }
      } catch (err) {
        console.error("Failed to fetch notification count", err);
      }
    };

    checkNotifications();
    const interval = setInterval(checkNotifications, 30000); // Poll every 30s as per spec
    return () => clearInterval(interval);
  }, []);

  if (!mounted) return null;

  return (
    <Link 
      href="/notifications" 
      className="relative p-2 rounded-xl bg-zinc-100 dark:bg-zinc-900 border border-zinc-200 dark:border-white/5 text-zinc-600 dark:text-zinc-400 hover:text-blue-500 transition-all shadow-sm group"
    >
      <span className="text-lg group-hover:scale-110 transition-transform block">🔔</span>
      {unreadCount > 0 && (
        <span className="absolute -top-1 -right-1 flex h-5 w-5 items-center justify-center rounded-full bg-rose-500 text-xs font-semibold text-white ring-2 ring-white dark:ring-[#050505] animate-bounce">
          {unreadCount > 9 ? '9+' : unreadCount}
        </span>
      )}
    </Link>
  );
}

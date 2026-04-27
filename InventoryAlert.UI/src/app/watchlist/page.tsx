'use client'

import { useState, useEffect } from "react";
import Link from "next/link";
import { fetchApi } from "@/lib/api";
import { AddSymbolModal } from "@/components/AddSymbolModal";
import { Toast } from "@/components/Toast";
import { ConfirmDialog } from "@/components/ConfirmDialog";
import Pagination from "@/components/ui/Pagination";
import { getErrorMessage } from "@/lib/error-utils";

interface WatchlistItem {
  symbol: string;
  name: string;
  currentPrice: number;
  change: number;
  logo?: string;
}

export default function WatchlistPage() {
  const [watchlist, setWatchlist] = useState<WatchlistItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [toast, setToast] = useState<{message: string, type: 'success' | 'error'} | null>(null);
  const [confirmDelete, setConfirmDelete] = useState<string | null>(null);

  const [page, setPage] = useState(1);
  const pageSize = 9;

  const loadWatchlist = async () => {
    try {
      setLoading(true);
      const data = await fetchApi("/api/v1/watchlist");
      setWatchlist(data || []);
      setPage(1);
    } catch (err: any) {
      setError(getErrorMessage(err));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadWatchlist();
  }, []);

  const removeFromWatchlist = async (symbol: string) => {
    try {
      const wasLastOnPage = watchlist.length > 0 && watchlist.slice((page - 1) * pageSize, page * pageSize).length === 1;
      await fetchApi(`/api/v1/watchlist/${symbol}`, { method: 'DELETE' });
      setWatchlist(prev => prev.filter(item => item.symbol !== symbol));
      setToast({ message: `${symbol} removed from watchlist`, type: 'success' });
      if (wasLastOnPage && page > 1) setPage(page - 1);
    } catch (err: any) {
      setToast({ message: getErrorMessage(err), type: 'error' });
    } finally {
      setConfirmDelete(null);
    }
  };

  const totalPages = Math.max(1, Math.ceil(watchlist.length / pageSize));
  const pageItems = watchlist.slice((page - 1) * pageSize, page * pageSize);

  return (
    <div className="max-w-7xl mx-auto space-y-10">
      <AddSymbolModal 
        isOpen={isModalOpen} 
        onClose={() => setIsModalOpen(false)} 
        onSuccess={() => { loadWatchlist(); setToast({ message: "Symbol added successfully", type: 'success' }); }}
      />
      
      {toast && <Toast message={toast.message} type={toast.type} onClose={() => setToast(null)} />}
      
      <ConfirmDialog
        isOpen={!!confirmDelete}
        title="Remove Symbol"
        message={`Are you sure you want to remove ${confirmDelete} from your watchlist?`}
        confirmText="Remove"
        type="danger"
        onConfirm={() => confirmDelete && removeFromWatchlist(confirmDelete)}
        onCancel={() => setConfirmDelete(null)}
      />

      <div className="flex flex-col md:flex-row justify-between items-start md:items-center gap-6">
        <div>
          <h1 className="text-4xl font-semibold tracking-tight text-zinc-900 dark:text-white uppercase">Watchlist</h1>
          <p className="text-zinc-500 dark:text-zinc-400 mt-2 font-medium">Keep a close eye on interesting symbols without opening positions.</p>
        </div>
        <button 
          onClick={() => setIsModalOpen(true)}
          className="px-8 py-4 bg-blue-600 hover:bg-blue-700 text-white font-semibold rounded-2xl shadow-xl shadow-blue-500/20 transition-all active:scale-95 uppercase tracking-wider text-xs"
        >
          + Add New Symbol
        </button>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {loading ? (
          Array.from({ length: 9 }).map((_, i) => (
            <div key={i} className="h-44 bg-zinc-100 dark:bg-zinc-800 rounded-3xl animate-pulse"></div>
          ))
        ) : watchlist.length === 0 ? (
          <div className="lg:col-span-3 py-32 text-center bg-zinc-50 dark:bg-zinc-900 rounded-[2.5rem] border border-dashed border-zinc-200 dark:border-white/10">
             <p className="text-zinc-500 font-bold uppercase tracking-wider text-xs mb-6 px-10">Your watchlist is empty</p>
             <button onClick={() => setIsModalOpen(true)} className="text-blue-500 font-semibold uppercase text-xs hover:underline decoration-2">Find your first stock →</button>
          </div>
        ) : (
          pageItems.map((item) => (
            <div key={item.symbol} className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-white/5 rounded-[2.5rem] p-8 hover:border-blue-500/50 transition-all group relative group shadow-sm dark:shadow-none">
                <button 
                    onClick={() => setConfirmDelete(item.symbol)}
                    className="absolute top-6 right-6 p-2 bg-rose-500/10 text-rose-500 rounded-xl opacity-0 group-hover:opacity-100 transition-opacity hover:bg-rose-500 hover:text-white"
                >
                    ✕
                </button>
                <div className="flex items-center gap-4 mb-8">
                    <div className="w-14 h-14 bg-zinc-50 dark:bg-zinc-800 rounded-2xl flex items-center justify-center p-2 border border-zinc-100 dark:border-white/5 overflow-hidden">
                        {item.logo ? <img src={item.logo} alt="" className="max-w-full max-h-full object-contain" /> : <span className="text-xs font-bold text-zinc-500">{item.symbol}</span>}
                    </div>
                    <div>
                        <Link href={`/stocks/${item.symbol.toLowerCase()}`}>
                            <h3 className="text-2xl font-semibold text-zinc-900 dark:text-white uppercase tracking-tight hover:text-blue-500 transition-colors cursor-pointer">{item.symbol}</h3>
                        </Link>
                        <p className="text-xs font-semibold uppercase tracking-wider text-zinc-400 dark:text-zinc-500 truncate max-w-[150px]">{item.name}</p>
                    </div>
                </div>
                <div className="flex justify-between items-end">
                    <div>
                        <p className="text-3xl font-semibold text-zinc-900 dark:text-white tracking-tight">${(item.currentPrice || 0).toFixed(2)}</p>
                        <p className={`text-xs font-semibold uppercase tracking-wider ${(item.change || 0) >= 0 ? "text-emerald-500" : "text-rose-500"}`}>
                            {(item.change || 0) >= 0 ? "+" : ""}{(item.change || 0).toFixed(2)}%
                        </p>
                    </div>
                    <Link href={`/stocks/${item.symbol.toLowerCase()}`} className="px-4 py-2 bg-zinc-100 dark:bg-zinc-800 text-xs font-semibold uppercase tracking-wider rounded-xl hover:bg-blue-600 hover:text-white transition-all">
                        Deep Dive
                    </Link>
                </div>
            </div>
          ))
        )}
      </div>

      {!loading && watchlist.length > 0 && (
        <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-white/5 rounded-3xl overflow-hidden shadow-sm dark:shadow-none">
          <Pagination
            currentPage={page}
            totalPages={totalPages}
            onPageChange={setPage}
            isLoading={loading}
          />
        </div>
      )}
    </div>
  );
}

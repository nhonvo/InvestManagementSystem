'use client'

import { useState, useEffect } from "react";
import Link from "next/link";
import { fetchApi } from "@/lib/api";

// Spec §4.4: StockProfileResponse (used for catalog browse)
interface StockListing {
  id: number;
  symbol: string;
  name: string;
  exchange: string;
  currency: string;
  country: string;
  industry: string;
  marketCap: number;
}

// Spec §4.4: SymbolSearchResponse
interface SymbolSearchResult {
  symbol: string;
  description: string;
  type: string;
  exchange: string;
}

export default function StockCatalog() {
  const [stocks, setStocks] = useState<StockListing[]>([]);
  const [searchResults, setSearchResults] = useState<SymbolSearchResult[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [isSearchMode, setIsSearchMode] = useState(false);

  // Spec §5.3: GET /api/v1/stocks — browse catalog with pagination
  const loadStocks = async (page: number) => {
    try {
      setLoading(true);
      const data = await fetchApi(`/api/v1/stocks?page=${page}&pageSize=20`);
      setStocks(data.items || []);
      setTotalPages(data.totalPages || 1);
    } catch (err) {
      console.error("Failed to load stocks", err);
    } finally {
      setLoading(false);
    }
  };

  // Spec §5.3: GET /api/v1/stocks/search?q= — dedicated search endpoint
  const searchStocks = async (q: string) => {
    try {
      setLoading(true);
      const data = await fetchApi(`/api/v1/stocks/search?q=${encodeURIComponent(q)}`);
      setSearchResults(data || []);
    } catch (err) {
      console.error("Failed to search stocks", err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (!search.trim()) {
      setIsSearchMode(false);
      setSearchResults([]);
      loadStocks(1);
      setCurrentPage(1);
      return;
    }
    setIsSearchMode(true);
    const timer = setTimeout(() => searchStocks(search.trim()), 300);
    return () => clearTimeout(timer);
  }, [search]);

  useEffect(() => {
    if (!isSearchMode) {
      loadStocks(currentPage);
    }
  }, [currentPage]);

  return (
    <div className="max-w-7xl mx-auto space-y-10">
      <div className="flex flex-col md:flex-row justify-between items-start md:items-center gap-6">
        <div>
          <h1 className="text-4xl font-semibold tracking-tight text-zinc-900 dark:text-white uppercase">Market Catalog</h1>
          <p className="text-zinc-500 dark:text-zinc-400 mt-2 font-medium">Browse thousands of global security listings.</p>
        </div>

        <div className="relative w-full max-w-md">
          <input
            type="text"
            placeholder="Search by name or ticker..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="w-full bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-white/5 rounded-2xl px-6 py-4 font-bold text-sm focus:ring-2 focus:ring-blue-500 outline-none transition-all"
          />
          <div className="absolute right-5 top-1/2 -translate-y-1/2 text-zinc-400">🔍</div>
        </div>
      </div>

      <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-white/5 rounded-3xl overflow-hidden shadow-sm dark:shadow-none">
        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="border-b border-zinc-200 dark:border-white/5 bg-zinc-50/50 dark:bg-white/5">
                <th className="px-6 py-5 text-xs font-semibold uppercase tracking-wider text-zinc-400">Ticker</th>
                <th className="px-6 py-5 text-xs font-semibold uppercase tracking-wider text-zinc-400">Company Name</th>
                <th className="px-6 py-5 text-xs font-semibold uppercase tracking-wider text-zinc-400">Exchange</th>
                <th className="px-6 py-5 text-xs font-semibold uppercase tracking-wider text-zinc-400">
                  {isSearchMode ? "Type" : "Industry"}
                </th>
                {!isSearchMode && (
                  <th className="px-6 py-5 text-xs font-semibold uppercase tracking-wider text-zinc-400 text-right">Market Cap</th>
                )}
                <th className="px-6 py-5 text-xs font-semibold uppercase tracking-wider text-zinc-400 text-center">Action</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-zinc-200 dark:divide-white/5">
              {loading && (isSearchMode ? searchResults : stocks).length === 0 ? (
                Array.from({ length: 10 }).map((_, i) => (
                  <tr key={i} className="animate-pulse">
                    <td colSpan={isSearchMode ? 5 : 6} className="px-6 py-6">
                      <div className="h-6 bg-zinc-100 dark:bg-zinc-800 rounded-lg w-full"></div>
                    </td>
                  </tr>
                ))
              ) : isSearchMode ? (
                searchResults.length === 0 ? (
                  <tr>
                    <td colSpan={5} className="px-6 py-20 text-center">
                      <p className="text-zinc-500 font-bold uppercase tracking-wider text-xs">No results for "{search}"</p>
                    </td>
                  </tr>
                ) : (
                  searchResults.map((r, i) => (
                    <tr key={`${r.symbol}-${i}`} className="hover:bg-zinc-50 dark:hover:bg-white/[0.02] transition-colors group">
                      <td className="px-6 py-5">
                        <span className="font-semibold text-blue-500 dark:text-blue-400 group-hover:underline text-lg uppercase tracking-tight">
                          <Link href={`/stocks/${r.symbol.toLowerCase()}`}>{r.symbol}</Link>
                        </span>
                      </td>
                      <td className="px-6 py-5 font-bold text-zinc-900 dark:text-white uppercase text-sm tracking-tight">{r.description}</td>
                      <td className="px-6 py-5">
                        <span className="text-xs font-semibold uppercase tracking-wider bg-zinc-100 dark:bg-zinc-800 text-zinc-500 px-2 py-1 rounded">
                          {r.exchange}
                        </span>
                      </td>
                      <td className="px-6 py-5 text-xs font-medium text-zinc-500 uppercase">{r.type}</td>
                      <td className="px-6 py-5 text-center">
                        <Link
                          href={`/stocks/${r.symbol.toLowerCase()}`}
                          className="text-xs font-semibold uppercase tracking-wider text-blue-500 hover:text-blue-600"
                        >
                          VIEW DETAIL
                        </Link>
                      </td>
                    </tr>
                  ))
                )
              ) : stocks.length === 0 ? (
                <tr>
                  <td colSpan={6} className="px-6 py-20 text-center">
                    <p className="text-zinc-500 font-bold uppercase tracking-wider text-xs">No securities found.</p>
                  </td>
                </tr>
              ) : (
                stocks.map((s, i) => (
                  <tr key={`${s.symbol}-${i}`} className="hover:bg-zinc-50 dark:hover:bg-white/[0.02] transition-colors group">
                    <td className="px-6 py-5">
                      <span className="font-semibold text-blue-500 dark:text-blue-400 group-hover:underline text-lg uppercase tracking-tight">
                        <Link href={`/stocks/${s.symbol.toLowerCase()}`}>{s.symbol}</Link>
                      </span>
                    </td>
                    <td className="px-6 py-5 font-bold text-zinc-900 dark:text-white uppercase text-sm tracking-tight">{s.name}</td>
                    <td className="px-6 py-5">
                      <span className="text-xs font-semibold uppercase tracking-wider bg-zinc-100 dark:bg-zinc-800 text-zinc-500 px-2 py-1 rounded">
                        {s.exchange}
                      </span>
                    </td>
                    <td className="px-6 py-5 text-xs font-medium text-zinc-500 uppercase">{s.industry}</td>
                    <td className="px-6 py-5 text-right font-semibold text-zinc-700 dark:text-zinc-300">
                      ${((s.marketCap || 0) / 1000).toFixed(1)}B
                    </td>
                    <td className="px-6 py-5 text-center">
                      <Link
                        href={`/stocks/${s.symbol.toLowerCase()}`}
                        className="text-xs font-semibold uppercase tracking-wider text-blue-500 hover:text-blue-600"
                      >
                        VIEW DETAIL
                      </Link>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      {!isSearchMode && (
        <div className="flex items-center justify-between pb-10">
          <p className="text-xs font-bold text-zinc-500 uppercase tracking-wider">
            Page {currentPage} of {totalPages}
          </p>
          <div className="flex gap-2">
            <button
              disabled={currentPage === 1 || loading}
              onClick={() => setCurrentPage((p) => p - 1)}
              className="px-4 py-2 bg-zinc-100 dark:bg-zinc-800 text-zinc-900 dark:text-white rounded-xl text-xs font-semibold uppercase disabled:opacity-50"
            >
              Prev
            </button>
            <button
              disabled={currentPage === totalPages || loading}
              onClick={() => setCurrentPage((p) => p + 1)}
              className="px-4 py-2 bg-zinc-100 dark:bg-zinc-800 text-zinc-900 dark:text-white rounded-xl text-xs font-semibold uppercase disabled:opacity-50"
            >
              Next
            </button>
          </div>
        </div>
      )}
    </div>
  );
}

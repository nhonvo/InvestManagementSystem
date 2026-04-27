'use client'

import React from 'react';

interface PaginationProps {
    currentPage: number;
    totalPages?: number;
    hasNextPage?: boolean;
    onPageChange: (page: number) => void;
    isLoading?: boolean;
}

export default function Pagination({ currentPage, totalPages, hasNextPage, onPageChange, isLoading }: PaginationProps) {
    const isKnownTotal = typeof totalPages === 'number' && Number.isFinite(totalPages);
    if (isKnownTotal && (totalPages as number) <= 1) return null;

    const clampPage = (page: number) => {
        if (!isKnownTotal) return Math.max(1, page);
        return Math.max(1, Math.min(totalPages as number, page));
    };

    if (!isKnownTotal) {
        return (
            <div className="flex items-center justify-between px-4 py-3 sm:px-6">
                <div className="flex items-center gap-3">
                    <button
                        onClick={() => onPageChange(clampPage(currentPage - 1))}
                        disabled={currentPage <= 1 || isLoading}
                        className="relative inline-flex items-center rounded-md border border-zinc-300 bg-white px-4 py-2 text-sm font-medium text-zinc-700 hover:bg-zinc-50 disabled:opacity-50 dark:border-white/10 dark:bg-zinc-900 dark:text-zinc-200 dark:hover:bg-white/5"
                    >
                        Previous
                    </button>
                    <button
                        onClick={() => onPageChange(clampPage(currentPage + 1))}
                        disabled={!hasNextPage || isLoading}
                        className="relative inline-flex items-center rounded-md border border-zinc-300 bg-white px-4 py-2 text-sm font-medium text-zinc-700 hover:bg-zinc-50 disabled:opacity-50 dark:border-white/10 dark:bg-zinc-900 dark:text-zinc-200 dark:hover:bg-white/5"
                    >
                        Next
                    </button>
                </div>
                <p className="text-sm text-zinc-700 dark:text-zinc-400">
                    Page <span className="font-medium">{currentPage}</span>
                </p>
            </div>
        );
    }

    const maxButtons = 5;
    const half = Math.floor(maxButtons / 2);
    const rawStart = Math.max(1, currentPage - half);
    const end = Math.min(totalPages as number, rawStart + maxButtons - 1);
    const start = Math.max(1, end - maxButtons + 1);
    const pages = Array.from({ length: end - start + 1 }, (_, i) => start + i);

    return (
        <div className="flex items-center justify-between px-4 py-3 sm:px-6">
            <div className="flex flex-1 justify-between sm:hidden">
                <button
                    onClick={() => onPageChange(clampPage(currentPage - 1))}
                    disabled={currentPage <= 1 || isLoading}
                    className="relative inline-flex items-center rounded-md border border-zinc-300 bg-white px-4 py-2 text-sm font-medium text-zinc-700 hover:bg-zinc-50 disabled:opacity-50 dark:border-white/10 dark:bg-zinc-900 dark:text-zinc-200 dark:hover:bg-white/5"
                >
                    Previous
                </button>
                <button
                    onClick={() => onPageChange(clampPage(currentPage + 1))}
                    disabled={currentPage >= totalPages || isLoading}
                    className="relative ml-3 inline-flex items-center rounded-md border border-zinc-300 bg-white px-4 py-2 text-sm font-medium text-zinc-700 hover:bg-zinc-50 disabled:opacity-50 dark:border-white/10 dark:bg-zinc-900 dark:text-zinc-200 dark:hover:bg-white/5"
                >
                    Next
                </button>
            </div>
            <div className="hidden sm:flex sm:flex-1 sm:items-center sm:justify-between">
                <div>
                    <p className="text-sm text-zinc-700 dark:text-zinc-400">
                        Page <span className="font-medium">{currentPage}</span> of{' '}
                        <span className="font-medium">{totalPages}</span>
                    </p>
                </div>
                <div>
                    <nav className="isolate inline-flex -space-x-px rounded-md shadow-sm" aria-label="Pagination">
                        <button
                            onClick={() => onPageChange(clampPage(currentPage - 1))}
                            disabled={currentPage <= 1 || isLoading}
                            className="relative inline-flex items-center rounded-l-md px-2 py-2 text-zinc-400 ring-1 ring-inset ring-zinc-300 hover:bg-zinc-50 focus:z-20 focus:outline-offset-0 disabled:opacity-50"
                        >
                            <span className="sr-only">Previous</span>
                            <span>←</span>
                        </button>
                        
                        {start > 1 && (
                            <>
                                <button
                                    onClick={() => onPageChange(1)}
                                    disabled={isLoading}
                                    className={`relative inline-flex items-center px-4 py-2 text-sm font-semibold ${
                                        currentPage === 1
                                            ? 'z-10 bg-blue-600 text-white focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-blue-600'
                                            : 'text-zinc-900 ring-1 ring-inset ring-zinc-300 hover:bg-zinc-50 focus:z-20 focus:outline-offset-0 dark:text-zinc-300'
                                    }`}
                                >
                                    1
                                </button>
                                <span className="relative inline-flex items-center px-4 py-2 text-sm text-zinc-500 ring-1 ring-inset ring-zinc-300 dark:text-zinc-400">
                                    …
                                </span>
                            </>
                        )}

                        {pages.map((pageNum) => (
                            <button
                                key={pageNum}
                                onClick={() => onPageChange(pageNum)}
                                disabled={isLoading}
                                className={`relative inline-flex items-center px-4 py-2 text-sm font-semibold ${
                                    currentPage === pageNum
                                        ? 'z-10 bg-blue-600 text-white focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-blue-600'
                                        : 'text-zinc-900 ring-1 ring-inset ring-zinc-300 hover:bg-zinc-50 focus:z-20 focus:outline-offset-0 dark:text-zinc-300'
                                }`}
                            >
                                {pageNum}
                            </button>
                        ))}

                        {end < totalPages && (
                            <>
                                <span className="relative inline-flex items-center px-4 py-2 text-sm text-zinc-500 ring-1 ring-inset ring-zinc-300 dark:text-zinc-400">
                                    …
                                </span>
                                <button
                                    onClick={() => onPageChange(totalPages)}
                                    disabled={isLoading}
                                    className={`relative inline-flex items-center px-4 py-2 text-sm font-semibold ${
                                        currentPage === totalPages
                                            ? 'z-10 bg-blue-600 text-white focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-blue-600'
                                            : 'text-zinc-900 ring-1 ring-inset ring-zinc-300 hover:bg-zinc-50 focus:z-20 focus:outline-offset-0 dark:text-zinc-300'
                                    }`}
                                >
                                    {totalPages}
                                </button>
                            </>
                        )}

                        <button
                            onClick={() => onPageChange(clampPage(currentPage + 1))}
                            disabled={currentPage >= totalPages || isLoading}
                            className="relative inline-flex items-center rounded-r-md px-2 py-2 text-zinc-400 ring-1 ring-inset ring-zinc-300 hover:bg-zinc-50 focus:z-20 focus:outline-offset-0 disabled:opacity-50"
                        >
                            <span className="sr-only">Next</span>
                            <span>→</span>
                        </button>
                    </nav>
                </div>
            </div>
        </div>
    );
}

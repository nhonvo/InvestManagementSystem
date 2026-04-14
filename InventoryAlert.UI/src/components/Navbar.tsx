'use client'

import { useState, useEffect } from 'react'
import Link from 'next/link'
import { usePathname } from 'next/navigation'
import UserNav from './UserNav'

const NAV_LINKS = [
  { href: '/', label: 'Overview' },
  { href: '/market', label: 'Market' },
  { href: '/stocks', label: 'Stocks' },
  { href: '/portfolio', label: 'Portfolio' },
  { href: '/watchlist', label: 'Watchlist' },
  { href: '/alerts', label: 'Alerts' },
] as const

interface NavbarProps {
  onSearchOpen: () => void
}

export default function Navbar({ onSearchOpen }: NavbarProps) {
  const pathname = usePathname()
  const [menuOpen, setMenuOpen] = useState(false)
  const [scrolled, setScrolled] = useState(false)

  useEffect(() => {
    const onScroll = () => setScrolled(window.scrollY > 8)
    window.addEventListener('scroll', onScroll, { passive: true })
    return () => window.removeEventListener('scroll', onScroll)
  }, [])

  // Close mobile menu on route change
  useEffect(() => {
    setMenuOpen(false)
  }, [pathname])

  const isActive = (href: string) =>
    href === '/' ? pathname === '/' : pathname.startsWith(href)

  return (
    <>
      <header
        className={`sticky top-0 z-50 border-b transition-all duration-500 ${
          scrolled
            ? 'border-zinc-200 dark:border-white/10 bg-white/70 dark:bg-black/70 backdrop-blur-3xl shadow-xl shadow-zinc-900/5 dark:shadow-black/50 py-1'
            : 'border-transparent bg-transparent py-3'
        }`}
      >
        <div className="max-w-[1400px] mx-auto px-6 h-14 flex items-center justify-between gap-6">
          {/* ── Logo ── */}
          <Link href="/" className="shrink-0 group flex items-center gap-2" aria-label="Go to dashboard">
            <div className="w-8 h-8 bg-blue-600 rounded-xl flex items-center justify-center shadow-lg shadow-blue-600/20 group-hover:shadow-blue-600/40 group-hover:scale-105 transition-all">
                <svg className="w-5 h-5 text-white" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={3}>
                    <path strokeLinecap="round" strokeLinejoin="round" d="M13 7h8m0 0v8m0-8l-8 8-4-4-6 6" />
                </svg>
            </div>
            <span className="text-xl font-black tracking-tight text-zinc-900 dark:text-white inline-block">
              INV<span className="text-blue-600 dark:text-blue-500">ALERT</span>
            </span>
          </Link>

          {/* ── Desktop nav ── */}
          <nav className="hidden lg:flex items-center gap-1 bg-white/50 dark:bg-white/5 backdrop-blur-md px-2 py-1.5 rounded-2xl border border-zinc-200 dark:border-white/10 shadow-sm" aria-label="Main navigation">
            {NAV_LINKS.map(({ href, label }) => {
              const active = isActive(href);
              return (
                <Link
                  key={href}
                  href={href}
                  className={`relative px-4 py-2 text-xs font-bold uppercase tracking-widest rounded-xl transition-all duration-300 ${
                    active
                      ? 'text-white bg-zinc-900 dark:bg-white dark:text-black shadow-md'
                      : 'text-zinc-500 dark:text-zinc-400 hover:text-zinc-900 dark:hover:text-white hover:bg-zinc-100 dark:hover:bg-white/10'
                  }`}
                >
                  {label}
                </Link>
              )
            })}
          </nav>

          {/* ── Right side ── */}
          <div className="flex items-center gap-4">
            {/* Search trigger */}
            <button
              id="navbar-search-trigger"
              onClick={onSearchOpen}
              title="Search (Ctrl+K)"
              aria-label="Open search"
              className="group flex items-center gap-3 bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-white/10 rounded-2xl px-4 py-2 hover:border-blue-500/50 hover:shadow-lg dark:hover:shadow-white/5 transition-all duration-300"
            >
              <svg className="w-4 h-4 text-zinc-400 group-hover:text-blue-500 transition-colors" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2.5}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-4.35-4.35M17 11A6 6 0 1 1 5 11a6 6 0 0 1 12 0z" />
              </svg>
              <span className="hidden sm:inline text-sm font-semibold text-zinc-400 group-hover:text-zinc-600 dark:group-hover:text-zinc-300">Search symbols...</span>
              <kbd className="hidden md:inline-flex items-center gap-1 bg-zinc-100 dark:bg-zinc-800 rounded bg-opacity-50 px-2 py-0.5 text-[10px] font-bold text-zinc-400 uppercase">
                ⌘K
              </kbd>
            </button>

            {/* User menu (desktop) */}
            <div className="hidden sm:block">
              <UserNav />
            </div>

            {/* Hamburger (mobile) */}
            <button
              id="navbar-mobile-menu-toggle"
              onClick={() => setMenuOpen((v) => !v)}
              aria-label={menuOpen ? 'Close menu' : 'Open menu'}
              aria-expanded={menuOpen}
              className="lg:hidden p-2.5 rounded-xl bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-white/10 text-zinc-900 dark:text-white shadow-sm transition-all active:scale-95"
            >
              <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2.5}>
                {menuOpen ? (
                  <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
                ) : (
                  <path strokeLinecap="round" strokeLinejoin="round" d="M4 6h16M4 12h16M4 18h16" />
                )}
              </svg>
            </button>
          </div>
        </div>
      </header>

      {/* ── Mobile slide-down menu ── */}
      <div
        className={`lg:hidden fixed inset-x-0 top-[72px] z-40 transition-all duration-300 ease-[cubic-bezier(0.16,1,0.3,1)] ${
          menuOpen ? 'opacity-100 translate-y-0 pointer-events-auto' : 'opacity-0 -translate-y-4 pointer-events-none'
        }`}
        aria-hidden={!menuOpen}
      >
        <div className="mx-4 mt-2">
            <nav
            className="bg-white/95 dark:bg-[#111]/95 backdrop-blur-3xl border border-zinc-200 dark:border-white/10 rounded-[2rem] p-3 shadow-2xl flex flex-col gap-1"
            aria-label="Mobile navigation"
            >
            {NAV_LINKS.map(({ href, label }) => (
                <Link
                key={href}
                href={href}
                className={`px-5 py-4 rounded-2xl text-sm font-bold uppercase tracking-widest transition-all ${
                    isActive(href)
                    ? 'text-white bg-blue-600 shadow-md shadow-blue-500/20'
                    : 'text-zinc-500 dark:text-zinc-400 hover:bg-zinc-100 dark:hover:bg-white/5 hover:text-zinc-900 dark:hover:text-white'
                }`}
                >
                {label}
                </Link>
            ))}
            <div className="mt-2 pt-4 px-2 border-t border-zinc-100 dark:border-white/10 flex justify-center">
                <UserNav />
            </div>
            </nav>
        </div>
      </div>

      {/* Mobile menu backdrop */}
      {menuOpen && (
        <div
          className="lg:hidden fixed inset-0 z-30 bg-black/40 backdrop-blur-sm"
          onClick={() => setMenuOpen(false)}
          aria-hidden
        />
      )}
    </>
  )
}

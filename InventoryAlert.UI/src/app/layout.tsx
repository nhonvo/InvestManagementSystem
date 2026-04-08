import type { Metadata } from "next";
import { Inter } from "next/font/google";
import "./globals.css";
import Link from "next/link";
import UserNav from "@/components/UserNav";

const inter = Inter({ subsets: ["latin"] });
// ... rest matches
export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en" className="dark">
      <body className={`${inter.className} bg-[#050505] text-white min-h-screen flex flex-col selection:bg-blue-500/30`}>
        <header className="sticky top-0 z-50 border-b border-white/5 bg-black/60 backdrop-blur-xl flex items-center justify-between px-8 py-4">
          <div className="flex items-center gap-10">
            <Link href="/" className="group">
              <h1 className="text-2xl font-black tracking-tighter text-white group-hover:scale-105 transition-transform">
                INVENTORY<span className="text-blue-500">ALERT</span>
              </h1>
            </Link>
            <nav className="hidden md:flex items-center gap-8 text-[11px] font-black uppercase tracking-[0.2em] text-zinc-500">
              <Link href="/" className="hover:text-white transition-colors">Dashboard</Link>
              <Link href="/market" className="hover:text-white transition-colors">Market</Link>
              <Link href="/alerts" className="hover:text-white transition-colors">Alerts</Link>
              <Link href="/admin/events" className="hover:text-white transition-colors font-bold text-zinc-600">Events</Link>
            </nav>
          </div>
          <div className="flex items-center gap-6">
            <div className="hidden sm:flex items-center gap-2.5 text-[10px] font-black uppercase tracking-widest border border-white/5 rounded-full px-4 py-1.5 bg-zinc-900/50 text-emerald-500">
              <span className="w-1.5 h-1.5 rounded-full bg-emerald-500 animate-pulse shadow-[0_0_8px_rgba(16,185,129,0.8)]"></span>
              Market Open
            </div>
            <UserNav />
          </div>
        </header>
        <main className="flex-1 overflow-auto p-8 lg:p-12">
          {children}
        </main>
      </body>
    </html>
  );
}

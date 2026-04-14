import type { Metadata } from 'next'
import './globals.css'
import ThemeProvider from '@/components/ThemeProvider'
import NavbarWrapper from '@/components/NavbarWrapper'

// System font stack — avoids network requests during Docker builds
const fontStyle = {
  fontFamily: 'Inter, -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif',
}

export const metadata: Metadata = {
  title: 'InventoryAlert | Modern Market Intelligence',
  description: 'Real-time stock monitoring and market news for professional traders.',
}

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode
}>) {
  return (
    <html lang="en" suppressHydrationWarning>
      <head>
        {/* Prevent flash of unstyled theme */}
        <script
          dangerouslySetInnerHTML={{
            __html: `(function(){try{var t=localStorage.getItem('theme')||'dark';document.documentElement.classList.add(t);}catch(e){}})()`,
          }}
        />
      </head>
      <body
        className="bg-zinc-50 dark:bg-[#050505] text-zinc-900 dark:text-white min-h-screen flex flex-col selection:bg-blue-500/30 antialiased transition-colors duration-300"
        style={{
          ...fontStyle,
          backgroundImage: `
            radial-gradient(circle at 15% 50%, rgba(59,130,246,0.10) 0%, transparent 40%),
            radial-gradient(circle at 85% 30%, rgba(139,92,246,0.10) 0%, transparent 40%),
            radial-gradient(circle at 50% 80%, rgba(16,185,129,0.07) 0%, transparent 50%)
          `,
          backgroundAttachment: 'fixed',
        }}
      >
        <ThemeProvider>
          {/* NavbarWrapper is a client component that owns searchOpen state */}
          <NavbarWrapper />
          <main className="flex-1 overflow-auto max-w-screen-2xl mx-auto w-full px-4 sm:px-6 lg:px-8 py-8 lg:py-12">
            {children}
          </main>
        </ThemeProvider>
      </body>
    </html>
  )
}

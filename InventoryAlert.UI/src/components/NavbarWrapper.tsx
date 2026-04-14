'use client'

import { useState, useEffect } from 'react'
import Navbar from './Navbar'
import { GlobalSearch } from './GlobalSearch'

export default function NavbarWrapper() {
  const [isSearchOpen, setIsSearchOpen] = useState(false)

  // Global keyboard shortcut: Ctrl/Cmd+K
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
        e.preventDefault()
        setIsSearchOpen(true)
      }
    }
    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [])

  return (
    <>
      <Navbar onSearchOpen={() => setIsSearchOpen(true)} />
      <GlobalSearch isOpen={isSearchOpen} onClose={() => setIsSearchOpen(false)} />
    </>
  )
}

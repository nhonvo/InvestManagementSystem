'use client'

interface ConfirmDialogProps {
  isOpen: boolean
  title: string
  message: string
  confirmText?: string
  cancelText?: string
  type?: 'danger' | 'info'
  onConfirm: () => void
  onCancel: () => void
}

export function ConfirmDialog({
  isOpen,
  title,
  message,
  confirmText = "Confirm",
  cancelText = "Cancel",
  type = 'info',
  onConfirm,
  onCancel
}: ConfirmDialogProps) {
  if (!isOpen) return null

  const confirmColors = {
    danger: 'bg-rose-600 hover:bg-rose-700 shadow-rose-500/20',
    info: 'bg-blue-600 hover:bg-blue-700 shadow-blue-500/20'
  }

  return (
    <div className="fixed inset-0 z-100 flex items-center justify-center p-4">
      <div 
        className="absolute inset-0 bg-black/60 backdrop-blur-sm transition-opacity" 
        onClick={onCancel}
      />
      <div className="relative w-full max-w-sm bg-zinc-900 border border-white/10 rounded-3xl shadow-2xl p-8 transform transition-all animate-in zoom-in-95 duration-200">
        <h3 className="text-xl font-semibold uppercase tracking-tight mb-2">{title}</h3>
        <p className="text-zinc-400 text-sm leading-relaxed mb-8">{message}</p>
        
        <div className="flex gap-3">
          <button
            onClick={onCancel}
            className="flex-1 bg-zinc-800 hover:bg-zinc-700 text-white rounded-2xl py-4 font-bold text-xs uppercase tracking-wider transition-all active:scale-95 px-4"
          >
            {cancelText}
          </button>
          <button
            onClick={onConfirm}
            className={`flex-1 text-white rounded-2xl py-4 font-bold text-xs uppercase tracking-wider shadow-xl transition-all active:scale-95 px-4 ${confirmColors[type]}`}
          >
            {confirmText}
          </button>
        </div>
      </div>
    </div>
  )
}

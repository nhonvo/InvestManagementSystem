'use client'

export default function Error({
  error,
  reset,
}: {
  error: Error & { digest?: string }
  reset: () => void
}) {
  return (
    <div className="flex h-full flex-col items-center justify-center gap-4">
      <h2 className="text-xl font-bold">Something went wrong!</h2>
      <button className="bg-blue-600 px-4 py-2 rounded-md" onClick={() => reset()}>Try again</button>
    </div>
  )
}

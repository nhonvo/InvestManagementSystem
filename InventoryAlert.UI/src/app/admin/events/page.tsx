export default function AdminEvents() {
  return (
    <div className="max-w-6xl mx-auto flex flex-col gap-6 h-full">
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <h1 className="text-3xl font-bold tracking-tight">System Event Logs</h1>
        
        <div className="flex gap-3">
          <select className="bg-zinc-900 border border-white/10 rounded-md px-3 py-2 text-sm text-white focus:outline-none focus:border-blue-500">
            <option>All Event Types</option>
            <option>Price Update</option>
            <option>News Sync</option>
            <option>Alert Triggered</option>
            <option>Error</option>
          </select>
          <input 
            type="text" 
            placeholder="Search payload..." 
            className="bg-zinc-900 border border-white/10 rounded-md px-3 py-2 text-sm focus:outline-none focus:border-blue-500 w-64"
          />
        </div>
      </div>

      <div className="bg-zinc-900 border border-white/5 rounded-xl flex-1 flex flex-col overflow-hidden min-h-0">
        <div className="overflow-auto flex-1">
          <table className="w-full text-left border-collapse">
            <thead className="sticky top-0 bg-zinc-900 shadow-md">
              <tr className="border-b border-white/10 text-sm">
                <th className="p-4 font-medium text-zinc-400 w-48">Timestamp</th>
                <th className="p-4 font-medium text-zinc-400 w-32">Status</th>
                <th className="p-4 font-medium text-zinc-400 w-48">Event Type</th>
                <th className="p-4 font-medium text-zinc-400">Payload Overview</th>
                <th className="p-4 font-medium text-zinc-400 w-24">Action</th>
              </tr>
            </thead>
            <tbody className="text-sm font-mono">
              <tr className="border-b border-white/5 hover:bg-white/5 transition-colors">
                 <td className="p-4 text-zinc-400">2026-04-07 14:32:01</td>
                 <td className="p-4">
                   <span className="px-2 py-1 bg-emerald-500/10 text-emerald-400 rounded text-xs">SUCCESS</span>
                 </td>
                 <td className="p-4 text-blue-400">PriceSyncWorker</td>
                 <td className="p-4 text-zinc-300 truncate max-w-md">Synced 50 tickers successfully.</td>
                 <td className="p-4">
                   <button className="text-zinc-400 hover:text-white">View</button>
                 </td>
              </tr>
              <tr className="border-b border-white/5 hover:bg-rose-500/5 transition-colors bg-rose-500/5">
                 <td className="p-4 text-zinc-400">2026-04-07 14:31:45</td>
                 <td className="p-4">
                   <span className="px-2 py-1 bg-rose-500/10 text-rose-400 rounded text-xs">ERROR</span>
                 </td>
                 <td className="p-4 text-rose-400">FinnhubApi</td>
                 <td className="p-4 text-zinc-300 truncate max-w-md">Rate limit exceeded for endpoint /quote.</td>
                 <td className="p-4">
                   <button className="text-zinc-400 hover:text-white">View</button>
                 </td>
              </tr>
              <tr className="border-b border-white/5 hover:bg-white/5 transition-colors">
                 <td className="p-4 text-zinc-400">2026-04-07 14:30:10</td>
                 <td className="p-4">
                   <span className="px-2 py-1 bg-blue-500/10 text-blue-400 rounded text-xs">INFO</span>
                 </td>
                 <td className="p-4 text-blue-400">AlertEngine</td>
                 <td className="p-4 text-zinc-300 truncate max-w-md">Processed rule #1234. No trigger condition met.</td>
                 <td className="p-4">
                   <button className="text-zinc-400 hover:text-white">View</button>
                 </td>
              </tr>
            </tbody>
          </table>
        </div>
        <div className="p-4 border-t border-white/10 flex items-center justify-between text-sm text-zinc-400">
          <span>Showing 1 to 3 of 10,482 events</span>
          <div className="flex gap-2">
            <button className="px-3 py-1 bg-zinc-800 hover:bg-zinc-700 rounded transition-colors disabled:opacity-50" disabled>Previous</button>
            <button className="px-3 py-1 bg-zinc-800 hover:bg-zinc-700 rounded transition-colors">Next</button>
          </div>
        </div>
      </div>
    </div>
  );
}

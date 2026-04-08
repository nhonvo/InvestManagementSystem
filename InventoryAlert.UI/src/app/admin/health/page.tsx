export default function AdminHealth() {
  return (
    <div className="max-w-6xl mx-auto flex flex-col gap-6">
      <h1 className="text-3xl font-bold tracking-tight">System Health</h1>

      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        {[
          { name: "API Gateway", status: "Healthy", uptime: "99.9%", latency: "45ms" },
          { name: "Worker Hub", status: "Healthy", uptime: "100%", latency: "N/A" },
          { name: "PostgreSQL DB", status: "Healthy", uptime: "99.9%", latency: "12ms" },
          { name: "Redis Cache", status: "Healthy", uptime: "100%", latency: "1ms" },
          { name: "DynamoDB (AWS)", status: "Healthy", uptime: "100%", latency: "25ms" },
          { name: "Hangfire", status: "Healthy", uptime: "100%", latency: "N/A" },
          { name: "Finnhub API", status: "Warning", uptime: "98.5%", latency: "350ms" },
          { name: "Telegram Bot", status: "Healthy", uptime: "100%", latency: "120ms" }
        ].map((service) => (
          <div key={service.name} className="bg-zinc-900 border border-white/5 rounded-xl p-4">
            <div className="flex justify-between items-start mb-4">
              <h3 className="font-semibold">{service.name}</h3>
              <span className={`w-3 h-3 rounded-full ${service.status === 'Healthy' ? 'bg-emerald-500' : 'bg-amber-500 animate-pulse'}`}></span>
            </div>
            <div className="space-y-1 text-sm">
              <div className="flex justify-between text-zinc-400">
                <span>Status</span>
                <span className={service.status === 'Healthy' ? 'text-emerald-400' : 'text-amber-400'}>{service.status}</span>
              </div>
              <div className="flex justify-between text-zinc-400">
                <span>Uptime</span>
                <span className="text-white">{service.uptime}</span>
              </div>
              <div className="flex justify-between text-zinc-400">
                <span>Avg Latency</span>
                <span className="text-white">{service.latency}</span>
              </div>
            </div>
          </div>
        ))}
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mt-4">
        <div className="bg-zinc-900 border border-white/5 rounded-xl p-5 h-80 flex flex-col">
          <h3 className="font-semibold text-lg mb-4">Hangfire Job Status (24h)</h3>
          <div className="flex-1 flex items-center justify-center text-zinc-500 border border-dashed border-white/10 rounded-lg">
            Job Status Timeline Chart Placeholder
          </div>
        </div>
        
        <div className="bg-zinc-900 border border-white/5 rounded-xl p-5 h-80 flex flex-col">
          <h3 className="font-semibold text-lg mb-4">Log Level Distribution</h3>
          <div className="flex-1 flex items-center justify-center text-zinc-500 border border-dashed border-white/10 rounded-lg">
            Log Distribution Bar Chart Placeholder
          </div>
        </div>
      </div>
    </div>
  );
}

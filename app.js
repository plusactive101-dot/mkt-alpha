// MK-T ALPHA Quantitative Screener & Data Store
const screenerData = [
  {
    ticker: "NVDA",
    tvSymbol: "NASDAQ:NVDA",
    name: "Nvidia Corporation",
    sector: "Semiconductors / AI Infra",
    setup: "Breakout",
    catalyst: "Data center GPU demand & Blackwell architecture ramp",
    entry: "$138.50",
    target: "$165.00",
    stop: "$129.00",
    rr: "2.8 : 1",
    status: "Triggered",
    statusColor: "emerald"
  },
  {
    ticker: "PLTR",
    tvSymbol: "NASDAQ:PLTR",
    name: "Palantir Technologies",
    sector: "Enterprise Software & AI",
    setup: "Momentum",
    catalyst: "AIP enterprise adoption & systematic momentum flow",
    entry: "$42.00",
    target: "$54.00",
    stop: "$38.50",
    rr: "3.4 : 1",
    status: "Active",
    statusColor: "cyan"
  },
  {
    ticker: "TSLA",
    tvSymbol: "NASDAQ:TSLA",
    name: "Tesla Inc",
    sector: "EV & Autonomous Robotics",
    setup: "Breakout",
    catalyst: "FSD fleet milestones & Megapack energy margin expansion",
    entry: "$245.00",
    target: "$295.00",
    stop: "$228.00",
    rr: "2.9 : 1",
    status: "Watching",
    statusColor: "amber"
  },
  {
    ticker: "APP",
    tvSymbol: "NASDAQ:APP",
    name: "AppLovin Corp",
    sector: "AdTech & AI Software",
    setup: "Momentum",
    catalyst: "AXON 2.0 AI engine accelerating e-commerce monetization",
    entry: "$165.00",
    target: "$220.00",
    stop: "$148.00",
    rr: "3.2 : 1",
    status: "Active",
    statusColor: "cyan"
  },
  {
    ticker: "FCX",
    tvSymbol: "NYSE:FCX",
    name: "Freeport-McMoRan",
    sector: "Copper & Critical Materials",
    setup: "Pullback",
    catalyst: "Global copper deficit & electrification infrastructure buildout",
    entry: "$48.20",
    target: "$58.00",
    stop: "$45.10",
    rr: "3.1 : 1",
    status: "Watching",
    statusColor: "amber"
  },
  {
    ticker: "AMD",
    tvSymbol: "NASDAQ:AMD",
    name: "Advanced Micro Devices",
    sector: "Semiconductors",
    setup: "Pullback",
    catalyst: "MI325X enterprise adoption & hyperscaler cluster refresh",
    entry: "$152.00",
    target: "$182.00",
    stop: "$142.50",
    rr: "3.1 : 1",
    status: "Triggered",
    statusColor: "emerald"
  }
];

let currentFilter = 'all';
let currentSymbol = 'NASDAQ:NVDA';

// Render Screener Table
function renderTable() {
  const tbody = document.getElementById('screenerTableBody');
  if (!tbody) return;

  const filtered = currentFilter === 'all' 
    ? screenerData 
    : screenerData.filter(item => item.setup.toLowerCase() === currentFilter.toLowerCase());

  tbody.innerHTML = '';

  filtered.forEach(item => {
    const isSelected = item.tvSymbol === currentSymbol;
    const tr = document.createElement('tr');
    tr.className = `hover:bg-dark-800/80 transition cursor-pointer border-b border-dark-800/60 ${isSelected ? 'bg-cyan-950/20 border-l-2 border-l-cyan-400' : ''}`;
    tr.onclick = () => selectTicker(item.tvSymbol, item.ticker);

    const badgeClasses = {
      emerald: 'bg-emerald-500/15 text-emerald-400 border-emerald-500/30',
      cyan: 'bg-cyan-500/15 text-cyan-300 border-cyan-500/30',
      amber: 'bg-amber-500/15 text-amber-400 border-amber-500/30'
    }[item.statusColor] || 'bg-slate-700 text-slate-300 border-slate-600';

    tr.innerHTML = `
      <td class="py-3 px-3 mono font-bold text-white flex items-center space-x-2">
        <span>${item.ticker}</span>
        ${isSelected ? '<span class="w-1.5 h-1.5 rounded-full bg-cyan-400 animate-ping"></span>' : ''}
      </td>
      <td class="py-3 px-3 text-slate-400">${item.sector}</td>
      <td class="py-3 px-3">
        <div class="font-semibold text-slate-200">${item.setup}</div>
        <div class="text-[11px] text-slate-400 max-w-xs truncate">${item.catalyst}</div>
      </td>
      <td class="py-3 px-3 mono text-slate-300">${item.entry}</td>
      <td class="py-3 px-3 mono text-emerald-400 font-semibold">${item.target}</td>
      <td class="py-3 px-3 mono text-rose-400">${item.stop}</td>
      <td class="py-3 px-3 mono text-cyan-300 font-bold">${item.rr}</td>
      <td class="py-3 px-3">
        <span class="px-2 py-0.5 rounded text-[10px] font-semibold border mono ${badgeClasses}">
          ${item.status}
        </span>
      </td>
      <td class="py-3 px-3 text-right">
        <button class="px-2.5 py-1 rounded bg-dark-800 hover:bg-cyan-500 hover:text-dark-950 text-slate-300 mono text-xs transition border border-dark-700">
          Chart <i class="fa-solid fa-arrow-up-right-from-square ml-1 text-[10px]"></i>
        </button>
      </td>
    `;
    tbody.appendChild(tr);
  });
}

// Select Ticker and update chart
function selectTicker(tvSymbol, ticker) {
  currentSymbol = tvSymbol;
  const badge = document.getElementById('activeChartTickerBadge');
  if (badge) badge.innerText = tvSymbol;
  renderTable();
  loadTradingViewChart(tvSymbol);

  // Smooth scroll to chart on mobile/tablet
  const chartSection = document.getElementById('chart-section');
  if (chartSection && window.innerWidth < 1024) {
    chartSection.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
  }
}

// Filter button toggle
function filterTable(filter) {
  currentFilter = filter;
  document.querySelectorAll('.filter-btn').forEach(btn => {
    if (btn.dataset.filter.toLowerCase() === filter.toLowerCase()) {
      btn.className = "filter-btn px-3 py-1.5 rounded-lg text-xs font-semibold bg-cyan-500 text-dark-950 mono transition";
    } else {
      btn.className = "filter-btn px-3 py-1.5 rounded-lg text-xs font-medium bg-dark-850 hover:bg-dark-800 text-slate-300 mono transition";
    }
  });
  renderTable();
}

// Load Advanced Real-Time TradingView Candlestick Chart via direct iframe
function loadTradingViewChart(symbol) {
  const badge = document.getElementById('activeChartTickerBadge');
  if (badge) badge.innerText = symbol;

  const encodedSymbol = encodeURIComponent(symbol);
  const chartUrl = `https://www.tradingview.com/widgetembed/?symbol=${encodedSymbol}&interval=D&symboledit=1&saveimage=1&toolbarbg=0a0f1d&studies=%5B%22MASimple%40tv-basicstudies%22%2C%22RSI%40tv-basicstudies%22%2C%22Volume%40tv-basicstudies%22%5D&theme=dark&style=1&timezone=Etc%2FUTC&locale=en`;

  const iframe = document.getElementById('tv_chart_iframe');
  if (iframe) {
    iframe.src = chartUrl;
  } else {
    const container = document.getElementById('tv_chart_container');
    if (container) {
      container.innerHTML = `
        <iframe 
          id="tv_chart_iframe"
          src="${chartUrl}" 
          style="width: 100%; height: 100%; border: none; border-radius: 12px; display: block;" 
          frameborder="0" 
          allowtransparency="true" 
          scrolling="no" 
          allowfullscreen>
        </iframe>
      `;
    }
  }
}

// Initialize on page load
document.addEventListener('DOMContentLoaded', () => {
  renderTable();
  loadTradingViewChart(currentSymbol);
  
  const now = new Date();
  const dateStr = now.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  const updatedEl = document.getElementById('lastUpdated');
  if (updatedEl) {
    updatedEl.innerText = `Desk Active • ${dateStr}`;
  }
});

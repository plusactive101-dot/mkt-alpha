// MK-T ALPHA | Unified Quantitative Terminal & ML4T Strategy Engine
// Combines Live Desk Setups with Stefan Jansen ML4T (3rd Edition) 9 Case Studies & 27 Chapters

// 1. LIVE DESK CANDIDATES DATA
const screenerData = [
  {
    id: "ST-01",
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
    id: "ST-02",
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
    id: "ST-03",
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
    id: "ST-04",
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
    id: "ST-05",
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
    id: "ST-06",
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

// 2. ML4T CASE STUDIES DATA (9 PIPELINES)
const caseStudiesData = [
  {
    id: "CS-01",
    key: "etfs",
    name: "Multi-Asset ETF Momentum & Reversion",
    assetClass: "Multi-Asset ETFs",
    frequency: "Daily",
    universe: "100 Liquid ETFs",
    primaryLabel: "fwd_ret_21d (1-Month)",
    leadModel: "LightGBM + Optuna",
    metricIC: "+0.062",
    metricSharpe: "1.42",
    benchmark: "Equal-Weight ETF Basket (0.85)",
    tvSymbol: "AMEX:SPY",
    status: "Validated",
    statusColor: "emerald",
    category: "factors",
    summary: "Cross-asset momentum, carry, and mean-reversion across equity, fixed income, commodity, and real estate ETFs.",
    pipeline: [
      { step: "01 Feasibility", script: "case_studies/etfs/01_feasibility_analysis.py", desc: "Universe breadth, liquidity thresholds, and purged walk-forward splits." },
      { step: "02 Labels", script: "case_studies/etfs/02_labels.py", desc: "21-day forward returns and volatility-adjusted quantile thresholds." },
      { step: "03 Features", script: "case_studies/etfs/03_financial_features.py", desc: "Cross-asset momentum, rolling volatility, carry, and Polars rank expressions." },
      { step: "04 Model Features", script: "case_studies/etfs/04_model_based_features.py", desc: "ARIMA residuals, GARCH volatility forecasts, and HMM regime states." },
      { step: "07 Modeling (GBM)", script: "case_studies/etfs/07_gbm.py", desc: "LightGBM with Bayesian hyperparameter optimization via Optuna." },
      { step: "14 Backtest", script: "case_studies/etfs/14_backtest.py", desc: "Simulation strictly falsified against equal-weight benchmark." },
      { step: "16 Risk & Sizing", script: "case_studies/etfs/16_risk_management.py", desc: "Hierarchical Risk Parity (HRP) position sizing & dynamic trailing stops." },
      { step: "20 Synthesis", script: "case_studies/etfs/20_strategy_analysis.py", desc: "Multiple testing adjustments, Deflated Sharpe Ratio, and holdout analysis." }
    ]
  },
  {
    id: "CS-02",
    key: "crypto_perps_funding",
    name: "Crypto Perps Funding Rate Arbitrage",
    assetClass: "Crypto Perpetual Futures",
    frequency: "8-Hourly",
    universe: "19 Binance/Bybit Pairs",
    primaryLabel: "fwd_ret_8h (8-Hour)",
    leadModel: "TSMixer / Ridge Regularized",
    metricIC: "+0.078",
    metricSharpe: "2.15",
    benchmark: "BTC Buy & Hold (1.10)",
    tvSymbol: "BINANCE:BTCUSDT",
    status: "Validated",
    statusColor: "emerald",
    category: "crypto",
    summary: "Exploiting funding rate premium dispersion and basis mean-reversion across major perpetual contracts.",
    pipeline: [
      { step: "01 Feasibility", script: "case_studies/crypto_perps_funding/01_feasibility_analysis.py", desc: "Funding rate settlement intervals and slippage estimation." },
      { step: "02 Labels", script: "case_studies/crypto_perps_funding/02_labels.py", desc: "8-hour forward return net of periodic exchange funding fees." },
      { step: "03 Features", script: "case_studies/crypto_perps_funding/03_features.py", desc: "Cumulative funding delta, open interest imbalance, basis spread." },
      { step: "06 Modeling", script: "case_studies/crypto_perps_funding/06_linear.py", desc: "L2 Ridge baseline & TSMixer deep time series architecture." },
      { step: "14 Backtest", script: "case_studies/crypto_perps_funding/14_backtest.py", desc: "Continuous inventory rebalancing and funding cashflow ledger." }
    ]
  },
  {
    id: "CS-03",
    key: "nasdaq100_microstructure",
    name: "NASDAQ-100 Intraday LOB Orderflow",
    assetClass: "Equities (High Frequency)",
    frequency: "15-Minute",
    universe: "114 Top QQQ Equities",
    primaryLabel: "fwd_ret_15m (15-Min)",
    leadModel: "PatchTST / TCN",
    metricIC: "+0.045",
    metricSharpe: "1.88",
    benchmark: "Intraday TWAP Baseline (0.72)",
    tvSymbol: "NASDAQ:QQQ",
    status: "In Production",
    statusColor: "cyan",
    category: "equities",
    summary: "Limit order book depth imbalance, spread dynamics, and trade flow toxicity from ITCH tick feeds.",
    pipeline: [
      { step: "01 Feasibility", script: "case_studies/nasdaq100_microstructure/01_feasibility_analysis.py", desc: "Sub-second tick message parsing and 15-min bar aggregation." },
      { step: "02 Labels", script: "case_studies/nasdaq100_microstructure/02_labels.py", desc: "15-minute mid-quote forward return with volume barrier exits." },
      { step: "03 Microstructure", script: "case_studies/nasdaq100_microstructure/03_features.py", desc: "Order book imbalance (OFI), Kyle's lambda, and VPBF metrics." },
      { step: "08 Sequence DL", script: "case_studies/nasdaq100_microstructure/08_dl_patchtst.py", desc: "PatchTST attention patches capturing intra-hour autocorrelation." }
    ]
  },
  {
    id: "CS-04",
    key: "sp500_equity_option_analytics",
    name: "S&P 500 Equity + IV Skew Analytics",
    assetClass: "Equities + Derivatives",
    frequency: "Daily",
    universe: "634 S&P Component Stocks",
    primaryLabel: "fwd_ret_5d (1-Week)",
    leadModel: "CatBoost + Vol Surface",
    metricIC: "+0.054",
    metricSharpe: "1.65",
    benchmark: "S&P 500 Cap-Weighted (0.95)",
    tvSymbol: "FOREXCOM:SPXUSD",
    status: "Validated",
    statusColor: "emerald",
    category: "equities",
    summary: "Cross-sectional equity stock selection augmented by implied volatility surface skew and term structure.",
    pipeline: [
      { step: "01 Feasibility", script: "case_studies/sp500_equity_option_analytics/01_feasibility.py", desc: "Options liquidity filters and delta-neutral mapping." },
      { step: "03 IV Features", script: "case_studies/sp500_equity_option_analytics/03_features.py", desc: "25-delta put-call skew, IV term structure slope, implied correlation." },
      { step: "07 Modeling", script: "case_studies/sp500_equity_option_analytics/07_gbm.py", desc: "CatBoost gradient boosting with monotonic constraint overlays." }
    ]
  },
  {
    id: "CS-05",
    key: "us_firm_characteristics",
    name: "US Firm Characteristics Factor Panel",
    assetClass: "US Equities (Fundamental)",
    frequency: "Monthly",
    universe: "~2,500 Listed US Stocks",
    primaryLabel: "fwd_ret_1m (1-Month)",
    leadModel: "Double ML (Causal) / TabM",
    metricIC: "+0.041",
    metricSharpe: "1.35",
    benchmark: "Russell 2000 Equal-Weight (0.68)",
    tvSymbol: "NYSE:IWM",
    status: "Validated",
    statusColor: "emerald",
    category: "factors",
    summary: "Point-in-time firm characteristics (size, value, profitability, investment, quality) from SEC EDGAR filings.",
    pipeline: [
      { step: "01 PIT Universe", script: "case_studies/us_firm_characteristics/01_feasibility.py", desc: "Point-in-time restatement correction and survival bias removal." },
      { step: "03 Accounting Ratios", script: "case_studies/us_firm_characteristics/03_features.py", desc: "Gross profitability, accruals, asset growth, and earnings surprises." },
      { step: "12 Causal Double ML", script: "case_studies/us_firm_characteristics/12_causal_dml.py", desc: "Separating true factor premia from confounding macro trends via DML." }
    ]
  },
  {
    id: "CS-06",
    key: "fx_pairs",
    name: "G10 FX Pairs Carry & Momentum",
    assetClass: "Foreign Exchange",
    frequency: "Daily",
    universe: "20 Liquid G10 Pairs",
    primaryLabel: "fwd_ret_1d (Daily)",
    leadModel: "ElasticNet / HMM Regimes",
    metricIC: "+0.038",
    metricSharpe: "1.25",
    benchmark: "Static Equal-Weight Carry (0.60)",
    tvSymbol: "FX:EURUSD",
    status: "Research",
    statusColor: "amber",
    category: "crypto",
    summary: "Carry trade yield differentials, cross-sectional trend, and central bank regime shift modeling.",
    pipeline: [
      { step: "01 Feasibility", script: "case_studies/fx_pairs/01_feasibility.py", desc: "Triangular arbitrage bounds and rollover interest rate calculation." },
      { step: "04 Model Features", script: "case_studies/fx_pairs/04_model_features.py", desc: "Hidden Markov Model regime detection on interest rate differentials." },
      { step: "14 Backtest", script: "case_studies/fx_pairs/14_backtest.py", desc: "Dynamic FX allocation with volatility-scaled position sizing." }
    ]
  },
  {
    id: "CS-07",
    key: "cme_futures",
    name: "CME Futures Term Structure & Roll Yield",
    assetClass: "Multi-Sector Futures",
    frequency: "Daily",
    universe: "30 Liquid Commodities & Rates",
    primaryLabel: "fwd_ret_5d (5-Day)",
    leadModel: "LightGBM + Curve Slopes",
    metricIC: "+0.051",
    metricSharpe: "1.55",
    benchmark: "BCOM Commodity Index (0.50)",
    tvSymbol: "CME:CL1!",
    status: "Validated",
    statusColor: "emerald",
    category: "futures",
    summary: "Commodity term-structure contango/backwardation, inventory metrics, and curve roll-yield harvesting.",
    pipeline: [
      { step: "01 Roll Mechanics", script: "case_studies/cme_futures/01_feasibility.py", desc: "Contract calendar stitching, first notice dates, and roll schedules." },
      { step: "03 Curve Features", script: "case_studies/cme_futures/03_features.py", desc: "Prompt vs 2nd month spread, curvature, and open interest concentration." },
      { step: "17 Portfolio", script: "case_studies/cme_futures/17_portfolio.py", desc: "Cross-sector risk parity allocation across Energy, Metals, and Ags." }
    ]
  },
  {
    id: "CS-08",
    key: "sp500_options",
    name: "S&P 500 Delta-Hedged Straddles",
    assetClass: "Equity Index Options",
    frequency: "Daily",
    universe: "S&P 500 Index Options Chain",
    primaryLabel: "fwd_ret_dh_10d (Delta-Hedged)",
    leadModel: "Autoencoders / TabPFN",
    metricIC: "+0.069",
    metricSharpe: "1.95",
    benchmark: "CBOE CPUT / BXM (0.80)",
    tvSymbol: "CBOE:VIX",
    status: "Research",
    statusColor: "amber",
    category: "futures",
    summary: "Pure volatility trading, variance risk premium (VRP) extraction, and delta-hedged gamma management.",
    pipeline: [
      { step: "01 Greeks Engine", script: "case_studies/sp500_options/01_feasibility.py", desc: "Black-Scholes & SABR volatility surface fitting with strike interpolation." },
      { step: "08 Deep Learning", script: "case_studies/sp500_options/08_autoencoder.py", desc: "Latent factor extraction from entire implied volatility surface matrix." },
      { step: "19 Risk Overlays", script: "case_studies/sp500_options/19_risk_management.py", desc: "Vega limits, tail-risk crash hedge triggers, and margin constraints." }
    ]
  },
  {
    id: "CS-09",
    key: "us_equities_panel",
    name: "Broad US Equities Cross-Sectional Alpha",
    assetClass: "US Equities (High Breadth)",
    frequency: "Daily",
    universe: "~3,200 Cross-Section Stocks",
    primaryLabel: "fwd_ret_1d (1-Day Cross-Section)",
    leadModel: "iTransformer / Hierarchical GBDT",
    metricIC: "+0.058",
    metricSharpe: "1.72",
    benchmark: "Equal-Weight Market Index (0.75)",
    tvSymbol: "NASDAQ:NVDA",
    status: "In Production",
    statusColor: "cyan",
    category: "equities",
    summary: "High-breadth daily equity universe with hundreds of engineered technical, fundamental, and factor features.",
    pipeline: [
      { step: "01 Breadth Check", script: "case_studies/us_equities_panel/01_feasibility.py", desc: "Polars vectorized universe filters, survivorship-free pricing matrix." },
      { step: "03 Vectorized Features", script: "case_studies/us_equities_panel/03_features.py", desc: "180+ alpha signals: momentum, reversal, volatility, volume flux." },
      { step: "10 iTransformer", script: "case_studies/us_equities_panel/10_dl_itransformer.py", desc: "Inverted transformer capturing cross-asset token interactions." },
      { step: "20 Full Synthesis", script: "case_studies/us_equities_panel/20_strategy_analysis.py", desc: "Net-of-cost backtesting with square-root market impact modeling." }
    ]
  }
];

// 3. 27 CHAPTERS MATRIX DATA
const chaptersData = [
  { part: "Part I", num: "01", title: "The Process Is Your Edge", path: "01_process_is_edge", focus: "Evidence boundary, macro regime detection on factor returns." },
  { part: "Part I", num: "02", title: "The Financial Data Universe", path: "02_financial_data_universe", focus: "Data sources, storage tiers, Polars expressions for high-throughput." },
  { part: "Part I", num: "03", title: "Market Microstructure", path: "03_market_microstructure", focus: "ITCH message feeds, limit order book dynamics, spread & imbalance." },
  { part: "Part I", num: "04", title: "Fundamental & Alternative Data", path: "04_fundamental_alternative_data", focus: "SEC EDGAR XBRL filings, point-in-time restatements, firm panel." },
  { part: "Part I", num: "05", title: "Synthetic Financial Data", path: "05_synthetic_data", focus: "TimeGAN, Tail-GAN, Sig-CWGAN path signatures, diffusion models." },
  
  { part: "Part II", num: "06", title: "Strategy Definition", path: "06_strategy_definition", focus: "Feasibility analysis, universe selection, walk-forward splitting." },
  { part: "Part II", num: "07", title: "Defining the Learning Task", path: "07_defining_the_learning_task", focus: "Forward returns, meta-labeling, triple barrier method with stops." },
  { part: "Part II", num: "08", title: "Financial Feature Engineering", path: "08_financial_features", focus: "Momentum, volatility, carry, cross-sectional rankings in Polars." },
  { part: "Part II", num: "09", title: "Model-Based Features", path: "09_model_based_features", focus: "ARIMA residuals, GARCH volatility forecasts, HMM regime filters." },
  { part: "Part II", num: "10", title: "Text Feature Engineering", path: "10_text_feature_engineering", focus: "FinBERT sentiment, earnings call transcripts, 10-K topic drift." },

  { part: "Part III", num: "11", title: "The Machine Learning Pipeline", path: "11_ml_pipeline", focus: "Ridge, LASSO, ElasticNet regularized baselines every model must beat." },
  { part: "Part III", num: "12", title: "Gradient Boosting & Tabular DL", path: "12_gradient_boosting", focus: "LightGBM, XGBoost, CatBoost + Optuna HPO, TabPFN, TabM models." },
  { part: "Part III", num: "13", title: "Deep Learning for Time Series", path: "13_dl_time_series", focus: "PatchTST, iTransformer, TSMixer, TCN architectures in PyTorch." },
  { part: "Part III", num: "14", title: "Latent Factor Models", path: "14_latent_factors", focus: "PCA, IPCA, conditional autoencoders for stochastic discount factors." },
  { part: "Part III", num: "15", title: "Causal Machine Learning", path: "15_causal_estimation", focus: "Double ML (DML) & Bayesian Structural Time Series (BSTS)." },

  { part: "Part IV", num: "16", title: "Strategy Simulation", path: "16_strategy_simulation", focus: "Backtest simulation, falsified against equal-weight reference benchmarks." },
  { part: "Part IV", num: "17", title: "Portfolio Construction", path: "17_portfolio_construction", focus: "Score-weighted, Risk Parity, Hierarchical Risk Parity (HRP), Conformal." },
  { part: "Part IV", num: "18", title: "Transaction Costs & Turnover", path: "18_transaction_costs", focus: "Spread models, square-root market impact, turnover penalty calibration." },
  { part: "Part IV", num: "19", title: "Risk Management & Overlays", path: "19_risk_management", focus: "Dynamic trailing stops, volatility targeting, maximum drawdown gates." },
  { part: "Part IV", num: "20", title: "Strategy Synthesis & Diagnostics", path: "20_strategy_synthesis", focus: "Multiple-testing control, Deflated Sharpe Ratio, White's Reality Check." },
  { part: "Part IV", num: "21", title: "Reinforcement Learning", path: "21_rl_execution_hedging", focus: "Optimal execution, market making with inventory, deep hedging." },

  { part: "Part V", num: "22", title: "Financial RAG", path: "22_rag_financial_research", focus: "Retrieval-augmented research grounded in SEC 10-K & 10-Q filings." },
  { part: "Part V", num: "23", title: "Financial Knowledge Graphs", path: "23_knowledge_graphs", focus: "Entity supply-chain graph embeddings, Graph RAG, contagion modeling." },
  { part: "Part V", num: "24", title: "Autonomous Research Agents", path: "24_autonomous_agents", focus: "Multi-agent research analysts, peer-review debate, auditable logs." },
  { part: "Part V", num: "25", title: "Live Trading Execution", path: "25_live_trading", focus: "Execution connectors: Interactive Brokers, Alpaca, QuantConnect." },
  { part: "Part V", num: "26", title: "MLOps & Governance", path: "26_mlops_governance", focus: "Concept drift detection, automated retraining triggers, circuit breakers." },
  { part: "Part V", num: "27", title: "Systematic Edge Lifecycle", path: "27_systematic_edge", focus: "Alpha decay monitoring, retraining, pause, or retire criteria." }
];

// STATE VARIABLES
let activeMode = "casestudies"; // "casestudies" or "screener"
let activeCategoryFilter = "all";
let currentSelection = caseStudiesData[0]; // active item

// SWITCH DESK MODE
function setDeskMode(mode) {
  activeMode = mode;
  activeCategoryFilter = "all";

  // Toggle button styling
  const btnCS = document.getElementById("modeBtnCaseStudies");
  const btnSC = document.getElementById("modeBtnScreener");
  if (mode === "casestudies") {
    btnCS.className = "px-4 py-2 rounded-xl text-xs font-bold bg-cyan-500 text-dark-950 mono transition shadow-md shadow-cyan-500/20";
    btnSC.className = "px-4 py-2 rounded-xl text-xs font-semibold bg-dark-850 hover:bg-dark-800 text-slate-300 mono transition border border-dark-700";
    document.getElementById("modeTitle").innerHTML = '<i class="fa-solid fa-layer-group text-cyan-400 mr-2"></i> ML4T 9 Case Study Pipelines';
    document.getElementById("modeSubtitle").innerText = "Cross-asset walk-forward pipelines (ETFs, Crypto Perps, NASDAQ-100, CME Futures, S&P 500)";
  } else {
    btnSC.className = "px-4 py-2 rounded-xl text-xs font-bold bg-cyan-500 text-dark-950 mono transition shadow-md shadow-cyan-500/20";
    btnCS.className = "px-4 py-2 rounded-xl text-xs font-semibold bg-dark-850 hover:bg-dark-800 text-slate-300 mono transition border border-dark-700";
    document.getElementById("modeTitle").innerHTML = '<i class="fa-solid fa-crosshairs text-emerald-400 mr-2"></i> Live Desk Candidates & Setups';
    document.getElementById("modeSubtitle").innerText = "High-energy equity breakouts, pullbacks, and momentum setups with explicit R:R asymmetry";
  }

  renderFilterButtons();
  renderMainTable();
}

// RENDER FILTER PILLS DYNAMICALLY
function renderFilterButtons() {
  const container = document.getElementById("filterButtonsContainer");
  if (!container) return;

  if (activeMode === "casestudies") {
    container.innerHTML = `
      <button onclick="setCategoryFilter('all')" class="cat-filter-btn px-3 py-1.5 rounded-lg text-xs font-semibold ${activeCategoryFilter === 'all' ? 'bg-cyan-500 text-dark-950' : 'bg-dark-850 hover:bg-dark-800 text-slate-300'} mono transition">All (9)</button>
      <button onclick="setCategoryFilter('equities')" class="cat-filter-btn px-3 py-1.5 rounded-lg text-xs font-medium ${activeCategoryFilter === 'equities' ? 'bg-cyan-500 text-dark-950' : 'bg-dark-850 hover:bg-dark-800 text-slate-300'} mono transition">Equities</button>
      <button onclick="setCategoryFilter('crypto')" class="cat-filter-btn px-3 py-1.5 rounded-lg text-xs font-medium ${activeCategoryFilter === 'crypto' ? 'bg-cyan-500 text-dark-950' : 'bg-dark-850 hover:bg-dark-800 text-slate-300'} mono transition">Crypto & FX</button>
      <button onclick="setCategoryFilter('futures')" class="cat-filter-btn px-3 py-1.5 rounded-lg text-xs font-medium ${activeCategoryFilter === 'futures' ? 'bg-cyan-500 text-dark-950' : 'bg-dark-850 hover:bg-dark-800 text-slate-300'} mono transition">Futures & Options</button>
      <button onclick="setCategoryFilter('factors')" class="cat-filter-btn px-3 py-1.5 rounded-lg text-xs font-medium ${activeCategoryFilter === 'factors' ? 'bg-cyan-500 text-dark-950' : 'bg-dark-850 hover:bg-dark-800 text-slate-300'} mono transition">Macro & Factors</button>
    `;
  } else {
    container.innerHTML = `
      <button onclick="setCategoryFilter('all')" class="cat-filter-btn px-3 py-1.5 rounded-lg text-xs font-semibold ${activeCategoryFilter === 'all' ? 'bg-cyan-500 text-dark-950' : 'bg-dark-850 hover:bg-dark-800 text-slate-300'} mono transition">All (6)</button>
      <button onclick="setCategoryFilter('Breakout')" class="cat-filter-btn px-3 py-1.5 rounded-lg text-xs font-medium ${activeCategoryFilter === 'Breakout' ? 'bg-cyan-500 text-dark-950' : 'bg-dark-850 hover:bg-dark-800 text-slate-300'} mono transition">Breakouts</button>
      <button onclick="setCategoryFilter('Pullback')" class="cat-filter-btn px-3 py-1.5 rounded-lg text-xs font-medium ${activeCategoryFilter === 'Pullback' ? 'bg-cyan-500 text-dark-950' : 'bg-dark-850 hover:bg-dark-800 text-slate-300'} mono transition">Pullbacks</button>
      <button onclick="setCategoryFilter('Momentum')" class="cat-filter-btn px-3 py-1.5 rounded-lg text-xs font-medium ${activeCategoryFilter === 'Momentum' ? 'bg-cyan-500 text-dark-950' : 'bg-dark-850 hover:bg-dark-800 text-slate-300'} mono transition">Momentum</button>
    `;
  }
}

function setCategoryFilter(cat) {
  activeCategoryFilter = cat;
  renderFilterButtons();
  renderMainTable();
}

// RENDER MAIN DATA TABLE
function renderMainTable() {
  const tableHead = document.getElementById("mainTableHead");
  const tableBody = document.getElementById("mainTableBody");
  if (!tableHead || !tableBody) return;

  tableBody.innerHTML = "";

  if (activeMode === "casestudies") {
    // 1. CASE STUDIES COLUMNS
    tableHead.innerHTML = `
      <tr class="text-slate-400 border-b border-dark-800 mono text-xs">
        <th class="py-3.5 px-3">CASE ID</th>
        <th class="py-3.5 px-3">STRATEGY / TITLE</th>
        <th class="py-3.5 px-3">ASSET & FREQ</th>
        <th class="py-3.5 px-3">TARGET LABEL</th>
        <th class="py-3.5 px-3">LEAD MODEL</th>
        <th class="py-3.5 px-3">SHARPE / IC</th>
        <th class="py-3.5 px-3">STATUS</th>
        <th class="py-3.5 px-3 text-right">ACTION</th>
      </tr>
    `;

    const filtered = activeCategoryFilter === "all"
      ? caseStudiesData
      : caseStudiesData.filter(cs => cs.category.toLowerCase() === activeCategoryFilter.toLowerCase());

    filtered.forEach(cs => {
      const isSelected = currentSelection.id === cs.id;
      const tr = document.createElement("tr");
      tr.className = `hover:bg-dark-850/80 transition cursor-pointer border-b border-dark-800/60 ${isSelected ? "bg-cyan-950/25 border-l-2 border-l-cyan-400" : ""}`;
      tr.onclick = () => selectItem(cs, "casestudy");

      const badgeColor = {
        emerald: "bg-emerald-500/15 text-emerald-400 border-emerald-500/30",
        cyan: "bg-cyan-500/15 text-cyan-300 border-cyan-500/30",
        amber: "bg-amber-500/15 text-amber-400 border-amber-500/30"
      }[cs.statusColor] || "bg-slate-700 text-slate-300";

      tr.innerHTML = `
        <td class="py-3.5 px-3 mono font-bold text-white flex items-center space-x-2">
          <span>${cs.id}</span>
          ${isSelected ? '<span class="w-2 h-2 rounded-full bg-cyan-400 animate-ping"></span>' : ''}
        </td>
        <td class="py-3.5 px-3">
          <div class="font-bold text-slate-100">${cs.name}</div>
          <div class="text-[11px] text-slate-400 max-w-xs truncate">${cs.summary}</div>
        </td>
        <td class="py-3.5 px-3">
          <div class="text-slate-300 text-xs font-semibold">${cs.assetClass}</div>
          <span class="text-[10px] mono text-slate-400">${cs.frequency}</span>
        </td>
        <td class="py-3.5 px-3 mono text-xs text-slate-300">${cs.primaryLabel}</td>
        <td class="py-3.5 px-3">
          <span class="px-2 py-0.5 rounded text-[11px] font-medium bg-dark-800 border border-slate-700 text-slate-200 mono">
            ${cs.leadModel}
          </span>
        </td>
        <td class="py-3.5 px-3 mono text-xs">
          <div class="text-cyan-300 font-bold">${cs.metricSharpe} SR</div>
          <div class="text-slate-400 text-[10px]">${cs.metricIC} IC</div>
        </td>
        <td class="py-3.5 px-3">
          <span class="px-2 py-0.5 rounded text-[10px] font-semibold border mono ${badgeColor}">
            ${cs.status}
          </span>
        </td>
        <td class="py-3.5 px-3 text-right">
          <button class="px-2.5 py-1 rounded bg-dark-800 hover:bg-cyan-500 hover:text-dark-950 text-slate-300 mono text-xs transition border border-dark-700">
            Inspect <i class="fa-solid fa-arrow-right ml-1 text-[10px]"></i>
          </button>
        </td>
      `;
      tableBody.appendChild(tr);
    });

  } else {
    // 2. SCREENER SETUPS COLUMNS
    tableHead.innerHTML = `
      <tr class="text-slate-400 border-b border-dark-800 mono text-xs">
        <th class="py-3.5 px-3">TICKER</th>
        <th class="py-3.5 px-3">SECTOR</th>
        <th class="py-3.5 px-3">SETUP / CONFLUENCE</th>
        <th class="py-3.5 px-3">ENTRY</th>
        <th class="py-3.5 px-3">TARGET</th>
        <th class="py-3.5 px-3">STOP</th>
        <th class="py-3.5 px-3">R:R</th>
        <th class="py-3.5 px-3">STATUS</th>
        <th class="py-3.5 px-3 text-right">ACTION</th>
      </tr>
    `;

    const filtered = activeCategoryFilter === "all"
      ? screenerData
      : screenerData.filter(s => s.setup.toLowerCase() === activeCategoryFilter.toLowerCase());

    filtered.forEach(item => {
      const isSelected = currentSelection.id === item.id;
      const tr = document.createElement("tr");
      tr.className = `hover:bg-dark-850/80 transition cursor-pointer border-b border-dark-800/60 ${isSelected ? "bg-cyan-950/25 border-l-2 border-l-cyan-400" : ""}`;
      tr.onclick = () => selectItem(item, "screener");

      const badgeColor = {
        emerald: "bg-emerald-500/15 text-emerald-400 border-emerald-500/30",
        cyan: "bg-cyan-500/15 text-cyan-300 border-cyan-500/30",
        amber: "bg-amber-500/15 text-amber-400 border-amber-500/30"
      }[item.statusColor] || "bg-slate-700 text-slate-300";

      tr.innerHTML = `
        <td class="py-3.5 px-3 mono font-bold text-white flex items-center space-x-2">
          <span>${item.ticker}</span>
          ${isSelected ? '<span class="w-2 h-2 rounded-full bg-cyan-400 animate-ping"></span>' : ''}
        </td>
        <td class="py-3.5 px-3 text-slate-400 text-xs">${item.sector}</td>
        <td class="py-3.5 px-3">
          <div class="font-semibold text-slate-200">${item.setup}</div>
          <div class="text-[11px] text-slate-400 max-w-xs truncate">${item.catalyst}</div>
        </td>
        <td class="py-3.5 px-3 mono text-slate-300 text-xs">${item.entry}</td>
        <td class="py-3.5 px-3 mono text-emerald-400 font-semibold text-xs">${item.target}</td>
        <td class="py-3.5 px-3 mono text-rose-400 text-xs">${item.stop}</td>
        <td class="py-3.5 px-3 mono text-cyan-300 font-bold text-xs">${item.rr}</td>
        <td class="py-3.5 px-3">
          <span class="px-2 py-0.5 rounded text-[10px] font-semibold border mono ${badgeColor}">
            ${item.status}
          </span>
        </td>
        <td class="py-3.5 px-3 text-right">
          <button class="px-2.5 py-1 rounded bg-dark-800 hover:bg-cyan-500 hover:text-dark-950 text-slate-300 mono text-xs transition border border-dark-700">
            Chart <i class="fa-solid fa-arrow-up-right-from-square ml-1 text-[10px]"></i>
          </button>
        </td>
      `;
      tableBody.appendChild(tr);
    });
  }
}

// SELECT ITEM & UPDATE DUAL TERMINAL
function selectItem(item, type) {
  currentSelection = item;

  // Update badge & title
  const badge = document.getElementById("activeChartTickerBadge");
  const inspectorTitle = document.getElementById("pipelineInspectorTitle");
  const inspectorSubtitle = document.getElementById("pipelineInspectorSubtitle");

  if (type === "casestudy") {
    if (badge) badge.innerText = `${item.name} (${item.tvSymbol})`;
    if (inspectorTitle) inspectorTitle.innerText = `${item.id}: ${item.name}`;
    if (inspectorSubtitle) inspectorSubtitle.innerText = `${item.universe} • Target: ${item.primaryLabel}`;
    renderPipelineInspector(item.pipeline);
  } else {
    if (badge) badge.innerText = `${item.ticker} • ${item.name} (${item.tvSymbol})`;
    if (inspectorTitle) inspectorTitle.innerText = `Screener Setup: ${item.ticker} (${item.setup})`;
    if (inspectorSubtitle) inspectorSubtitle.innerText = `Target: ${item.target} | Stop: ${item.stop} | R:R ${item.rr}`;
    renderScreenerInspector(item);
  }

  renderMainTable();
  loadTradingViewChart(item.tvSymbol);
}

// RENDER CASE STUDY PIPELINE INSPECTOR
function renderPipelineInspector(pipeline) {
  const container = document.getElementById("pipelineInspectorContainer");
  if (!container) return;

  container.innerHTML = "";
  pipeline.forEach((p, idx) => {
    const card = document.createElement("div");
    card.className = "p-3 rounded-xl bg-dark-900 border border-dark-800 hover:border-cyan-500/40 transition flex flex-col space-y-1.5";
    card.innerHTML = `
      <div class="flex items-center justify-between text-xs">
        <span class="mono font-bold text-cyan-400">STAGE ${idx + 1}: ${p.step}</span>
        <span class="text-[10px] px-1.5 py-0.5 rounded bg-emerald-500/10 text-emerald-400 mono">VERIFIED</span>
      </div>
      <p class="text-xs text-slate-300 leading-snug">${p.desc}</p>
      <div class="pt-1.5 border-t border-dark-800/80 flex items-center justify-between text-[11px] mono text-slate-400">
        <span class="truncate max-w-[280px]"><code>uv run python ${p.script}</code></span>
        <button onclick="copyToClipboard('uv run python ${p.script}')" title="Copy command" class="text-slate-500 hover:text-cyan-400 transition ml-2">
          <i class="fa-regular fa-copy"></i>
        </button>
      </div>
    `;
    container.appendChild(card);
  });
}

// RENDER SCREENER DETAILS IN INSPECTOR
function renderScreenerInspector(item) {
  const container = document.getElementById("pipelineInspectorContainer");
  if (!container) return;

  container.innerHTML = `
    <div class="p-4 rounded-xl bg-dark-900 border border-dark-800 space-y-3">
      <div class="flex items-center justify-between text-xs mono">
        <span class="text-cyan-400 font-bold">${item.ticker} EXECUTION PARAMETERS</span>
        <span class="text-emerald-400 font-semibold">${item.status}</span>
      </div>
      <div class="grid grid-cols-2 gap-2 text-xs mono">
        <div class="p-2 rounded bg-dark-950 border border-dark-800">
          <span class="text-slate-500 text-[10px] block">ENTRY PIVOT</span>
          <span class="text-white font-bold">${item.entry}</span>
        </div>
        <div class="p-2 rounded bg-dark-950 border border-dark-800">
          <span class="text-slate-500 text-[10px] block">STOP LOSS</span>
          <span class="text-rose-400 font-bold">${item.stop}</span>
        </div>
        <div class="p-2 rounded bg-dark-950 border border-dark-800">
          <span class="text-slate-500 text-[10px] block">PROFIT TARGET</span>
          <span class="text-emerald-400 font-bold">${item.target}</span>
        </div>
        <div class="p-2 rounded bg-dark-950 border border-dark-800">
          <span class="text-slate-500 text-[10px] block">EXPECTANCY (R:R)</span>
          <span class="text-cyan-300 font-bold">${item.rr}</span>
        </div>
      </div>
      <p class="text-xs text-slate-300 leading-relaxed pt-1">
        <strong>Macro Catalyst:</strong> ${item.catalyst}
      </p>
    </div>
  `;
}

// LOAD TRADINGVIEW CHART IFRAME
function loadTradingViewChart(symbol) {
  const iframe = document.getElementById("tv_chart_iframe");
  if (!iframe) return;
  const encoded = encodeURIComponent(symbol);
  iframe.src = `https://www.tradingview.com/widgetembed/?symbol=${encoded}&interval=D&symboledit=1&saveimage=1&toolbarbg=0a0f1d&studies=%5B%22MASimple%40tv-basicstudies%22%2C%22RSI%40tv-basicstudies%22%2C%22Volume%40tv-basicstudies%22%5D&theme=dark&style=1&timezone=Etc%2FUTC&locale=en`;
}

// RENDER 27 CHAPTERS MATRIX
function renderChaptersGrid(searchTerm = "") {
  const container = document.getElementById("chaptersGridContainer");
  if (!container) return;

  const filtered = chaptersData.filter(ch => {
    const term = searchTerm.toLowerCase();
    return ch.title.toLowerCase().includes(term) ||
           ch.focus.toLowerCase().includes(term) ||
           ch.num.includes(term) ||
           ch.part.toLowerCase().includes(term);
  });

  container.innerHTML = "";

  filtered.forEach(ch => {
    const card = document.createElement("div");
    card.className = "glass-card rounded-xl p-4 border border-dark-800 hover:border-cyan-500/50 transition flex flex-col justify-between space-y-2.5";
    card.innerHTML = `
      <div>
        <div class="flex items-center justify-between text-[11px] mono text-slate-400 mb-1">
          <span class="text-cyan-400 font-bold">${ch.part}</span>
          <span>CH ${ch.num}</span>
        </div>
        <h4 class="text-sm font-bold text-white group-hover:text-cyan-300 transition">${ch.title}</h4>
        <p class="text-xs text-slate-300 mt-1 leading-relaxed">${ch.focus}</p>
      </div>
      <div class="pt-2 border-t border-dark-800 flex items-center justify-between text-[10px] mono text-slate-400">
        <span class="truncate max-w-[200px]">${ch.path}/</span>
        <span class="px-1.5 py-0.5 rounded bg-dark-800 text-slate-300 border border-slate-700">Guide</span>
      </div>
    `;
    container.appendChild(card);
  });
}

// DEFLATED SHARPE RATIO (DSR) CALCULATOR
function calculateDSR() {
  const srInput = parseFloat(document.getElementById("dsr_sr")?.value || 1.4);
  const nTrials = parseInt(document.getElementById("dsr_trials")?.value || 50);
  const trackYears = parseFloat(document.getElementById("dsr_years")?.value || 5);
  const skew = parseFloat(document.getElementById("dsr_skew")?.value || -0.2);
  const kurtosis = parseFloat(document.getElementById("dsr_kurtosis")?.value || 3.8);

  const eulerGamma = 0.5772156649;
  const tSamples = trackYears * 252;

  let eMaxSR = 0;
  if (nTrials > 1) {
    const term = Math.sqrt(2 * Math.log(nTrials));
    eMaxSR = (1 - eulerGamma / (2 * Math.log(nTrials))) * term + (eulerGamma / term);
    const seSR = Math.sqrt((1 - skew * srInput / Math.sqrt(252) + (kurtosis - 1) / 4 * Math.pow(srInput / Math.sqrt(252), 2)) / tSamples) * Math.sqrt(252);
    eMaxSR = eMaxSR * seSR;
  }

  const seAnnualized = Math.sqrt(
    (1 + 0.5 * Math.pow(srInput, 2) - skew * srInput + ((kurtosis - 3) / 4) * Math.pow(srInput, 2)) / trackYears
  );

  const zScore = (srInput - eMaxSR) / Math.max(seAnnualized, 0.001);
  const pValue = 1 / (1 + Math.exp(-0.07056 * Math.pow(zScore, 3) - 1.5976 * zScore));
  const confidence = Math.max(0, Math.min(100, (pValue * 100))).toFixed(1);

  const resMaxSR = document.getElementById("res_max_sr");
  if (resMaxSR) resMaxSR.innerText = eMaxSR.toFixed(2);

  const resZ = document.getElementById("res_z_score");
  if (resZ) resZ.innerText = zScore.toFixed(2);

  const resProb = document.getElementById("res_confidence");
  if (resProb) resProb.innerText = `${confidence}%`;

  const badge = document.getElementById("dsr_verdict_badge");
  if (badge) {
    if (pValue >= 0.95) {
      badge.className = "px-3 py-1 rounded-full text-xs font-bold bg-emerald-500/20 text-emerald-400 border border-emerald-500/30 mono";
      badge.innerText = "PASSED: Statistically Significant Edge (p > 0.95)";
    } else if (pValue >= 0.80) {
      badge.className = "px-3 py-1 rounded-full text-xs font-bold bg-amber-500/20 text-amber-400 border border-amber-500/30 mono";
      badge.innerText = "MARGINAL: Moderate Multiple-Testing Haircut (0.80 < p < 0.95)";
    } else {
      badge.className = "px-3 py-1 rounded-full text-xs font-bold bg-rose-500/20 text-rose-400 border border-rose-500/30 mono";
      badge.innerText = "REJECTED: Likely False Discovery / Overfitted (p < 0.80)";
    }
  }
}

// COPY HELPER
function copyToClipboard(text) {
  navigator.clipboard.writeText(text).then(() => {
    alert(`Copied to clipboard:\n${text}`);
  }).catch(() => {
    prompt("Copy command:", text);
  });
}

// INITIALIZE ON DOM READY
document.addEventListener("DOMContentLoaded", () => {
  renderFilterButtons();
  renderMainTable();
  renderPipelineInspector(caseStudiesData[0].pipeline);
  renderChaptersGrid();
  calculateDSR();

  const searchInput = document.getElementById("chapterSearchInput");
  if (searchInput) {
    searchInput.addEventListener("input", (e) => {
      renderChaptersGrid(e.target.value);
    });
  }
});

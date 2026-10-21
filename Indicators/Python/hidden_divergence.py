"""
Hidden and Regular Divergence Detector (Stochastic-based).
Extracted from: MKHiddenDivergence.cs / MK HD Test 10 Stocks.py

Detects strictly consecutive crests and troughs:
  1. Hidden Bearish (Price LH + Stoch HH) -> Trend Continuation Down
  2. Hidden Bullish (Price HL + Stoch LL) -> Trend Continuation Up
  3. Regular Bearish (Price HH + Stoch LH) -> Reversal Down
  4. Regular Bullish (Price LL + Stoch HL) -> Reversal Up
"""

import numpy as np
import pandas as pd
from typing import Dict, Any, List, Optional


def compute_stochastic(
    high: np.ndarray,
    low: np.ndarray,
    close: np.ndarray,
    period_k: int = 10,
    smooth: int = 10,
    period_d: int = 3
) -> Dict[str, np.ndarray]:
    """Compute Fast %K, Slow %K, and Slow %D."""
    n = len(close)
    fast_k = np.full(n, 50.0, dtype=float)
    slow_k = np.full(n, 50.0, dtype=float)
    slow_d = np.full(n, 50.0, dtype=float)

    for i in range(n):
        k_lookback = min(i + 1, period_k)
        start = i - k_lookback + 1
        min_low = np.min(low[start:i + 1])
        max_high = np.max(high[start:i + 1])
        rng = max_high - min_low
        if rng > 1e-8:
            fast_k[i] = np.clip(100.0 * (close[i] - min_low) / rng, 0.0, 100.0)
        else:
            fast_k[i] = 50.0 if i == 0 else fast_k[i - 1]

    # Slow %K = SMA(fast_k, smooth)
    if smooth <= 1:
        slow_k = fast_k.copy()
    else:
        for i in range(n):
            s_count = min(i + 1, smooth)
            slow_k[i] = np.mean(fast_k[i - s_count + 1:i + 1])

    # Slow %D = SMA(slow_k, period_d)
    if period_d <= 1:
        slow_d = slow_k.copy()
    else:
        for i in range(n):
            d_count = min(i + 1, period_d)
            slow_d[i] = np.mean(slow_k[i - d_count + 1:i + 1])

    return {"fast_k": fast_k, "stoch_k": slow_k, "stoch_d": slow_d}


def detect_divergences(
    high: np.ndarray,
    low: np.ndarray,
    close: np.ndarray,
    period_k: int = 10,
    smooth: int = 10,
    period_d: int = 3,
    swing_lookback: int = 3,
    swing_lookahead: int = 3,
    price_bar_window: int = 4,
    min_bars_between: int = 4,
    max_bars_between: int = 45,
    min_stoch_diff: float = 2.0,
    min_price_diff: float = 0.0001,
    peak_min_stoch: float = 45.0,
    trough_max_stoch: float = 55.0
) -> Dict[str, Any]:
    """
    Detect Hidden and Regular Divergences matching MKHiddenDivergence logic.
    Returns:
        signals: array where:
            1: Hidden Bullish
            2: Regular Bullish
           -1: Hidden Bearish
           -2: Regular Bearish
            0: None
        events: list of divergence event dicts with exact swing anchors
    """
    n = len(close)
    stoch = compute_stochastic(high, low, close, period_k, smooth, period_d)
    k = stoch["stoch_k"]

    signals = np.zeros(n, dtype=int)
    events = []

    last_peak_stoch_bar = -1
    last_peak_stoch_val = 0.0
    last_peak_price_bar = -1
    last_peak_price_val = 0.0

    last_trough_stoch_bar = -1
    last_trough_stoch_val = 0.0
    last_trough_price_bar = -1
    last_trough_price_val = 0.0

    min_required = period_k + smooth + period_d + swing_lookback + swing_lookahead + price_bar_window + 2

    # Step through closed bars
    for current_bar in range(min_required, n):
        swing_bar = current_bar - swing_lookahead
        ki = k[swing_bar]

        # -------------------------------------------------------------
        # 1. EVALUATE PEAKS (Bearish Divergence)
        # -------------------------------------------------------------
        if ki > peak_min_stoch:
            is_peak = True
            for j in range(1, swing_lookback + 1):
                if ki <= k[swing_bar - j]:
                    is_peak = False
                    break
            if is_peak:
                for j in range(1, swing_lookahead + 1):
                    if ki < k[swing_bar + j]:
                        is_peak = False
                        break

            if is_peak:
                # Find localized price extreme
                w_start = max(0, swing_bar - price_bar_window)
                w_end = min(current_bar, swing_bar + price_bar_window)
                best_price_bar = w_start + int(np.argmax(high[w_start:w_end + 1]))
                best_h = high[best_price_bar]

                if last_peak_stoch_bar >= 0:
                    s_dist = swing_bar - last_peak_stoch_bar
                    p_dist = abs(best_price_bar - last_peak_price_bar)

                    if min_bars_between <= s_dist <= max_bars_between and p_dist >= min_bars_between:
                        p_delta = best_h - last_peak_price_val
                        s_delta = ki - last_peak_stoch_val

                        # Hidden Bearish: Price LH + Stoch HH
                        if p_delta < -min_price_diff and s_delta > min_stoch_diff:
                            signals[current_bar] = -1
                            events.append({
                                "type": "HIDDEN_BEARISH",
                                "bar_index": current_bar,
                                "p_start_bar": last_peak_price_bar,
                                "p_start_val": last_peak_price_val,
                                "p_end_bar": best_price_bar,
                                "p_end_val": best_h,
                                "s_start_bar": last_peak_stoch_bar,
                                "s_start_val": last_peak_stoch_val,
                                "s_end_bar": swing_bar,
                                "s_end_val": ki
                            })
                        # Regular Bearish: Price HH + Stoch LH
                        elif p_delta > min_price_diff and s_delta < -min_stoch_diff:
                            signals[current_bar] = -2
                            events.append({
                                "type": "REGULAR_BEARISH",
                                "bar_index": current_bar,
                                "p_start_bar": last_peak_price_bar,
                                "p_start_val": last_peak_price_val,
                                "p_end_bar": best_price_bar,
                                "p_end_val": best_h,
                                "s_start_bar": last_peak_stoch_bar,
                                "s_start_val": last_peak_stoch_val,
                                "s_end_bar": swing_bar,
                                "s_end_val": ki
                            })

                # Advance peak anchor strictly consecutive
                last_peak_stoch_bar = swing_bar
                last_peak_stoch_val = ki
                last_peak_price_bar = best_price_bar
                last_peak_price_val = best_h

        # -------------------------------------------------------------
        # 2. EVALUATE TROUGHS (Bullish Divergence)
        # -------------------------------------------------------------
        if ki < trough_max_stoch:
            is_trough = True
            for j in range(1, swing_lookback + 1):
                if ki >= k[swing_bar - j]:
                    is_trough = False
                    break
            if is_trough:
                for j in range(1, swing_lookahead + 1):
                    if ki > k[swing_bar + j]:
                        is_trough = False
                        break

            if is_trough:
                # Find localized price extreme
                w_start = max(0, swing_bar - price_bar_window)
                w_end = min(current_bar, swing_bar + price_bar_window)
                best_price_bar = w_start + int(np.argmin(low[w_start:w_end + 1]))
                best_l = low[best_price_bar]

                if last_trough_stoch_bar >= 0:
                    s_dist = swing_bar - last_trough_stoch_bar
                    p_dist = abs(best_price_bar - last_trough_price_bar)

                    if min_bars_between <= s_dist <= max_bars_between and p_dist >= min_bars_between:
                        p_delta = best_l - last_trough_price_val
                        s_delta = ki - last_trough_stoch_val

                        # Hidden Bullish: Price HL + Stoch LL
                        if p_delta > min_price_diff and s_delta < -min_stoch_diff:
                            signals[current_bar] = 1
                            events.append({
                                "type": "HIDDEN_BULLISH",
                                "bar_index": current_bar,
                                "p_start_bar": last_trough_price_bar,
                                "p_start_val": last_trough_price_val,
                                "p_end_bar": best_price_bar,
                                "p_end_val": best_l,
                                "s_start_bar": last_trough_stoch_bar,
                                "s_start_val": last_trough_stoch_val,
                                "s_end_bar": swing_bar,
                                "s_end_val": ki
                            })
                        # Regular Bullish: Price LL + Stoch HL
                        elif p_delta < -min_price_diff and s_delta > min_stoch_diff:
                            signals[current_bar] = 2
                            events.append({
                                "type": "REGULAR_BULLISH",
                                "bar_index": current_bar,
                                "p_start_bar": last_trough_price_bar,
                                "p_start_val": last_trough_price_val,
                                "p_end_bar": best_price_bar,
                                "p_end_val": best_l,
                                "s_start_bar": last_trough_stoch_bar,
                                "s_start_val": last_trough_stoch_val,
                                "s_end_bar": swing_bar,
                                "s_end_val": ki
                            })

                # Advance trough anchor strictly consecutive
                last_trough_stoch_bar = swing_bar
                last_trough_stoch_val = ki
                last_trough_price_bar = best_price_bar
                last_trough_price_val = best_l

    return {
        "stoch_k": k,
        "stoch_d": stoch["stoch_d"],
        "signals": signals,
        "events": events
    }


# ==============================================================================
# Platform Code Exporters
# ==============================================================================

def to_pinescript(period_k: int = 10, smooth: int = 10, period_d: int = 3) -> str:
    """Generate TradingView Pine Script v5."""
    return f"""//@version=5
indicator("MK Hidden & Regular Divergence", overlay=true)

// Parameters
periodK       = input.int({period_k}, "Stoch %K Period", minval=1)
smoothK       = input.int({smooth}, "Stoch %K Smooth", minval=1)
periodD       = input.int({period_d}, "Stoch %D Period", minval=1)
lookback      = input.int(3, "Swing Lookback", minval=1)
lookahead     = input.int(3, "Swing Lookahead", minval=1)
priceWin      = input.int(4, "Price Search Window", minval=1)
minDist       = input.int(4, "Min Bars Between Swings", minval=1)
maxDist       = input.int(45, "Max Bars Between Swings", minval=1)
minStochDiff  = input.float(2.0, "Min Stoch Delta", minval=0.0)

// Calculate Stochastics
rawK = ta.stoch(close, high, low, periodK)
k = ta.sma(rawK, smoothK)
d = ta.sma(k, periodD)

// Swing detection (lagged by lookahead bars)
swingBar = bar_index - lookahead
ki = k[lookahead]

isPeak = ki > 45.0
if isPeak
    for j = 1 to lookback
        if ki <= k[lookahead + j]
            isPeak := false
            break
    if isPeak
        for j = 1 to lookahead
            if ki < k[lookahead - j]
                isPeak := false
                break

isTrough = ki < 55.0
if isTrough
    for j = 1 to lookback
        if ki >= k[lookahead + j]
            isTrough := false
            break
    if isTrough
        for j = 1 to lookahead
            if ki > k[lookahead - j]
                isTrough := false
                break

var int lastPeakSBar = na
var float lastPeakSVal = na
var int lastPeakPBar = na
var float lastPeakPVal = na

var int lastTroughSBar = na
var float lastTroughSVal = na
var int lastTroughPBar = na
var float lastTroughPVal = na

var int sig = 0
sig := 0

if isPeak
    // Find highest high in price window
    int bestPBar = swingBar
    float bestH = high[lookahead]
    for w = -priceWin to priceWin
        int offset = lookahead - w
        if offset >= 0 and high[offset] > bestH
            bestH := high[offset]
            bestPBar := bar_index - offset

    if not na(lastPeakSBar)
        int sDist = swingBar - lastPeakSBar
        int pDist = math.abs(bestPBar - lastPeakPBar)
        if sDist >= minDist and sDist <= maxDist and pDist >= minDist
            float pDelta = bestH - lastPeakPVal
            float sDelta = ki - lastPeakSVal
            if pDelta < 0 and sDelta > minStochDiff
                sig := -1 // Hidden Bearish
                line.new(lastPeakPBar, lastPeakPVal, bestPBar, bestH, color=color.cyan, width=2)
            else if pDelta > 0 and sDelta < -minStochDiff
                sig := -2 // Regular Bearish
                line.new(lastPeakPBar, lastPeakPVal, bestPBar, bestH, color=color.orange, width=2, style=line.style_dashed)

    lastPeakSBar := swingBar
    lastPeakSVal := ki
    lastPeakPBar := bestPBar
    lastPeakPVal := bestH

if isTrough
    // Find lowest low in price window
    int bestPBar = swingBar
    float bestL = low[lookahead]
    for w = -priceWin to priceWin
        int offset = lookahead - w
        if offset >= 0 and low[offset] < bestL
            bestL := low[offset]
            bestPBar := bar_index - offset

    if not na(lastTroughSBar)
        int sDist = swingBar - lastTroughSBar
        int pDist = math.abs(bestPBar - lastTroughPBar)
        if sDist >= minDist and sDist <= maxDist and pDist >= minDist
            float pDelta = bestL - lastTroughPVal
            float sDelta = ki - lastTroughSVal
            if pDelta > 0 and sDelta < -minStochDiff
                sig := 1 // Hidden Bullish
                line.new(lastTroughPBar, lastTroughPVal, bestPBar, bestL, color=color.cyan, width=2)
            else if pDelta < 0 and sDelta > minStochDiff
                sig := 2 // Regular Bullish
                line.new(lastTroughPBar, lastTroughPVal, bestPBar, bestL, color=color.lime, width=2, style=line.style_dashed)

    lastTroughSBar := swingBar
    lastTroughSVal := ki
    lastTroughPBar := bestPBar
    lastTroughPVal := bestL

plotshape(sig == 1, title="Hidden Bull", style=shape.triangleup, location=location.belowbar, color=color.cyan, size=size.small)
plotshape(sig == -1, title="Hidden Bear", style=shape.triangledown, location=location.abovebar, color=color.cyan, size=size.small)
plotshape(sig == 2, title="Regular Bull", style=shape.triangleup, location=location.belowbar, color=color.lime, size=size.small)
plotshape(sig == -2, title="Regular Bear", style=shape.triangledown, location=location.abovebar, color=color.orange, size=size.small)
"""


def to_ninjatrader8() -> str:
    """Generate NinjaTrader 8 indicator from archived source."""
    with open("/home/mk/MK-T Alpha/Canonical_Indicator_Specification/hidden_divergence/source_archive/original_source.cs", "r") as f:
        return f.read()


def to_ctrader(period_k: int = 10, smooth: int = 10, period_d: int = 3) -> str:
    """Generate cTrader C# indicator."""
    return f"""using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;

namespace cAlgo
{{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class MKHiddenDivergence : Indicator
    {{
        [Parameter("%K Period", DefaultValue = {period_k}, MinValue = 1)]
        public int PeriodK {{ get; set; }}

        [Parameter("%K Smooth", DefaultValue = {smooth}, MinValue = 1)]
        public int SmoothK {{ get; set; }}

        [Parameter("%D Period", DefaultValue = {period_d}, MinValue = 1)]
        public int PeriodD {{ get; set; }}

        [Parameter("Swing Lookback", DefaultValue = 3, MinValue = 1)]
        public int Lookback {{ get; set; }}

        [Parameter("Swing Lookahead", DefaultValue = 3, MinValue = 1)]
        public int Lookahead {{ get; set; }}

        [Parameter("Price Window", DefaultValue = 4, MinValue = 1)]
        public int PriceWindow {{ get; set; }}

        [Parameter("Min Bars Dist", DefaultValue = 4, MinValue = 1)]
        public int MinBarsDist {{ get; set; }}

        [Parameter("Max Bars Dist", DefaultValue = 45, MinValue = 1)]
        public int MaxBarsDist {{ get; set; }}

        [Parameter("Min Stoch Diff", DefaultValue = 2.0, MinValue = 0.0)]
        public double MinStochDiff {{ get; set; }}

        private StochasticOscillator _stoch;
        private int _lastPeakSBar = -1, _lastPeakPBar = -1;
        private double _lastPeakSVal = 0.0, _lastPeakPVal = 0.0;
        private int _lastTroughSBar = -1, _lastTroughPBar = -1;
        private double _lastTroughSVal = 0.0, _lastTroughPVal = 0.0;

        protected override void Initialize()
        {{
            _stoch = Indicators.StochasticOscillator(PeriodK, SmoothK, PeriodD, MovingAverageType.Simple);
        }}

        public override void Calculate(int index)
        {{
            if (index < PeriodK + SmoothK + PeriodD + Lookback + Lookahead + PriceWindow + 2)
                return;

            int swingBar = index - Lookahead;
            double ki = _stoch.PercentK[swingBar];

            // Evaluate Peaks
            if (ki > 45.0)
            {{
                bool isPeak = true;
                for (int j = 1; j <= Lookback; j++)
                    if (ki <= _stoch.PercentK[swingBar - j]) {{ isPeak = false; break; }}
                if (isPeak)
                    for (int j = 1; j <= Lookahead; j++)
                        if (ki < _stoch.PercentK[swingBar + j]) {{ isPeak = false; break; }}

                if (isPeak)
                {{
                    int bestPBar = swingBar;
                    double bestH = Bars.HighPrices[swingBar];
                    int wStart = Math.Max(0, swingBar - PriceWindow);
                    int wEnd = Math.Min(index, swingBar + PriceWindow);
                    for (int w = wStart; w <= wEnd; w++)
                    {{
                        if (Bars.HighPrices[w] > bestH)
                        {{
                            bestH = Bars.HighPrices[w];
                            bestPBar = w;
                        }}
                    }}

                    if (_lastPeakSBar >= 0)
                    {{
                        int sDist = swingBar - _lastPeakSBar;
                        int pDist = Math.Abs(bestPBar - _lastPeakPBar);
                        if (sDist >= MinBarsDist && sDist <= MaxBarsDist && pDist >= MinBarsDist)
                        {{
                            double pDelta = bestH - _lastPeakPVal;
                            double sDelta = ki - _lastPeakSVal;

                            if (pDelta < -Symbol.PipSize && sDelta > MinStochDiff)
                            {{
                                // Hidden Bearish
                                Chart.DrawTrendLine("HBear_" + index, _lastPeakPBar, _lastPeakPVal, bestPBar, bestH, Color.Cyan, 2, LineStyle.Solid);
                                Chart.DrawIcon("HBearIcon_" + index, ChartIconType.DownArrow, bestPBar, bestH + Symbol.PipSize * 4, Color.Cyan);
                            }}
                            else if (pDelta > Symbol.PipSize && sDelta < -MinStochDiff)
                            {{
                                // Regular Bearish
                                Chart.DrawTrendLine("RBear_" + index, _lastPeakPBar, _lastPeakPVal, bestPBar, bestH, Color.DarkOrange, 2, LineStyle.Dots);
                                Chart.DrawIcon("RBearIcon_" + index, ChartIconType.DownArrow, bestPBar, bestH + Symbol.PipSize * 4, Color.DarkOrange);
                            }}
                        }}
                    }}

                    _lastPeakSBar = swingBar;
                    _lastPeakSVal = ki;
                    _lastPeakPBar = bestPBar;
                    _lastPeakPVal = bestH;
                }}
            }}

            // Evaluate Troughs
            if (ki < 55.0)
            {{
                bool isTrough = true;
                for (int j = 1; j <= Lookback; j++)
                    if (ki >= _stoch.PercentK[swingBar - j]) {{ isTrough = false; break; }}
                if (isTrough)
                    for (int j = 1; j <= Lookahead; j++)
                        if (ki > _stoch.PercentK[swingBar + j]) {{ isTrough = false; break; }}

                if (isTrough)
                {{
                    int bestPBar = swingBar;
                    double bestL = Bars.LowPrices[swingBar];
                    int wStart = Math.Max(0, swingBar - PriceWindow);
                    int wEnd = Math.Min(index, swingBar + PriceWindow);
                    for (int w = wStart; w <= wEnd; w++)
                    {{
                        if (Bars.LowPrices[w] < bestL)
                        {{
                            bestL = Bars.LowPrices[w];
                            bestPBar = w;
                        }}
                    }}

                    if (_lastTroughSBar >= 0)
                    {{
                        int sDist = swingBar - _lastTroughSBar;
                        int pDist = Math.Abs(bestPBar - _lastTroughPBar);
                        if (sDist >= MinBarsDist && sDist <= MaxBarsDist && pDist >= MinBarsDist)
                        {{
                            double pDelta = bestL - _lastTroughPVal;
                            double sDelta = ki - _lastTroughSVal;

                            if (pDelta > Symbol.PipSize && sDelta < -MinStochDiff)
                            {{
                                // Hidden Bullish
                                Chart.DrawTrendLine("HBull_" + index, _lastTroughPBar, _lastTroughPVal, bestPBar, bestL, Color.Cyan, 2, LineStyle.Solid);
                                Chart.DrawIcon("HBullIcon_" + index, ChartIconType.UpArrow, bestPBar, bestL - Symbol.PipSize * 4, Color.Cyan);
                            }}
                            else if (pDelta < -Symbol.PipSize && sDelta > MinStochDiff)
                            {{
                                // Regular Bullish
                                Chart.DrawTrendLine("RBull_" + index, _lastTroughPBar, _lastTroughPVal, bestPBar, bestL, Color.LimeGreen, 2, LineStyle.Dots);
                                Chart.DrawIcon("RBullIcon_" + index, ChartIconType.UpArrow, bestPBar, bestL - Symbol.PipSize * 4, Color.LimeGreen);
                            }}
                        }}
                    }}

                    _lastTroughSBar = swingBar;
                    _lastTroughSVal = ki;
                    _lastTroughPBar = bestPBar;
                    _lastTroughPVal = bestL;
                }}
            }}
        }}
    }}
}}
"""


def to_mt4(period_k: int = 10, smooth: int = 10, period_d: int = 3) -> str:
    """Generate MetaTrader 4 (MQL4) indicator."""
    return f"""//+------------------------------------------------------------------+
//|                                           MKHiddenDivergence.mq4 |
//|                                  Copyright 2026, MK Quantitative |
//+------------------------------------------------------------------+
#property copyright "MK Quantitative"
#property link      ""
#property version   "1.00"
#property strict
#property indicator_chart_window
#property indicator_buffers 4
#property indicator_color1  clrCyan
#property indicator_color2  clrLimeGreen
#property indicator_color3  clrCyan
#property indicator_color4  clrDarkOrange

input int    InpKPeriod     = {period_k};
input int    InpKSmooth     = {smooth};
input int    InpDPeriod     = {period_d};
input int    InpLookback    = 3;
input int    InpLookahead   = 3;
input int    InpPriceWin    = 4;
input int    InpMinDist     = 4;
input int    InpMaxDist     = 45;
input double InpMinStochDif = 2.0;

double HBullBuffer[];
double RBullBuffer[];
double HBearBuffer[];
double RBearBuffer[];

int OnInit()
{{
   SetIndexBuffer(0, HBullBuffer);
   SetIndexStyle(0, DRAW_ARROW);
   SetIndexArrow(0, 233);
   SetIndexLabel(0, "Hidden Bullish");

   SetIndexBuffer(1, RBullBuffer);
   SetIndexStyle(1, DRAW_ARROW);
   SetIndexArrow(1, 233);
   SetIndexLabel(1, "Regular Bullish");

   SetIndexBuffer(2, HBearBuffer);
   SetIndexStyle(2, DRAW_ARROW);
   SetIndexArrow(2, 234);
   SetIndexLabel(2, "Hidden Bearish");

   SetIndexBuffer(3, RBearBuffer);
   SetIndexStyle(3, DRAW_ARROW);
   SetIndexArrow(3, 234);
   SetIndexLabel(3, "Regular Bearish");

   return(INIT_SUCCEEDED);
}}

int OnCalculate(const int rates_total,
                const int prev_calculated,
                const datetime &time[],
                const double &open[],
                const double &high[],
                const double &low[],
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
{{
   return(rates_total);
}}
"""


def to_mt5(period_k: int = 10, smooth: int = 10, period_d: int = 3) -> str:
    """Generate MetaTrader 5 (MQL5) indicator."""
    return f"""//+------------------------------------------------------------------+
//|                                           MKHiddenDivergence.mq5 |
//|                                  Copyright 2026, MK Quantitative |
//+------------------------------------------------------------------+
#property copyright "MK Quantitative"
#property link      ""
#property version   "1.00"
#property indicator_chart_window
#property indicator_buffers 4
#property indicator_plots   4

input int    InpKPeriod     = {period_k};
input int    InpKSmooth     = {smooth};
input int    InpDPeriod     = {period_d};
input int    InpLookback    = 3;
input int    InpLookahead   = 3;

double HBullBuffer[];
double RBullBuffer[];
double HBearBuffer[];
double RBearBuffer[];

int OnInit()
{{
   SetIndexBuffer(0, HBullBuffer, INDICATOR_DATA);
   SetIndexBuffer(1, RBullBuffer, INDICATOR_DATA);
   SetIndexBuffer(2, HBearBuffer, INDICATOR_DATA);
   SetIndexBuffer(3, RBearBuffer, INDICATOR_DATA);
   return(INIT_SUCCEEDED);
}}

int OnCalculate(const int rates_total,
                const int prev_calculated,
                const datetime &time[],
                const double &open[],
                const double &high[],
                const double &low[],
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
{{
   return(rates_total);
}}
"""


if __name__ == "__main__":
    print("Testing Hidden Divergence calculation...")
    np.random.seed(42)
    n = 100
    p = 100.0 + np.cumsum(np.random.randn(n))
    res = detect_divergences(p + 1.0, p - 1.0, p)
    print("Detected events:", len(res["events"]))

"""
Volatility Stop Indicator - Universal Calculation Engine & Platform Generators.
Extracted from: MK_FVG_VolatilityStop.cs

Provides:
  1. Pure Python calculation (numpy / pandas)
  2. Standalone Code Generators:
     - cTrader (C#)
     - TradingView (Pine Script v5)
     - NinjaTrader 8 (C#)
"""

import numpy as np
import pandas as pd
from typing import Dict, Any, Tuple


def calculate_atr(df: pd.DataFrame, period: int = 20, ma_type: str = "SMA") -> pd.Series:
    """Calculate Average True Range (ATR)."""
    high = df['high']
    low = df['low']
    close = df['close']
    prev_close = close.shift(1)

    tr1 = high - low
    tr2 = (high - prev_close).abs()
    tr3 = (low - prev_close).abs()
    tr = pd.concat([tr1, tr2, tr3], axis=1).max(axis=1)

    ma_type = ma_type.upper()
    if ma_type == "EMA":
        return tr.ewm(span=period, adjust=False).mean()
    elif ma_type == "RMA" or ma_type == "WILDER":
        return tr.ewm(alpha=1.0 / period, adjust=False).mean()
    else:  # Default SMA
        return tr.rolling(window=period).mean()


def calculate_volatility_stop(
    df: pd.DataFrame,
    period: int = 20,
    multiplier: float = 3.0,
    ma_type: str = "SMA"
) -> pd.DataFrame:
    """
    Calculate Volatility Stop with exact ratchet mechanics matching MK_FVG_VolatilityStop.cs.

    Returns DataFrame with columns:
      - 'vstop': active stop line value
      - 'is_bullish': boolean trend series
      - 'bullish_stop': lower stop line (NaN during bear)
      - 'bearish_stop': upper stop line (NaN during bull)
      - 'trend_flip': 1 for bull flip, -1 for bear flip, 0 otherwise
    """
    n = len(df)
    close = df['close'].to_numpy(dtype=float)
    high = df['high'].to_numpy(dtype=float)
    low = df['low'].to_numpy(dtype=float)
    
    atr_series = calculate_atr(df, period=period, ma_type=ma_type)
    atr = atr_series.to_numpy(dtype=float)

    is_bullish = np.zeros(n, dtype=bool)
    lower = np.full(n, np.nan, dtype=float)
    upper = np.full(n, np.nan, dtype=float)
    bullish_stop = np.full(n, np.nan, dtype=float)
    bearish_stop = np.full(n, np.nan, dtype=float)
    trend_flip = np.zeros(n, dtype=int)

    for i in range(n):
        c0 = close[i]
        a0 = atr[i]
        if np.isnan(a0) or a0 <= 0:
            a0 = high[i] - low[i]

        if i <= period:
            is_bullish[i] = True
            lower[i] = c0 - multiplier * a0
            upper[i] = c0 + multiplier * a0
            if i == period:
                bullish_stop[i] = lower[i]
            continue

        prev_trend = is_bullish[i - 1]
        prev_lower = lower[i - 1]
        prev_upper = upper[i - 1]
        a1 = atr[i - 1]
        if np.isnan(a1):
            a1 = a0

        curr_trend = prev_trend
        curr_lower = prev_lower
        curr_upper = prev_upper

        if not prev_trend and c0 > prev_upper:
            # Bearish -> Bullish Flip
            curr_trend = True
            curr_lower = c0 - multiplier * a0
            trend_flip[i] = 1
        elif prev_trend and c0 < prev_lower:
            # Bullish -> Bearish Flip
            curr_trend = False
            curr_upper = c0 + multiplier * a0
            trend_flip[i] = -1
        elif curr_trend:
            # Trailing Uptrend with Ratchet
            candidate1 = c0 - multiplier * a0
            candidate2 = prev_lower + multiplier * (a1 - a0)
            curr_lower = max(prev_lower, candidate1, candidate2)
        else:
            # Trailing Downtrend with Ratchet
            candidate1 = c0 + multiplier * a0
            candidate2 = prev_upper - multiplier * (a1 - a0)
            curr_upper = min(prev_upper, candidate1, candidate2)

        is_bullish[i] = curr_trend
        lower[i] = curr_lower
        upper[i] = curr_upper

        if curr_trend:
            bullish_stop[i] = curr_lower
        else:
            bearish_stop[i] = curr_upper

    res = df.copy()
    res['is_bullish'] = is_bullish
    res['vstop'] = np.where(is_bullish, lower, upper)
    res['bullish_stop'] = bullish_stop
    res['bearish_stop'] = bearish_stop
    res['trend_flip'] = trend_flip
    return res


# ==============================================================================
# Platform Code Exporters
# ==============================================================================

def to_pinescript(period: int = 20, multiplier: float = 3.0) -> str:
    """Generate TradingView Pine Script v5."""
    return f"""//@version=5
indicator("MK Volatility Stop", overlay=true)

period = input.int({period}, title="ATR Period", minval=1)
multiplier = input.float({multiplier}, title="ATR Multiplier", minval=0.1, step=0.1)

// ATR calculation (TradingView uses RMA by default, or change to ta.sma)
atr = ta.sma(ta.tr, period)

var bool isBullish = true
var float lower = na
var float upper = na

c0 = close
a0 = na(atr) ? (high - low) : atr
a1 = nz(atr[1], a0)

if bar_index == 0
    isBullish := true
    lower := c0 - multiplier * a0
    upper := c0 + multiplier * a0
else
    prevTrend = isBullish
    prevLower = lower
    prevUpper = upper
    
    if not prevTrend and c0 > prevUpper
        isBullish := true
        lower := c0 - multiplier * a0
    else if prevTrend and c0 < prevLower
        isBullish := false
        upper := c0 + multiplier * a0
    else if isBullish
        cand1 = c0 - multiplier * a0
        cand2 = prevLower + multiplier * (a1 - a0)
        lower := math.max(prevLower, math.max(cand1, cand2))
    else
        cand1 = c0 + multiplier * a0
        cand2 = prevUpper - multiplier * (a1 - a0)
        upper := math.min(prevUpper, math.min(cand1, cand2))

bullStop = isBullish ? lower : na
bearStop = not isBullish ? upper : na

plot(bullStop, title="Bullish Stop", color=color.green, style=plot.style_linebr, linewidth=2)
plot(bearStop, title="Bearish Stop", color=color.red, style=plot.style_linebr, linewidth=2)
"""


def to_ctrader(period: int = 20, multiplier: float = 3.0) -> str:
    """Generate standalone cTrader (C#) indicator."""
    return f"""using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;

namespace cAlgo
{{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class VolatilityStop : Indicator
    {{
        [Parameter("ATR Period", DefaultValue = {period}, MinValue = 1)]
        public int Period {{ get; set; }}

        [Parameter("ATR Multiplier", DefaultValue = {multiplier}, MinValue = 0.1, Step = 0.1)]
        public double Multiplier {{ get; set; }}

        [Parameter("ATR MA Type", DefaultValue = MovingAverageType.Simple)]
        public MovingAverageType MAType {{ get; set; }}

        [Output("Bullish Stop", LineColor = "LimeGreen", PlotType = PlotType.DiscontinuousLine, Thickness = 2)]
        public IndicatorDataSeries BullishStop {{ get; set; }}

        [Output("Bearish Stop", LineColor = "Crimson", PlotType = PlotType.DiscontinuousLine, Thickness = 2)]
        public IndicatorDataSeries BearishStop {{ get; set; }}

        private AverageTrueRange _atr;
        private bool[] _isBullish;
        private double[] _lower;
        private double[] _upper;

        protected override void Initialize()
        {{
            _atr = Indicators.AverageTrueRange(Period, MAType);
            int capacity = Math.Max(Bars.Count + 1024, 4096);
            _isBullish = new bool[capacity];
            _lower = new double[capacity];
            _upper = new double[capacity];
        }}

        public override void Calculate(int index)
        {{
            if (index >= _isBullish.Length)
            {{
                int newSize = Math.Max(index + 1024, _isBullish.Length * 2);
                Array.Resize(ref _isBullish, newSize);
                Array.Resize(ref _lower, newSize);
                Array.Resize(ref _upper, newSize);
            }}

            double close0 = Bars.ClosePrices[index];
            double atr0 = _atr.Result[index];
            if (double.IsNaN(atr0) || atr0 <= 0)
                atr0 = Bars.HighPrices[index] - Bars.LowPrices[index];

            if (index <= Period)
            {{
                _isBullish[index] = true;
                _lower[index] = close0 - Multiplier * atr0;
                _upper[index] = close0 + Multiplier * atr0;
                BullishStop[index] = (index == Period) ? _lower[index] : double.NaN;
                BearishStop[index] = double.NaN;
                return;
            }}

            bool prevTrend = _isBullish[index - 1];
            double prevLower = _lower[index - 1];
            double prevUpper = _upper[index - 1];
            double atr1 = _atr.Result[index - 1];
            if (double.IsNaN(atr1)) atr1 = atr0;

            bool currTrend = prevTrend;
            double currLower = prevLower;
            double currUpper = prevUpper;

            if (!prevTrend && close0 > prevUpper)
            {{
                currTrend = true;
                currLower = close0 - Multiplier * atr0;
            }}
            else if (prevTrend && close0 < prevLower)
            {{
                currTrend = false;
                currUpper = close0 + Multiplier * atr0;
            }}
            else if (currTrend)
            {{
                double cand1 = close0 - Multiplier * atr0;
                double cand2 = prevLower + Multiplier * (atr1 - atr0);
                currLower = Math.Max(Math.Max(prevLower, cand1), cand2);
            }}
            else
            {{
                double cand1 = close0 + Multiplier * atr0;
                double cand2 = prevUpper - Multiplier * (atr1 - atr0);
                currUpper = Math.Min(Math.Min(prevUpper, cand1), cand2);
            }}

            _isBullish[index] = currTrend;
            _lower[index] = currLower;
            _upper[index] = currUpper;

            if (currTrend)
            {{
                BullishStop[index] = currLower;
                BearishStop[index] = double.NaN;
            }}
            else
            {{
                BearishStop[index] = currUpper;
                BullishStop[index] = double.NaN;
            }}
        }}
    }}
}}
"""


def to_ninjatrader8(period: int = 20, multiplier: float = 3.0) -> str:
    """Generate NinjaTrader 8 C# indicator."""
    return f"""#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{{
    public class VolatilityStop : Indicator
    {{
        private Series<bool> isBullish;
        private Series<double> lower;
        private Series<double> upper;
        private ATR atr;

        protected override void OnStateChange()
        {{
            if (State == State.SetDefaults)
            {{
                Description = @"MK Volatility Stop Indicator";
                Name = "VolatilityStop";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                Period = {period};
                Multiplier = {multiplier};

                AddPlot(new Stroke(Brushes.LimeGreen, 2), PlotStyle.Line, "BullishStop");
                AddPlot(new Stroke(Brushes.Crimson, 2), PlotStyle.Line, "BearishStop");
            }}
            else if (State == State.DataLoaded)
            {{
                atr = ATR(Period);
                isBullish = new Series<bool>(this);
                lower = new Series<double>(this);
                upper = new Series<double>(this);
            }}
        }}

        protected override void OnBarUpdate()
        {{
            if (CurrentBar < Period)
            {{
                isBullish[0] = true;
                lower[0] = Close[0] - Multiplier * atr[0];
                upper[0] = Close[0] + Multiplier * atr[0];
                return;
            }}

            double c0 = Close[0];
            double a0 = atr[0];
            double a1 = atr[1];

            bool prevTrend = isBullish[1];
            double prevLower = lower[1];
            double prevUpper = upper[1];

            bool currTrend = prevTrend;
            double currLower = prevLower;
            double currUpper = prevUpper;

            if (!prevTrend && c0 > prevUpper)
            {{
                currTrend = true;
                currLower = c0 - Multiplier * a0;
            }}
            else if (prevTrend && c0 < prevLower)
            {{
                currTrend = false;
                currUpper = c0 + Multiplier * a0;
            }}
            else if (currTrend)
            {{
                double cand1 = c0 - Multiplier * a0;
                double cand2 = prevLower + Multiplier * (a1 - a0);
                currLower = Math.Max(Math.Max(prevLower, cand1), cand2);
            }}
            else
            {{
                double cand1 = c0 + Multiplier * a0;
                double cand2 = prevUpper - Multiplier * (a1 - a0);
                currUpper = Math.Min(Math.Min(prevUpper, cand1), cand2);
            }}

            isBullish[0] = currTrend;
            lower[0] = currLower;
            upper[0] = currUpper;

            if (currTrend)
            {{
                Values[0][0] = currLower;
                Values[1].Reset();
            }}
            else
            {{
                Values[1][0] = currUpper;
                Values[0].Reset();
            }}
        }}

        #region Properties
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name="Period", GroupName="Parameters", Order=1)]
        public int Period {{ get; set; }}

        [NinjaScriptProperty]
        [Range(0.1, double.MaxValue)]
        [Display(Name="Multiplier", GroupName="Parameters", Order=2)]
        public double Multiplier {{ get; set; }}
        #endregion
    }}
}}
"""


def to_mt4(period: int = 20, multiplier: float = 3.0) -> str:
    """Generate MetaTrader 4 (MQL4) indicator."""
    return f"""//+------------------------------------------------------------------+
//|                                              VolatilityStop.mq4  |
//|                                  Copyright 2026, MK Quantitative |
//+------------------------------------------------------------------+
#property copyright "MK Quantitative"
#property link      ""
#property version   "1.00"
#property strict
#property indicator_chart_window
#property indicator_buffers 2
#property indicator_color1  clrLimeGreen
#property indicator_color2  clrCrimson
#property indicator_width1  2
#property indicator_width2  2

// Inputs
input int    InpAtrPeriod     = {period};      // ATR Period
input double InpAtrMultiplier = {multiplier};  // ATR Multiplier

// Buffers
double BullishStopBuffer[];
double BearishStopBuffer[];

// State tracking arrays
bool   isBullish[];
double lowerStop[];
double upperStop[];

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
{{
   SetIndexBuffer(0, BullishStopBuffer);
   SetIndexStyle(0, DRAW_LINE);
   SetIndexEmptyValue(0, EMPTY_VALUE);
   SetIndexLabel(0, "Bullish Stop");

   SetIndexBuffer(1, BearishStopBuffer);
   SetIndexStyle(1, DRAW_LINE);
   SetIndexEmptyValue(1, EMPTY_VALUE);
   SetIndexLabel(1, "Bearish Stop");

   IndicatorShortName("Volatility Stop (" + IntegerToString(InpAtrPeriod) + ", " + DoubleToString(InpAtrMultiplier, 1) + ")");
   return(INIT_SUCCEEDED);
}}

//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
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
   if(rates_total <= InpAtrPeriod) return(0);

   ArrayResize(isBullish, rates_total);
   ArrayResize(lowerStop, rates_total);
   ArrayResize(upperStop, rates_total);

   int start = prev_calculated - 1;
   if(start < 0) start = 0;

   // Process in chronological order (0 to rates_total - 1)
   for(int i = start; i < rates_total; i++)
   {{
      int shift = rates_total - 1 - i;
      double c0 = close[shift];
      double a0 = iATR(NULL, 0, InpAtrPeriod, shift);
      if(a0 <= 0) a0 = high[shift] - low[shift];

      if(i <= InpAtrPeriod)
      {{
         isBullish[i] = true;
         lowerStop[i] = c0 - InpAtrMultiplier * a0;
         upperStop[i] = c0 + InpAtrMultiplier * a0;

         if(i == InpAtrPeriod)
         {{
            BullishStopBuffer[shift] = lowerStop[i];
            BearishStopBuffer[shift] = EMPTY_VALUE;
         }}
         else
         {{
            BullishStopBuffer[shift] = EMPTY_VALUE;
            BearishStopBuffer[shift] = EMPTY_VALUE;
         }}
         continue;
      }}

      bool prevTrend = isBullish[i - 1];
      double prevLower = lowerStop[i - 1];
      double prevUpper = upperStop[i - 1];
      
      int prevShift = rates_total - i; // shift for i - 1
      double a1 = iATR(NULL, 0, InpAtrPeriod, prevShift);
      if(a1 <= 0) a1 = a0;

      bool currTrend = prevTrend;
      double currLower = prevLower;
      double currUpper = prevUpper;

      if(!prevTrend && c0 > prevUpper)
      {{
         currTrend = true;
         currLower = c0 - InpAtrMultiplier * a0;
      }}
      else if(prevTrend && c0 < prevLower)
      {{
         currTrend = false;
         currUpper = c0 + InpAtrMultiplier * a0;
      }}
      else if(currTrend)
      {{
         double cand1 = c0 - InpAtrMultiplier * a0;
         double cand2 = prevLower + InpAtrMultiplier * (a1 - a0);
         currLower = MathMax(MathMax(prevLower, cand1), cand2);
      }}
      else
      {{
         double cand1 = c0 + InpAtrMultiplier * a0;
         double cand2 = prevUpper - InpAtrMultiplier * (a1 - a0);
         currUpper = MathMin(MathMin(prevUpper, cand1), cand2);
      }}

      isBullish[i] = currTrend;
      lowerStop[i] = currLower;
      upperStop[i] = currUpper;

      if(currTrend)
      {{
         BullishStopBuffer[shift] = currLower;
         BearishStopBuffer[shift] = EMPTY_VALUE;
      }}
      else
      {{
         BearishStopBuffer[shift] = currUpper;
         BullishStopBuffer[shift] = EMPTY_VALUE;
      }}
   }}

   return(rates_total);
}}
"""


def to_mt5(period: int = 20, multiplier: float = 3.0) -> str:
    """Generate MetaTrader 5 (MQL5) indicator."""
    return f"""//+------------------------------------------------------------------+
//|                                              VolatilityStop.mq5  |
//|                                  Copyright 2026, MK Quantitative |
//+------------------------------------------------------------------+
#property copyright "MK Quantitative"
#property link      ""
#property version   "1.00"
#property indicator_chart_window
#property indicator_buffers 2
#property indicator_plots   2
#property indicator_type1   DRAW_LINE
#property indicator_color1  clrLimeGreen
#property indicator_width1  2
#property indicator_label1  "Bullish Stop"
#property indicator_type2   DRAW_LINE
#property indicator_color2  clrCrimson
#property indicator_width2  2
#property indicator_label2  "Bearish Stop"

// Inputs
input int    InpAtrPeriod     = {period};      // ATR Period
input double InpAtrMultiplier = {multiplier};  // ATR Multiplier

// Buffers
double BullishStopBuffer[];
double BearishStopBuffer[];

// State tracking arrays
bool   isBullish[];
double lowerStop[];
double upperStop[];
int    atrHandle;

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
{{
   SetIndexBuffer(0, BullishStopBuffer, INDICATOR_DATA);
   PlotIndexSetDouble(0, PLOT_EMPTY_VALUE, EMPTY_VALUE);

   SetIndexBuffer(1, BearishStopBuffer, INDICATOR_DATA);
   PlotIndexSetDouble(1, PLOT_EMPTY_VALUE, EMPTY_VALUE);

   atrHandle = iATR(_Symbol, _Period, InpAtrPeriod);
   if(atrHandle == INVALID_HANDLE)
   {{
      Print("Failed to create ATR handle.");
      return(INIT_FAILED);
   }}

   IndicatorSetString(INDICATOR_SHORTNAME, "Volatility Stop (" + IntegerToString(InpAtrPeriod) + ", " + DoubleToString(InpAtrMultiplier, 1) + ")");
   return(INIT_SUCCEEDED);
}}

//+------------------------------------------------------------------+
//| Custom indicator deinitialization function                       |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
{{
   if(atrHandle != INVALID_HANDLE)
      IndicatorRelease(atrHandle);
}}

//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
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
   if(rates_total <= InpAtrPeriod) return(0);

   ArrayResize(isBullish, rates_total);
   ArrayResize(lowerStop, rates_total);
   ArrayResize(upperStop, rates_total);

   double atrVals[];
   ArraySetAsSeries(atrVals, false);
   if(CopyBuffer(atrHandle, 0, 0, rates_total, atrVals) <= 0)
      return(0);

   int start = prev_calculated - 1;
   if(start < 0) start = 0;

   // Process in chronological order (0 to rates_total - 1)
   for(int i = start; i < rates_total; i++)
   {{
      double c0 = close[i];
      double a0 = atrVals[i];
      if(a0 <= 0) a0 = high[i] - low[i];

      if(i <= InpAtrPeriod)
      {{
         isBullish[i] = true;
         lowerStop[i] = c0 - InpAtrMultiplier * a0;
         upperStop[i] = c0 + InpAtrMultiplier * a0;

         if(i == InpAtrPeriod)
         {{
            BullishStopBuffer[i] = lowerStop[i];
            BearishStopBuffer[i] = EMPTY_VALUE;
         }}
         else
         {{
            BullishStopBuffer[i] = EMPTY_VALUE;
            BearishStopBuffer[i] = EMPTY_VALUE;
         }}
         continue;
      }}

      bool prevTrend = isBullish[i - 1];
      double prevLower = lowerStop[i - 1];
      double prevUpper = upperStop[i - 1];
      double a1 = atrVals[i - 1];
      if(a1 <= 0) a1 = a0;

      bool currTrend = prevTrend;
      double currLower = prevLower;
      double currUpper = prevUpper;

      if(!prevTrend && c0 > prevUpper)
      {{
         currTrend = true;
         currLower = c0 - InpAtrMultiplier * a0;
      }}
      else if(prevTrend && c0 < prevLower)
      {{
         currTrend = false;
         currUpper = c0 + InpAtrMultiplier * a0;
      }}
      else if(currTrend)
      {{
         double cand1 = c0 - InpAtrMultiplier * a0;
         double cand2 = prevLower + InpAtrMultiplier * (a1 - a0);
         currLower = MathMax(MathMax(prevLower, cand1), cand2);
      }}
      else
      {{
         double cand1 = c0 + InpAtrMultiplier * a0;
         double cand2 = prevUpper - InpAtrMultiplier * (a1 - a0);
         currUpper = MathMin(MathMin(prevUpper, cand1), cand2);
      }}

      isBullish[i] = currTrend;
      lowerStop[i] = currLower;
      upperStop[i] = currUpper;

      if(currTrend)
      {{
         BullishStopBuffer[i] = currLower;
         BearishStopBuffer[i] = EMPTY_VALUE;
      }}
      else
      {{
         BearishStopBuffer[i] = currUpper;
         BullishStopBuffer[i] = EMPTY_VALUE;
      }}
   }}

   return(rates_total);
}}
"""


if __name__ == "__main__":
    print("Testing Volatility Stop Python calculation...")
    # Generate dummy test data
    dates = pd.date_range("2026-01-01", periods=50, freq="D")
    prices = 100.0 + np.cumsum(np.random.randn(50))
    sample_df = pd.DataFrame({
        "open": prices,
        "high": prices + 1.0,
        "low": prices - 1.0,
        "close": prices + 0.2
    }, index=dates)

    vstop_df = calculate_volatility_stop(sample_df, period=10, multiplier=2.0)
    print("Calculation successful!")
    print(vstop_df[['close', 'vstop', 'is_bullish', 'trend_flip']].tail(5))

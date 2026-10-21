#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

// This namespace holds indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
    /// <summary>
    /// MK Hidden & Regular Divergence Indicator for NinjaTrader 8.
    /// Direct, faithful 1-to-1 conversion from the Python engine (MK HD Test 10 Stocks.py).
    /// Detects:
    ///   1. Hidden Bearish (Price LH + Stoch HH) -> Trend Continuation Down
    ///   2. Hidden Bullish (Price HL + Stoch LL) -> Trend Continuation Up
    ///   3. Regular Bearish (Price HH + Stoch LH) -> Reversal Down
    ///   4. Regular Bullish (Price LL + Stoch HL) -> Reversal Up
    /// Strictly connects immediately adjacent crests and troughs (no skipping).
    /// Fully autonomous, self-calculating, and crash-proof.
    /// </summary>
    public class MKHiddenDivergence : Indicator
    {
        private Series<double> fastKSeries;
        private Series<double> stochKSeries;
        private Series<double> stochDSeries;
        private SimpleFont textFont;

        private Indicator targetStochIndicator = null;
        private int targetStochPanel = 1;

        // Last confirmed swing points (strictly consecutive swing tracking)
        private int lastPeakStochBar = -1;
        private double lastPeakStochVal = 0.0;
        private int lastPeakPriceBar = -1;
        private double lastPeakPriceVal = 0.0;

        private int lastTroughStochBar = -1;
        private double lastTroughStochVal = 0.0;
        private int lastTroughPriceBar = -1;
        private double lastTroughPriceVal = 0.0;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description                 = "Stochastic Hidden & Regular Divergence Detector converted directly from Python.";
                Name                        = "MKHiddenDivergence";
                Calculate                   = Calculate.OnBarClose;
                IsOverlay                   = true;
                IsAutoScale                 = false;
                DisplayInDataBox            = true;
                DrawOnPricePanel            = true;
                PaintPriceMarkers           = false;
                ScaleJustification          = NinjaTrader.Gui.Chart.ScaleJustification.Right;
                IsSuspendedWhileInactive    = true;

                // 1. Stochastic Parameters (Python defaults: 10, 10, 3)
                AutoMatchChartStochastics   = true; // Automatically sync with Stochastics on chart
                ChartStochasticsIndex       = 1;    // 1 = First Stochastics panel on chart
                StochPanelIndex             = 0;    // 0 = Auto-detect panel with Stochastics (or set 1, 2, etc.)
                PeriodK                     = 10;
                Smooth                      = 10;
                PeriodD                     = 3;

                // 2. Swing & Price Bar Settings (Python defaults)
                PriceBarWindow              = 4;   // Python: [i - 4, i + 4] localized price extreme window
                SwingLookback               = 3;   // Python: lookback = 3
                SwingLookahead              = 3;   // Python: lookahead = 3
                MinBarsBetween              = 4;   // Python: min_bar_dist = 4
                MaxBarsBetween              = 45;  // Python: max_bar_dist = 45
                MinStochDiff                = 2.0; // Python: min_stoch_diff = 2.0
                MinPriceDiffTicks           = 1;   // Minimum price difference in ticks
                PeakMinStoch                = 45.0;// Python: ki > 45.0
                TroughMaxStoch              = 55.0;// Python: ki < 55.0

                // 3. Signal Toggles (Both Hidden and Regular Divergences Enabled by default)
                ShowHiddenBearish           = true;
                ShowHiddenBullish           = true;
                ShowRegularBearish          = true;
                ShowRegularBullish          = true;

                // 4. Visual Elements - Paired Trendlines on Price and Stochastics
                DrawLines                   = true; // Line #1 on Price Chart
                DrawStochLines              = true; // Line #2 on Stochastics Panel
                DrawLabels                  = true;
                DrawArrows                  = true;
                LineWidth                   = 2;

                // Default Hidden Divergence lines to Cyan (matching chart template)
                HiddenBearBrush             = Brushes.Cyan;
                HiddenBullBrush             = Brushes.Cyan;
                RegularBearBrush            = Brushes.DarkOrange;
                RegularBullBrush            = Brushes.LimeGreen;

                // 5. Alerts
                EnableAlerts                = true;
            }
            else if (State == State.Configure)
            {
            }
            else if (State == State.DataLoaded)
            {
                fastKSeries = new Series<double>(this, MaximumBarsLookBack.Infinite);
                stochKSeries = new Series<double>(this, MaximumBarsLookBack.Infinite);
                stochDSeries = new Series<double>(this, MaximumBarsLookBack.Infinite);

                textFont = new SimpleFont("Arial", 10) { Bold = true };

                lastPeakStochBar        = -1;
                lastPeakStochVal        = 0.0;
                lastPeakPriceBar        = -1;
                lastPeakPriceVal        = 0.0;

                lastTroughStochBar      = -1;
                lastTroughStochVal      = 0.0;
                lastTroughPriceBar      = -1;
                lastTroughPriceVal      = 0.0;

                targetStochIndicator    = null;
                targetStochPanel        = 1;
                UpdateTargetStochastics();
            }
        }

        private Indicator GetTargetStochIndicator()
        {
            if (targetStochIndicator == null)
                UpdateTargetStochastics();
            return targetStochIndicator;
        }

        private void UpdateTargetStochastics()
        {
            if (ChartControl == null || ChartControl.Indicators == null)
                return;

            try
            {
                int stochCount = 0;
                int targetIdx = Math.Max(1, ChartStochasticsIndex);

                foreach (var ind in ChartControl.Indicators)
                {
                    if (ind != this && ind.Panel > 0 && ind.GetType().Name.IndexOf("Stoch", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        stochCount++;
                        if (stochCount == targetIdx || (StochPanelIndex > 0 && ind.Panel == StochPanelIndex))
                        {
                            targetStochIndicator = ind as Indicator;
                            targetStochPanel = ind.Panel;

                            if (AutoMatchChartStochastics)
                            {
                                var t = ind.GetType();
                                var pk = t.GetProperty("PeriodK");
                                var sm = t.GetProperty("Smooth");
                                var pd = t.GetProperty("PeriodD");

                                if (pk != null)
                                {
                                    object val = pk.GetValue(ind, null);
                                    if (val is int ik && ik > 0) PeriodK = ik;
                                }
                                if (sm != null)
                                {
                                    object val = sm.GetValue(ind, null);
                                    if (val is int ism && ism > 0) Smooth = ism;
                                }
                                if (pd != null)
                                {
                                    object val = pd.GetValue(ind, null);
                                    if (val is int ipd && ipd > 0) PeriodD = ipd;
                                }
                            }
                            return;
                        }
                    }
                }
            }
            catch
            {
                // Fallback safely
            }
        }

        private int GetTargetStochPanel()
        {
            if (StochPanelIndex > 0)
                return StochPanelIndex;

            var ind = GetTargetStochIndicator();
            if (ind != null && ind.Panel > 0)
                return ind.Panel;

            return targetStochPanel > 0 ? targetStochPanel : 1;
        }

        private double GetStochValue(int barsAgo)
        {
            if (stochKSeries != null && barsAgo >= 0 && barsAgo < stochKSeries.Count)
                return stochKSeries[barsAgo];
            return 50.0;
        }

        protected override void OnBarUpdate()
        {
            try
            {
                // -------------------------------------------------------------
                // 1. Calculate Stochastics Series on Active Chart Bars
                //    (matching compute_indicators in Python)
                // -------------------------------------------------------------
                int kLookback = Math.Min(CurrentBar + 1, PeriodK);
                double minLow = Low[0];
                double maxHigh = High[0];

                for (int i = 1; i < kLookback; i++)
                {
                    if (Low[i] < minLow) minLow = Low[i];
                    if (High[i] > maxHigh) maxHigh = High[i];
                }

                double range = maxHigh - minLow;
                double rawK = (range > 0.00000001)
                    ? Math.Min(100.0, Math.Max(0.0, 100.0 * (Close[0] - minLow) / range))
                    : (CurrentBar == 0 ? 50.0 : fastKSeries[1]);

                fastKSeries[0] = rawK;

                // Slow %K = SMA(fastK, Smooth)
                if (Smooth <= 1)
                {
                    stochKSeries[0] = rawK;
                }
                else
                {
                    int sCount = Math.Min(CurrentBar + 1, Smooth);
                    double sum = 0.0;
                    for (int i = 0; i < sCount; i++)
                        sum += fastKSeries[i];
                    stochKSeries[0] = sum / sCount;
                }

                // %D = SMA(stochK, PeriodD)
                if (PeriodD <= 1)
                {
                    stochDSeries[0] = stochKSeries[0];
                }
                else
                {
                    int dCount = Math.Min(CurrentBar + 1, PeriodD);
                    double sum = 0.0;
                    for (int i = 0; i < dCount; i++)
                        sum += stochKSeries[i];
                    stochDSeries[0] = sum / dCount;
                }

                // Defensive check: ensure sufficient bars loaded for lookback + lookahead
                int minRequired = PeriodK + Smooth + PeriodD + SwingLookback + SwingLookahead + PriceBarWindow + 2;
                if (CurrentBar < minRequired)
                    return;

                // -------------------------------------------------------------
                // 2. Swing & Divergence Detection (Direct Python 1-to-1 Conversion)
                //    Evaluates candidate swing bar located 'SwingLookahead' bars ago (bar 'i' in Python)
                // -------------------------------------------------------------
                int swingBarAgo = SwingLookahead;
                double ki = GetStochValue(swingBarAgo);
                double minPriceDelta = Math.Max(TickSize, MinPriceDiffTicks * TickSize);
                int targetPanel = GetTargetStochPanel();

                // -------------------------------------------------------------
                // A. EVALUATE PEAKS (Python: ki > 45.0) -> Bearish Divergence
                // -------------------------------------------------------------
                if (ki > PeakMinStoch)
                {
                    bool isPeak = true;

                    // Python: all(ki > bars[i - j]['k'] for j in range(1, lookback + 1))
                    for (int j = 1; j <= SwingLookback; j++)
                    {
                        if (ki <= GetStochValue(swingBarAgo + j))
                        {
                            isPeak = false;
                            break;
                        }
                    }

                    // Python: all(ki >= bars[i + j]['k'] for j in range(1, lookahead + 1))
                    if (isPeak)
                    {
                        for (int j = 1; j <= SwingLookahead; j++)
                        {
                            if (ki < GetStochValue(swingBarAgo - j))
                            {
                                isPeak = false;
                                break;
                            }
                        }
                    }

                    if (isPeak)
                    {
                        // Python: localized price extreme search within [i - PriceBarWindow, i + PriceBarWindow]
                        int bestPriceBarAgo = swingBarAgo;
                        double bestH = High[swingBarAgo];
                        for (int w = swingBarAgo - PriceBarWindow; w <= swingBarAgo + PriceBarWindow; w++)
                        {
                            if (w >= 0 && w <= CurrentBar)
                            {
                                if (High[w] > bestH)
                                {
                                    bestH = High[w];
                                    bestPriceBarAgo = w;
                                }
                            }
                        }

                        int currStochBar = CurrentBar - swingBarAgo;
                        int currPriceBar = CurrentBar - bestPriceBarAgo;

                        // Evaluate divergence strictly against the immediately preceding peak (no skipping)
                        if (lastPeakStochBar >= 0)
                        {
                            int s_dist = currStochBar - lastPeakStochBar;
                            int p_dist = Math.Abs(currPriceBar - lastPeakPriceBar);

                            // Python: if min_bar_dist <= s_dist <= max_bar_dist and p_dist >= 4:
                            if (s_dist >= MinBarsBetween && s_dist <= MaxBarsBetween && p_dist >= MinBarsBetween)
                            {
                                double p_delta = bestH - lastPeakPriceVal;
                                double s_delta = ki - lastPeakStochVal;

                                int prevPriceBarsAgo = CurrentBar - lastPeakPriceBar;
                                int currPriceBarsAgo = bestPriceBarAgo;
                                int prevStochBarsAgo = CurrentBar - lastPeakStochBar;
                                int currStochBarsAgo = swingBarAgo;

                                if (prevPriceBarsAgo > currPriceBarsAgo && currPriceBarsAgo >= 0 && prevPriceBarsAgo <= CurrentBar &&
                                    prevStochBarsAgo > currStochBarsAgo && currStochBarsAgo >= 0 && prevStochBarsAgo <= CurrentBar)
                                {
                                    DateTime priceStartTime = Time[prevPriceBarsAgo];
                                    DateTime priceEndTime   = Time[currPriceBarsAgo];
                                    if (priceEndTime <= priceStartTime) priceEndTime = priceStartTime.AddMilliseconds(1);

                                    DateTime stochStartTime = Time[prevStochBarsAgo];
                                    DateTime stochEndTime   = Time[currStochBarsAgo];
                                    if (stochEndTime <= stochStartTime) stochEndTime = stochStartTime.AddMilliseconds(1);

                                    string idStr = string.Format("Bear_{0}_{1}", lastPeakStochBar, currStochBar);

                                    // 1. Hidden Bearish (Price LH + Stoch HH) -> Trend Continuation Down
                                    if (ShowHiddenBearish && p_delta < -minPriceDelta && s_delta > MinStochDiff)
                                    {
                                        if (DrawLines)
                                        {
                                            Draw.Line(this, "Line_" + idStr, false, priceStartTime, lastPeakPriceVal, priceEndTime, bestH, HiddenBearBrush ?? Brushes.Cyan, DashStyleHelper.Solid, LineWidth, true);
                                        }

                                         if (DrawStochLines && targetPanel > 0)
                                        {
                                            NinjaScriptBase stochOwner = (NinjaScriptBase)GetTargetStochIndicator() ?? this;
                                            var sLine = Draw.Line(stochOwner, "StochLine_" + idStr, false, stochStartTime, lastPeakStochVal, stochEndTime, ki, HiddenBearBrush ?? Brushes.Cyan, DashStyleHelper.Solid, LineWidth, false);
                                            if (sLine != null)
                                            {
                                                sLine.PanelIndex = targetPanel;
                                                if (ChartControl != null && targetPanel >= 0 && targetPanel < ChartControl.ChartPanels.Count)
                                                    sLine.ChartPanel = ChartControl.ChartPanels[targetPanel];
                                            }
                                        }

                                        if (DrawArrows)
                                        {
                                            Draw.ArrowDown(this, "Arr_" + idStr, false, priceEndTime, bestH + (TickSize * 4), HiddenBearBrush ?? Brushes.Cyan, true);
                                        }

                                        if (DrawLabels)
                                        {
                                            Draw.Text(this, "Txt_" + idStr, false, "HD Bear", priceEndTime, bestH + (TickSize * 9), 0, HiddenBearBrush ?? Brushes.Cyan, textFont, TextAlignment.Center, Brushes.Transparent, Brushes.Transparent, 0);
                                        }

                                        if (EnableAlerts && State == State.Realtime)
                                        {
                                            Alert("Alert_" + idStr, Priority.High, "MK Hidden Bearish Divergence on " + Instrument.FullName, "Alert1.wav", 10, Brushes.Black, Brushes.Red);
                                        }
                                    }
                                    // 2. Regular Bearish (Price HH + Stoch LH) -> Reversal Down
                                    else if (ShowRegularBearish && p_delta > minPriceDelta && s_delta < -MinStochDiff)
                                    {
                                        if (DrawLines)
                                        {
                                            Draw.Line(this, "Line_" + idStr, false, priceStartTime, lastPeakPriceVal, priceEndTime, bestH, RegularBearBrush ?? Brushes.DarkOrange, DashStyleHelper.Dash, LineWidth, true);
                                        }

                                        if (DrawStochLines && targetPanel > 0)
                                        {
                                            NinjaScriptBase stochOwner = (NinjaScriptBase)GetTargetStochIndicator() ?? this;
                                            var sLine = Draw.Line(stochOwner, "StochLine_" + idStr, false, stochStartTime, lastPeakStochVal, stochEndTime, ki, RegularBearBrush ?? Brushes.DarkOrange, DashStyleHelper.Dash, LineWidth, false);
                                            if (sLine != null)
                                            {
                                                sLine.PanelIndex = targetPanel;
                                                if (ChartControl != null && targetPanel >= 0 && targetPanel < ChartControl.ChartPanels.Count)
                                                    sLine.ChartPanel = ChartControl.ChartPanels[targetPanel];
                                            }
                                        }

                                        if (DrawArrows)
                                        {
                                            Draw.ArrowDown(this, "Arr_" + idStr, false, priceEndTime, bestH + (TickSize * 4), RegularBearBrush ?? Brushes.DarkOrange, true);
                                        }

                                        if (DrawLabels)
                                        {
                                            Draw.Text(this, "Txt_" + idStr, false, "Reg Bear", priceEndTime, bestH + (TickSize * 9), 0, RegularBearBrush ?? Brushes.DarkOrange, textFont, TextAlignment.Center, Brushes.Transparent, Brushes.Transparent, 0);
                                        }
                                    }
                                }
                            }
                        }

                        // ALWAYS advance peak anchor to current peak - strictly consecutive, no skipping!
                        lastPeakStochBar  = currStochBar;
                        lastPeakStochVal  = ki;
                        lastPeakPriceBar  = currPriceBar;
                        lastPeakPriceVal  = bestH;
                    }
                }

                // -------------------------------------------------------------
                // B. EVALUATE TROUGHS (Python: ki < 55.0) -> Bullish Divergence
                // -------------------------------------------------------------
                if (ki < TroughMaxStoch)
                {
                    bool isTrough = true;

                    // Python: all(ki < bars[i - j]['k'] for j in range(1, lookback + 1))
                    for (int j = 1; j <= SwingLookback; j++)
                    {
                        if (ki >= GetStochValue(swingBarAgo + j))
                        {
                            isTrough = false;
                            break;
                        }
                    }

                    // Python: all(ki <= bars[i + j]['k'] for j in range(1, lookahead + 1))
                    if (isTrough)
                    {
                        for (int j = 1; j <= SwingLookahead; j++)
                        {
                            if (ki > GetStochValue(swingBarAgo - j))
                            {
                                isTrough = false;
                                break;
                            }
                        }
                    }

                    if (isTrough)
                    {
                        // Python: localized price extreme search within [i - PriceBarWindow, i + PriceBarWindow]
                        int bestPriceBarAgo = swingBarAgo;
                        double bestL = Low[swingBarAgo];
                        for (int w = swingBarAgo - PriceBarWindow; w <= swingBarAgo + PriceBarWindow; w++)
                        {
                            if (w >= 0 && w <= CurrentBar)
                            {
                                if (Low[w] < bestL)
                                {
                                    bestL = Low[w];
                                    bestPriceBarAgo = w;
                                }
                            }
                        }

                        int currStochBar = CurrentBar - swingBarAgo;
                        int currPriceBar = CurrentBar - bestPriceBarAgo;

                        // Evaluate divergence strictly against the immediately preceding trough (no skipping)
                        if (lastTroughStochBar >= 0)
                        {
                            int s_dist = currStochBar - lastTroughStochBar;
                            int p_dist = Math.Abs(currPriceBar - lastTroughPriceBar);

                            // Python: if min_bar_dist <= s_dist <= max_bar_dist and p_dist >= 4:
                            if (s_dist >= MinBarsBetween && s_dist <= MaxBarsBetween && p_dist >= MinBarsBetween)
                            {
                                double p_delta = bestL - lastTroughPriceVal;
                                double s_delta = ki - lastTroughStochVal;

                                int prevPriceBarsAgo = CurrentBar - lastTroughPriceBar;
                                int currPriceBarsAgo = bestPriceBarAgo;
                                int prevStochBarsAgo = CurrentBar - lastTroughStochBar;
                                int currStochBarsAgo = swingBarAgo;

                                if (prevPriceBarsAgo > currPriceBarsAgo && currPriceBarsAgo >= 0 && prevPriceBarsAgo <= CurrentBar &&
                                    prevStochBarsAgo > currStochBarsAgo && currStochBarsAgo >= 0 && prevStochBarsAgo <= CurrentBar)
                                {
                                    DateTime priceStartTime = Time[prevPriceBarsAgo];
                                    DateTime priceEndTime   = Time[currPriceBarsAgo];
                                    if (priceEndTime <= priceStartTime) priceEndTime = priceStartTime.AddMilliseconds(1);

                                    DateTime stochStartTime = Time[prevStochBarsAgo];
                                    DateTime stochEndTime   = Time[currStochBarsAgo];
                                    if (stochEndTime <= stochStartTime) stochEndTime = stochStartTime.AddMilliseconds(1);

                                    string idStr = string.Format("Bull_{0}_{1}", lastTroughStochBar, currStochBar);

                                    // 1. Hidden Bullish (Price HL + Stoch LL) -> Trend Continuation Up
                                    if (ShowHiddenBullish && p_delta > minPriceDelta && s_delta < -MinStochDiff)
                                    {
                                        if (DrawLines)
                                        {
                                            Draw.Line(this, "Line_" + idStr, false, priceStartTime, lastTroughPriceVal, priceEndTime, bestL, HiddenBullBrush ?? Brushes.Cyan, DashStyleHelper.Solid, LineWidth, true);
                                        }

                                         if (DrawStochLines && targetPanel > 0)
                                        {
                                            NinjaScriptBase stochOwner = (NinjaScriptBase)GetTargetStochIndicator() ?? this;
                                            var sLine = Draw.Line(stochOwner, "StochLine_" + idStr, false, stochStartTime, lastTroughStochVal, stochEndTime, ki, HiddenBullBrush ?? Brushes.Cyan, DashStyleHelper.Solid, LineWidth, false);
                                            if (sLine != null)
                                            {
                                                sLine.PanelIndex = targetPanel;
                                                if (ChartControl != null && targetPanel >= 0 && targetPanel < ChartControl.ChartPanels.Count)
                                                    sLine.ChartPanel = ChartControl.ChartPanels[targetPanel];
                                            }
                                        }

                                        if (DrawArrows)
                                        {
                                            Draw.ArrowUp(this, "Arr_" + idStr, false, priceEndTime, bestL - (TickSize * 4), HiddenBullBrush ?? Brushes.Cyan, true);
                                        }

                                        if (DrawLabels)
                                        {
                                            Draw.Text(this, "Txt_" + idStr, false, "HD Bull", priceEndTime, bestL - (TickSize * 9), 0, HiddenBullBrush ?? Brushes.Cyan, textFont, TextAlignment.Center, Brushes.Transparent, Brushes.Transparent, 0);
                                        }

                                        if (EnableAlerts && State == State.Realtime)
                                        {
                                            Alert("Alert_" + idStr, Priority.High, "MK Hidden Bullish Divergence on " + Instrument.FullName, "Alert1.wav", 10, Brushes.Black, Brushes.Green);
                                        }
                                    }
                                    // 2. Regular Bullish (Price LL + Stoch HL) -> Reversal Up
                                    else if (ShowRegularBullish && p_delta < -minPriceDelta && s_delta > MinStochDiff)
                                    {
                                        if (DrawLines)
                                        {
                                            Draw.Line(this, "Line_" + idStr, false, priceStartTime, lastTroughPriceVal, priceEndTime, bestL, RegularBullBrush ?? Brushes.LimeGreen, DashStyleHelper.Dash, LineWidth, true);
                                        }

                                        if (DrawStochLines && targetPanel > 0)
                                        {
                                            NinjaScriptBase stochOwner = (NinjaScriptBase)GetTargetStochIndicator() ?? this;
                                            var sLine = Draw.Line(stochOwner, "StochLine_" + idStr, false, stochStartTime, lastTroughStochVal, stochEndTime, ki, RegularBullBrush ?? Brushes.LimeGreen, DashStyleHelper.Dash, LineWidth, false);
                                            if (sLine != null)
                                            {
                                                sLine.PanelIndex = targetPanel;
                                                if (ChartControl != null && targetPanel >= 0 && targetPanel < ChartControl.ChartPanels.Count)
                                                    sLine.ChartPanel = ChartControl.ChartPanels[targetPanel];
                                            }
                                        }

                                        if (DrawArrows)
                                        {
                                            Draw.ArrowUp(this, "Arr_" + idStr, false, priceEndTime, bestL - (TickSize * 4), RegularBullBrush ?? Brushes.LimeGreen, true);
                                        }

                                        if (DrawLabels)
                                        {
                                            Draw.Text(this, "Txt_" + idStr, false, "Reg Bull", priceEndTime, bestL - (TickSize * 9), 0, RegularBullBrush ?? Brushes.LimeGreen, textFont, TextAlignment.Center, Brushes.Transparent, Brushes.Transparent, 0);
                                        }
                                    }
                                }
                            }
                        }

                        // ALWAYS advance trough anchor to current trough - strictly consecutive, no skipping!
                        lastTroughStochBar  = currStochBar;
                        lastTroughStochVal  = ki;
                        lastTroughPriceBar  = currPriceBar;
                        lastTroughPriceVal  = bestL;
                    }
                }
            }
            catch (Exception)
            {
                // Never allow unhandled exceptions to break chart or indicator execution
            }
        }

        #region Properties
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> StochK => stochKSeries;

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> StochD => stochDSeries;

        public override string DisplayName
        {
            get
            {
                string periodStr = (BarsPeriod != null) ? BarsPeriod.ToString() : "Chart";
                return string.Format("{0}({1}, K:{2}, S:{3}, D:{4})", Name, periodStr, PeriodK, Smooth, PeriodD);
            }
        }

        [Display(Name = "Auto-Match Chart Stochastics", Description = "Automatically sync settings with Stochastics on the chart", Order = 1, GroupName = "1. Stochastic Settings")]
        public bool AutoMatchChartStochastics { get; set; }

        [Range(1, 10)]
        [Display(Name = "Chart Stochastics Index", Description = "Which Stochastics on chart to link (1 = First, 2 = Second)", Order = 2, GroupName = "1. Stochastic Settings")]
        public int ChartStochasticsIndex { get; set; }

        [Range(0, 10)]
        [Display(Name = "Stochastics Target Panel #", Description = "Chart sub-panel index where Stochastics is displayed (0 = Auto-detect). Set to 0 to auto-detect.", Order = 3, GroupName = "1. Stochastic Settings")]
        public int StochPanelIndex { get; set; }

        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(Name = "Period K (%K Lookback)", Description = "%K Lookback period", Order = 4, GroupName = "1. Stochastic Settings")]
        public int PeriodK { get; set; }

        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(Name = "Smooth K (%K Slowing)", Description = "%K Smoothing period", Order = 5, GroupName = "1. Stochastic Settings")]
        public int Smooth { get; set; }

        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(Name = "Period D (%D Moving Average)", Description = "%D Smoothing period", Order = 6, GroupName = "1. Stochastic Settings")]
        public int PeriodD { get; set; }

        // -------------------------------------------------------------
        // Group 2: Swing & Price Bar Settings (Direct Python Parameters)
        // -------------------------------------------------------------
        [Range(1, 50)]
        [Display(Name = "Price Bar Search Window", Description = "Bars left and right (+/-) around Stochastic swing to locate candle extreme (Python default = 4)", Order = 1, GroupName = "2. Swing & Price Bar Settings")]
        public int PriceBarWindow { get; set; }

        [Range(1, 20)]
        [Display(Name = "Swing Lookback", Description = "Bars left to confirm wave peak/trough (Python default = 3)", Order = 2, GroupName = "2. Swing & Price Bar Settings")]
        public int SwingLookback { get; set; }

        [Range(1, 20)]
        [Display(Name = "Swing Lookahead", Description = "Bars right to confirm wave peak/trough (Python default = 3)", Order = 3, GroupName = "2. Swing & Price Bar Settings")]
        public int SwingLookahead { get; set; }

        [Range(1, 50)]
        [Display(Name = "Min Bars Between Swings", Description = "Minimum bar distance between paired swings (Python default = 4)", Order = 4, GroupName = "2. Swing & Price Bar Settings")]
        public int MinBarsBetween { get; set; }

        [Range(5, 300)]
        [Display(Name = "Max Bars Between Swings", Description = "Maximum bar distance between paired swings (Python default = 45)", Order = 5, GroupName = "2. Swing & Price Bar Settings")]
        public int MaxBarsBetween { get; set; }

        [Range(0.1, 50.0)]
        [Display(Name = "Min Stochastic Delta", Description = "Minimum %K difference required for divergence (Python default = 2.0)", Order = 6, GroupName = "2. Swing & Price Bar Settings")]
        public double MinStochDiff { get; set; }

        [Range(0, 50)]
        [Display(Name = "Min Price Difference (Ticks)", Description = "Minimum price delta in ticks (Default = 1 tick)", Order = 7, GroupName = "2. Swing & Price Bar Settings")]
        public int MinPriceDiffTicks { get; set; }

        [Range(10.0, 90.0)]
        [Display(Name = "Peak Min Stochastic Level", Description = "Stochastic must be above this value to qualify as a peak (Python default = 45.0)", Order = 8, GroupName = "2. Swing & Price Bar Settings")]
        public double PeakMinStoch { get; set; }

        [Range(10.0, 90.0)]
        [Display(Name = "Trough Max Stochastic Level", Description = "Stochastic must be below this value to qualify as a trough (Python default = 55.0)", Order = 9, GroupName = "2. Swing & Price Bar Settings")]
        public double TroughMaxStoch { get; set; }

        // -------------------------------------------------------------
        // Group 3: Signal Toggles
        // -------------------------------------------------------------
        [Display(Name = "Show Hidden Bearish (LH vs HH)", Description = "Plot Hidden Bearish Divergence lines (Trend Continuation Down)", Order = 1, GroupName = "3. Signal Toggles")]
        public bool ShowHiddenBearish { get; set; }

        [Display(Name = "Show Hidden Bullish (HL vs LL)", Description = "Plot Hidden Bullish Divergence lines (Trend Continuation Up)", Order = 2, GroupName = "3. Signal Toggles")]
        public bool ShowHiddenBullish { get; set; }

        [Display(Name = "Show Regular Bearish (HH vs LH)", Description = "Plot Regular Bearish Reversal lines", Order = 3, GroupName = "3. Signal Toggles")]
        public bool ShowRegularBearish { get; set; }

        [Display(Name = "Show Regular Bullish (LL vs HL)", Description = "Plot Regular Bullish Reversal lines", Order = 4, GroupName = "3. Signal Toggles")]
        public bool ShowRegularBullish { get; set; }

        // -------------------------------------------------------------
        // Group 4: Visuals
        // -------------------------------------------------------------
        [Display(Name = "Draw Trendlines on Price Chart", Description = "Draw lines connecting price swing highs and lows", Order = 1, GroupName = "4. Visuals")]
        public bool DrawLines { get; set; }

        [Display(Name = "Draw Trendlines on Stochastics Panel", Description = "Draw paired divergence lines touching the oscillator crests and troughs", Order = 2, GroupName = "4. Visuals")]
        public bool DrawStochLines { get; set; }

        [Display(Name = "Draw Text Labels", Description = "Draw text tags (e.g. HD Bear) on the chart", Order = 3, GroupName = "4. Visuals")]
        public bool DrawLabels { get; set; }

        [Display(Name = "Draw Arrows", Description = "Draw directional entry arrows on the chart", Order = 4, GroupName = "4. Visuals")]
        public bool DrawArrows { get; set; }

        [Range(1, 10)]
        [Display(Name = "Trendline Width", Description = "Line thickness in pixels", Order = 5, GroupName = "4. Visuals")]
        public int LineWidth { get; set; }

        [XmlIgnore]
        [Display(Name = "Hidden Bearish Color", Description = "Color for Hidden Bearish lines and arrows (Default Cyan)", Order = 6, GroupName = "4. Visuals")]
        public Brush HiddenBearBrush { get; set; }

        [Browsable(false)]
        public string HiddenBearBrushSerializable
        {
            get { return Serialize.BrushToString(HiddenBearBrush); }
            set { HiddenBearBrush = Serialize.StringToBrush(value); }
        }

        [XmlIgnore]
        [Display(Name = "Hidden Bullish Color", Description = "Color for Hidden Bullish lines and arrows (Default Cyan)", Order = 7, GroupName = "4. Visuals")]
        public Brush HiddenBullBrush { get; set; }

        [Browsable(false)]
        public string HiddenBullBrushSerializable
        {
            get { return Serialize.BrushToString(HiddenBullBrush); }
            set { HiddenBullBrush = Serialize.StringToBrush(value); }
        }

        [XmlIgnore]
        [Display(Name = "Regular Bearish Color", Description = "Color for Regular Bearish lines and arrows", Order = 8, GroupName = "4. Visuals")]
        public Brush RegularBearBrush { get; set; }

        [Browsable(false)]
        public string RegularBearBrushSerializable
        {
            get { return Serialize.BrushToString(RegularBearBrush); }
            set { RegularBearBrush = Serialize.StringToBrush(value); }
        }

        [XmlIgnore]
        [Display(Name = "Regular Bullish Color", Description = "Color for Regular Bullish lines and arrows", Order = 9, GroupName = "4. Visuals")]
        public Brush RegularBullBrush { get; set; }

        [Browsable(false)]
        public string RegularBullBrushSerializable
        {
            get { return Serialize.BrushToString(RegularBullBrush); }
            set { RegularBullBrush = Serialize.StringToBrush(value); }
        }

        // -------------------------------------------------------------
        // Group 5: Alerts
        // -------------------------------------------------------------
        [Display(Name = "Enable Audio Alerts", Description = "Play sound when setup is confirmed in real-time", Order = 1, GroupName = "5. Alerts")]
        public bool EnableAlerts { get; set; }
        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private MKHiddenDivergence[] cacheMKHiddenDivergence;
		public MKHiddenDivergence MKHiddenDivergence(int periodK, int smooth, int periodD)
		{
			return MKHiddenDivergence(Input, periodK, smooth, periodD);
		}

		public MKHiddenDivergence MKHiddenDivergence(ISeries<double> input, int periodK, int smooth, int periodD)
		{
			if (cacheMKHiddenDivergence != null)
				for (int idx = 0; idx < cacheMKHiddenDivergence.Length; idx++)
					if (cacheMKHiddenDivergence[idx] != null && cacheMKHiddenDivergence[idx].PeriodK == periodK && cacheMKHiddenDivergence[idx].Smooth == smooth && cacheMKHiddenDivergence[idx].PeriodD == periodD && cacheMKHiddenDivergence[idx].EqualsInput(input))
						return cacheMKHiddenDivergence[idx];
			return CacheIndicator<MKHiddenDivergence>(new MKHiddenDivergence(){ PeriodK = periodK, Smooth = smooth, PeriodD = periodD }, input, ref cacheMKHiddenDivergence);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.MKHiddenDivergence MKHiddenDivergence(int periodK, int smooth, int periodD)
		{
			return indicator.MKHiddenDivergence(Input, periodK, smooth, periodD);
		}

		public Indicators.MKHiddenDivergence MKHiddenDivergence(ISeries<double> input , int periodK, int smooth, int periodD)
		{
			return indicator.MKHiddenDivergence(input, periodK, smooth, periodD);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.MKHiddenDivergence MKHiddenDivergence(int periodK, int smooth, int periodD)
		{
			return indicator.MKHiddenDivergence(Input, periodK, smooth, periodD);
		}

		public Indicators.MKHiddenDivergence MKHiddenDivergence(ISeries<double> input , int periodK, int smooth, int periodD)
		{
			return indicator.MKHiddenDivergence(input, periodK, smooth, periodD);
		}
	}
}

#endregion

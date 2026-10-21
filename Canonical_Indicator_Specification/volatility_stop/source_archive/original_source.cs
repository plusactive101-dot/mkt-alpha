using System;
using System.Collections.Generic;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;

namespace cAlgo
{
    /// <summary>
    /// MK FVG + Volatility Stop Combined Indicator
    /// Combines:
    ///   1. Volatility Stop (ATR Trailing Trend Stop with Bullish & Bearish Discontinuous Plots)
    ///   2. Dynamic Structural Swing Detection (Method A: Indicator Flip):
    ///      - Swing Low (Cyan/Aqua family):
    ///          * 1st Low (First / Lower Low) -> SMALLER, LIGHTER Circle (#87CEFA LightSkyBlue, Thickness = 4).
    ///          * 2nd Low (Higher Low - HL)   -> LARGER, BRIGHTER Circle (#00FFFF Aqua, Thickness = 9) + Highlighted Aqua Up-Arrow.
    ///      - Swing High (Gold/Warm family - distinct from Swing Lows):
    ///          * 1st High (First / Higher High) -> SMALLER, LIGHTER Circle (#EEE8AA PaleGoldenrod, Thickness = 4).
    ///          * 2nd High (Lower High - LH)     -> LARGER, BRIGHTER Circle (#FFD700 Gold, Thickness = 9) + Highlighted Gold Down-Arrow.
    ///   3. Fair Value Gap (FVG) 3-Candle Imbalance Detection, Mitigation Tracking & Dynamic Box Extension
    ///   4. Pullback Arrow Signals near Volatility Stop Trend Flips:
    ///      - Uptrend (Red/Orange 1 -> Blue 2 -> Red/Orange 3):
    ///          * Candle price in Blue 2 cannot be lower than candle price in Red/Orange 1.
    ///          * Bar 1 (Up) -> Bar 2 (Smaller Down, Low[2] >= Low[1]) -> Exceeded by Bar 3, 4, or 5.
    ///      - Downtrend (Blue 1 -> Red/Orange 2 -> Blue 3):
    ///          * Candle price in Red/Orange 2 cannot be higher than candle price in Blue 1.
    ///          * Bar 1 (Down) -> Bar 2 (Smaller Up, High[2] <= High[1]) -> Dropped below by Bar 3, 4, or 5.
    /// </summary>
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class MK_FVG_VolatilityStop : Indicator
    {
        // =====================================================================
        // 1. VOLATILITY STOP PARAMETERS & OUTPUTS
        // =====================================================================
        [Parameter("Enable Volatility Stop", Group = "1. Volatility Stop", DefaultValue = true)]
        public bool EnableVStop { get; set; }

        [Parameter("ATR Period", Group = "1. Volatility Stop", DefaultValue = 20, MinValue = 1)]
        public int VStopPeriod { get; set; }

        [Parameter("ATR Multiplier", Group = "1. Volatility Stop", DefaultValue = 3.0, MinValue = 0.1, Step = 0.1)]
        public double VStopMultiplier { get; set; }

        [Parameter("ATR Moving Average Type", Group = "1. Volatility Stop", DefaultValue = MovingAverageType.Simple)]
        public MovingAverageType VStopMAType { get; set; }

        [Output("Bullish Stop", LineColor = "#FF8C00", PlotType = PlotType.DiscontinuousLine, LineStyle = LineStyle.Solid, Thickness = 2)]
        public IndicatorDataSeries BullishStop { get; set; }

        [Output("Bearish Stop", LineColor = "#1E90FF", PlotType = PlotType.DiscontinuousLine, LineStyle = LineStyle.Solid, Thickness = 2)]
        public IndicatorDataSeries BearishStop { get; set; }

        // Swing Low Circles (Small Lighter 1st Circle vs Large Brighter 2nd Circle)
        [Output("1st Swing Low (Small Lighter)", LineColor = "#87CEFA", PlotType = PlotType.Points, Thickness = 4)]
        public IndicatorDataSeries FirstSwingLowSeries { get; set; }

        [Output("2nd Swing Low - HL (Large Brighter)", LineColor = "#00FFFF", PlotType = PlotType.Points, Thickness = 9)]
        public IndicatorDataSeries SecondSwingLowSeries { get; set; }

        // Swing High Circles (Small Lighter 1st Circle vs Large Brighter 2nd Circle - Distinct Color Family)
        [Output("1st Swing High (Small Lighter)", LineColor = "#EEE8AA", PlotType = PlotType.Points, Thickness = 4)]
        public IndicatorDataSeries FirstSwingHighSeries { get; set; }

        [Output("2nd Swing High - LH (Large Brighter)", LineColor = "#FFD700", PlotType = PlotType.Points, Thickness = 9)]
        public IndicatorDataSeries SecondSwingHighSeries { get; set; }

        // =====================================================================
        // 2. DYNAMIC SWING DETECTION - METHOD A (INDICATOR FLIP)
        // =====================================================================
        [Parameter("Show Structural Swing Lows", Group = "2. Swings (Method A: Indicator Flip)", DefaultValue = true)]
        public bool ShowSwingLows { get; set; }

        [Parameter("Show Structural Swing Highs", Group = "2. Swings (Method A: Indicator Flip)", DefaultValue = true)]
        public bool ShowSwingHighs { get; set; }

        [Parameter("Higher Low (HL) Arrow Color", Group = "2. Swings (Method A: Indicator Flip)", DefaultValue = "Aqua")]
        public string HigherLowColorStr { get; set; }

        [Parameter("Lower High (LH) Arrow Color", Group = "2. Swings (Method A: Indicator Flip)", DefaultValue = "Gold")]
        public string LowerHighColorStr { get; set; }

        // =====================================================================
        // 3. FAIR VALUE GAP (FVG) LOGIC PARAMETERS
        // =====================================================================
        [Parameter("Enable Fair Value Gaps", Group = "3. Fair Value Gap (FVG)", DefaultValue = true)]
        public bool EnableFvg { get; set; }

        [Parameter("Show Bullish FVGs", Group = "3. Fair Value Gap (FVG)", DefaultValue = true)]
        public bool ShowBullishFvg { get; set; }

        [Parameter("Show Bearish FVGs", Group = "3. Fair Value Gap (FVG)", DefaultValue = true)]
        public bool ShowBearishFvg { get; set; }

        [Parameter("Hide Mitigated Gaps", Group = "3. Fair Value Gap (FVG)", DefaultValue = true)]
        public bool HideMitigated { get; set; }

        [Parameter("Min Gap Size (Pips)", Group = "3. Fair Value Gap (FVG)", DefaultValue = 0.0, MinValue = 0.0, Step = 0.1)]
        public double MinGapPips { get; set; }

        [Parameter("Max Active Gaps", Group = "3. Fair Value Gap (FVG)", DefaultValue = 5, MinValue = 1)]
        public int MaxActiveGaps { get; set; }

        [Parameter("Expire After X Bars (0 = Off)", Group = "3. Fair Value Gap (FVG)", DefaultValue = 200, MinValue = 0)]
        public int ExpireBars { get; set; }

        [Parameter("Filter FVGs by VStop Trend", Group = "3. Fair Value Gap (FVG)", DefaultValue = false)]
        public bool FilterFvgByTrend { get; set; }

        [Parameter("Bullish Color", Group = "3. Fair Value Gap (FVG)", DefaultValue = "LimeGreen")]
        public string BullishColorStr { get; set; }

        [Parameter("Bearish Color", Group = "3. Fair Value Gap (FVG)", DefaultValue = "Crimson")]
        public string BearishColorStr { get; set; }

        [Parameter("FVG Opacity (0-255)", Group = "3. Fair Value Gap (FVG)", DefaultValue = 70, MinValue = 0, MaxValue = 255)]
        public int Opacity { get; set; }

        // =====================================================================
        // 4. TREND CHANGE PULLBACK ARROWS (EDGE OF LINES)
        // =====================================================================
        [Parameter("Show Pullback Arrows", Group = "4. Trend Flip Pullback Arrows", DefaultValue = true)]
        public bool ShowPullbackArrows { get; set; }

        [Parameter("Max Bars Before/After Flip", Group = "4. Trend Flip Pullback Arrows", DefaultValue = 3, MinValue = 1, MaxValue = 10)]
        public int MaxBarsFromFlip { get; set; }

        [Parameter("Max Vertical Distance (ATR Mult)", Group = "4. Trend Flip Pullback Arrows", DefaultValue = 2.0, MinValue = 0.1, Step = 0.1)]
        public double MaxVerticalAtrDist { get; set; }

        [Parameter("Max Arrows Per Flip (1-2)", Group = "4. Trend Flip Pullback Arrows", DefaultValue = 1, MinValue = 1, MaxValue = 2)]
        public int MaxArrowsPerFlip { get; set; }

        [Parameter("Enable Audio Alerts", Group = "4. Trend Flip Pullback Arrows", DefaultValue = false)]
        public bool EnableAlerts { get; set; }

        // =====================================================================
        // INTERNAL STATE
        // =====================================================================
        private AverageTrueRange _atr;
        private Color _bullColor;
        private Color _bearColor;
        private Color _bullSolidColor;
        private Color _bearSolidColor;
        private Color _higherLowColor;
        private Color _lowerHighColor;

        private bool[] _vstopIsBullish;
        private double[] _vstopLower;
        private double[] _vstopUpper;

        private int _lastProcessedClosedBar = -1;
        private int _lastAlertBar = -1;

        // Method A (Indicator Flip) Swing Tracking State:
        private double _currBearSegMinLow = double.NaN;
        private int _currBearSegMinLowBar = -1;
        private double _currBearSegMaxHigh = double.NaN;
        private double _prevBearSegMaxHigh = double.NaN;

        private double _currBullSegMinLow = double.NaN;
        private double _currBullSegMaxHigh = double.NaN;
        private int _currBullSegMaxHighBar = -1;
        private double _prevBullSegMinLow = double.NaN;

        // Track previous confirmed Swing Low & Swing High to detect Higher Lows (HL) & Lower Highs (LH)
        private double _prevConfirmedSwingLow = double.NaN;
        private double _prevConfirmedSwingHigh = double.NaN;

        // Pullback arrow state
        private int _activeFlipBar = -1;
        private bool _activeFlipIsBullish;
        private bool _activeFlipSegmentValid;
        private double _activeFlipSegmentBoundPrice = double.NaN;
        private double _activeFlipPrevLineEdge;
        private double _activeFlipNewLineEdge;
        private int _activeFlipArrowsCount;
        private double _activeFlipFirstArrowPrice = double.NaN;
        private readonly HashSet<int> _drawnBar2Indices = new HashSet<int>();

        private class FvgZone
        {
            public string Id;
            public bool IsBullish;
            public int StartBarIndex;
            public int CreatedBarIndex;
            public int EndBarIndex;
            public double Top;
            public double Bottom;
        }

        private readonly List<FvgZone> _activeFvgZones = new List<FvgZone>();

        protected override void Initialize()
        {
            _atr = Indicators.AverageTrueRange(VStopPeriod, VStopMAType);

            _bullSolidColor = ParseColor(BullishColorStr, Color.LimeGreen);
            _bearSolidColor = ParseColor(BearishColorStr, Color.Crimson);
            _higherLowColor = ParseColor(HigherLowColorStr, Color.Aqua);
            _lowerHighColor = ParseColor(LowerHighColorStr, Color.Gold);

            int alpha = Math.Max(0, Math.Min(255, Opacity));
            _bullColor = Color.FromArgb(alpha, _bullSolidColor.R, _bullSolidColor.G, _bullSolidColor.B);
            _bearColor = Color.FromArgb(alpha, _bearSolidColor.R, _bearSolidColor.G, _bearSolidColor.B);

            int initialCapacity = Math.Max(Bars.Count + 1024, 4096);
            _vstopIsBullish = new bool[initialCapacity];
            _vstopLower = new double[initialCapacity];
            _vstopUpper = new double[initialCapacity];
        }

        public override void Calculate(int index)
        {
            EnsureCapacity(index);

            // 1. Volatility Stop Calculation
            CalculateVolatilityStop(index);

            // 2. Closed-Bar Processing
            int closedBar = index - 1;
            if (closedBar >= 2 && closedBar > _lastProcessedClosedBar)
            {
                if (closedBar == VStopPeriod)
                {
                    _currBullSegMinLow = Bars.LowPrices[closedBar];
                    _currBullSegMaxHigh = Bars.HighPrices[closedBar];
                    _currBullSegMaxHighBar = closedBar;
                }

                if (EnableFvg)
                {
                    ProcessClosedBarFvg(closedBar);
                }

                if (closedBar > VStopPeriod)
                {
                    ProcessMethodASwingAndPullbacks(closedBar);
                }

                _lastProcessedClosedBar = closedBar;
            }

            if (EnableFvg)
            {
                UpdateActiveFvgRectangles(index);
            }
        }

        // =====================================================================
        // VOLATILITY STOP ENGINE
        // =====================================================================
        private void CalculateVolatilityStop(int index)
        {
            double close0 = Bars.ClosePrices[index];
            double atr0 = _atr.Result[index];

            if (double.IsNaN(atr0) || atr0 <= 0)
                atr0 = Bars.HighPrices[index] - Bars.LowPrices[index];

            if (index <= VStopPeriod)
            {
                _vstopIsBullish[index] = true;
                _vstopLower[index] = close0 - VStopMultiplier * atr0;
                _vstopUpper[index] = close0 + VStopMultiplier * atr0;

                if (EnableVStop && index == VStopPeriod)
                {
                    BullishStop[index] = _vstopLower[index];
                    BearishStop[index] = double.NaN;
                }
                else
                {
                    BullishStop[index] = double.NaN;
                    BearishStop[index] = double.NaN;
                }
                return;
            }

            bool prevTrend = _vstopIsBullish[index - 1];
            double prevLower = _vstopLower[index - 1];
            double prevUpper = _vstopUpper[index - 1];
            double atr1 = _atr.Result[index - 1];
            if (double.IsNaN(atr1))
                atr1 = atr0;

            bool currTrend = prevTrend;
            double currLower = prevLower;
            double currUpper = prevUpper;

            if (!prevTrend && close0 > prevUpper)
            {
                currTrend = true;
                currLower = close0 - VStopMultiplier * atr0;
            }
            else if (prevTrend && close0 < prevLower)
            {
                currTrend = false;
                currUpper = close0 + VStopMultiplier * atr0;
            }
            else if (currTrend)
            {
                double candidate1 = close0 - VStopMultiplier * atr0;
                double candidate2 = prevLower + VStopMultiplier * (atr1 - atr0);
                currLower = Math.Max(Math.Max(prevLower, candidate1), candidate2);
            }
            else
            {
                double candidate1 = close0 + VStopMultiplier * atr0;
                double candidate2 = prevUpper - VStopMultiplier * (atr1 - atr0);
                currUpper = Math.Min(Math.Min(prevUpper, candidate1), candidate2);
            }

            _vstopIsBullish[index] = currTrend;
            _vstopLower[index] = currLower;
            _vstopUpper[index] = currUpper;

            if (EnableVStop)
            {
                if (currTrend)
                {
                    BullishStop[index] = currLower;
                    BearishStop[index] = double.NaN;
                }
                else
                {
                    BearishStop[index] = currUpper;
                    BullishStop[index] = double.NaN;
                }
            }
            else
            {
                BullishStop[index] = double.NaN;
                BearishStop[index] = double.NaN;
            }
        }

        // =====================================================================
        // METHOD A: INDICATOR FLIP SWING DETECTION + PULLBACK ENGINE
        // =====================================================================
        private void ProcessMethodASwingAndPullbacks(int barIndex)
        {
            bool currBull = _vstopIsBullish[barIndex];
            bool prevBull = _vstopIsBullish[barIndex - 1];
            double barLow = Bars.LowPrices[barIndex];
            double barHigh = Bars.HighPrices[barIndex];

            // 1. Check if the Volatility Stop band just flipped at barIndex
            if (currBull != prevBull)
            {
                _activeFlipBar = barIndex;
                _activeFlipIsBullish = currBull;
                _activeFlipPrevLineEdge = currBull ? _vstopUpper[barIndex - 1] : _vstopLower[barIndex - 1];
                _activeFlipNewLineEdge = currBull ? _vstopLower[barIndex] : _vstopUpper[barIndex];
                _activeFlipArrowsCount = 0;
                _activeFlipFirstArrowPrice = double.NaN;

                if (currBull)
                {
                    // ---------------------------------------------------------
                    // METHOD A SWING LOW CONFIRMATION:
                    // Band flipped from Blue (overhead resistance) -> Orange (support below)
                    // ---------------------------------------------------------
                    if (double.IsNaN(_currBearSegMinLow) || barLow < _currBearSegMinLow)
                    {
                        _currBearSegMinLow = barLow;
                        _currBearSegMinLowBar = barIndex;
                    }

                    if (_currBearSegMinLowBar >= 0 && !double.IsNaN(_currBearSegMinLow))
                    {
                        DrawSwingLowMarker(_currBearSegMinLowBar, _currBearSegMinLow);
                        _prevConfirmedSwingLow = _currBearSegMinLow;
                    }

                    // Uptrend structure check: Blue 2's swing low cannot be lower than Red/Orange 1's lowest candle
                    _activeFlipSegmentValid = !double.IsNaN(_prevBullSegMinLow) &&
                                              !double.IsNaN(_currBearSegMinLow) &&
                                              (_currBearSegMinLow >= _prevBullSegMinLow);
                    _activeFlipSegmentBoundPrice = _prevBullSegMinLow;

                    _prevBearSegMaxHigh = _currBearSegMaxHigh;

                    // Initialize new Red/Orange segment tracking
                    _currBullSegMinLow = Math.Min(barLow, Bars.LowPrices[barIndex - 1]);
                    _currBullSegMaxHigh = barHigh;
                    _currBullSegMaxHighBar = barIndex;
                }
                else
                {
                    // ---------------------------------------------------------
                    // METHOD A SWING HIGH CONFIRMATION:
                    // Band flipped from Orange (support below) -> Blue (overhead resistance)
                    // ---------------------------------------------------------
                    if (double.IsNaN(_currBullSegMaxHigh) || barHigh > _currBullSegMaxHigh)
                    {
                        _currBullSegMaxHigh = barHigh;
                        _currBullSegMaxHighBar = barIndex;
                    }

                    if (_currBullSegMaxHighBar >= 0 && !double.IsNaN(_currBullSegMaxHigh))
                    {
                        DrawSwingHighMarker(_currBullSegMaxHighBar, _currBullSegMaxHigh);
                        _prevConfirmedSwingHigh = _currBullSegMaxHigh;
                    }

                    // Downtrend structure check: Red/Orange 2's highest candle cannot be higher than Blue 1's highest candle
                    _activeFlipSegmentValid = !double.IsNaN(_prevBearSegMaxHigh) &&
                                              !double.IsNaN(_currBullSegMaxHigh) &&
                                              (_currBullSegMaxHigh <= _prevBearSegMaxHigh);
                    _activeFlipSegmentBoundPrice = _prevBearSegMaxHigh;

                    _prevBullSegMinLow = _currBullSegMinLow;

                    // Initialize new Blue (Bearish) segment tracking
                    _currBearSegMaxHigh = Math.Max(barHigh, Bars.HighPrices[barIndex - 1]);
                    _currBearSegMinLow = barLow;
                    _currBearSegMinLowBar = barIndex;
                }

                if (!ShowPullbackArrows || !_activeFlipSegmentValid)
                    return;

                int earliestBar2 = Math.Max(2, barIndex - MaxBarsFromFlip);
                int latestBar2Now = barIndex - 1;

                for (int b2 = earliestBar2; b2 <= latestBar2Now; b2++)
                {
                    if (_activeFlipArrowsCount >= MaxArrowsPerFlip)
                        break;

                    if (!_drawnBar2Indices.Contains(b2) &&
                        IsQualifyingPullbackPattern(b2, barIndex, _activeFlipIsBullish, _activeFlipPrevLineEdge, _activeFlipNewLineEdge))
                    {
                        RecordAndDrawArrowAtBar2(b2, _activeFlipIsBullish);
                    }
                }
                return;
            }

            // 2. Update active segment extremes on non-flip bars
            if (currBull)
            {
                if (double.IsNaN(_currBullSegMinLow) || barLow < _currBullSegMinLow)
                    _currBullSegMinLow = barLow;

                if (double.IsNaN(_currBullSegMaxHigh) || barHigh > _currBullSegMaxHigh)
                {
                    _currBullSegMaxHigh = barHigh;
                    _currBullSegMaxHighBar = barIndex;
                }
            }
            else
            {
                if (double.IsNaN(_currBearSegMinLow) || barLow < _currBearSegMinLow)
                {
                    _currBearSegMinLow = barLow;
                    _currBearSegMinLowBar = barIndex;
                }

                if (double.IsNaN(_currBearSegMaxHigh) || barHigh > _currBearSegMaxHigh)
                    _currBearSegMaxHigh = barHigh;
            }

            if (!ShowPullbackArrows)
                return;

            // 3. Post-flip pullback check
            if (_activeFlipBar > 0 &&
                _activeFlipSegmentValid &&
                _activeFlipArrowsCount < MaxArrowsPerFlip &&
                currBull == _activeFlipIsBullish)
            {
                if (barIndex <= _activeFlipBar + MaxBarsFromFlip + 3)
                {
                    int minB2 = Math.Max(2, Math.Max(_activeFlipBar - MaxBarsFromFlip, barIndex - 3));
                    int maxB2 = Math.Min(_activeFlipBar + MaxBarsFromFlip, barIndex - 1);

                    for (int b2 = minB2; b2 <= maxB2; b2++)
                    {
                        if (_activeFlipArrowsCount >= MaxArrowsPerFlip)
                            break;

                        if (!_drawnBar2Indices.Contains(b2) &&
                            IsQualifyingPullbackPattern(b2, barIndex, _activeFlipIsBullish, _activeFlipPrevLineEdge, _activeFlipNewLineEdge))
                        {
                            RecordAndDrawArrowAtBar2(b2, _activeFlipIsBullish);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Draws a Structural Swing Low marker at the exact candle Low:
        ///   - 1st Circle (First Low or Lower Low): Smaller, Lighter Blue-Cyan Circle (FirstSwingLowSeries, Thickness = 4).
        ///   - 2nd Circle (Higher Low - HL):        Larger, Brighter Aqua Circle (SecondSwingLowSeries, Thickness = 9)
        ///                                          + Highlighted Aqua Up-Arrow below the candle Low.
        /// </summary>
        private void DrawSwingLowMarker(int swingBarIndex, double exactLowPrice)
        {
            if (!ShowSwingLows || swingBarIndex < 0)
                return;

            bool isHigherLow = !double.IsNaN(_prevConfirmedSwingLow) && (exactLowPrice > _prevConfirmedSwingLow);
            string iconId = "MK_SWING_LOW_" + swingBarIndex;
            double atrVal = _atr.Result[swingBarIndex];
            double offset = (!double.IsNaN(atrVal) && atrVal > 0 ? atrVal : Symbol.PipSize * 10) * 0.32;

            if (isHigherLow)
            {
                // 2nd Circle: Larger, Brighter Circle at exact candle Low + Highlighted Up-Arrow
                FirstSwingLowSeries[swingBarIndex] = double.NaN;
                SecondSwingLowSeries[swingBarIndex] = exactLowPrice;
                Chart.DrawIcon(iconId, ChartIconType.UpArrow, swingBarIndex, exactLowPrice - offset, _higherLowColor);
            }
            else
            {
                // 1st Circle: Smaller, Lighter Circle at exact candle Low
                SecondSwingLowSeries[swingBarIndex] = double.NaN;
                FirstSwingLowSeries[swingBarIndex] = exactLowPrice;
            }
        }

        /// <summary>
        /// Draws a Structural Swing High marker at the exact candle High:
        ///   - 1st Circle (First High or Higher High): Smaller, Lighter Pale-Gold Circle (FirstSwingHighSeries, Thickness = 4).
        ///   - 2nd Circle (Lower High - LH):           Larger, Brighter Gold Circle (SecondSwingHighSeries, Thickness = 9)
        ///                                             + Highlighted Gold Down-Arrow above the candle High.
        /// </summary>
        private void DrawSwingHighMarker(int swingBarIndex, double exactHighPrice)
        {
            if (!ShowSwingHighs || swingBarIndex < 0)
                return;

            bool isLowerHigh = !double.IsNaN(_prevConfirmedSwingHigh) && (exactHighPrice < _prevConfirmedSwingHigh);
            string iconId = "MK_SWING_HIGH_" + swingBarIndex;
            double atrVal = _atr.Result[swingBarIndex];
            double offset = (!double.IsNaN(atrVal) && atrVal > 0 ? atrVal : Symbol.PipSize * 10) * 0.32;

            if (isLowerHigh)
            {
                // 2nd Circle: Larger, Brighter Circle at exact candle High + Highlighted Down-Arrow
                FirstSwingHighSeries[swingBarIndex] = double.NaN;
                SecondSwingHighSeries[swingBarIndex] = exactHighPrice;
                Chart.DrawIcon(iconId, ChartIconType.DownArrow, swingBarIndex, exactHighPrice + offset, _lowerHighColor);
            }
            else
            {
                // 1st Circle: Smaller, Lighter Circle at exact candle High
                SecondSwingHighSeries[swingBarIndex] = double.NaN;
                FirstSwingHighSeries[swingBarIndex] = exactHighPrice;
            }
        }

        private bool IsQualifyingPullbackPattern(int b2, int maxAvailableBar, bool isBullishFlip, double prevLineEdge, double newLineEdge)
        {
            int b1 = b2 - 1;
            if (b1 < 1 || b2 + 1 > maxAvailableBar)
                return false;

            double open1 = Bars.OpenPrices[b1];
            double close1 = Bars.ClosePrices[b1];
            double high1 = Bars.HighPrices[b1];
            double low1 = Bars.LowPrices[b1];

            double open2 = Bars.OpenPrices[b2];
            double close2 = Bars.ClosePrices[b2];
            double high2 = Bars.HighPrices[b2];
            double low2 = Bars.LowPrices[b2];

            double range1 = high1 - low1;
            double range2 = high2 - low2;
            if (range1 <= 0 || range2 >= range1)
                return false;

            int maxConfirmBar = Math.Min(b2 + 3, maxAvailableBar);
            bool confirmedBy345 = false;

            if (isBullishFlip)
            {
                if (close1 <= open1)
                    return false;

                if (close2 >= open2 || low2 < low1)
                    return false;

                if (!double.IsNaN(_activeFlipSegmentBoundPrice) && low2 < _activeFlipSegmentBoundPrice)
                    return false;

                if (!double.IsNaN(_activeFlipFirstArrowPrice) && low2 < _activeFlipFirstArrowPrice)
                    return false;

                for (int k = b2 + 1; k <= maxConfirmBar; k++)
                {
                    if (!double.IsNaN(_activeFlipSegmentBoundPrice) && Bars.LowPrices[k] < _activeFlipSegmentBoundPrice)
                        return false;

                    if (Bars.ClosePrices[k] > Bars.OpenPrices[k] && Bars.HighPrices[k] > high2)
                    {
                        confirmedBy345 = true;
                        break;
                    }
                }
            }
            else
            {
                if (close1 >= open1)
                    return false;

                if (close2 <= open2 || high2 > high1)
                    return false;

                if (!double.IsNaN(_activeFlipSegmentBoundPrice) && high2 > _activeFlipSegmentBoundPrice)
                    return false;

                if (!double.IsNaN(_activeFlipFirstArrowPrice) && high2 > _activeFlipFirstArrowPrice)
                    return false;

                for (int k = b2 + 1; k <= maxConfirmBar; k++)
                {
                    if (!double.IsNaN(_activeFlipSegmentBoundPrice) && Bars.HighPrices[k] > _activeFlipSegmentBoundPrice)
                        return false;

                    if (Bars.ClosePrices[k] < Bars.OpenPrices[k] && Bars.LowPrices[k] < low2)
                    {
                        confirmedBy345 = true;
                        break;
                    }
                }
            }

            if (!confirmedBy345)
                return false;

            double atr2 = _atr.Result[b2];
            if (double.IsNaN(atr2) || atr2 <= 0)
                atr2 = range1;

            double currentLineAtB2 = _vstopIsBullish[b2] ? _vstopLower[b2] : _vstopUpper[b2];

            double distToPrevEdge = GetBarVerticalDistanceToLevel(low2, high2, prevLineEdge);
            double distToNewEdge = GetBarVerticalDistanceToLevel(low2, high2, newLineEdge);
            double distToCurrLine = GetBarVerticalDistanceToLevel(low2, high2, currentLineAtB2);

            double minVerticalDist = Math.Min(distToPrevEdge, Math.Min(distToNewEdge, distToCurrLine));

            return minVerticalDist <= (MaxVerticalAtrDist * atr2);
        }

        private double GetBarVerticalDistanceToLevel(double barLow, double barHigh, double levelPrice)
        {
            if (double.IsNaN(levelPrice))
                return double.MaxValue;

            if (levelPrice >= barLow && levelPrice <= barHigh)
                return 0.0;

            return Math.Min(Math.Abs(barLow - levelPrice), Math.Abs(barHigh - levelPrice));
        }

        private void RecordAndDrawArrowAtBar2(int bar2Index, bool isBullish)
        {
            _drawnBar2Indices.Add(bar2Index);
            _activeFlipArrowsCount++;

            if (double.IsNaN(_activeFlipFirstArrowPrice))
            {
                _activeFlipFirstArrowPrice = isBullish ? Bars.LowPrices[bar2Index] : Bars.HighPrices[bar2Index];
            }

            string iconId = (isBullish ? "MK_PB_BULL_" : "MK_PB_BEAR_") + bar2Index;
            double atrVal = _atr.Result[bar2Index];
            double offset = (!double.IsNaN(atrVal) && atrVal > 0 ? atrVal : Symbol.PipSize * 10) * 0.35;

            if (isBullish)
            {
                double y = Bars.LowPrices[bar2Index] - offset;
                Chart.DrawIcon(iconId, ChartIconType.UpArrow, bar2Index, y, _bullSolidColor);
            }
            else
            {
                double y = Bars.HighPrices[bar2Index] + offset;
                Chart.DrawIcon(iconId, ChartIconType.DownArrow, bar2Index, y, _bearSolidColor);
            }

            if (EnableAlerts && IsLastBar && _lastAlertBar != bar2Index)
            {
                _lastAlertBar = bar2Index;
                Notifications.PlaySound(SoundType.Announcement);
                Print("[MK FVG + VStop] {0} Pullback Arrow on {1} ({2}) at Bar 2 ({3})",
                    isBullish ? "BULLISH" : "BEARISH", Symbol.Name, TimeFrame, Bars.OpenTimes[bar2Index]);
            }
        }

        // =====================================================================
        // FAIR VALUE GAP (FVG) ENGINE
        // =====================================================================
        private void ProcessClosedBarFvg(int barIndex)
        {
            double barHigh = Bars.HighPrices[barIndex];
            double barLow = Bars.LowPrices[barIndex];

            for (int i = _activeFvgZones.Count - 1; i >= 0; i--)
            {
                FvgZone zone = _activeFvgZones[i];

                bool isExpired = ExpireBars > 0 && (barIndex - zone.CreatedBarIndex) >= ExpireBars;
                bool isMitigated = zone.IsBullish
                    ? (barLow <= zone.Bottom)
                    : (barHigh >= zone.Top);

                if (isExpired || isMitigated)
                {
                    if (HideMitigated)
                    {
                        Chart.RemoveObject(zone.Id);
                    }
                    else
                    {
                        zone.EndBarIndex = barIndex;
                        DrawOrUpdateFvgRectangle(zone, barIndex);
                    }
                    _activeFvgZones.RemoveAt(i);
                }
            }

            int c1 = barIndex - 2;
            int c3 = barIndex;

            double high1 = Bars.HighPrices[c1];
            double low1 = Bars.LowPrices[c1];
            double high3 = Bars.HighPrices[c3];
            double low3 = Bars.LowPrices[c3];

            bool isVStopBullish = _vstopIsBullish[barIndex];

            if (ShowBullishFvg && low3 > high1)
            {
                double gapSizePips = (low3 - high1) / Symbol.PipSize;
                bool passesTrendFilter = !FilterFvgByTrend || isVStopBullish;

                if (gapSizePips >= MinGapPips && passesTrendFilter)
                {
                    FvgZone newZone = new FvgZone
                    {
                        Id = "MK_FVG_BULL_" + c3,
                        IsBullish = true,
                        StartBarIndex = c1,
                        CreatedBarIndex = c3,
                        EndBarIndex = -1,
                        Bottom = high1,
                        Top = low3
                    };

                    _activeFvgZones.Add(newZone);
                    EnforceMaxActiveGaps();
                    DrawOrUpdateFvgRectangle(newZone, c3);
                }
            }

            if (ShowBearishFvg && high3 < low1)
            {
                double gapSizePips = (low1 - high3) / Symbol.PipSize;
                bool passesTrendFilter = !FilterFvgByTrend || !isVStopBullish;

                if (gapSizePips >= MinGapPips && passesTrendFilter)
                {
                    FvgZone newZone = new FvgZone
                    {
                        Id = "MK_FVG_BEAR_" + c3,
                        IsBullish = false,
                        StartBarIndex = c1,
                        CreatedBarIndex = c3,
                        EndBarIndex = -1,
                        Top = low1,
                        Bottom = high3
                    };

                    _activeFvgZones.Add(newZone);
                    EnforceMaxActiveGaps();
                    DrawOrUpdateFvgRectangle(newZone, c3);
                }
            }
        }

        private void EnforceMaxActiveGaps()
        {
            while (_activeFvgZones.Count > MaxActiveGaps)
            {
                FvgZone oldest = _activeFvgZones[0];
                Chart.RemoveObject(oldest.Id);
                _activeFvgZones.RemoveAt(0);
            }
        }

        private void UpdateActiveFvgRectangles(int currentIndex)
        {
            for (int i = 0; i < _activeFvgZones.Count; i++)
            {
                FvgZone zone = _activeFvgZones[i];
                if (zone.EndBarIndex != currentIndex)
                {
                    DrawOrUpdateFvgRectangle(zone, currentIndex);
                }
            }
        }

        private void DrawOrUpdateFvgRectangle(FvgZone zone, int rightBarIndex)
        {
            zone.EndBarIndex = rightBarIndex;
            Color fillColor = zone.IsBullish ? _bullColor : _bearColor;
            var rect = Chart.DrawRectangle(
                zone.Id,
                zone.StartBarIndex,
                zone.Top,
                rightBarIndex,
                zone.Bottom,
                fillColor,
                1,
                LineStyle.Solid
            );
            rect.IsFilled = true;
        }

        private void EnsureCapacity(int index)
        {
            if (index < _vstopIsBullish.Length)
                return;

            int newSize = Math.Max(index + 1024, _vstopIsBullish.Length * 2);
            Array.Resize(ref _vstopIsBullish, newSize);
            Array.Resize(ref _vstopLower, newSize);
            Array.Resize(ref _vstopUpper, newSize);
        }

        private Color ParseColor(string colorName, Color fallback)
        {
            if (string.IsNullOrWhiteSpace(colorName))
                return fallback;

            Color parsed = Color.FromName(colorName.Trim());
            if (parsed.A == 0 && parsed.R == 0 && parsed.G == 0 && parsed.B == 0 &&
                !colorName.Trim().Equals("Black", StringComparison.OrdinalIgnoreCase))
            {
                return fallback;
            }
            return parsed;
        }
    }
}

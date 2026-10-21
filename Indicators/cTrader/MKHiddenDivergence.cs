using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;

namespace cAlgo
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class MKHiddenDivergence : Indicator
    {
        [Parameter("%K Period", DefaultValue = 10, MinValue = 1)]
        public int PeriodK { get; set; }

        [Parameter("%K Smooth", DefaultValue = 10, MinValue = 1)]
        public int SmoothK { get; set; }

        [Parameter("%D Period", DefaultValue = 3, MinValue = 1)]
        public int PeriodD { get; set; }

        [Parameter("Swing Lookback", DefaultValue = 3, MinValue = 1)]
        public int Lookback { get; set; }

        [Parameter("Swing Lookahead", DefaultValue = 3, MinValue = 1)]
        public int Lookahead { get; set; }

        [Parameter("Price Window", DefaultValue = 4, MinValue = 1)]
        public int PriceWindow { get; set; }

        [Parameter("Min Bars Dist", DefaultValue = 4, MinValue = 1)]
        public int MinBarsDist { get; set; }

        [Parameter("Max Bars Dist", DefaultValue = 45, MinValue = 1)]
        public int MaxBarsDist { get; set; }

        [Parameter("Min Stoch Diff", DefaultValue = 2.0, MinValue = 0.0)]
        public double MinStochDiff { get; set; }

        private StochasticOscillator _stoch;
        private int _lastPeakSBar = -1, _lastPeakPBar = -1;
        private double _lastPeakSVal = 0.0, _lastPeakPVal = 0.0;
        private int _lastTroughSBar = -1, _lastTroughPBar = -1;
        private double _lastTroughSVal = 0.0, _lastTroughPVal = 0.0;

        protected override void Initialize()
        {
            _stoch = Indicators.StochasticOscillator(PeriodK, SmoothK, PeriodD, MovingAverageType.Simple);
        }

        public override void Calculate(int index)
        {
            if (index < PeriodK + SmoothK + PeriodD + Lookback + Lookahead + PriceWindow + 2)
                return;

            int swingBar = index - Lookahead;
            double ki = _stoch.PercentK[swingBar];

            // Evaluate Peaks
            if (ki > 45.0)
            {
                bool isPeak = true;
                for (int j = 1; j <= Lookback; j++)
                    if (ki <= _stoch.PercentK[swingBar - j]) { isPeak = false; break; }
                if (isPeak)
                    for (int j = 1; j <= Lookahead; j++)
                        if (ki < _stoch.PercentK[swingBar + j]) { isPeak = false; break; }

                if (isPeak)
                {
                    int bestPBar = swingBar;
                    double bestH = Bars.HighPrices[swingBar];
                    int wStart = Math.Max(0, swingBar - PriceWindow);
                    int wEnd = Math.Min(index, swingBar + PriceWindow);
                    for (int w = wStart; w <= wEnd; w++)
                    {
                        if (Bars.HighPrices[w] > bestH)
                        {
                            bestH = Bars.HighPrices[w];
                            bestPBar = w;
                        }
                    }

                    if (_lastPeakSBar >= 0)
                    {
                        int sDist = swingBar - _lastPeakSBar;
                        int pDist = Math.Abs(bestPBar - _lastPeakPBar);
                        if (sDist >= MinBarsDist && sDist <= MaxBarsDist && pDist >= MinBarsDist)
                        {
                            double pDelta = bestH - _lastPeakPVal;
                            double sDelta = ki - _lastPeakSVal;

                            if (pDelta < -Symbol.PipSize && sDelta > MinStochDiff)
                            {
                                // Hidden Bearish
                                Chart.DrawTrendLine("HBear_" + index, _lastPeakPBar, _lastPeakPVal, bestPBar, bestH, Color.Cyan, 2, LineStyle.Solid);
                                Chart.DrawIcon("HBearIcon_" + index, ChartIconType.DownArrow, bestPBar, bestH + Symbol.PipSize * 4, Color.Cyan);
                            }
                            else if (pDelta > Symbol.PipSize && sDelta < -MinStochDiff)
                            {
                                // Regular Bearish
                                Chart.DrawTrendLine("RBear_" + index, _lastPeakPBar, _lastPeakPVal, bestPBar, bestH, Color.DarkOrange, 2, LineStyle.Dots);
                                Chart.DrawIcon("RBearIcon_" + index, ChartIconType.DownArrow, bestPBar, bestH + Symbol.PipSize * 4, Color.DarkOrange);
                            }
                        }
                    }

                    _lastPeakSBar = swingBar;
                    _lastPeakSVal = ki;
                    _lastPeakPBar = bestPBar;
                    _lastPeakPVal = bestH;
                }
            }

            // Evaluate Troughs
            if (ki < 55.0)
            {
                bool isTrough = true;
                for (int j = 1; j <= Lookback; j++)
                    if (ki >= _stoch.PercentK[swingBar - j]) { isTrough = false; break; }
                if (isTrough)
                    for (int j = 1; j <= Lookahead; j++)
                        if (ki > _stoch.PercentK[swingBar + j]) { isTrough = false; break; }

                if (isTrough)
                {
                    int bestPBar = swingBar;
                    double bestL = Bars.LowPrices[swingBar];
                    int wStart = Math.Max(0, swingBar - PriceWindow);
                    int wEnd = Math.Min(index, swingBar + PriceWindow);
                    for (int w = wStart; w <= wEnd; w++)
                    {
                        if (Bars.LowPrices[w] < bestL)
                        {
                            bestL = Bars.LowPrices[w];
                            bestPBar = w;
                        }
                    }

                    if (_lastTroughSBar >= 0)
                    {
                        int sDist = swingBar - _lastTroughSBar;
                        int pDist = Math.Abs(bestPBar - _lastTroughPBar);
                        if (sDist >= MinBarsDist && sDist <= MaxBarsDist && pDist >= MinBarsDist)
                        {
                            double pDelta = bestL - _lastTroughPVal;
                            double sDelta = ki - _lastTroughSVal;

                            if (pDelta > Symbol.PipSize && sDelta < -MinStochDiff)
                            {
                                // Hidden Bullish
                                Chart.DrawTrendLine("HBull_" + index, _lastTroughPBar, _lastTroughPVal, bestPBar, bestL, Color.Cyan, 2, LineStyle.Solid);
                                Chart.DrawIcon("HBullIcon_" + index, ChartIconType.UpArrow, bestPBar, bestL - Symbol.PipSize * 4, Color.Cyan);
                            }
                            else if (pDelta < -Symbol.PipSize && sDelta > MinStochDiff)
                            {
                                // Regular Bullish
                                Chart.DrawTrendLine("RBull_" + index, _lastTroughPBar, _lastTroughPVal, bestPBar, bestL, Color.LimeGreen, 2, LineStyle.Dots);
                                Chart.DrawIcon("RBullIcon_" + index, ChartIconType.UpArrow, bestPBar, bestL - Symbol.PipSize * 4, Color.LimeGreen);
                            }
                        }
                    }

                    _lastTroughSBar = swingBar;
                    _lastTroughSVal = ki;
                    _lastTroughPBar = bestPBar;
                    _lastTroughPVal = bestL;
                }
            }
        }
    }
}

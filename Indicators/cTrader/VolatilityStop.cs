using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;

namespace cAlgo
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class VolatilityStop : Indicator
    {
        [Parameter("ATR Period", DefaultValue = 20, MinValue = 1)]
        public int Period { get; set; }

        [Parameter("ATR Multiplier", DefaultValue = 3.0, MinValue = 0.1, Step = 0.1)]
        public double Multiplier { get; set; }

        [Parameter("ATR MA Type", DefaultValue = MovingAverageType.Simple)]
        public MovingAverageType MAType { get; set; }

        [Output("Bullish Stop", LineColor = "LimeGreen", PlotType = PlotType.DiscontinuousLine, Thickness = 2)]
        public IndicatorDataSeries BullishStop { get; set; }

        [Output("Bearish Stop", LineColor = "Crimson", PlotType = PlotType.DiscontinuousLine, Thickness = 2)]
        public IndicatorDataSeries BearishStop { get; set; }

        private AverageTrueRange _atr;
        private bool[] _isBullish;
        private double[] _lower;
        private double[] _upper;

        protected override void Initialize()
        {
            _atr = Indicators.AverageTrueRange(Period, MAType);
            int capacity = Math.Max(Bars.Count + 1024, 4096);
            _isBullish = new bool[capacity];
            _lower = new double[capacity];
            _upper = new double[capacity];
        }

        public override void Calculate(int index)
        {
            if (index >= _isBullish.Length)
            {
                int newSize = Math.Max(index + 1024, _isBullish.Length * 2);
                Array.Resize(ref _isBullish, newSize);
                Array.Resize(ref _lower, newSize);
                Array.Resize(ref _upper, newSize);
            }

            double close0 = Bars.ClosePrices[index];
            double atr0 = _atr.Result[index];
            if (double.IsNaN(atr0) || atr0 <= 0)
                atr0 = Bars.HighPrices[index] - Bars.LowPrices[index];

            if (index <= Period)
            {
                _isBullish[index] = true;
                _lower[index] = close0 - Multiplier * atr0;
                _upper[index] = close0 + Multiplier * atr0;
                BullishStop[index] = (index == Period) ? _lower[index] : double.NaN;
                BearishStop[index] = double.NaN;
                return;
            }

            bool prevTrend = _isBullish[index - 1];
            double prevLower = _lower[index - 1];
            double prevUpper = _upper[index - 1];
            double atr1 = _atr.Result[index - 1];
            if (double.IsNaN(atr1)) atr1 = atr0;

            bool currTrend = prevTrend;
            double currLower = prevLower;
            double currUpper = prevUpper;

            if (!prevTrend && close0 > prevUpper)
            {
                currTrend = true;
                currLower = close0 - Multiplier * atr0;
            }
            else if (prevTrend && close0 < prevLower)
            {
                currTrend = false;
                currUpper = close0 + Multiplier * atr0;
            }
            else if (currTrend)
            {
                double cand1 = close0 - Multiplier * atr0;
                double cand2 = prevLower + Multiplier * (atr1 - atr0);
                currLower = Math.Max(Math.Max(prevLower, cand1), cand2);
            }
            else
            {
                double cand1 = close0 + Multiplier * atr0;
                double cand2 = prevUpper - Multiplier * (atr1 - atr0);
                currUpper = Math.Min(Math.Min(prevUpper, cand1), cand2);
            }

            _isBullish[index] = currTrend;
            _lower[index] = currLower;
            _upper[index] = currUpper;

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
    }
}

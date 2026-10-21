#region Using declarations
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
{
    public class VolatilityStop : Indicator
    {
        private Series<bool> isBullish;
        private Series<double> lower;
        private Series<double> upper;
        private ATR atr;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"MK Volatility Stop Indicator";
                Name = "VolatilityStop";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                Period = 20;
                Multiplier = 3.0;

                AddPlot(new Stroke(Brushes.LimeGreen, 2), PlotStyle.Line, "BullishStop");
                AddPlot(new Stroke(Brushes.Crimson, 2), PlotStyle.Line, "BearishStop");
            }
            else if (State == State.DataLoaded)
            {
                atr = ATR(Period);
                isBullish = new Series<bool>(this);
                lower = new Series<double>(this);
                upper = new Series<double>(this);
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < Period)
            {
                isBullish[0] = true;
                lower[0] = Close[0] - Multiplier * atr[0];
                upper[0] = Close[0] + Multiplier * atr[0];
                return;
            }

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
            {
                currTrend = true;
                currLower = c0 - Multiplier * a0;
            }
            else if (prevTrend && c0 < prevLower)
            {
                currTrend = false;
                currUpper = c0 + Multiplier * a0;
            }
            else if (currTrend)
            {
                double cand1 = c0 - Multiplier * a0;
                double cand2 = prevLower + Multiplier * (a1 - a0);
                currLower = Math.Max(Math.Max(prevLower, cand1), cand2);
            }
            else
            {
                double cand1 = c0 + Multiplier * a0;
                double cand2 = prevUpper - Multiplier * (a1 - a0);
                currUpper = Math.Min(Math.Min(prevUpper, cand1), cand2);
            }

            isBullish[0] = currTrend;
            lower[0] = currLower;
            upper[0] = currUpper;

            if (currTrend)
            {
                Values[0][0] = currLower;
                Values[1].Reset();
            }
            else
            {
                Values[1][0] = currUpper;
                Values[0].Reset();
            }
        }

        #region Properties
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name="Period", GroupName="Parameters", Order=1)]
        public int Period { get; set; }

        [NinjaScriptProperty]
        [Range(0.1, double.MaxValue)]
        [Display(Name="Multiplier", GroupName="Parameters", Order=2)]
        public double Multiplier { get; set; }
        #endregion
    }
}

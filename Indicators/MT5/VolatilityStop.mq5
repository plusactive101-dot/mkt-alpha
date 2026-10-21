//+------------------------------------------------------------------+
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
input int    InpAtrPeriod     = 20;      // ATR Period
input double InpAtrMultiplier = 3.0;  // ATR Multiplier

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
{
   SetIndexBuffer(0, BullishStopBuffer, INDICATOR_DATA);
   PlotIndexSetDouble(0, PLOT_EMPTY_VALUE, EMPTY_VALUE);

   SetIndexBuffer(1, BearishStopBuffer, INDICATOR_DATA);
   PlotIndexSetDouble(1, PLOT_EMPTY_VALUE, EMPTY_VALUE);

   atrHandle = iATR(_Symbol, _Period, InpAtrPeriod);
   if(atrHandle == INVALID_HANDLE)
   {
      Print("Failed to create ATR handle.");
      return(INIT_FAILED);
   }

   IndicatorSetString(INDICATOR_SHORTNAME, "Volatility Stop (" + IntegerToString(InpAtrPeriod) + ", " + DoubleToString(InpAtrMultiplier, 1) + ")");
   return(INIT_SUCCEEDED);
}

//+------------------------------------------------------------------+
//| Custom indicator deinitialization function                       |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
{
   if(atrHandle != INVALID_HANDLE)
      IndicatorRelease(atrHandle);
}

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
{
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
   {
      double c0 = close[i];
      double a0 = atrVals[i];
      if(a0 <= 0) a0 = high[i] - low[i];

      if(i <= InpAtrPeriod)
      {
         isBullish[i] = true;
         lowerStop[i] = c0 - InpAtrMultiplier * a0;
         upperStop[i] = c0 + InpAtrMultiplier * a0;

         if(i == InpAtrPeriod)
         {
            BullishStopBuffer[i] = lowerStop[i];
            BearishStopBuffer[i] = EMPTY_VALUE;
         }
         else
         {
            BullishStopBuffer[i] = EMPTY_VALUE;
            BearishStopBuffer[i] = EMPTY_VALUE;
         }
         continue;
      }

      bool prevTrend = isBullish[i - 1];
      double prevLower = lowerStop[i - 1];
      double prevUpper = upperStop[i - 1];
      double a1 = atrVals[i - 1];
      if(a1 <= 0) a1 = a0;

      bool currTrend = prevTrend;
      double currLower = prevLower;
      double currUpper = prevUpper;

      if(!prevTrend && c0 > prevUpper)
      {
         currTrend = true;
         currLower = c0 - InpAtrMultiplier * a0;
      }
      else if(prevTrend && c0 < prevLower)
      {
         currTrend = false;
         currUpper = c0 + InpAtrMultiplier * a0;
      }
      else if(currTrend)
      {
         double cand1 = c0 - InpAtrMultiplier * a0;
         double cand2 = prevLower + InpAtrMultiplier * (a1 - a0);
         currLower = MathMax(MathMax(prevLower, cand1), cand2);
      }
      else
      {
         double cand1 = c0 + InpAtrMultiplier * a0;
         double cand2 = prevUpper - InpAtrMultiplier * (a1 - a0);
         currUpper = MathMin(MathMin(prevUpper, cand1), cand2);
      }

      isBullish[i] = currTrend;
      lowerStop[i] = currLower;
      upperStop[i] = currUpper;

      if(currTrend)
      {
         BullishStopBuffer[i] = currLower;
         BearishStopBuffer[i] = EMPTY_VALUE;
      }
      else
      {
         BearishStopBuffer[i] = currUpper;
         BullishStopBuffer[i] = EMPTY_VALUE;
      }
   }

   return(rates_total);
}

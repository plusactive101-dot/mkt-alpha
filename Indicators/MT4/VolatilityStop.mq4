//+------------------------------------------------------------------+
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
input int    InpAtrPeriod     = 20;      // ATR Period
input double InpAtrMultiplier = 3.0;  // ATR Multiplier

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
{
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

   int start = prev_calculated - 1;
   if(start < 0) start = 0;

   // Process in chronological order (0 to rates_total - 1)
   for(int i = start; i < rates_total; i++)
   {
      int shift = rates_total - 1 - i;
      double c0 = close[shift];
      double a0 = iATR(NULL, 0, InpAtrPeriod, shift);
      if(a0 <= 0) a0 = high[shift] - low[shift];

      if(i <= InpAtrPeriod)
      {
         isBullish[i] = true;
         lowerStop[i] = c0 - InpAtrMultiplier * a0;
         upperStop[i] = c0 + InpAtrMultiplier * a0;

         if(i == InpAtrPeriod)
         {
            BullishStopBuffer[shift] = lowerStop[i];
            BearishStopBuffer[shift] = EMPTY_VALUE;
         }
         else
         {
            BullishStopBuffer[shift] = EMPTY_VALUE;
            BearishStopBuffer[shift] = EMPTY_VALUE;
         }
         continue;
      }

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
         BullishStopBuffer[shift] = currLower;
         BearishStopBuffer[shift] = EMPTY_VALUE;
      }
      else
      {
         BearishStopBuffer[shift] = currUpper;
         BullishStopBuffer[shift] = EMPTY_VALUE;
      }
   }

   return(rates_total);
}

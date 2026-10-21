# Canonical Indicator Specification: Volatility Stop (`volatility_stop`)

## 1. Indicator Overview
The **Volatility Stop** is an ATR-based trend-following and trailing stop indicator. It projects a trailing support line during an uptrend and a trailing resistance line during a downtrend.

A defining characteristic of this indicator is its **volatility ratchet mechanism**:
- In an uptrend, if price rises or if volatility contracts ($ATR_{t-1} > ATR_t$), the stop line ratchets **higher** closer to price. It will **never loosen downward** during an ongoing uptrend.
- In a downtrend, if price drops or if volatility contracts ($ATR_{t-1} > ATR_t$), the stop line ratchets **lower** toward price. It will **never loosen upward** during an ongoing downtrend.

---

## 2. Mathematical Definition

### True Range ($TR_t$)
$$TR_t = \max\left( High_t - Low_t, \, |High_t - Close_{t-1}|, \, |Low_t - Close_{t-1}| \right)$$
For the initial bar ($t = 0$):
$$TR_0 = High_0 - Low_0$$

### Average True Range ($ATR_t$)
Using period $P$ and moving average type (default: Simple Moving Average):
$$ATR_t = \frac{1}{P} \sum_{i=0}^{P-1} TR_{t-i}$$

### Trailing Stop Transitions

#### Initialization / Warm-up ($t \le P$)
- $Trend_t = \text{Bullish}$
- $Lower_t = Close_t - M \cdot ATR_t$
- $Upper_t = Close_t + M \cdot ATR_t$

#### Trend Flip Conditions ($t > P$)
1. **Bearish to Bullish Flip** (triggered when price penetrates upper resistance):
   $$\text{If } Trend_{t-1} = \text{Bearish} \quad \text{and} \quad Close_t > Upper_{t-1}:$$
   $$Trend_t = \text{Bullish}, \quad Lower_t = Close_t - M \cdot ATR_t, \quad Flip_t = +1$$

2. **Bullish to Bearish Flip** (triggered when price breaches lower support):
   $$\text{Else if } Trend_{t-1} = \text{Bullish} \quad \text{and} \quad Close_t < Lower_{t-1}:$$
   $$Trend_t = \text{Bearish}, \quad Upper_t = Close_t + M \cdot ATR_t, \quad Flip_t = -1$$

#### Continuing Trend & Ratchet Conditions
3. **Continuing Bullish Trend**:
   $$Candidate_1 = Close_t - M \cdot ATR_t$$
   $$Candidate_2 = Lower_{t-1} + M \cdot (ATR_{t-1} - ATR_t)$$
   $$Lower_t = \max\left( Lower_{t-1}, \, Candidate_1, \, Candidate_2 \right)$$
   $$Trend_t = \text{Bullish}, \quad Flip_t = 0$$

4. **Continuing Bearish Trend**:
   $$Candidate_1 = Close_t + M \cdot ATR_t$$
   $$Candidate_2 = Upper_{t-1} - M \cdot (ATR_{t-1} - ATR_t)$$
   $$Upper_t = \min\left( Upper_{t-1}, \, Candidate_1, \, Candidate_2 \right)$$
   $$Trend_t = \text{Bearish}, \quad Flip_t = 0$$

---

## 3. Lookback & Warm-up Requirements
- **Minimum Bars Needed**: Equal to $P$ (`period`, default: 20).
- **Execution Bar Model**: On Bar Close (`Calculate = OnBarClose` / closed candle). Intrabar calculations should track against closed bar stops or handle real-time tick updates without prematurely freezing state.

---

## 4. Cross-Platform Porting Caveats

| Platform | Smoothing Difference | Plotting Nuance |
| :--- | :--- | :--- |
| **cTrader** | Default ATR is `MovingAverageType.Simple` (SMA). | Use `PlotType.DiscontinuousLine` for both BullishStop and BearishStop to create gap breaks on flips. |
| **TradingView (Pine v5)** | `ta.atr(p)` uses Wilder's Smoothing (`RMA`). To match standard cTrader exactly, use `ta.sma(ta.tr, p)`. | Use `plot(bull, style=plot.style_linebr)` so lines disconnect smoothly on trend change. |
| **NinjaTrader 8** | `ATR(Period)` uses SMA. | Use `Values[0].Reset()` on non-active plot series to prevent connecting lines across flips. |
| **Python / Backtesting** | Requires sequential loop or numba due to stateful dependency on $Lower_{t-1}$ and $Upper_{t-1}$. | Keep boolean trend series aligned with bar close index. |

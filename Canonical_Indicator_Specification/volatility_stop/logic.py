"""
Canonical Indicator Specification (CIS) - Core Mathematical Engine
Indicator: Volatility Stop (VStop)
Category: Volatility / Trend
Platform-Independent Vectorized Python / NumPy Implementation
"""

import numpy as np
import pandas as pd
from typing import Dict, Any, Tuple, Optional


def compute_true_range(high: np.ndarray, low: np.ndarray, close: np.ndarray) -> np.ndarray:
    """Compute True Range across arrays."""
    n = len(close)
    tr = np.zeros(n, dtype=float)
    if n == 0:
        return tr

    tr[0] = high[0] - low[0]
    for i in range(1, n):
        hl = high[i] - low[i]
        hc = abs(high[i] - close[i - 1])
        lc = abs(low[i] - close[i - 1])
        tr[i] = max(hl, hc, lc)
    return tr


def compute_atr(tr: np.ndarray, period: int = 20, ma_type: str = "SMA") -> np.ndarray:
    """Compute ATR using SMA, EMA, or Wilder/RMA smoothing."""
    n = len(tr)
    atr = np.full(n, np.nan, dtype=float)
    if n < period:
        return atr

    ma_type_upper = ma_type.upper()
    if ma_type_upper == "EMA":
        # Exponential Moving Average
        alpha = 2.0 / (period + 1.0)
        atr[period - 1] = np.mean(tr[:period])
        for i in range(period, n):
            atr[i] = alpha * tr[i] + (1.0 - alpha) * atr[i - 1]
    elif ma_type_upper in ["RMA", "WILDER"]:
        # Wilder's Smoothing (TradingView ta.atr default)
        alpha = 1.0 / period
        atr[period - 1] = np.mean(tr[:period])
        for i in range(period, n):
            atr[i] = alpha * tr[i] + (1.0 - alpha) * atr[i - 1]
    else:
        # Default SMA (cTrader MovingAverageType.Simple default)
        cumsum = np.cumsum(np.insert(tr, 0, 0.0))
        atr[period - 1:] = (cumsum[period:] - cumsum[:-period]) / period

    return atr


def compute_volatility_stop(
    high: np.ndarray,
    low: np.ndarray,
    close: np.ndarray,
    period: int = 20,
    multiplier: float = 3.0,
    ma_type: str = "SMA"
) -> Dict[str, np.ndarray]:
    """
    Compute Volatility Stop with dynamic volatility ratchet.

    Parameters:
        high (np.ndarray): High prices
        low (np.ndarray): Low prices
        close (np.ndarray): Close prices
        period (int): ATR lookback period (default: 20)
        multiplier (float): ATR multiplier (default: 3.0)
        ma_type (str): Moving average type ('SMA', 'EMA', 'RMA')

    Returns:
        Dict[str, np.ndarray]:
            - 'stop': Continuous active trailing stop value
            - 'is_bullish': Boolean mask (True = Uptrend, False = Downtrend)
            - 'bullish_stop': Lower stop during uptrend, NaN during downtrend
            - 'bearish_stop': Upper stop during downtrend, NaN during uptrend
            - 'trend_flip': 1 on bullish flip, -1 on bearish flip, 0 otherwise
    """
    n = len(close)
    tr = compute_true_range(high, low, close)
    atr = compute_atr(tr, period=period, ma_type=ma_type)

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

        # Warmup phase
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
            cand1 = c0 - multiplier * a0
            cand2 = prev_lower + multiplier * (a1 - a0)
            curr_lower = max(prev_lower, cand1, cand2)
        else:
            # Trailing Downtrend with Ratchet
            cand1 = c0 + multiplier * a0
            cand2 = prev_upper - multiplier * (a1 - a0)
            curr_upper = min(prev_upper, cand1, cand2)

        is_bullish[i] = curr_trend
        lower[i] = curr_lower
        upper[i] = curr_upper

        if curr_trend:
            bullish_stop[i] = curr_lower
        else:
            bearish_stop[i] = curr_upper

    stop = np.where(is_bullish, lower, upper)

    return {
        "stop": stop,
        "is_bullish": is_bullish,
        "bullish_stop": bullish_stop,
        "bearish_stop": bearish_stop,
        "trend_flip": trend_flip
    }

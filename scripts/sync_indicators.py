#!/usr/bin/env python3
"""
Sync & Compile Script: Single Source of Truth for Indicators
Generates all platform files (cTrader, TradingView, NT8) whenever the core indicator changes.
"""

import os
import sys

# Add python indicators path
base_dir = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.append(os.path.join(base_dir, "Indicators", "Python"))

import volatility_stop

def sync_volatility_stop():
    print("🔄 Compiling Volatility Stop to all platforms from Python source...")

    ctrader_path = os.path.join(base_dir, "Indicators", "cTrader", "VolatilityStop.cs")
    pine_path = os.path.join(base_dir, "Indicators", "TradingView", "volatility_stop.pine")
    nt8_path = os.path.join(base_dir, "Indicators", "NinjaTrader8", "VolatilityStop.cs")
    mt4_path = os.path.join(base_dir, "Indicators", "MT4", "VolatilityStop.mq4")
    mt5_path = os.path.join(base_dir, "Indicators", "MT5", "VolatilityStop.mq5")

    # Write cTrader
    with open(ctrader_path, "w") as f:
        f.write(volatility_stop.to_ctrader())
    print(f"  ✅ Updated cTrader: {ctrader_path}")

    # Write TradingView
    with open(pine_path, "w") as f:
        f.write(volatility_stop.to_pinescript())
    print(f"  ✅ Updated TradingView: {pine_path}")

    # Write NinjaTrader 8
    with open(nt8_path, "w") as f:
        f.write(volatility_stop.to_ninjatrader8())
    print(f"  ✅ Updated NinjaTrader 8: {nt8_path}")

    # Write MetaTrader 4
    with open(mt4_path, "w") as f:
        f.write(volatility_stop.to_mt4())
    print(f"  ✅ Updated MetaTrader 4: {mt4_path}")

    # Write MetaTrader 5
    with open(mt5_path, "w") as f:
        f.write(volatility_stop.to_mt5())
    print(f"  ✅ Updated MetaTrader 5: {mt5_path}")

    print("✨ Volatility Stop synced across all 5 platforms!")


def sync_hidden_divergence():
    print("\n🔄 Compiling Hidden Divergence to all platforms from Python source...")
    import hidden_divergence

    ctrader_path = os.path.join(base_dir, "Indicators", "cTrader", "MKHiddenDivergence.cs")
    pine_path = os.path.join(base_dir, "Indicators", "TradingView", "hidden_divergence.pine")
    nt8_path = os.path.join(base_dir, "Indicators", "NinjaTrader8", "MKHiddenDivergence.cs")
    mt4_path = os.path.join(base_dir, "Indicators", "MT4", "MKHiddenDivergence.mq4")
    mt5_path = os.path.join(base_dir, "Indicators", "MT5", "MKHiddenDivergence.mq5")

    with open(ctrader_path, "w") as f:
        f.write(hidden_divergence.to_ctrader())
    print(f"  ✅ Updated cTrader: {ctrader_path}")

    with open(pine_path, "w") as f:
        f.write(hidden_divergence.to_pinescript())
    print(f"  ✅ Updated TradingView: {pine_path}")

    with open(nt8_path, "w") as f:
        f.write(hidden_divergence.to_ninjatrader8())
    print(f"  ✅ Updated NinjaTrader 8: {nt8_path}")

    with open(mt4_path, "w") as f:
        f.write(hidden_divergence.to_mt4())
    print(f"  ✅ Updated MetaTrader 4: {mt4_path}")

    with open(mt5_path, "w") as f:
        f.write(hidden_divergence.to_mt5())
    print(f"  ✅ Updated MetaTrader 5: {mt5_path}")

    print("✨ Hidden Divergence synced across all 5 platforms!")


if __name__ == "__main__":
    target = sys.argv[1].lower() if len(sys.argv) > 1 else "all"

    if target in ["all", "vstop", "volatility", "volatility_stop"]:
        sync_volatility_stop()

    if target in ["all", "hd", "divergence", "hidden_divergence"]:
        sync_hidden_divergence()


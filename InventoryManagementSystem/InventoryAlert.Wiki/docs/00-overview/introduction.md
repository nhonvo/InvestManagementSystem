# InventoryAlert — Introduction

> A real-time stock inventory and alert system built on .NET 10 + Next.js 15.

InventoryAlert is a full-stack Inventory Management System that monitors stock prices via the Finnhub API and automatically dispatches alerts when user-defined thresholds are triggered.

## What Problem Does It Solve?

Users who maintain a portfolio of stock watchlists need to know immediately when prices hit critical levels — without constantly checking manually. InventoryAlert automates this through background jobs, scheduled workers, and event-driven webhooks.

## Core Capabilities

| Feature | Description |
|---|---|
| 🔍 **Watchlist Management** | Track multiple symbols across your portfolio |
| ⚡ **Alert Rules** | Define threshold conditions to trigger real-time notifications |
| 📈 **Price Sync** | Automatic price updates via Finnhub every scheduled interval |
| 🔔 **Alert Dispatch** | Telegram notification when alert conditions are met |
| 🧮 **Market Status** | Detects market open/closed hours to suppress noise |
| 🗂️ **Audit Log** | Persistent log of alert events and price history |

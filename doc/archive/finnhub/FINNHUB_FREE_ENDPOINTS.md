# 🆓 Finnhub Free-Tier Endpoints

This document lists all the endpoints available on the Finnhub Free/Personal tier (excluding "Premium" and "Enterprise" labels).

---

## 🏛️ General & Symbols

| Endpoint           | URL Path                | Description                                           |
| :----------------- | :---------------------- | :---------------------------------------------------- |
| **Symbol Lookup**  | `/search`               | Search for best-matching symbols (Name, ISIN, Cusip). |
| **Stock Symbols**  | `/stock/symbol`         | List supported stocks by exchange.                    |
| **Market Status**  | `/stock/market-status`  | Get current market status for global exchanges.       |
| **Market Holiday** | `/stock/market-holiday` | Get a list of holidays for global exchanges.          |

## 🏢 Company Data

| Endpoint                   | URL Path                      | Description                                             |
| :------------------------- | :---------------------------- | :------------------------------------------------------ |
| **Company Profile 2**      | `/stock/profile2`             | General info (Logo, Market Cap, IPO, WebURL, Industry). |
| **Basic Financials**       | `/stock/metric`               | Ratios like P/E, Margin, 52-Week High/Low.              |
| **Financials As Reported** | `/stock/financials-reported`  | Data as reported to the SEC (Annual/Quarterly).         |
| **Insider Transactions**   | `/stock/insider-transactions` | Latest insider buying/selling (Last 100).               |
| **Peers**                  | `/stock/peers`                | List of peers in the same country/sector.               |

## 📈 Price & Estimates

| Endpoint                  | URL Path                | Description                                                               |
| :------------------------ | :---------------------- | :------------------------------------------------------------------------ |
| **Quote**                 | `/quote`                | **Real-time quote for US stocks** (Current, High, Low, Open, Prev Close). |
| **Earnings Surprises**    | `/stock/earnings`       | Historical quarterly earnings surprise (Last 4 quarters free).            |
| **Earnings Calendar**     | `/calendar/earnings`    | Upcoming earnings release (1 month history/update on free tier).          |
| **IPO Calendar**          | `/calendar/ipo`         | Recent and upcoming IPOs.                                                 |
| **Recommendation Trends** | `/stock/recommendation` | Latest analyst recommendation trends.                                     |

## 📰 News

| Endpoint         | URL Path        | Description                                            |
| :--------------- | :-------------- | :----------------------------------------------------- |
| **Market News**  | `/news`         | Latest market news (General, Forex, Crypto, Merger).   |
| **Company News** | `/company-news` | Latest company-specific news (1 year historical free). |

## 💹 Forex & Crypto

| Endpoint             | URL Path           | Description                                |
| :------------------- | :----------------- | :----------------------------------------- |
| **Forex Exchanges**  | `/forex/exchange`  | List supported forex exchanges.            |
| **Forex Symbols**    | `/forex/symbol`    | List supported forex symbols by exchange.  |
| **Crypto Exchanges** | `/crypto/exchange` | List supported crypto exchanges.           |
| **Crypto Symbols**   | `/crypto/symbol`   | List supported crypto symbols by exchange. |

---

### ⚠️ Free Tier Limits

- **Rate Limit:** 60 API calls per minute.
- **Global Limit:** 30 API calls per second (system-wide).
- **History:** Generally limited (e.g., 1 year for news, 4 quarters for earnings).

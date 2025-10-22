# ⚓ Dock Exports Mod – Schedule I (MelonLoader + Harmony)

> “Heard you control the docks. I move product overseas.” – The Broker

---

## 📖 Overview

**Dock Exports** expands Schedule I’s economy by turning the **Docks property** into a new business system for late-game players.
Once the player reaches **Rank Hustler III (≈ Level 13)** and owns the **Docks**, a mysterious **Broker** contacts them to propose an overseas trade partnership.

This unlocks the **Dock Exports** phone app, offering two distinct routes to profit:

* **Wholesale:** safe, instant payment, limited quantity.
* **Consignment:** riskier, more lucrative, paid weekly with a chance of shipment loss.

Both paths are balanced for long-term play and designed to feel like natural extensions of Schedule I’s world.

---

## 🧭 Unlock Conditions

| Requirement | Description                                            |
| ----------- | ------------------------------------------------------ |
| Rank        | Hustler III (≈ Level 13)                               |
| Property    | Docks owned                                            |
| Trigger     | Broker text message → unlocks “Dock Exports” phone app |

**Intro Dialogue**

> **Broker:** “Heard you control the docks. I move product overseas.”  
> **Player:** “Details?”  
> **Broker:** “Two ways to move weight. Wholesale: quick cash, safe. Consignment: more product, more money, but spread out and risky.”  
> **Player:** “I’m in.”

---

## 💼 Export Options

> 💬 *All monetary examples below are based on the current in-game fair price for the popular 8-mix coke setup (735 × 20 = 14 700 per brick).*

### 🟢 Wholesale Example (Safe Route, but Locked at Fair Price)

| Parameter       | Value                          |
| --------------- | ------------------------------ |
| Quantity Cap    | **100 bricks**                 |
| Price per brick | Fair price = 735 × 20 = 14 700 |
| Total Value     | **1 470 000**                  |
| Payout          | Instant, on confirmation       |
| Cooldown        | Once every 30 in-game days     |

> ✅ Low-risk, reliable income.

---

### 🔵 Consignment Example (Risky Route, but 1.6 × the Fair Price)

| Parameter       | Value                                        |
| --------------- | -------------------------------------------- |
| Quantity Cap    | **200 bricks**                               |
| Price per brick | 1.6 × 14 700 = 23 520                        |
| Total (no loss) | **4 704 000**                                |
| Payout          | 25 % each Friday (4 weeks total)             |
| Risk            | 25 % weekly chance of 15–60 % loss that week |

**Expected Weekly Payout (no loss):**
`4 704 000 × 25 % = 1 176 000`

---

### 📉 Loss Roll Example

| Week | Roll    | Loss % | Payout     | Broker Message                                                                                                              |
| ---- | ------- | ------ | ---------- | --------------------------------------------------------------------------------------------------------------------------- |
| 1    | No loss | —      | $1 176 000 | “Week 1 cleared, $1 176 000 released.”                                                                                      |
| 2    | Loss    | 37 %   | $740 880   | “Customs flagged a container. 37 % of your shipment didn’t make it. You received $740 880 instead of $1 176 000 this week.” |
| 3    | No loss | —      | $1 176 000 | “Shipment arrived without issue. $1 176 000 sent.”                                                                          |
| 4    | Loss    | 22 %   | $917 280   | “Shipment delayed in transit. 22 % of your shipment didn’t make it. You received $917 280 instead of $1 176 000 this week.” |

**Total:** ≈ $4 010 000 (≈ 2.7 × wholesale value, still worth it)

---

### 💲 Profit Floor Rule

If the total consignment payout ends below the safe wholesale total ($1 470 000),
the Broker provides a top-up to match it.

> **Broker:** “Tough month, but I made sure you didn’t take a total loss. Let’s call it even.”

---

## ⚖️ Expected Outcomes

| Scenario                       | Total Payout (≈)           | vs Wholesale | Notes                       |
| ------------------------------ | -------------------------- | ------------ | --------------------------- |
| Best Case (no loss)            | 4.704 M                    | × 3.2        | Perfect run                 |
| Typical (25 % loss chance)     | 4.26 M                     | × 2.9        | Balanced risk               |
| Heavy Loss (50 % chance)       | 3.82 M                     | × 2.6        | Still better than wholesale |
| Extreme (60 % loss every week) | 1.88 M → floored to 1.47 M | × 1.0        | Floor protection            |

Consignment remains the most profitable path on average, even with setbacks.

---

## 📱 Phone App Design

**Tabs**

1. **Create Shipment:** Select route and quantity
2. **Active Shipments:** Track progress and next payout countdown
3. **History:** View completed shipments and loss records

**Style**

* Industrial dock theme (steel blues and crane silhouettes)
* Broker portrait icon (black figure on container yard backdrop)
* Subtle sound on weekly payout notifications

---

## 💬 Broker Message Samples

**Normal Week**

> “Week cleared, $1 176 000 released.”
> “Shipment arrived without issue. $1 176 000 sent.”

**Loss Events**

> - “Customs flagged a container. 37 % of your shipment didn’t make it. You received $740 880 instead of $1 176 000 this week.”  
> - “Shipment delayed in transit. 24 % of product spoiled. You received $893 760 instead of $1 176 000 this week.”  
> - “Port inspection found discrepancies. 41 % lost. You received $693 840 instead of $1 176 000 this week.” 
> - “Crew shorted your cut. 15 % underdelivered. You received $999 600 instead of $1 176 000 this week.”

---

## ⚙️ Integration Hooks

| Hook                  | Purpose                              |
| --------------------- | ------------------------------------ |
| Rank + Property Check | Unlocks Broker contact and first SMS |
| Phone App Render      | Adds Dock Exports icon after unlock  |
| Shipment Creation     | Calculates caps and sets schedule    |
| Weekly Tick           | Handles loss roll and payout events  |
| Floor Check           | Guarantees minimum payout            |
| Cooldown Tracker      | Enforces 30-day wholesale limit      |
| Save/Load             | Stores active deals and cooldowns    |

---

## 🔧 Tunable Parameters

| Parameter               | Default            | Description          |
| ----------------------- | ------------------ | -------------------- |
| `RequiredRank`          | Hustler III (≈ 13) | Unlock threshold     |
| `WholesaleCap`          | 100 bricks         | Safe limit           |
| `ConsignmentCap`        | 200 bricks         | Risk limit           |
| `WholesaleCooldown`     | 30 days            | Monthly limit        |
| `ConsignmentMultiplier` | 1.6                | Price boost          |
| `WeeklyLossChance`      | 0.25               | 25 % chance          |
| `LossRange`             | 15–60 %            | Loss severity        |
| `Installments`          | 4                  | Weekly payouts       |
| `ConsignmentFloor`      | same as wholesale  | Minimum total return |

---

## 🗺️ Development Roadmap

### Phase 1: Core System

* [ ] Rank + property check trigger
* [ ] Broker intro SMS and phone app unlock
* [ ] Wholesale mode and 30-day timer

### Phase 2: Consignment Mechanics

* [ ] Weekly loss roll logic and payout scheduler
* [ ] Broker payout messages (show expected vs actual)
* [ ] Profit floor implementation

### Phase 3: UI & Feedback

* [ ] Phone app (3 tabs and dock theme)
* [ ] Broker portrait and notification sounds
* [ ] Save/Load support for active shipments

### Phase 4: Balance & Expansion

* [ ] Tune brick caps and pricing
* [ ] Add dynamic Broker dialogue (**Loyalty System**)
* [ ] Future routes (Airport Connect, Downtown Exports)

---

## 🤝 Future Features

### Loyalty System (Planned)

The Broker will track the player’s successful consignments over time.
Higher loyalty reduces loss odds and increases negotiated price caps.

| Tier     | Completed Consignments | Bonus Effect                        |
| -------- | ---------------------- | ----------------------------------- |
| Bronze   | 0–2                    | Base risk (25 %)                    |
| Silver   | 3–5                    | Loss chance −5 %                    |
| Gold     | 6–9                    | Loss chance −10 %, max price × 1.65 |
| Platinum | 10 +                   | Loss chance −15 %, special dialogue |

---

## 🎯 Design Goals

* Keep economy balanced (100 / 200 brick caps).
* Make Consignment profitable yet risky.
* Reward late-game players without breaking progression.
* Deliver everything in-world via SMS and app UI.
* Maintain player trust with a profit floor.

---

### 📊 Economy Impact Summary

> *All totals below assume the current in-game fair price for the 8-mix coke setup (735 × 20 = 14 700 per brick).*

| Weekly Loss Chance | Expected Consignment Total | vs Wholesale (1.47 M) |
| ------------------ | -------------------------- | --------------------- |
| 0 %                | 4.704 M                    | × 3.20                |
| 10 %               | 4.528 M                    | × 3.08                |
| 25 %               | 4.263 M                    | × 2.90                |
| 40 %               | 3.998 M                    | × 2.72                |
| 50 %               | 3.822 M                    | × 2.60                |
| 75 %               | 3.381 M                    | × 2.30                |
| 100 %              | 2.940 M                    | × 2.00                |

**Worst Case (not expectation):** 60 % loss each week → 4.704 M × 40 % = **1.882 M** (about × 1.28 wholesale).
The floor would only trigger if future tuning reduced totals below below Wholesale, (1.47 M, in this scenario)

---

## 🧩 Technical Notes

* Built for **Schedule I (Il2Cpp)** using **MelonLoader 0.7.0 + Harmony**
* Compatible with **S1API v1.6.2 or later**
* Development recommended in **Visual Studio Code** with .NET SDK 7 installed

---
## 🧰 Installation
1. Install [MelonLoader 0.7.0+](https://melonwiki.xyz/#/?id=automated-installation)
2. Install [S1API v1.6.2+](https://github.com/ifBars/S1APITemplate/releases)
3. Place `S1DockExports.dll` in `Mods/`
4. Launch the game and check your phone for the **Dock Exports** app.

### 🧠 Hoping to Learn and Explore

This mod is both a gameplay system and a learning tool. It demonstrates how risk mechanics, probability, and narrative design can coexist naturally inside a Unity-based sandbox world while allowing me to grow as a game developer.

---

**Credit: Grateful thanks to [@ifBars](https://github.com/ifBars/S1APITemplate) for providing the S1API Template and Tutorial that made this project possible.**
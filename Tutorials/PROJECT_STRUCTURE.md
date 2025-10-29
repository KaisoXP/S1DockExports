# Project Structure & Architecture Guide

> **How S1DockExports is Organized and Why**

**Author:** Claude Code + KaisoXP
**Date:** October 25, 2025
**Project:** S1DockExports
**Audience:** Developers extending or learning from this codebase

---

## Table of Contents

1. [Overview](#overview)
2. [Current File Structure](#current-file-structure)
3. [Architecture Principles](#architecture-principles)
4. [Folder Breakdown](#folder-breakdown)
5. [Namespace Strategy](#namespace-strategy)
6. [Class Responsibilities](#class-responsibilities)
7. [Dependency Flow](#dependency-flow)
8. [Recommended Scaffolding for Growth](#recommended-scaffolding-for-growth)
9. [Naming Conventions](#naming-conventions)
10. [Adding New Features](#adding-new-features)

---

## Overview

S1DockExports is structured using **separation of concerns**, where each part of the code has a clear, single purpose.

**Think of it like a restaurant:**
- **Entry Point** (`DockExportsMod.cs`) = Restaurant manager
- **Services** (`ShipmentManager.cs`) = Kitchen (business logic)
- **Integrations** (Harmony patches) = Waiters (interface with game)
- **Config** (`DockExportsConfig.cs`) = Recipe book (constants & settings)
- **UI** (`DockExportsApp.cs`) = Dining room (player interface)

---

## Current File Structure

```
S1DockExports/
├── 📁 Integrations/                 ← Game integration layer
│   ├── PhoneAppInjector.cs          │ Harmony patches for phone app injection
│   ├── GameAccessPatches.cs         │ Direct game system access (workarounds)
│   └── HarmonyPatches.cs            │ Base Harmony infrastructure
│
├── 📁 Services/                     ← Business logic layer
│   └── ShipmentManager.cs           │ Shipment creation, processing, persistence
│
├── 📁 Tutorials/                    ← Documentation
│   ├── MODDING_TUTORIAL.md          │ Complete modding guide
│   ├── LOGGING_GUIDE.md             │ Logging best practices
│   ├── ICON_LOADING_TUTORIAL.md     │ Image loading specifics
│   ├── PROJECT_STRUCTURE.md         │ This file!
│   └── CSPROJ_EXPLAINED.md          │ Build system guide
│
├── 📁 Utils/                        ← Utility helpers (currently empty)
│   └── Constants.cs                 │ (Reserved for shared constants)
│
├── 📄 DockExportsMod.cs             ← Main entry point & lifecycle
├── 📄 DockExportsApp.cs             ← Phone app UI implementation
├── 📄 DockExportsConfig.cs          ← Configuration & constants
├── 📄 DE.png                        ← Embedded icon resource
├── 📄 S1DockExports.csproj          ← Build configuration
├── 📄 CLAUDE.md                     ← Instructions for Claude Code
└── 📄 README.md                     ← Project overview & features
```

---

## Architecture Principles

### 1. **Separation of Concerns**

Each file/class has ONE primary responsibility:

✅ **Good:** `ShipmentManager` handles ALL shipment logic
❌ **Bad:** Shipment logic scattered across 5 different files

### 2. **Dependency Direction**

Dependencies flow in ONE direction:

```
UI Layer (DockExportsApp)
    ↓
Business Logic (ShipmentManager)
    ↓
Integration Layer (GameAccessPatches)
    ↓
Game Systems (TimeManager, etc.)
```

**Rule:** Higher layers can depend on lower layers, but NEVER the reverse.

❌ **Bad:** `ShipmentManager` directly references `DockExportsApp`
✅ **Good:** `DockExportsApp` calls `ShipmentManager` methods

### 3. **Loose Coupling via Events**

When lower layers need to notify higher layers, use **events**:

```csharp
// ShipmentManager (low layer)
public static event Action? OnShipmentsLoaded;
protected override void OnLoaded()
{
    OnShipmentsLoaded?.Invoke(); // Notify anyone listening
}

// DockExportsApp (high layer)
protected override void OnCreated()
{
    ShipmentManager.OnShipmentsLoaded += RefreshUI; // Listen
}
```

### 4. **Configuration Centralization**

All magic numbers and settings go in **one place**: `DockExportsConfig.cs`

❌ **Bad:**
```csharp
// In ShipmentManager.cs
if (quantity > 100) // What is 100?

// In DockExportsApp.cs
text.text = "Cap: 100 bricks"; // Duplicated!
```

✅ **Good:**
```csharp
// In DockExportsConfig.cs
public const int WHOLESALE_CAP = 100;

// In ShipmentManager.cs
if (quantity > DockExportsConfig.WHOLESALE_CAP)

// In DockExportsApp.cs
text.text = $"Cap: {DockExportsConfig.WHOLESALE_CAP} bricks";
```

Now changing the cap only requires editing ONE place!

---

## Folder Breakdown

### `Integrations/` - Game Integration Layer

**Purpose:** All code that directly interacts with the game or modifies game behavior.

**Contents:**
- **Harmony patches** (intercept game methods)
- **Direct game access** (workarounds for broken APIs)
- **Game object manipulation** (UI injection, scene modification)

**Why separate?**
- Game APIs change with updates → isolated impact
- Easy to find all game-specific code
- Clear boundary between "our code" and "game code"

**Example files:**
- `PhoneAppInjector.cs` - Patches `HomeScreen.Start()` to add our app icon
- `GameAccessPatches.cs` - Direct access to `TimeManager`, `LevelManager`, etc.

### `Services/` - Business Logic Layer

**Purpose:** Core mod functionality that's independent of the game's UI or specific APIs.

**Contents:**
- **Managers** (ShipmentManager, future: BrokerManager, LoyaltyManager)
- **Data models** (structs, enums, data classes)
- **Algorithms** (pricing calculations, loss rolls, cooldown tracking)
- **Persistence** (save/load logic)

**Why separate?**
- Reusable across different UIs (phone app, debug menu, etc.)
- Testable without running the game
- Clear "source of truth" for business rules

**Example files:**
- `ShipmentManager.cs` - Manages all shipment state, processing, and persistence

### `Utils/` - Utility Helpers

**Purpose:** Shared helper functions used across the mod.

**Contents:**
- Math helpers
- String formatting
- Extension methods
- Shared constants (if not in Config)

**Currently:**
- `Constants.cs` exists but is empty (reserved for future use)

**Future additions:**
- `StringExtensions.cs` - Custom string formatting
- `MathHelpers.cs` - Price calculations, probability helpers

### Root Files - Entry Point & Core Components

**`DockExportsMod.cs`** - The Restaurant Manager
- Mod entry point (MelonMod subclass)
- Lifecycle coordination (OnInitializeMelon, OnUpdate, etc.)
- Unlocking logic
- High-level event orchestration (Friday payouts, unlock checks)

**`DockExportsApp.cs`** - The Phone App UI
- Extends `S1API.PhoneApp`
- Three tabs: Create, Active, History
- UI building and refresh logic
- Event handlers for button clicks

**`DockExportsConfig.cs`** - The Recipe Book
- All tunable constants
- Broker message templates
- Color definitions for UI
- Helper methods (price calculations, message formatting)

**`DE.png`** - The Icon
- Embedded resource (256x256 PNG)
- Displayed on phone app icon

---

## Namespace Strategy

S1DockExports uses **hierarchical namespaces** to organize code:

```csharp
// Root namespace
namespace S1DockExports
{
    public class DockExportsMod { }
    public class DockExportsApp { }
    public class DockExportsConfig { }
}

// Services namespace
namespace S1DockExports.Services
{
    public class ShipmentManager { }
    public class BrokerManager { }  // Future
}

// Integrations namespace
namespace S1DockExports.Integrations
{
    public static class PhoneAppInjector { }
    public static class GameAccess { }
}

// Utils namespace
namespace S1DockExports.Utils
{
    public static class PriceHelpers { }  // Future
}
```

**Benefits:**
- Clear organizational hierarchy
- Avoid name collisions
- IntelliSense groups related classes
- Easier to find code

**Convention:**
- **Root namespace** = Core/entry classes
- **Namespace = Folder** (namespace matches folder structure)

---

## Class Responsibilities

Let's break down what each class does and why:

### Entry Point & Lifecycle

#### `DockExportsMod` (Main mod class)

**Responsibility:** Orchestrate the entire mod.

**Key Methods:**
- `OnInitializeMelon()` - Initialize mod systems
- `OnApplicationStart()` - Early setup (before game loads)
- `OnSceneWasLoaded()` - Respond to scene changes
- `OnUpdate()` - Check conditions every frame (throttled)
- `CanUnlock()` - Validate unlock requirements
- `ProcessConsignmentWeek()` - Friday payout logic

**Dependencies:**
- `ShipmentManager` - Business logic
- `GameAccess` - Game system access
- `DockExportsConfig` - Settings

**Pattern:** **Coordinator/Orchestrator**
- Doesn't do heavy lifting itself
- Delegates to specialists (ShipmentManager, GameAccess)
- Responds to lifecycle events

### UI Layer

#### `DockExportsApp` (Phone app)

**Responsibility:** Present shipment data to the player.

**Key Methods:**
- `OnCreatedUI()` - Build the phone app interface
- `BuildCreatePanel()` - Create shipment UI
- `BuildActivePanel()` - Active shipment display
- `BuildHistoryPanel()` - Shipment history
- `RefreshActivePanel()` - Update displayed data

**Dependencies:**
- `DockExportsMod` - Access to data (via properties)
- `ShipmentManager` - Listen to data loaded events
- `UIFactory` (S1API) - UI element creation

**Pattern:** **View/Presenter**
- Displays data, doesn't process it
- Reacts to user input (button clicks)
- Calls into business logic (ShipmentManager)

### Business Logic Layer

#### `ShipmentManager` (Services/)

**Responsibility:** ALL shipment-related logic.

**Key Methods:**
- `CreateWholesaleShipment()` - Validates and creates wholesale
- `CreateConsignmentShipment()` - Validates and creates consignment
- `ProcessWholesalePayment()` - Instant payout + cooldown
- `ProcessConsignmentPayment()` - Weekly payout + loss roll
- `OnLoaded()` / `OnCreated()` - Save system integration

**Dependencies:**
- `DockExportsConfig` - Constants (caps, multipliers)
- `S1API.Saveables` - Persistence framework

**Pattern:** **Manager/Service**
- Encapsulates all business rules
- Single source of truth for shipment state
- Provides clean API for other classes

**Data:**
- `_activeShipment` - Current shipment (nullable)
- `_history` - Past shipments
- `_lastProcessedDay` - Prevent double-processing
- `_wholesaleCooldownEndDay` - Cooldown tracking

### Integration Layer

#### `PhoneAppInjector` (Integrations/)

**Responsibility:** Inject our custom icon into the game's phone.

**Key Methods:**
- `InjectAppIcon()` - Harmony postfix on `HomeScreen.Start`
- `LoadIconSprite()` - Load embedded PNG resource
- `OpenDockExportsApp()` - Manually open our app UI

**Dependencies:**
- Game types: `HomeScreen`, Unity UI components
- `DockExportsApp` - The app to open

**Pattern:** **Adapter/Bridge**
- Bridges between game's phone system and our app
- Handles game-specific details (icon cloning, sprite replacement)

#### `GameAccess` (Integrations/)

**Responsibility:** Direct access to game systems (workaround for broken S1API).

**Key Methods:**
- `GetPlayerRank()` - Access `NetworkSingleton<LevelManager>`
- `GetCurrentDayOfWeek()` - Access `TimeManager.DayIndex % 7`
- `GetElapsedDays()` - Access `TimeManager.DayIndex`
- `IsPropertyOwned()` - Property ownership check (TODO)

**Dependencies:**
- Game types: `NetworkSingleton<T>`, `TimeManager`, `LevelManager`

**Pattern:** **Facade**
- Simplifies access to complex game systems
- Abstracts away NetworkSingleton boilerplate
- Centralizes game API workarounds

### Configuration

#### `DockExportsConfig` (Root)

**Responsibility:** Central repository for all constants and settings.

**Contents:**
- Caps: `WHOLESALE_CAP`, `CONSIGNMENT_CAP`
- Multipliers: `CONSIGNMENT_MULTIPLIER`
- Risk: `WEEKLY_LOSS_CHANCE`, `LOSS_MIN_PERCENT`, `LOSS_MAX_PERCENT`
- Colors: `Bg`, `Header`, `Accent`, `Success`, `Warning`
- Requirements: `REQUIRED_RANK`, `DOCKS_PROPERTY_ID`

**Pattern:** **Static Configuration Class**
- No instances created
- All `public static const` or `public static readonly`
- Grouped by logical sections (comments)

#### `BrokerMessages` (Inside DockExportsConfig.cs)

**Responsibility:** Generate broker dialogue messages.

**Key Methods:**
- `WholesaleConfirmed()` - Wholesale completion message
- `ConsignmentLocked()` - Consignment created message
- `WeekCleared()` - No loss messages
- `GetRandomLossMessage()` - Random loss flavor text

**Pattern:** **Static Helper Class**
- Pure functions (no state)
- String formatting and templating

---

## Dependency Flow

Here's how classes depend on each other:

```eraser
graph TD
  DockExportsMod[DockExportsMod<br/>Main Entry Point]
  DockExportsApp[DockExportsApp<br/>Phone App UI]
  ShipmentManager[ShipmentManager<br/>Business Logic]
  PhoneAppInjector[PhoneAppInjector<br/>Harmony Patches]
  GameAccess[GameAccess<br/>Direct Game Access]
  DockExportsConfig[DockExportsConfig<br/>Constants & Settings]
  S1API[S1API<br/>Framework]
  Game[Game Systems<br/>TimeManager, LevelManager, etc.]

  DockExportsMod --> ShipmentManager
  DockExportsMod --> GameAccess
  DockExportsMod --> DockExportsConfig

  DockExportsApp --> DockExportsMod
  DockExportsApp --> ShipmentManager
  DockExportsApp --> DockExportsConfig
  DockExportsApp --> S1API

  ShipmentManager --> DockExportsConfig
  ShipmentManager --> S1API

  PhoneAppInjector --> DockExportsApp
  PhoneAppInjector --> Game

  GameAccess --> Game

  S1API --> Game
```

**Key observations:**
- **Config** is depended on by everyone (central truth)
- **ShipmentManager** has no dependencies on UI (reusable!)
- **Game** is only accessed through `GameAccess` and `S1API` (isolation)
- **DockExportsMod** coordinates but doesn't do heavy lifting

---

## Recommended Scaffolding for Growth

As the mod grows, here's how to scale the structure:

### Option 1: Stay Flat (Current Approach)

✅ **Good for:** Small to medium mods (< 15 classes)

```
S1DockExports/
├── Integrations/
├── Services/
├── Utils/
├── DockExportsMod.cs
├── DockExportsApp.cs
└── DockExportsConfig.cs
```

### Option 2: Sub-Namespace by Feature (Recommended for Growth)

✅ **Good for:** Medium to large mods (15-50 classes)

```
S1DockExports/
├── 📁 Core/
│   ├── DockExportsMod.cs
│   └── DockExportsConfig.cs
│
├── 📁 Features/
│   ├── 📁 Shipments/
│   │   ├── ShipmentManager.cs
│   │   ├── ShipmentData.cs
│   │   └── ShipmentHistoryEntry.cs
│   │
│   ├── 📁 Broker/
│   │   ├── BrokerManager.cs
│   │   └── BrokerMessages.cs
│   │
│   └── 📁 Loyalty/
│       ├── LoyaltyManager.cs
│       └── LoyaltyTier.cs
│
├── 📁 UI/
│   ├── DockExportsApp.cs
│   ├── ShipmentCreationPanel.cs
│   ├── ActiveShipmentPanel.cs
│   └── HistoryPanel.cs
│
├── 📁 Integrations/
│   ├── PhoneAppInjector.cs
│   ├── GameAccessPatches.cs
│   └── HarmonyPatches.cs
│
└── 📁 Utils/
    ├── PriceHelpers.cs
    ├── StringExtensions.cs
    └── Constants.cs
```

**Namespaces:**
```csharp
namespace S1DockExports.Core { }
namespace S1DockExports.Features.Shipments { }
namespace S1DockExports.Features.Broker { }
namespace S1DockExports.UI { }
namespace S1DockExports.Integrations { }
namespace S1DockExports.Utils { }
```

### Option 3: Modular Architecture (For Very Large Mods)

✅ **Good for:** Large mods (50+ classes) or multiple developers

```
S1DockExports/
├── 📁 Core/
│   ├── DockExportsMod.cs
│   ├── ModConfig.cs
│   └── EventBus.cs  (central event dispatcher)
│
├── 📁 Modules/
│   ├── 📁 ShipmentModule/
│   │   ├── ShipmentModuleEntry.cs  (IModule interface)
│   │   ├── ShipmentManager.cs
│   │   ├── ShipmentUI.cs
│   │   └── ShipmentConfig.cs
│   │
│   ├── 📁 BrokerModule/
│   │   ├── BrokerModuleEntry.cs
│   │   ├── BrokerManager.cs
│   │   └── BrokerMessages.cs
│   │
│   └── 📁 LoyaltyModule/
│       ├── LoyaltyModuleEntry.cs
│       └── LoyaltyManager.cs
│
├── 📁 Shared/
│   ├── GameAccess.cs
│   └── UIFactory.cs
│
└── 📁 Integrations/
    └── HarmonyPatches.cs
```

Each module is self-contained and can be enabled/disabled.

---

## Naming Conventions

### Classes

**Pattern:** `PascalCase` + descriptive noun

```csharp
// Managers end in "Manager"
public class ShipmentManager { }
public class BrokerManager { }

// UI ends in "App" or "Panel"
public class DockExportsApp { }
public class ShipmentCreationPanel { }

// Configuration ends in "Config"
public class DockExportsConfig { }
public class ShipmentConfig { }

// Data structures: descriptive nouns
public struct ShipmentData { }
public class ShipmentHistoryEntry { }

// Static helpers: plural or descriptive
public static class BrokerMessages { }
public static class PriceHelpers { }
```

### Fields & Properties

**Pattern:**
- **Private fields:** `_camelCase` with underscore prefix
- **Public properties:** `PascalCase`
- **Constants:** `UPPER_SNAKE_CASE`

```csharp
public class Example
{
    // Private field
    private int _activeShipmentCount;

    // Public property
    public int ActiveShipmentCount => _activeShipmentCount;

    // Constant
    public const int MAX_SHIPMENTS = 10;
}
```

### Methods

**Pattern:** `PascalCase` + verb

```csharp
// Action methods: imperative verb
public void CreateShipment() { }
public void ProcessPayment() { }
public void SendMessage() { }

// Query methods: "Get" or "Is" prefix
public int GetCurrentDay() { }
public bool IsWholesaleOnCooldown() { }
public bool CanUnlock() { }

// Event handlers: "On" prefix
protected override void OnLoaded() { }
private void OnShipmentsLoaded() { }
```

### Events

**Pattern:** `PascalCase` + past tense verb

```csharp
public static event Action? OnShipmentsLoaded;
public static event Action<int>? OnPaymentProcessed;
public static event Action? OnBrokerUnlocked;
```

### Files

**Pattern:** Match class name exactly

```
ShipmentManager.cs   → public class ShipmentManager
DockExportsApp.cs    → public class DockExportsApp
```

**Exception:** Multiple related classes in one file

```
DockExportsConfig.cs
  ↳ public static class DockExportsConfig
  ↳ public static class BrokerMessages
  ↳ public static class PriceHelper
```

---

## Adding New Features

### Step-by-Step Guide

**Example:** Adding a Loyalty System

#### Step 1: Decide Where It Goes

**Question:** Is this UI, business logic, or game integration?

**Answer:** Business logic (tracks player's successful consignments)

**Location:** `Services/LoyaltyManager.cs`

#### Step 2: Create the Manager Class

```csharp
// Services/LoyaltyManager.cs
using S1API.Saveables;
using System.Collections.Generic;

namespace S1DockExports.Services
{
    /// <summary>
    /// Manages player loyalty tiers based on successful consignments
    /// </summary>
    public class LoyaltyManager : Saveable
    {
        [SaveableField("CompletedConsignments")]
        private int _completedConsignments = 0;

        public static LoyaltyManager Instance { get; private set; } = new LoyaltyManager();

        public LoyaltyTier CurrentTier => CalculateTier();

        public void RecordSuccessfulConsignment()
        {
            _completedConsignments++;
            MelonLoader.MelonLogger.Msg($"[DockExports] Loyalty: {_completedConsignments} consignments, tier: {CurrentTier}");
        }

        private LoyaltyTier CalculateTier()
        {
            if (_completedConsignments >= 10) return LoyaltyTier.Platinum;
            if (_completedConsignments >= 6) return LoyaltyTier.Gold;
            if (_completedConsignments >= 3) return LoyaltyTier.Silver;
            return LoyaltyTier.Bronze;
        }

        protected override void OnLoaded()
        {
            Instance = this;
        }
    }

    public enum LoyaltyTier
    {
        Bronze,
        Silver,
        Gold,
        Platinum
    }
}
```

#### Step 3: Add Configuration

```csharp
// DockExportsConfig.cs
public static class DockExportsConfig
{
    // ... existing constants ...

    // Loyalty System
    public const int SILVER_TIER_THRESHOLD = 3;
    public const int GOLD_TIER_THRESHOLD = 6;
    public const int PLATINUM_TIER_THRESHOLD = 10;

    public const float SILVER_LOSS_REDUCTION = 0.05f; // -5%
    public const float GOLD_LOSS_REDUCTION = 0.10f;   // -10%
    public const float PLATINUM_LOSS_REDUCTION = 0.15f; // -15%
}
```

#### Step 4: Integrate with Existing System

```csharp
// ShipmentManager.cs
public int ProcessConsignmentPayment(out int lossPercent)
{
    // ... existing code ...

    // Apply loyalty bonus
    float lossChance = DockExportsConfig.WEEKLY_LOSS_CHANCE;
    var loyaltyTier = LoyaltyManager.Instance.CurrentTier;

    switch (loyaltyTier)
    {
        case LoyaltyTier.Silver:
            lossChance -= DockExportsConfig.SILVER_LOSS_REDUCTION;
            break;
        case LoyaltyTier.Gold:
            lossChance -= DockExportsConfig.GOLD_LOSS_REDUCTION;
            break;
        case LoyaltyTier.Platinum:
            lossChance -= DockExportsConfig.PLATINUM_LOSS_REDUCTION;
            break;
    }

    // Roll for loss with adjusted chance
    lossPercent = UnityEngine.Random.value < lossChance
        ? UnityEngine.Random.Range(DockExportsConfig.LOSS_MIN_PERCENT, DockExportsConfig.LOSS_MAX_PERCENT + 1)
        : 0;

    // ... rest of payment processing ...

    // Record success if no loss
    if (lossPercent == 0 && shipment.PaymentsMade >= DockExportsConfig.CONSIGNMENT_INSTALLMENTS)
    {
        LoyaltyManager.Instance.RecordSuccessfulConsignment();
    }

    return actualPayout;
}
```

#### Step 5: Add UI (Optional)

```csharp
// DockExportsApp.cs
private void BuildCreatePanel(Transform parent)
{
    // ... existing code ...

    // Add loyalty display
    var loyaltyTier = LoyaltyManager.Instance.CurrentTier;
    UIFactory.Text("Loyalty_Status", $"Loyalty Tier: {loyaltyTier}", parent, 14, TextAnchor.MiddleLeft);

    // ... rest of panel ...
}
```

#### Step 6: Test & Iterate

1. Build and run
2. Check logs for loyalty messages
3. Verify tiers change after consignments
4. Confirm loss chance is reduced
5. Test save/load persistence

---

## Summary

### Key Takeaways

✅ **Organize by responsibility:** Each class has ONE clear job
✅ **Separate concerns:** UI, business logic, game integration
✅ **Centralize configuration:** One place for all constants
✅ **Use events for decoupling:** Low layers notify high layers
✅ **Match namespaces to folders:** Easy to navigate
✅ **Name consistently:** Readers know what to expect

### When Adding Code, Ask:

1. **What layer does this belong to?**
   - UI? → `DockExportsApp.cs` or new panel class
   - Business logic? → `Services/`
   - Game integration? → `Integrations/`

2. **Does it have dependencies?**
   - Depend on abstractions, not concrete implementations
   - Keep dependency direction consistent (high → low)

3. **Is it configurable?**
   - Magic numbers → `DockExportsConfig.cs`
   - Messages → `BrokerMessages`

4. **Does it need persistence?**
   - Yes → Extend `Saveable`, use `[SaveableField]`

5. **Will it grow?**
   - Consider splitting into sub-classes early

### Next Steps

- **Read the code:** Study existing classes with this structure in mind
- **Experiment:** Try adding a small feature following the patterns
- **Refactor:** If you find code in the wrong place, move it!

---

**See Also:**
- [MODDING_TUTORIAL.md](./MODDING_TUTORIAL.md) - Learn modding fundamentals
- [LOGGING_GUIDE.md](./LOGGING_GUIDE.md) - Master debugging
- [CSPROJ_EXPLAINED.md](./CSPROJ_EXPLAINED.md) - Understand the build system

**Questions?** Open an issue on GitHub or ask the community!

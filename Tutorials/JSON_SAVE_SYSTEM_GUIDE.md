# JSON-Based Save System Implementation Guide

This guide explains how to implement a JSON-based save system for S1DockExports that stores mod data separately from the game's save files.

## Table of Contents

1. [Overview](#overview)
2. [Project Structure Comparison](#project-structure-comparison)
3. [Implementation Approaches](#implementation-approaches)
4. [Approach A: S1API Saveable System](#approach-a-s1api-saveable-system)
5. [Approach B: Pure Custom JSON](#approach-b-pure-custom-json)
6. [Migration Guide](#migration-guide)
7. [Testing](#testing)

---

## Overview

### Why Separate Save Data?

The game's native save system stores all data together. Having mod data in a separate JSON file provides:

- **Independence**: Mod save data won't corrupt or interfere with game saves
- **Portability**: Easy to backup, transfer, or reset mod data independently
- **Debugging**: Simple to inspect and edit JSON files manually
- **Version Control**: Easier to migrate data when mod structure changes

### Two Approaches

1. **S1API Saveable System** (Recommended): Uses S1API's `ModSaveableRegistry` to manage separate JSON files in a `ModData/` folder structure. Still integrated with game save/load events.

2. **Pure Custom JSON**: Completely independent JSON file handling with manual save/load. Total separation from game save system.

---

## Project Structure Comparison

### Current Structure

```
S1DockExports/
├── DockExportsMod.cs          # Main mod entry, contains save logic
├── DockExportsApp.cs          # Phone app UI
├── DockExportsConfig.cs       # Configuration constants
└── Resources/
    └── DE.png                 # App icon
```

**Current Save Implementation:**
- Uses `SaveSystem.RegisterSaveHandler()` (undocumented API)
- Save data mixed with game's save system
- All logic in `DockExportsMod.cs`

```csharp
// Current approach (in DockExportsMod.cs)
SaveSystem.RegisterSaveHandler(
    "DockExports",
    () => JsonUtility.ToJson(new SaveData { /* ... */ }),
    (json) => { /* load logic */ }
);
```

### Proposed Structure (Approach A - S1API Saveable)

```
S1DockExports/
├── Core/
│   ├── DockExportsMod.cs      # Main mod entry (simplified)
│   └── ShipmentManager.cs     # NEW: Manages shipments & saves
├── Models/
│   ├── ShipmentData.cs        # NEW: Shipment data model
│   └── ShipmentHistoryEntry.cs # NEW: History entry model
├── UI/
│   └── DockExportsApp.cs      # Phone app UI (moved to UI folder)
├── DockExportsConfig.cs       # Configuration constants
└── Resources/
    └── DE.png                 # App icon
```

**Save File Location:**
```
Schedule I/
└── UserData/
    └── Saves/
        └── [SaveSlot]/
            └── ModData/
                └── DockExports/
                    └── DockExports.json    # Separate from game save!
```

### Proposed Structure (Approach B - Pure JSON)

```
S1DockExports/
├── Core/
│   ├── DockExportsMod.cs      # Main mod entry (simplified)
│   ├── ShipmentManager.cs     # NEW: Manages shipments
│   └── SaveManager.cs         # NEW: Handles JSON save/load
├── Models/
│   ├── ShipmentData.cs        # NEW: Shipment data model
│   ├── ShipmentHistoryEntry.cs # NEW: History entry model
│   └── ModSaveData.cs         # NEW: Root save data container
├── UI/
│   └── DockExportsApp.cs      # Phone app UI
├── DockExportsConfig.cs       # Configuration constants
└── Resources/
    └── DE.png                 # App icon
```

**Save File Location:**
```
Schedule I/
└── UserData/
    └── DockExportsMod_Save.json    # Completely separate!
```

---

## Implementation Approaches

## Approach A: S1API Saveable System

**Recommended for:** Mods that want integration with game save/load events but separate data files.

### Advantages
- Automatic save/load on game save/load
- Integrated with S1API's save management
- Per-save-slot data (different saves have different mod data)
- Event system (`OnLoaded` callback)

### Disadvantages
- Still tied to game save/load flow
- Requires understanding S1API's `Saveable` system

---

### Step 1: Create Data Models

Create `Models/ShipmentData.cs`:

```csharp
using System;

namespace S1DockExports.Models
{
    [Serializable]
    public class ShipmentData
    {
        public ShipmentType Type;
        public int Quantity;
        public float TotalValue;
        public float TotalPaid;
        public int WeeksPaid;
        public int StartWeek;

        public ShipmentData() { }

        public ShipmentData(ShipmentType type, int quantity, float totalValue)
        {
            Type = type;
            Quantity = quantity;
            TotalValue = totalValue;
            TotalPaid = 0f;
            WeeksPaid = 0;
            StartWeek = 0;
        }
    }

    public enum ShipmentType
    {
        Wholesale,
        Consignment
    }
}
```

Create `Models/ShipmentHistoryEntry.cs`:

```csharp
using System;

namespace S1DockExports.Models
{
    [Serializable]
    public class ShipmentHistoryEntry
    {
        public ShipmentType Type;
        public int Quantity;
        public float TotalValue;
        public float TotalPaid;
        public int WeeksPaid;
        public int CompletedWeek;

        public ShipmentHistoryEntry() { }

        public ShipmentHistoryEntry(ShipmentData data, int completedWeek)
        {
            Type = data.Type;
            Quantity = data.Quantity;
            TotalValue = data.TotalValue;
            TotalPaid = data.TotalPaid;
            WeeksPaid = data.WeeksPaid;
            CompletedWeek = completedWeek;
        }
    }
}
```

---

### Step 2: Create ShipmentManager (Saveable)

Create `Core/ShipmentManager.cs`:

```csharp
using System;
using System.Collections.Generic;
using MelonLoader;
using S1API.Saveables;
using S1DockExports.Models;

namespace S1DockExports.Core
{
    /// <summary>
    /// Manages shipment data and save/load operations using S1API's Saveable system.
    /// Data is stored in ModData/DockExports/DockExports.json
    /// </summary>
    public class ShipmentManager : Saveable
    {
        // Singleton instance
        public static ShipmentManager Instance { get; private set; }

        // Events
        public static event Action OnDataLoaded;
        public static event Action OnShipmentChanged;

        // Save fields (automatically serialized by S1API)
        [SaveableField("BrokerUnlocked")]
        private bool _brokerUnlocked = false;

        [SaveableField("LastProcessedDay")]
        private int _lastProcessedDay = -1;

        [SaveableField("ActiveShipment")]
        private ShipmentData _activeShipment = null;

        [SaveableField("ShipmentHistory")]
        private List<ShipmentHistoryEntry> _history = new List<ShipmentHistoryEntry>();

        // Public properties
        public bool BrokerUnlocked
        {
            get => _brokerUnlocked;
            set
            {
                _brokerUnlocked = value;
                RequestSave();
            }
        }

        public int LastProcessedDay
        {
            get => _lastProcessedDay;
            set
            {
                _lastProcessedDay = value;
                RequestSave();
            }
        }

        public ShipmentData ActiveShipment
        {
            get => _activeShipment;
            set
            {
                _activeShipment = value;
                OnShipmentChanged?.Invoke();
                RequestSave();
            }
        }

        public List<ShipmentHistoryEntry> History => _history;

        // Constructor
        public ShipmentManager()
        {
            Instance = this;
        }

        /// <summary>
        /// Called by S1API when save data is loaded
        /// </summary>
        protected override void OnLoaded()
        {
            Instance = this;
            MelonLogger.Msg($"[ShipmentManager] Loaded: Broker={_brokerUnlocked}, Active={_activeShipment != null}, History={_history.Count}");
            OnDataLoaded?.Invoke();
        }

        /// <summary>
        /// Register this manager with S1API's save system
        /// Call this once during mod initialization
        /// </summary>
        public static void RegisterWithS1API()
        {
            if (Instance == null)
            {
                Instance = new ShipmentManager();
            }

            ModSaveableRegistry.Register(Instance, folderName: "DockExports");
            MelonLogger.Msg("[ShipmentManager] Registered with S1API save system");
        }

        /// <summary>
        /// Request an immediate save
        /// </summary>
        public void RequestSave()
        {
            Saveable.RequestGameSave(immediate: true);
        }

        /// <summary>
        /// Create a new shipment
        /// </summary>
        public void CreateShipment(ShipmentType type, int quantity, float totalValue, int startWeek)
        {
            if (_activeShipment != null)
            {
                MelonLogger.Warning("[ShipmentManager] Cannot create shipment: active shipment already exists");
                return;
            }

            _activeShipment = new ShipmentData(type, quantity, totalValue)
            {
                StartWeek = startWeek
            };

            OnShipmentChanged?.Invoke();
            RequestSave();

            MelonLogger.Msg($"[ShipmentManager] Created {type} shipment: {quantity} bricks, ${totalValue:F0}");
        }

        /// <summary>
        /// Complete the active shipment and add to history
        /// </summary>
        public void CompleteShipment(int completedWeek)
        {
            if (_activeShipment == null)
            {
                MelonLogger.Warning("[ShipmentManager] No active shipment to complete");
                return;
            }

            var historyEntry = new ShipmentHistoryEntry(_activeShipment, completedWeek);
            _history.Add(historyEntry);

            MelonLogger.Msg($"[ShipmentManager] Completed shipment: Total ${_activeShipment.TotalPaid:F0} / ${_activeShipment.TotalValue:F0}");

            _activeShipment = null;
            OnShipmentChanged?.Invoke();
            RequestSave();
        }

        /// <summary>
        /// Update active shipment payment progress
        /// </summary>
        public void RecordPayment(float amount)
        {
            if (_activeShipment == null)
            {
                MelonLogger.Warning("[ShipmentManager] No active shipment for payment");
                return;
            }

            _activeShipment.TotalPaid += amount;
            _activeShipment.WeeksPaid++;

            OnShipmentChanged?.Invoke();
            RequestSave();
        }

        /// <summary>
        /// Clear all data (for testing or reset)
        /// </summary>
        public void ClearAllData()
        {
            _brokerUnlocked = false;
            _lastProcessedDay = -1;
            _activeShipment = null;
            _history.Clear();

            OnShipmentChanged?.Invoke();
            RequestSave();

            MelonLogger.Msg("[ShipmentManager] All data cleared");
        }
    }
}
```

---

### Step 3: Update DockExportsMod.cs

Simplify `DockExportsMod.cs` to use the new manager:

```csharp
using MelonLoader;
using S1API.GameTime;
using S1DockExports.Core;
using S1DockExports.Models;
using UnityEngine;

namespace S1DockExports
{
    public class DockExportsMod : MelonMod
    {
        private const int RequiredLevel = 13; // Hustler III
        private const string DocksPropertyId = "docks";

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("DockExports mod initialized");

            // Register save system
            ShipmentManager.RegisterWithS1API();

            // Subscribe to data loaded event
            ShipmentManager.OnDataLoaded += OnSaveDataLoaded;
        }

        private void OnSaveDataLoaded()
        {
            MelonLogger.Msg("[DockExports] Save data loaded");

            // Check unlock conditions
            CheckBrokerUnlock();
        }

        public override void OnUpdate()
        {
            // Check for broker unlock
            if (!ShipmentManager.Instance.BrokerUnlocked)
            {
                CheckBrokerUnlock();
                return;
            }

            // Process weekly payments on Friday
            if (TimeManager.CurrentDay == Day.Friday)
            {
                int currentDay = TimeManager.ElapsedDays;

                if (currentDay != ShipmentManager.Instance.LastProcessedDay)
                {
                    ProcessConsignmentWeek();
                    ShipmentManager.Instance.LastProcessedDay = currentDay;
                }
            }
        }

        private void CheckBrokerUnlock()
        {
            // Check rank and property ownership
            bool hasRank = Leveling.GetPlayerLevel() >= RequiredLevel;
            bool hasProperty = Property.IsPropertyOwned(DocksPropertyId);

            if (hasRank && hasProperty && !ShipmentManager.Instance.BrokerUnlocked)
            {
                ShipmentManager.Instance.BrokerUnlocked = true;

                PhoneCalls.SendSMS("The Broker",
                    "Heard you control the docks. I move product overseas. " +
                    "Wholesale or consignment - your call.");

                // Register phone app
                PhoneApp.RegisterApp(new DockExportsApp(this));

                MelonLogger.Msg("[DockExports] Broker unlocked!");
            }
        }

        private void ProcessConsignmentWeek()
        {
            var shipment = ShipmentManager.Instance.ActiveShipment;

            if (shipment == null || shipment.Type != ShipmentType.Consignment)
                return;

            float weeklyAmount = shipment.TotalValue / 4f;

            // 25% chance of loss
            if (Random.value < 0.25f)
            {
                float lossPercent = Random.Range(0.15f, 0.60f);
                weeklyAmount *= (1f - lossPercent);

                PhoneCalls.SendSMS("The Broker",
                    $"Bad news. Lost {lossPercent:P0} this week. You get ${weeklyAmount:F0}.");
            }
            else
            {
                PhoneCalls.SendSMS("The Broker",
                    $"Clean run. ${weeklyAmount:F0} wired.");
            }

            Money.AddMoney(weeklyAmount);
            ShipmentManager.Instance.RecordPayment(weeklyAmount);

            // Complete after 4 weeks
            if (shipment.WeeksPaid >= 4)
            {
                int currentWeek = TimeManager.ElapsedDays / 7;
                ShipmentManager.Instance.CompleteShipment(currentWeek);

                PhoneCalls.SendSMS("The Broker",
                    "Shipment complete. Ready for the next one.");
            }
        }

        // Public methods for UI
        public void CreateWholesaleShipment(int quantity)
        {
            float value = quantity * 2500f; // Example pricing
            int currentWeek = TimeManager.ElapsedDays / 7;

            ShipmentManager.Instance.CreateShipment(
                ShipmentType.Wholesale,
                quantity,
                value,
                currentWeek
            );
        }

        public void CreateConsignmentShipment(int quantity)
        {
            float value = quantity * 4000f; // 1.6x multiplier
            int currentWeek = TimeManager.ElapsedDays / 7;

            ShipmentManager.Instance.CreateShipment(
                ShipmentType.Consignment,
                quantity,
                value,
                currentWeek
            );
        }
    }
}
```

---

### Step 4: Update DockExportsApp.cs

Access shipment data through the manager:

```csharp
protected override void OnCreatedUI(GameObject container)
{
    // Subscribe to shipment changes
    ShipmentManager.OnShipmentChanged += RefreshActiveTab;

    // Build UI...
    RefreshActiveTab();
}

protected override void OnDestroyed()
{
    // Unsubscribe
    ShipmentManager.OnShipmentChanged -= RefreshActiveTab;
}

private void RefreshActiveTab()
{
    var shipment = ShipmentManager.Instance.ActiveShipment;

    if (shipment != null)
    {
        // Display active shipment
        _statusText.text = $"Active: {shipment.Quantity} bricks\n" +
                          $"Paid: ${shipment.TotalPaid:F0} / ${shipment.TotalValue:F0}";
    }
    else
    {
        _statusText.text = "No active shipment";
    }
}
```

---

## Approach B: Pure Custom JSON

**Recommended for:** Mods that want complete independence from game save system.

### Advantages
- Complete control over when saves occur
- Not tied to game save/load events
- Can save/load at any time
- Simpler to understand (just JSON file I/O)

### Disadvantages
- Must manually handle save/load timing
- Single save file (not per save-slot)
- More boilerplate code

---

### Step 1: Create Data Models

Same as Approach A (create `ShipmentData` and `ShipmentHistoryEntry`).

Additionally, create `Models/ModSaveData.cs` as the root container:

```csharp
using System;
using System.Collections.Generic;

namespace S1DockExports.Models
{
    /// <summary>
    /// Root container for all mod save data
    /// </summary>
    [Serializable]
    public class ModSaveData
    {
        public bool BrokerUnlocked;
        public int LastProcessedDay;
        public ShipmentData ActiveShipment;
        public List<ShipmentHistoryEntry> History;

        public ModSaveData()
        {
            BrokerUnlocked = false;
            LastProcessedDay = -1;
            ActiveShipment = null;
            History = new List<ShipmentHistoryEntry>();
        }
    }
}
```

---

### Step 2: Create SaveManager

Create `Core/SaveManager.cs`:

```csharp
using System;
using System.IO;
using MelonLoader;
using Newtonsoft.Json;
using S1DockExports.Models;

namespace S1DockExports.Core
{
    /// <summary>
    /// Handles JSON save/load operations completely independent of game saves
    /// </summary>
    public static class SaveManager
    {
        private static readonly string SaveDirectory = Path.Combine(
            MelonEnvironment.UserDataDirectory,
            ".."  // Go up from MelonLoader/UserData to Schedule I root
        );

        private static readonly string SaveFilePath = Path.Combine(
            SaveDirectory,
            "UserData",
            "DockExportsMod_Save.json"
        );

        /// <summary>
        /// Save data to JSON file
        /// </summary>
        public static void SaveData(ModSaveData data)
        {
            try
            {
                // Ensure directory exists
                string directory = Path.GetDirectoryName(SaveFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Serialize to JSON with formatting
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);

                // Write to file
                File.WriteAllText(SaveFilePath, json);

                MelonLogger.Msg($"[SaveManager] Saved to: {SaveFilePath}");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[SaveManager] Failed to save: {ex.Message}");
            }
        }

        /// <summary>
        /// Load data from JSON file
        /// </summary>
        public static ModSaveData LoadData()
        {
            try
            {
                if (!File.Exists(SaveFilePath))
                {
                    MelonLogger.Msg("[SaveManager] No save file found, creating new data");
                    return new ModSaveData();
                }

                // Read from file
                string json = File.ReadAllText(SaveFilePath);

                // Deserialize from JSON
                var data = JsonConvert.DeserializeObject<ModSaveData>(json);

                MelonLogger.Msg($"[SaveManager] Loaded from: {SaveFilePath}");
                return data ?? new ModSaveData();
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[SaveManager] Failed to load: {ex.Message}");
                return new ModSaveData();
            }
        }

        /// <summary>
        /// Delete save file (for testing/reset)
        /// </summary>
        public static void DeleteSaveFile()
        {
            try
            {
                if (File.Exists(SaveFilePath))
                {
                    File.Delete(SaveFilePath);
                    MelonLogger.Msg("[SaveManager] Save file deleted");
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[SaveManager] Failed to delete save: {ex.Message}");
            }
        }

        /// <summary>
        /// Check if save file exists
        /// </summary>
        public static bool SaveFileExists()
        {
            return File.Exists(SaveFilePath);
        }
    }
}
```

**Note**: This approach uses `Newtonsoft.Json` for better JSON handling. Add to your `.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
</ItemGroup>
```

---

### Step 3: Create ShipmentManager (Pure)

Create `Core/ShipmentManager.cs` (non-Saveable version):

```csharp
using System;
using System.Collections.Generic;
using MelonLoader;
using S1DockExports.Models;

namespace S1DockExports.Core
{
    /// <summary>
    /// Manages shipment data with manual save/load
    /// </summary>
    public class ShipmentManager
    {
        // Singleton
        public static ShipmentManager Instance { get; private set; }

        // Events
        public static event Action OnDataLoaded;
        public static event Action OnShipmentChanged;

        // Data (in-memory)
        private ModSaveData _data;

        // Auto-save flag
        private bool _autoSaveEnabled = true;

        // Properties
        public bool BrokerUnlocked
        {
            get => _data.BrokerUnlocked;
            set
            {
                _data.BrokerUnlocked = value;
                Save();
            }
        }

        public int LastProcessedDay
        {
            get => _data.LastProcessedDay;
            set
            {
                _data.LastProcessedDay = value;
                Save();
            }
        }

        public ShipmentData ActiveShipment
        {
            get => _data.ActiveShipment;
            set
            {
                _data.ActiveShipment = value;
                OnShipmentChanged?.Invoke();
                Save();
            }
        }

        public List<ShipmentHistoryEntry> History => _data.History;

        // Constructor
        private ShipmentManager()
        {
            _data = new ModSaveData();
        }

        /// <summary>
        /// Initialize the manager (call once on mod startup)
        /// </summary>
        public static void Initialize()
        {
            if (Instance == null)
            {
                Instance = new ShipmentManager();
                Instance.Load();
            }
        }

        /// <summary>
        /// Load data from JSON file
        /// </summary>
        public void Load()
        {
            _data = SaveManager.LoadData();
            MelonLogger.Msg($"[ShipmentManager] Loaded: Broker={_data.BrokerUnlocked}, " +
                           $"Active={_data.ActiveShipment != null}, History={_data.History.Count}");
            OnDataLoaded?.Invoke();
        }

        /// <summary>
        /// Save data to JSON file
        /// </summary>
        public void Save()
        {
            if (!_autoSaveEnabled) return;

            SaveManager.SaveData(_data);
        }

        /// <summary>
        /// Force save regardless of auto-save setting
        /// </summary>
        public void ForceSave()
        {
            SaveManager.SaveData(_data);
        }

        /// <summary>
        /// Enable/disable auto-save (useful for batch operations)
        /// </summary>
        public void SetAutoSave(bool enabled)
        {
            _autoSaveEnabled = enabled;
        }

        /// <summary>
        /// Create a new shipment
        /// </summary>
        public void CreateShipment(ShipmentType type, int quantity, float totalValue, int startWeek)
        {
            if (_data.ActiveShipment != null)
            {
                MelonLogger.Warning("[ShipmentManager] Cannot create shipment: active shipment exists");
                return;
            }

            _data.ActiveShipment = new ShipmentData(type, quantity, totalValue)
            {
                StartWeek = startWeek
            };

            OnShipmentChanged?.Invoke();
            Save();

            MelonLogger.Msg($"[ShipmentManager] Created {type} shipment: {quantity} bricks, ${totalValue:F0}");
        }

        /// <summary>
        /// Complete the active shipment
        /// </summary>
        public void CompleteShipment(int completedWeek)
        {
            if (_data.ActiveShipment == null)
            {
                MelonLogger.Warning("[ShipmentManager] No active shipment to complete");
                return;
            }

            var historyEntry = new ShipmentHistoryEntry(_data.ActiveShipment, completedWeek);
            _data.History.Add(historyEntry);

            MelonLogger.Msg($"[ShipmentManager] Completed: ${_data.ActiveShipment.TotalPaid:F0} / ${_data.ActiveShipment.TotalValue:F0}");

            _data.ActiveShipment = null;
            OnShipmentChanged?.Invoke();
            Save();
        }

        /// <summary>
        /// Record payment for active shipment
        /// </summary>
        public void RecordPayment(float amount)
        {
            if (_data.ActiveShipment == null)
            {
                MelonLogger.Warning("[ShipmentManager] No active shipment for payment");
                return;
            }

            _data.ActiveShipment.TotalPaid += amount;
            _data.ActiveShipment.WeeksPaid++;

            OnShipmentChanged?.Invoke();
            Save();
        }

        /// <summary>
        /// Clear all data
        /// </summary>
        public void ClearAllData()
        {
            _data = new ModSaveData();
            OnShipmentChanged?.Invoke();
            Save();

            MelonLogger.Msg("[ShipmentManager] All data cleared");
        }
    }
}
```

---

### Step 4: Update DockExportsMod.cs

```csharp
using MelonLoader;
using S1API.GameTime;
using S1DockExports.Core;
using S1DockExports.Models;
using UnityEngine;

namespace S1DockExports
{
    public class DockExportsMod : MelonMod
    {
        private const int RequiredLevel = 13;
        private const string DocksPropertyId = "docks";

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("DockExports mod initialized");

            // Initialize save system (loads data immediately)
            ShipmentManager.Initialize();

            // Subscribe to events
            ShipmentManager.OnDataLoaded += OnDataLoaded;
        }

        private void OnDataLoaded()
        {
            MelonLogger.Msg("[DockExports] Data loaded");
            CheckBrokerUnlock();
        }

        public override void OnApplicationQuit()
        {
            // Save on exit
            ShipmentManager.Instance?.ForceSave();
        }

        public override void OnUpdate()
        {
            // Same logic as Approach A...
            // (Use ShipmentManager.Instance to access data)
        }

        // ... rest of implementation
    }
}
```

---

## Comparison: Approach A vs B

| Feature | Approach A (S1API) | Approach B (Pure JSON) |
|---------|-------------------|------------------------|
| **Save Integration** | Tied to game saves | Completely independent |
| **Save Location** | `ModData/DockExports/` (per save slot) | `UserData/DockExportsMod_Save.json` (global) |
| **Automatic Saving** | On game save | Manual (property setters) |
| **Code Complexity** | Low (S1API handles it) | Medium (manual file I/O) |
| **Dependencies** | S1API.Saveables | Newtonsoft.Json |
| **Per-Save-Slot Data** | Yes (different saves = different data) | No (single file for all saves) |
| **Manual Save Control** | Limited | Full control |
| **Recommended For** | Most mods, integrated experience | Mods needing full independence |

---

## Migration Guide

### From Current Implementation

Current code uses this pattern:

```csharp
// OLD (current code in DockExportsMod.cs)
SaveSystem.RegisterSaveHandler(
    "DockExports",
    () => JsonUtility.ToJson(new SaveData { /* ... */ }),
    (json) => { /* manual load */ }
);
```

### Migration Steps

1. **Create Data Models** (both approaches)
   - Extract save data into `ShipmentData`, `ShipmentHistoryEntry` models
   - Add `[Serializable]` attribute

2. **Create ShipmentManager** (choose approach)
   - **Approach A**: Extend `Saveable`, add `[SaveableField]` attributes
   - **Approach B**: Create pure C# class with manual save/load

3. **Update DockExportsMod.cs**
   - Remove `SaveSystem.RegisterSaveHandler()` call
   - Replace direct field access with `ShipmentManager.Instance` properties
   - **Approach A**: Call `ShipmentManager.RegisterWithS1API()`
   - **Approach B**: Call `ShipmentManager.Initialize()`

4. **Update DockExportsApp.cs**
   - Replace `_mod.Active` with `ShipmentManager.Instance.ActiveShipment`
   - Subscribe to `ShipmentManager.OnShipmentChanged` event

5. **Test**
   - Delete old save data (if incompatible format)
   - Verify broker unlock works
   - Test creating shipments
   - Test save/load (exit game, restart, check data persists)

### Data Migration Script (Optional)

If you need to migrate existing saves:

```csharp
// Add to DockExportsMod.OnInitializeMelon()
private void MigrateOldSaveData()
{
    // Try to load old format
    string oldKey = "DockExports";
    // ... load old JSON from game save system
    // ... convert to new format
    // ... save using new system
}
```

---

## Testing

### Test Checklist

- [ ] **Fresh Install**: Start new game, verify no errors
- [ ] **Unlock Conditions**: Reach Hustler III + buy Docks, verify broker SMS
- [ ] **Create Shipment**: Use phone app, verify shipment created
- [ ] **Save/Load**: Exit game, restart, verify shipment persists
- [ ] **Weekly Processing**: Advance to Friday, verify payment received
- [ ] **Completion**: Complete 4 weeks, verify shipment moves to history
- [ ] **Multiple Saves** (Approach A only): Test with different save slots

### Debug Commands

Add these to DockExportsMod for testing:

```csharp
[HarmonyPatch(typeof(Console), "ProcessCommand")]
public static class ConsoleCommands
{
    [HarmonyPrefix]
    public static bool Prefix(string command)
    {
        if (command == "dockexports.debug")
        {
            var mgr = ShipmentManager.Instance;
            MelonLogger.Msg($"Broker Unlocked: {mgr.BrokerUnlocked}");
            MelonLogger.Msg($"Active Shipment: {mgr.ActiveShipment != null}");
            MelonLogger.Msg($"History Count: {mgr.History.Count}");
            return false; // Block original command
        }

        if (command == "dockexports.clear")
        {
            ShipmentManager.Instance.ClearAllData();
            MelonLogger.Msg("All data cleared");
            return false;
        }

        return true; // Allow original command
    }
}
```

Usage in-game console:
```
dockexports.debug    // Show current state
dockexports.clear    // Reset all data
```

---

## Recommended Approach

**For S1DockExports: Use Approach A (S1API Saveable)**

Reasons:
1. Already using S1API for other systems
2. Better integration with game save flow
3. Per-save-slot data makes sense for economy mod
4. Less code to maintain (S1API handles file I/O)
5. Matches S1NotesApp reference implementation

**Use Approach B if:**
- You need data to persist across all save slots
- You want save/load at specific times (not tied to game saves)
- You need to frequently save (e.g., every frame)

---

## File Structure Summary

### Approach A Final Structure
```
S1DockExports/
├── Core/
│   ├── DockExportsMod.cs          (120 lines, simplified)
│   └── ShipmentManager.cs         (180 lines, Saveable)
├── Models/
│   ├── ShipmentData.cs            (30 lines)
│   └── ShipmentHistoryEntry.cs    (25 lines)
├── UI/
│   └── DockExportsApp.cs          (Original, minimal changes)
├── DockExportsConfig.cs           (Original)
└── Resources/
    └── DE.png

Save location:
Schedule I/UserData/Saves/[Slot]/ModData/DockExports/DockExports.json
```

### Approach B Final Structure
```
S1DockExports/
├── Core/
│   ├── DockExportsMod.cs          (130 lines, simplified)
│   ├── ShipmentManager.cs         (200 lines, pure class)
│   └── SaveManager.cs             (110 lines, static utility)
├── Models/
│   ├── ModSaveData.cs             (20 lines, root container)
│   ├── ShipmentData.cs            (30 lines)
│   └── ShipmentHistoryEntry.cs    (25 lines)
├── UI/
│   └── DockExportsApp.cs          (Original, minimal changes)
├── DockExportsConfig.cs           (Original)
└── Resources/
    └── DE.png

Save location:
Schedule I/UserData/DockExportsMod_Save.json
```

---

## Additional Resources

- **S1API Documentation**: Check S1API source code for `Saveable` class details
- **S1NotesApp Reference**: `C:\Users\kaisoxp\Documents\Repositories\kaisoxp\S1NotesApp\NotesManager.cs`
- **JSON Serialization**: Unity's `JsonUtility` vs Newtonsoft.Json comparison
- **MelonLoader Paths**: `MelonEnvironment.UserDataDirectory` for file locations

---

## Next Steps

1. Choose an approach (A recommended)
2. Create folder structure (`Core/`, `Models/`, `UI/`)
3. Implement data models
4. Implement ShipmentManager
5. Update DockExportsMod.cs
6. Update DockExportsApp.cs
7. Test thoroughly
8. Consider adding migration script for existing saves (if needed)

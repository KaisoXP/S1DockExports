# S1API v2.4.2 Compatibility Issues with Schedule I v0.4.0f9

This document tracks all S1API compatibility issues encountered while developing S1DockExports mod.

## Environment

- **Game Version**: Schedule I v0.4.0f9
- **Unity Version**: 2022.3.62f2
- **MelonLoader**: v0.7.1 Open-Beta
- **S1API Version**: v2.4.2 (from NuGet package `S1API.Forked`)
- **S1API Release Date**: October 2, 2023
- **Build Configuration**: Il2cpp

## Summary

S1API v2.4.2 appears to be outdated compared to the current game version. Multiple manager classes fail to initialize due to missing game types. The root cause is that S1API references game types that have been moved, renamed, or removed in game updates after S1API v2.4.2 was released.

## Critical Issues

### 1. LevelManager - TypeLoadException

**S1API Component**: `S1API.Leveling.LevelManager`
**Error**: `TypeLoadException: Could not load type 'ScheduleOne.DevUtilities.NetworkSingleton`1' from assembly 'Assembly-CSharp'`

**Log Evidence**:
```
[00:12:52.514] [S1DockExports] System.TypeInitializationException: The type initializer for 'S1API.Leveling.LevelManager' threw an exception.
 ---> System.TypeLoadException: Could not load type 'ScheduleOne.DevUtilities.NetworkSingleton`1' from assembly 'Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
   at S1API.Leveling.LevelManager..cctor()
   --- End of inner exception stack trace ---
   at S1DockExports.DockExportsMod.CanUnlock()
```

**Impact**: Cannot check player rank for unlock conditions

**Workaround**: Direct game access via `NetworkSingleton<PlayerLevelManager>.Instance.CurrentRank`

**Proof it works**: S1FuelMod successfully uses `NetworkSingleton<T>` at lines 370, 570, 572 of HarmonyPatches.cs

### 2. PropertyManager - TypeLoadException

**S1API Component**: `S1API.Property.PropertyManager`
**Error**: Same `NetworkSingleton`1'` error as LevelManager

**Impact**: Cannot check if player owns the Docks property for unlock conditions

**Workaround**: Direct game access via `NetworkSingleton<PropertyManager>.Instance.AllProperties`

### 3. TimeManager - TypeLoadException

**S1API Component**: `S1API.GameTime.TimeManager`
**Error**: Same `NetworkSingleton`1'` error as LevelManager

**Impact**: Cannot check current day (for Friday payouts) or elapsed days (for tracking processed weeks)

**Workaround**: Direct game access via `NetworkSingleton<GameTimeManager>.Instance`

### 4. CallManager - TypeLoadException

**S1API Component**: `S1API.PhoneCalls.CallManager`
**Error**: `TypeLoadException: Could not load type 'ScheduleOne.Calling.CallManager' from assembly 'Assembly-CSharp'`

**Log Evidence**:
```
[00:12:44.131] [S1API_(Forked_by_Bars)] System.TypeLoadException: Could not load type 'ScheduleOne.Calling.CallManager' from assembly 'Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
```

**Impact**: Cannot send phone call notifications to player (broker messages)

**Workaround**: TBD - need to investigate game's current phone call system

## Working S1API Components

These S1API components work correctly and should continue to be used:

- ✅ `S1API.PhoneApp` - Phone app framework
- ✅ `S1API.UI` - UI factory helpers
- ✅ `S1API.Saveables` - Save system (`Saveable` base class, `[SaveableField]` attribute)
- ✅ `S1API.Input` - Input management (`Controls.IsTyping`)
- ✅ `S1API.Internal.Utils` - Utility helpers (ImageUtils, ButtonUtils, EventHelper)
- ✅ `S1API.Internal.Abstraction` - Base classes
- ✅ `S1API.Money` - Cash system (CashDefinition, CashInstance)
- ✅ `S1API.Items` - Item system (ItemManager)

**Evidence**: S1NotesApp mod uses these components successfully without errors

## Refactoring Plan

### Phase 1: Implement Direct Game Access (COMPLETED)

Created `Integrations/GameAccessPatches.cs` with helper methods:
- `GameAccess.GetPlayerRank()` - Replaces `LevelManager.Rank`
- `GameAccess.IsPropertyOwned(string)` - Replaces `PropertyManager.FindPropertyByName().IsOwned`
- `GameAccess.GetCurrentDayOfWeek()` - Replaces `TimeManager.CurrentDay`
- `GameAccess.GetElapsedDays()` - Replaces `TimeManager.ElapsedDays`
- `GameAccess.GetBrickPrice()` - Gets product pricing (no S1API equivalent)

### Phase 2: Update DockExportsMod.cs (PENDING)

Replace broken S1API calls with direct game access:

```csharp
// OLD (broken):
if (LevelManager.Rank < Rank.Hustler)

// NEW (working):
if (GameAccess.GetPlayerRank() < 3) // Hustler = rank 3
```

```csharp
// OLD (broken):
var docksProperty = PropertyManager.FindPropertyByName("Docks Warehouse");
return docksProperty?.IsOwned ?? false;

// NEW (working):
return GameAccess.IsPropertyOwned("Docks Warehouse");
```

```csharp
// OLD (broken):
bool isFriday = TimeManager.CurrentDay == Day.Friday;

// NEW (working):
bool isFriday = GameAccess.GetCurrentDayOfWeek() == 4; // Friday = 4
```

```csharp
// OLD (broken):
int currentDay = TimeManager.ElapsedDays;

// NEW (working):
int currentDay = GameAccess.GetElapsedDays();
```

### Phase 3: Phone Call Workaround (PENDING)

Need to investigate how to send phone calls without S1API.PhoneCalls.CallManager:
- Option A: Direct access to game's phone call system
- Option B: Use on-screen notifications instead
- Option C: Wait for S1API fix

### Phase 4: Remove Unused S1API References

After refactoring, remove these broken imports:
```csharp
using S1API.Leveling;      // REMOVE
using S1API.Property;      // REMOVE
using S1API.GameTime;      // REMOVE
using S1API.PhoneCalls;    // REMOVE (until workaround found)
```

## GitHub Issue Checklist

When filing the comprehensive S1API issue, include:

- [ ] This entire document
- [ ] Full MelonLoader log excerpt showing errors
- [ ] S1NotesApp as proof that some components work
- [ ] S1FuelMod as proof that NetworkSingleton<T> exists in current game
- [ ] Request for S1API update to support game v0.4.0f9
- [ ] Offer to test updated S1API version

## Additional Notes

The S1API documentation at https://ifbars.github.io/S1API/ accurately describes what the API *should* do. The problem is the implementation (S1API.dll v2.4.2) references types that no longer exist in the updated game.

This suggests the game's internal structure changed significantly between when S1API v2.4.2 was released (October 2023) and the current game version (0.4.0f9).

# Harmony Patching Guide

A comprehensive guide to using Harmony for modding Schedule I with MelonLoader.

## Table of Contents

- [What Is Harmony Patching?](#what-is-harmony-patching)
- [Basic Setup](#basic-setup)
- [Patch Types](#patch-types)
  - [Prefix Patches](#prefix-patches)
  - [Postfix Patches](#postfix-patches)
  - [Transpiler Patches](#transpiler-patches)
- [Special Parameters](#special-parameters)
- [Patching Properties](#patching-properties)
- [Il2Cpp Considerations](#il2cpp-considerations)
- [Common Patterns](#common-patterns)
- [Troubleshooting](#troubleshooting)
- [Complete Example](#complete-example)

---

## What Is Harmony Patching?

**Harmony** is a library that lets you intercept and modify methods at runtime without changing the original code. Think of it like **intercepting a phone call**:

1. The game calls a method (e.g., `AddMoney(100)`)
2. Harmony intercepts it and runs YOUR code first (Prefix) or after (Postfix)
3. You can read parameters, change them, modify return values, or even prevent the original method from running

**Key Benefits:**
- No need to decompile and modify game DLLs
- Multiple mods can patch the same method without conflicts
- Changes are temporary (only active when your mod is loaded)
- Perfect for adding features, fixing bugs, or exploring game internals

---

## Basic Setup

### 1. Enable Harmony in Your Mod

In your main mod class (extends `MelonMod`):

```csharp
using MelonLoader;
using HarmonyLib;

public class MyMod : MelonMod
{
    public override void OnInitializeMelon()
    {
        // This finds and applies ALL Harmony patches in your assembly
        HarmonyInstance.PatchAll();

        LoggerInstance.Msg("Harmony patches applied!");
    }
}
```

That's it for setup! Now you just need to write patch classes.

---

## Patch Types

### Prefix Patches

**Runs BEFORE the original method.** Use when you want to:
- Intercept and log method calls
- Modify parameters before the method runs
- Prevent the original method from running (return `false`)

#### Basic Prefix

```csharp
using HarmonyLib;

// This attribute tells Harmony WHAT to patch
[HarmonyPatch(typeof(MoneyManager), "AddMoney")]
public class AddMoney_Patch
{
    // Prefix runs BEFORE AddMoney()
    [HarmonyPrefix]
    public static void Prefix(float amount)
    {
        // Parameter names must match the original method
        MelonLogger.Msg($"Player is about to receive ${amount}");
    }
}
```

**What happens:**
1. Game calls `MoneyManager.AddMoney(100)`
2. Your `Prefix` runs first → logs "Player is about to receive $100"
3. Original `AddMoney(100)` runs normally

#### Modifying Parameters

Use `ref` to modify parameters:

```csharp
[HarmonyPatch(typeof(MoneyManager), "AddMoney")]
public class DoubleMoney_Patch
{
    [HarmonyPrefix]
    public static void Prefix(ref float amount)
    {
        // 'ref' lets you MODIFY the parameter
        amount *= 2; // Double all money gained!
        MelonLogger.Msg($"Doubled money from ${amount/2} to ${amount}");
    }
}
```

**Result:** When the game tries to add $100, your prefix changes it to $200 before the original method runs.

#### Skipping the Original Method

Return `false` to prevent the original method from running:

```csharp
[HarmonyPatch(typeof(MoneyManager), "RemoveMoney")]
public class PreventMoneyLoss_Patch
{
    [HarmonyPrefix]
    public static bool Prefix(float amount)
    {
        MelonLogger.Msg($"Blocked attempt to remove ${amount}!");

        // Return FALSE = skip the original method entirely
        return false;
    }
}
```

**Result:** When the game tries to call `RemoveMoney(50)`, your prefix returns `false` and the original method never runs. Player keeps their money!

**Important:** If you return `bool`, you MUST return a value:
- `return true` → Run the original method (default behavior)
- `return false` → Skip the original method

---

### Postfix Patches

**Runs AFTER the original method.** Use when you want to:
- See or modify the return value
- React to method completion
- Log what happened

#### Basic Postfix

```csharp
[HarmonyPatch(typeof(InventoryManager), "GetItemInSlot")]
public class LogInventoryAccess_Patch
{
    // Postfix runs AFTER GetItemInSlot()
    [HarmonyPostfix]
    public static void Postfix(int slotIndex, Item __result)
    {
        // __result is the return value from the original method
        if (__result != null)
        {
            MelonLogger.Msg($"Slot {slotIndex} contains: {__result.name}");
        }
        else
        {
            MelonLogger.Msg($"Slot {slotIndex} is empty");
        }
    }
}
```

**What happens:**
1. Game calls `GetItemInSlot(5)`
2. Original method runs → returns an `Item` (or null)
3. Your postfix receives the result via `__result`

#### Modifying Return Values

Use `ref` on `__result` to change the return value:

```csharp
[HarmonyPatch(typeof(DealManager), "CalculateProfit")]
public class BetterProfits_Patch
{
    [HarmonyPostfix]
    public static void Postfix(ref float __result)
    {
        float originalProfit = __result;

        // Increase all profits by 50%
        __result *= 1.5f;

        MelonLogger.Msg($"Boosted profit from ${originalProfit} to ${__result}");
    }
}
```

**Result:** Any time the game calculates profit, your postfix multiplies it by 1.5x before returning to the caller.

---

### Transpiler Patches

**Modifies the IL (Intermediate Language) code itself.** This is advanced and rarely needed.

Use when you need to:
- Change specific instructions in the middle of a method
- Replace conditional checks
- Optimize performance

**Example use case:** Change `if (rank >= 10)` to `if (rank >= 5)` without rewriting the entire method.

We won't cover transpilers in detail here, but know they exist for advanced scenarios.

---

## Special Parameters

Harmony recognizes special parameter names that give you access to method context:

### Available Special Parameters

```csharp
[HarmonyPrefix]
public static bool MyPatch(
    YourClass __instance,           // The object the method was called on
    MethodBase __originalMethod,    // Info about the original method
    float normalParam,              // Normal parameters (match original method)
    ref int modifiableParam         // Use 'ref' to modify parameters
)
{
    // Access instance fields
    var data = __instance.someField;

    // Get method info
    string methodName = __originalMethod.Name;

    // Modify parameters
    modifiableParam = 999;

    return true; // Run original method
}
```

For Postfix:

```csharp
[HarmonyPostfix]
public static void MyPatch(
    ref ReturnType __result,    // The return value (use 'ref' to modify it)
    YourClass __instance        // The instance (same as prefix)
)
{
    // Modify return value
    __result = someNewValue;

    // Access instance data
    MelonLogger.Msg($"Method called on: {__instance.name}");
}
```

### `__instance` Example

Access the object the method was called on:

```csharp
[HarmonyPatch(typeof(Player), "TakeDamage")]
public class TakeDamage_Patch
{
    [HarmonyPrefix]
    public static bool Prefix(Player __instance, float damage)
    {
        // __instance is the Player object
        MelonLogger.Msg($"Player {__instance.name} taking {damage} damage");

        // Access player's health
        if (__instance.health <= 10)
        {
            MelonLogger.Msg("Player is low health! Canceling damage");
            return false; // Invincibility when low health
        }

        return true;
    }
}
```

---

## Patching Properties

Properties (getters/setters) need special handling:

### Patching a Getter

```csharp
// Original property:
// public int Money { get; set; }

// Patch the getter
[HarmonyPatch(typeof(PlayerData), "Money", MethodType.Getter)]
public class MoneyGetter_Patch
{
    [HarmonyPostfix]
    public static void Postfix(ref int __result)
    {
        MelonLogger.Msg($"Someone read money: ${__result}");

        // Optionally modify what value is returned
        // __result += 1000000; // Always show +$1M more than actual
    }
}
```

### Patching a Setter

```csharp
[HarmonyPatch(typeof(PlayerData), "Money", MethodType.Setter)]
public class MoneySetter_Patch
{
    [HarmonyPrefix]
    public static void Prefix(int value)
    {
        MelonLogger.Msg($"Money is being set to: ${value}");
    }
}
```

---

## Il2Cpp Considerations

Schedule I is an **Il2Cpp game**, which requires special handling:

### Conditional Compilation

Always use conditional compilation for game types:

```csharp
#if IL2CPP
using Il2CppScheduleOne.Levelling;
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.Inventory;
#else
using ScheduleOne.Levelling;
using ScheduleOne.GameTime;
using ScheduleOne.Inventory;
#endif
```

### Il2Cpp Collections

Il2Cpp collections need casting:

```csharp
// Il2Cpp List
var items = inventory.items.Cast<Il2CppSystem.Collections.Generic.List<Item>>();

// Iterate Il2Cpp collections
foreach (var item in items)
{
    MelonLogger.Msg($"Item: {item.name}");
}

// Il2Cpp arrays
var itemArray = inventory.GetItems();
for (int i = 0; i < itemArray.Count; i++)
{
    var item = itemArray[i];
}
```

### NetworkSingleton Pattern

Schedule I uses `NetworkSingleton<T>` for global managers. Always check existence:

```csharp
if (NetworkSingleton<InventoryManager>.InstanceExists)
{
    var inventory = NetworkSingleton<InventoryManager>.Instance;
    // Safe to use inventory
}
```

See `GameAccessPatches.cs` in this project for real examples.

---

## Common Patterns

### Pattern 1: Logging All Calls

```csharp
[HarmonyPatch(typeof(GameClass), "ImportantMethod")]
public class LogCalls_Patch
{
    [HarmonyPrefix]
    public static void Prefix(int param1, string param2)
    {
        MelonLogger.Msg($"ImportantMethod called: param1={param1}, param2={param2}");
    }
}
```

### Pattern 2: Conditional Behavior

```csharp
[HarmonyPatch(typeof(Enemy), "Attack")]
public class PeacefulMode_Patch
{
    public static bool PeacefulModeEnabled = false;

    [HarmonyPrefix]
    public static bool Prefix()
    {
        if (PeacefulModeEnabled)
        {
            MelonLogger.Msg("Peaceful mode: Blocking enemy attack");
            return false; // Cancel attack
        }
        return true; // Allow attack
    }
}
```

### Pattern 3: Modifying Calculations

```csharp
[HarmonyPatch(typeof(Stats), "CalculateXP")]
public class DoubleXP_Patch
{
    [HarmonyPostfix]
    public static void Postfix(ref int __result)
    {
        __result *= 2; // Double XP weekend!
    }
}
```

### Pattern 4: Accessing Private Fields

Use `__instance` to access private fields:

```csharp
[HarmonyPatch(typeof(Player), "SomeMethod")]
public class AccessPrivateData_Patch
{
    [HarmonyPostfix]
    public static void Postfix(Player __instance)
    {
        // Access private field using reflection
        var privateData = Traverse.Create(__instance).Field("_privateField").GetValue();

        // Or if you know the type:
        var inventory = Traverse.Create(__instance).Field<Inventory>("_inventory").Value;
    }
}
```

---

## Troubleshooting

### Patch Not Running

**Symptoms:** Your patch code never executes, no logs appear.

**Solutions:**
1. Check you called `HarmonyInstance.PatchAll()` in `OnInitializeMelon()`
2. Verify the class/method names are correct (case-sensitive!)
3. Check namespace imports (Il2Cpp vs Mono)
4. Ensure the method you're patching actually exists in the game

**Debug it:**
```csharp
public override void OnInitializeMelon()
{
    var harmony = HarmonyInstance;
    harmony.PatchAll();

    // List all patches
    var patches = harmony.GetPatchedMethods();
    foreach (var method in patches)
    {
        MelonLogger.Msg($"Patched: {method.DeclaringType.Name}.{method.Name}");
    }
}
```

### TypeLoadException

**Symptoms:** `TypeLoadException: Could not load type...`

**Solutions:**
1. Check Il2Cpp conditional compilation (`#if IL2CPP`)
2. Verify namespace: `Il2CppScheduleOne` not `ScheduleOne`
3. Check build configuration (should be `Il2cpp`, not `CrossCompat` for this mod)
4. Ensure `Assembly-CSharp.dll` is referenced in your project

### Parameter Name Mismatch

**Symptoms:** Patch runs but parameters are null or wrong values.

**Solutions:**
1. Parameter names MUST match the original method signature exactly
2. Parameter types MUST match exactly
3. Use dnSpy to verify the exact method signature

**Example:**
```csharp
// Wrong:
[HarmonyPrefix]
public static void Prefix(int money) // Parameter name doesn't match!

// Correct (if original method has 'amount'):
[HarmonyPrefix]
public static void Prefix(int amount)
```

### Forgot PatchAll()

**Symptoms:** No patches work at all.

**Solution:**
```csharp
public override void OnInitializeMelon()
{
    HarmonyInstance.PatchAll(); // Don't forget this!
}
```

### Wrong Namespace

**Symptoms:** `The type or namespace name 'X' could not be found`

**Solutions:**
1. Add correct using statement
2. Check Il2Cpp prefix (`Il2CppScheduleOne` vs `ScheduleOne`)
3. Verify the type exists in the game's Assembly-CSharp.dll

---

## Complete Example

Here's a complete, working example that demonstrates multiple concepts:

### InventoryDebugger.cs

```csharp
using MelonLoader;
using HarmonyLib;
using UnityEngine;

#if IL2CPP
using Il2CppScheduleOne.Inventory;
using Il2CppScheduleOne.DevUtilities;
#else
using ScheduleOne.Inventory;
using ScheduleOne.DevUtilities;
#endif

namespace MyMod
{
    /// <summary>
    /// Debug mod that logs all inventory operations
    /// </summary>
    public class InventoryDebugger : MelonMod
    {
        public override void OnInitializeMelon()
        {
            // Apply all Harmony patches
            HarmonyInstance.PatchAll();
            LoggerInstance.Msg("Inventory Debugger loaded!");
        }

        public override void OnUpdate()
        {
            // Press F6 to dump full inventory
            if (Input.GetKeyDown(KeyCode.F6))
            {
                DumpInventory();
            }
        }

        private void DumpInventory()
        {
            LoggerInstance.Msg("=== INVENTORY DUMP ===");

            try
            {
                if (NetworkSingleton<InventoryManager>.InstanceExists)
                {
                    var inventory = NetworkSingleton<InventoryManager>.Instance;

                    // Assuming inventory has a GetSlot method
                    for (int i = 0; i < 10; i++) // Check first 10 slots
                    {
                        var item = inventory.GetSlot(i);
                        if (item != null)
                        {
                            LoggerInstance.Msg($"  Slot {i}: {item.name} x{item.quantity}");
                        }
                    }
                }
                else
                {
                    LoggerInstance.Warning("InventoryManager not found!");
                }
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Failed to dump inventory: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Logs every time an item is added to inventory
    /// </summary>
    [HarmonyPatch(typeof(InventoryManager), "AddItem")]
    public class AddItem_Patch
    {
        [HarmonyPrefix]
        public static void Prefix(Item item, int slotIndex)
        {
            MelonLogger.Msg($"[AddItem] Adding {item.name} to slot {slotIndex}");
        }

        [HarmonyPostfix]
        public static void Postfix(Item item, bool __result)
        {
            if (__result)
            {
                MelonLogger.Msg($"[AddItem] Successfully added {item.name}");
            }
            else
            {
                MelonLogger.Warning($"[AddItem] Failed to add {item.name} (full?)");
            }
        }
    }

    /// <summary>
    /// Logs every time an item is removed
    /// </summary>
    [HarmonyPatch(typeof(InventoryManager), "RemoveItem")]
    public class RemoveItem_Patch
    {
        [HarmonyPrefix]
        public static void Prefix(InventoryManager __instance, int slotIndex)
        {
            var item = __instance.GetSlot(slotIndex);
            if (item != null)
            {
                MelonLogger.Msg($"[RemoveItem] Removing {item.name} from slot {slotIndex}");
            }
        }
    }

    /// <summary>
    /// Logs inventory slot access
    /// </summary>
    [HarmonyPatch(typeof(InventoryManager), "GetSlot")]
    public class GetSlot_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(int index, Item __result)
        {
            if (__result != null)
            {
                MelonLogger.Msg($"[GetSlot] Slot {index} accessed: {__result.name}");
            }
        }
    }
}
```

### How to Use This Example

1. Copy the code to your mod project
2. Replace `InventoryManager`, `Item`, etc. with actual game types (use dnSpy to find them)
3. Build and run the mod
4. Watch the MelonLoader console for logs
5. Press F6 in-game to dump inventory

---

## Next Steps

1. **Explore the game:** Use dnSpy to decompile `Assembly-CSharp.dll` and find interesting classes/methods
2. **Start small:** Begin with simple logging patches to understand how methods are called
3. **Experiment:** Try modifying parameters and return values
4. **Reference implementation:** Check `GameAccessPatches.cs` in this project for real-world examples
5. **Read more:** Official Harmony docs: https://harmony.pardeike.net/

Happy modding!

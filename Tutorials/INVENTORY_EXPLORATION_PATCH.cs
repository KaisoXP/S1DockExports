/// <summary>
/// Example Harmony patches for exploring and accessing player inventory in Schedule I.
/// </summary>
/// <remarks>
/// <para><strong>Purpose:</strong></para>
/// <para>
/// This file demonstrates how to use Harmony patches to discover, access, and manipulate
/// the player's inventory system in Schedule I. It's a learning tool and starting point
/// for inventory-related mods.
/// </para>
/// <para><strong>What This Does:</strong></para>
/// <list type="bullet">
/// <item>Finds the inventory manager using NetworkSingleton pattern</item>
/// <item>Logs all inventory slot access operations</item>
/// <item>Intercepts item add/remove operations</item>
/// <item>Provides debug hotkey (F6) to dump full inventory contents</item>
/// <item>Shows runtime discovery of game types using reflection</item>
/// </list>
/// <para><strong>How to Use:</strong></para>
/// <list type="number">
/// <item>Copy this file to your mod project</item>
/// <item>Use dnSpy to find the actual inventory class names in Assembly-CSharp.dll</item>
/// <item>Replace placeholder class names (InventoryManager, Item, etc.) with real ones</item>
/// <item>Build and run your mod</item>
/// <item>Press F6 in-game to dump inventory</item>
/// <item>Watch MelonLoader console for inventory operation logs</item>
/// </list>
/// <para><strong>⚠️ IMPORTANT:</strong></para>
/// <para>
/// The class and method names used here (InventoryManager, GetSlot, AddItem, etc.) are
/// PLACEHOLDERS. You must use dnSpy to find the actual names in Schedule I's code.
/// </para>
/// <para><strong>Finding Inventory Classes:</strong></para>
/// <list type="number">
/// <item>Open dnSpy and load Assembly-CSharp.dll</item>
/// <item>Search for "inventory" (Ctrl+Shift+K)</item>
/// <item>Look for classes like:
///   <list type="bullet">
///   <item>PlayerInventory</item>
///   <item>InventoryManager</item>
///   <item>StorageManager</item>
///   <item>ItemManager</item>
///   </list>
/// </item>
/// <item>Check if they use NetworkSingleton pattern</item>
/// <item>Find methods like GetSlot(), AddItem(), RemoveItem()</item>
/// <item>Replace placeholders in this file with actual names</item>
/// </list>
/// <para><strong>Pattern:</strong> Direct Game Access + Harmony Patching</para>
/// <para>
/// Combines NetworkSingleton direct access (like GameAccessPatches.cs) with Harmony
/// patches to intercept inventory operations as they happen.
/// </para>
/// </remarks>
using System;
using System.Reflection;
using MelonLoader;
using HarmonyLib;
using UnityEngine;

// Conditional compilation for Il2Cpp vs Mono builds
#if IL2CPP
using Il2CppScheduleOne.DevUtilities;
// TODO: Replace these with actual namespaces found in dnSpy
// Example guesses (verify with dnSpy):
// using Il2CppScheduleOne.Inventory;
// using Il2CppScheduleOne.Items;
// using Il2CppScheduleOne.Player;
#else
using ScheduleOne.DevUtilities;
// using ScheduleOne.Inventory;
// using ScheduleOne.Items;
// using ScheduleOne.Player;
#endif

namespace S1DockExports.Tutorials
{
    /// <summary>
    /// Example mod demonstrating inventory access and exploration techniques.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a REFERENCE IMPLEMENTATION showing multiple approaches to accessing
    /// inventory data. Use the techniques that work for your specific needs.
    /// </para>
    /// <para><strong>Techniques Demonstrated:</strong></para>
    /// <list type="number">
    /// <item>NetworkSingleton pattern (direct access)</item>
    /// <item>Harmony Prefix patches (intercept before method runs)</item>
    /// <item>Harmony Postfix patches (intercept after method runs)</item>
    /// <item>Runtime type discovery using reflection</item>
    /// <item>Debug hotkeys for testing</item>
    /// </list>
    /// </remarks>
    public class InventoryExplorerMod : MelonMod
    {
        /// <summary>
        /// Singleton instance for easy access from patches.
        /// </summary>
        public static InventoryExplorerMod? Instance { get; private set; }

        /// <summary>
        /// Called when mod is initialized. Applies all Harmony patches.
        /// </summary>
        public override void OnInitializeMelon()
        {
            Instance = this;

            // Apply all Harmony patches in this assembly
            HarmonyInstance.PatchAll();

            LoggerInstance.Msg("[InventoryExplorer] Mod initialized");
            LoggerInstance.Msg("[InventoryExplorer] Press F6 to dump inventory");
            LoggerInstance.Msg("[InventoryExplorer] Press F7 to discover inventory types");
        }

        /// <summary>
        /// Called every frame. Handles debug hotkeys.
        /// </summary>
        public override void OnUpdate()
        {
            // F6: Dump full inventory contents
            if (Input.GetKeyDown(KeyCode.F6))
            {
                DumpInventoryDirect();
            }

            // F7: Discover inventory-related types at runtime
            if (Input.GetKeyDown(KeyCode.F7))
            {
                DiscoverInventoryTypes();
            }

            // F8: Try to find inventory via GameObject search
            if (Input.GetKeyDown(KeyCode.F8))
            {
                FindInventoryGameObject();
            }
        }

        /// <summary>
        /// Approach 1: Direct access using NetworkSingleton pattern.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is the same pattern used in GameAccessPatches.cs for accessing LevelManager,
        /// TimeManager, etc. If the inventory system uses NetworkSingleton, this will work.
        /// </para>
        /// <para><strong>Steps:</strong></para>
        /// <list type="number">
        /// <item>Find the inventory manager class name in dnSpy</item>
        /// <item>Replace "InventoryManager" with the actual class name</item>
        /// <item>Check if NetworkSingleton&lt;InventoryManager&gt;.InstanceExists</item>
        /// <item>Access via NetworkSingleton&lt;InventoryManager&gt;.Instance</item>
        /// </list>
        /// </remarks>
        private void DumpInventoryDirect()
        {
            LoggerInstance.Msg("=== INVENTORY DUMP (Direct Access) ===");

            try
            {
                // TODO: Replace "InventoryManager" with actual class name from dnSpy
                // Example: Check if this exists in Schedule I's code

                // if (NetworkSingleton<InventoryManager>.InstanceExists)
                // {
                //     var inventory = NetworkSingleton<InventoryManager>.Instance;
                //
                //     LoggerInstance.Msg($"Inventory found! Type: {inventory.GetType().Name}");
                //
                //     // Try to access slots (adjust based on actual API)
                //     for (int i = 0; i < 10; i++)
                //     {
                //         // Replace GetSlot() with actual method name
                //         var item = inventory.GetSlot(i);
                //         if (item != null)
                //         {
                //             LoggerInstance.Msg($"  Slot {i}: {item.name} x{item.quantity}");
                //         }
                //         else
                //         {
                //             LoggerInstance.Msg($"  Slot {i}: Empty");
                //         }
                //     }
                // }
                // else
                // {
                //     LoggerInstance.Warning("InventoryManager NetworkSingleton not found");
                // }

                // Placeholder message
                LoggerInstance.Warning("⚠️ DumpInventoryDirect() is a template!");
                LoggerInstance.Warning("   Use dnSpy to find the actual inventory class name");
                LoggerInstance.Warning("   Then uncomment and update the code above");
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Failed to dump inventory: {ex.Message}");
                LoggerInstance.Error($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Approach 2: Use reflection to discover inventory-related types at runtime.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This approach scans all loaded types to find classes with "Inventory" in the name.
        /// Useful for exploration when you don't know the exact class names.
        /// </para>
        /// <para><strong>What It Does:</strong></para>
        /// <list type="bullet">
        /// <item>Scans Assembly-CSharp for types containing "Inventory"</item>
        /// <item>Lists all matching classes</item>
        /// <item>Shows their methods and properties</item>
        /// <item>Helps you identify the correct class to patch</item>
        /// </list>
        /// </remarks>
        private void DiscoverInventoryTypes()
        {
            LoggerInstance.Msg("=== DISCOVERING INVENTORY TYPES ===");

            try
            {
                // Get Assembly-CSharp (where game code lives)
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                Assembly? gameAssembly = null;

                foreach (var asm in assemblies)
                {
                    if (asm.GetName().Name == "Assembly-CSharp")
                    {
                        gameAssembly = asm;
                        break;
                    }
                }

                if (gameAssembly == null)
                {
                    LoggerInstance.Error("Assembly-CSharp not found!");
                    return;
                }

                LoggerInstance.Msg($"Scanning {gameAssembly.GetName().Name}...");

                // Find all types with "Inventory" in the name
                var inventoryTypes = gameAssembly.GetTypes()
                    .Where(t => t.Name.ToLower().Contains("inventory") ||
                                t.Name.ToLower().Contains("item") ||
                                t.Name.ToLower().Contains("storage"))
                    .ToList();

                LoggerInstance.Msg($"Found {inventoryTypes.Count} potential inventory-related types:");

                foreach (var type in inventoryTypes)
                {
                    LoggerInstance.Msg($"\n--- {type.FullName} ---");

                    // Check if it's a NetworkSingleton
                    bool isNetworkSingleton = type.BaseType?.Name.Contains("NetworkSingleton") ?? false;
                    if (isNetworkSingleton)
                    {
                        LoggerInstance.Msg("  ✓ Uses NetworkSingleton pattern!");
                    }

                    // List public methods (first 10)
                    var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                        .Take(10)
                        .ToList();

                    if (methods.Any())
                    {
                        LoggerInstance.Msg("  Methods:");
                        foreach (var method in methods)
                        {
                            var parameters = string.Join(", ", method.GetParameters()
                                .Select(p => $"{p.ParameterType.Name} {p.Name}"));
                            LoggerInstance.Msg($"    - {method.ReturnType.Name} {method.Name}({parameters})");
                        }
                    }

                    // List public properties (first 10)
                    var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Take(10)
                        .ToList();

                    if (properties.Any())
                    {
                        LoggerInstance.Msg("  Properties:");
                        foreach (var prop in properties)
                        {
                            LoggerInstance.Msg($"    - {prop.PropertyType.Name} {prop.Name}");
                        }
                    }
                }

                LoggerInstance.Msg("\n=== END DISCOVERY ===");
                LoggerInstance.Msg("Use this info to identify the correct class to patch!");
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Failed to discover types: {ex.Message}");
            }
        }

        /// <summary>
        /// Approach 3: Find inventory by searching Unity GameObjects.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Some game systems are attached as components to GameObjects in the scene.
        /// This approach searches for inventory-related components.
        /// </para>
        /// </remarks>
        private void FindInventoryGameObject()
        {
            LoggerInstance.Msg("=== SEARCHING FOR INVENTORY GAMEOBJECTS ===");

            try
            {
                // Search for GameObjects with "inventory" in the name
                var allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
                LoggerInstance.Msg($"Scanning {allObjects.Length} GameObjects...");

                var matches = allObjects
                    .Where(obj => obj.name.ToLower().Contains("inventory") ||
                                  obj.name.ToLower().Contains("player") ||
                                  obj.name.ToLower().Contains("storage"))
                    .ToList();

                LoggerInstance.Msg($"Found {matches.Count} potential matches:");

                foreach (var obj in matches)
                {
                    LoggerInstance.Msg($"\n--- GameObject: {obj.name} ---");
                    LoggerInstance.Msg($"  Active: {obj.activeInHierarchy}");
                    LoggerInstance.Msg($"  Path: {GetGameObjectPath(obj)}");

                    // List all components
                    var components = obj.GetComponents<Component>();
                    LoggerInstance.Msg($"  Components ({components.Length}):");

                    foreach (var component in components)
                    {
                        if (component != null)
                        {
                            LoggerInstance.Msg($"    - {component.GetType().Name}");
                        }
                    }
                }

                LoggerInstance.Msg("\n=== END GAMEOBJECT SEARCH ===");
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Failed to search GameObjects: {ex.Message}");
            }
        }

        /// <summary>
        /// Helper: Get full path of a GameObject in the hierarchy.
        /// </summary>
        private string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            Transform current = obj.transform.parent;

            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return path;
        }
    }

    #region Harmony Patches (Examples)

    /// <summary>
    /// Example Prefix patch: Intercept BEFORE inventory slot is accessed.
    /// </summary>
    /// <remarks>
    /// <para><strong>⚠️ TEMPLATE CODE:</strong></para>
    /// <para>
    /// Replace "InventoryManager" and "GetSlot" with actual class/method names
    /// found in dnSpy. This patch will not work until you do!
    /// </para>
    /// <para><strong>What This Does:</strong></para>
    /// <para>
    /// Logs every time the game tries to read an inventory slot. Useful for
    /// understanding when and why the game accesses inventory.
    /// </para>
    /// </remarks>
    // [HarmonyPatch(typeof(InventoryManager), "GetSlot")]
    // public class GetSlot_Prefix
    // {
    //     [HarmonyPrefix]
    //     public static void Prefix(int slotIndex)
    //     {
    //         MelonLogger.Msg($"[GetSlot] About to read slot {slotIndex}");
    //     }
    // }

    /// <summary>
    /// Example Postfix patch: Intercept AFTER inventory slot is accessed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Logs what item (if any) was retrieved from the slot. The __result parameter
    /// contains the return value from the original GetSlot() method.
    /// </para>
    /// </remarks>
    // [HarmonyPatch(typeof(InventoryManager), "GetSlot")]
    // public class GetSlot_Postfix
    // {
    //     [HarmonyPostfix]
    //     public static void Postfix(int slotIndex, Item __result)
    //     {
    //         if (__result != null)
    //         {
    //             MelonLogger.Msg($"[GetSlot] Slot {slotIndex} returned: {__result.name} x{__result.quantity}");
    //         }
    //         else
    //         {
    //             MelonLogger.Msg($"[GetSlot] Slot {slotIndex} is empty");
    //         }
    //     }
    // }

    /// <summary>
    /// Example Prefix patch: Intercept item addition to inventory.
    /// </summary>
    /// <remarks>
    /// <para><strong>Use Cases:</strong></para>
    /// <list type="bullet">
    /// <item>Log what items are being added</item>
    /// <item>Modify the item before it's added (use ref parameter)</item>
    /// <item>Block certain items from being added (return false)</item>
    /// </list>
    /// </remarks>
    // [HarmonyPatch(typeof(InventoryManager), "AddItem")]
    // public class AddItem_Patch
    // {
    //     [HarmonyPrefix]
    //     public static void Prefix(Item item, int slotIndex)
    //     {
    //         MelonLogger.Msg($"[AddItem] Adding {item.name} x{item.quantity} to slot {slotIndex}");
    //     }
    //
    //     [HarmonyPostfix]
    //     public static void Postfix(Item item, bool __result)
    //     {
    //         if (__result)
    //         {
    //             MelonLogger.Msg($"[AddItem] ✓ Successfully added {item.name}");
    //         }
    //         else
    //         {
    //             MelonLogger.Warning($"[AddItem] ✗ Failed to add {item.name} (slot full?)");
    //         }
    //     }
    // }

    /// <summary>
    /// Example Prefix patch: Intercept item removal from inventory.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Demonstrates accessing the InventoryManager instance via __instance to
    /// read what item is about to be removed before the removal happens.
    /// </para>
    /// </remarks>
    // [HarmonyPatch(typeof(InventoryManager), "RemoveItem")]
    // public class RemoveItem_Patch
    // {
    //     [HarmonyPrefix]
    //     public static void Prefix(InventoryManager __instance, int slotIndex)
    //     {
    //         // Access the instance to see what's in the slot before removal
    //         var item = __instance.GetSlot(slotIndex);
    //
    //         if (item != null)
    //         {
    //             MelonLogger.Msg($"[RemoveItem] Removing {item.name} x{item.quantity} from slot {slotIndex}");
    //         }
    //         else
    //         {
    //             MelonLogger.Warning($"[RemoveItem] Slot {slotIndex} is already empty");
    //         }
    //     }
    // }

    /// <summary>
    /// Example: Block specific items from being added to inventory.
    /// </summary>
    /// <remarks>
    /// <para><strong>Advanced Example:</strong></para>
    /// <para>
    /// This patch prevents certain items (like explosives) from being added to inventory.
    /// Demonstrates how to cancel the original method by returning false.
    /// </para>
    /// </remarks>
    // [HarmonyPatch(typeof(InventoryManager), "AddItem")]
    // public class BlockDangerousItems_Patch
    // {
    //     // List of banned item names
    //     private static readonly string[] BannedItems = { "Explosives", "Poison", "Contraband" };
    //
    //     [HarmonyPrefix]
    //     public static bool Prefix(Item item)
    //     {
    //         // Check if item is banned
    //         if (BannedItems.Contains(item.name))
    //         {
    //             MelonLogger.Warning($"[BlockDangerousItems] Blocked: {item.name} is not allowed!");
    //             return false; // Cancel AddItem, item won't be added
    //         }
    //
    //         return true; // Allow normal AddItem to proceed
    //     }
    // }

    /// <summary>
    /// Example: Modify item quantities when added to inventory.
    /// </summary>
    /// <remarks>
    /// <para><strong>Advanced Example:</strong></para>
    /// <para>
    /// This patch doubles the quantity of any item added to inventory.
    /// Demonstrates modifying parameters using ref.
    /// </para>
    /// </remarks>
    // [HarmonyPatch(typeof(InventoryManager), "AddItem")]
    // public class DoubleItemQuantity_Patch
    // {
    //     [HarmonyPrefix]
    //     public static void Prefix(ref Item item)
    //     {
    //         // Double the quantity before adding
    //         int originalQty = item.quantity;
    //         item.quantity *= 2;
    //
    //         MelonLogger.Msg($"[DoubleItemQuantity] Boosted {item.name} from x{originalQty} to x{item.quantity}");
    //     }
    // }

    #endregion

    #region Utility Class: GameAccess for Inventory

    /// <summary>
    /// Static utility class for direct inventory access (like GameAccessPatches.cs).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class provides static methods for accessing inventory data using the
    /// NetworkSingleton pattern, similar to how GameAccessPatches.cs accesses
    /// LevelManager, TimeManager, etc.
    /// </para>
    /// <para><strong>Pattern:</strong></para>
    /// <code>
    /// if (NetworkSingleton&lt;InventoryManager&gt;.InstanceExists)
    /// {
    ///     var inventory = NetworkSingleton&lt;InventoryManager&gt;.Instance;
    ///     // Use inventory...
    /// }
    /// </code>
    /// </remarks>
    public static class InventoryAccess
    {
        /// <summary>
        /// Gets an item from a specific inventory slot.
        /// </summary>
        /// <param name="slotIndex">Slot index (0-based)</param>
        /// <returns>Item in the slot, or null if empty or not found</returns>
        /// <remarks>
        /// <para><strong>⚠️ TEMPLATE:</strong></para>
        /// <para>
        /// Replace "InventoryManager" with the actual class name from dnSpy.
        /// Replace "GetSlot" with the actual method name.
        /// </para>
        /// </remarks>
        public static object? GetInventorySlot(int slotIndex)
        {
            try
            {
                // TODO: Replace with actual type
                // if (NetworkSingleton<InventoryManager>.InstanceExists)
                // {
                //     var inventory = NetworkSingleton<InventoryManager>.Instance;
                //     return inventory.GetSlot(slotIndex);
                // }

                MelonLogger.Warning("InventoryAccess.GetInventorySlot() is a template - implement it!");
                return null;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Failed to get inventory slot: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets the total number of inventory slots.
        /// </summary>
        /// <returns>Slot count, or 0 if not found</returns>
        public static int GetInventorySize()
        {
            try
            {
                // TODO: Replace with actual type and property
                // if (NetworkSingleton<InventoryManager>.InstanceExists)
                // {
                //     var inventory = NetworkSingleton<InventoryManager>.Instance;
                //     return inventory.SlotCount; // Or maxSlots, size, etc.
                // }

                MelonLogger.Warning("InventoryAccess.GetInventorySize() is a template - implement it!");
                return 0;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Failed to get inventory size: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Checks if inventory has any empty slots.
        /// </summary>
        /// <returns>True if at least one slot is empty, false otherwise</returns>
        public static bool HasEmptySlot()
        {
            try
            {
                int size = GetInventorySize();
                for (int i = 0; i < size; i++)
                {
                    if (GetInventorySlot(i) == null)
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Failed to check empty slots: {ex.Message}");
                return false;
            }
        }
    }

    #endregion
}

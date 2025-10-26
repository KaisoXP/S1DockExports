/// <summary>
/// Harmony patch container and mod instance holder.
/// </summary>
/// <remarks>
/// <para>
/// This file serves as a centralized container for Harmony patches and maintains a reference
/// to the mod instance for use in patch methods.
/// </para>
/// <para><strong>Contents:</strong></para>
/// <list type="bullet">
/// <item><see cref="HarmonyPatches"/> - Static patch container class with mod instance reference</item>
/// </list>
/// <para><strong>Architecture Pattern:</strong> Harmony Patch Container + Singleton Reference</para>
/// <para>
/// Harmony patches are static methods, but they often need access to instance data or configuration.
/// This class holds a static reference to the <see cref="DockExportsMod"/> instance, allowing
/// patch methods to access mod state, configuration, or invoke mod methods.
/// </para>
/// <para><strong>Current State:</strong></para>
/// <para>
/// This file currently contains no actual patches - all active patches have been moved to
/// <see cref="PhoneAppInjector"/>. This class remains as a potential container for future
/// patches that may need centralized mod instance access.
/// </para>
/// <para><strong>Design Pattern Benefits:</strong></para>
/// <list type="bullet">
/// <item>✓ Centralized mod instance storage for all patches</item>
/// <item>✓ Clear separation of concerns (patches vs mod logic)</item>
/// <item>✓ Easy to add new patches with mod instance access</item>
/// <item>✓ Single initialization point via <see cref="SetModInstance"/></item>
/// </list>
/// <para><strong>Alternative Approach:</strong></para>
/// <para>
/// <see cref="PhoneAppInjector"/> uses a different pattern: storing mod instance directly
/// in the injector class and handling its own patches. Both approaches are valid; this class
/// represents a more traditional "patch container" pattern.
/// </para>
/// </remarks>
/// <example>
/// Setting up the patch container (called from DockExportsMod.OnInitializeMelon):
/// <code>
/// public override void OnInitializeMelon()
/// {
///     HarmonyPatches.SetModInstance(this);
///
///     // Harmony will automatically find and apply all [HarmonyPatch] methods
///     // in the HarmonyPatches class
/// }
/// </code>
/// </example>
/// <example>
/// Example of how a patch would use the mod instance:
/// <code>
/// [HarmonyPatch]
/// public static class HarmonyPatches
/// {
///     private static DockExportsMod? _modInstance;
///
///     [HarmonyPatch(typeof(SomeGameClass), "SomeMethod")]
///     [HarmonyPostfix]
///     public static void SomeMethodPatch()
///     {
///         if (_modInstance == null) return;
///
///         // Access mod configuration
///         int requiredRank = _modInstance.RequiredRank;
///
///         // Call mod methods
///         _modInstance.CheckUnlockConditions();
///     }
/// }
/// </code>
/// </example>
#if MONO
using ScheduleOne;
#elif IL2CPP
using Il2CppScheduleOne;
#endif
using HarmonyLib;

namespace S1DockExports.Integrations
{
    /// <summary>
    /// Harmony patch container with mod instance reference for patch callbacks.
    /// </summary>
    /// <remarks>
    /// <para><strong>Design Pattern: Static Patch Container</strong></para>
    /// <para>
    /// This class serves as a centralized location for Harmony patches. The <c>[HarmonyPatch]</c>
    /// attribute on the class allows Harmony to discover patch methods within it.
    /// </para>
    /// <para><strong>Mod Instance Pattern:</strong></para>
    /// <para>
    /// Harmony patch methods are static (they must be static to work as patches), but often
    /// need access to instance data from the mod (configuration, state, methods). This class
    /// solves that by storing a static reference to the mod instance via <see cref="SetModInstance"/>.
    /// </para>
    /// <para><strong>Lifecycle:</strong></para>
    /// <list type="number">
    /// <item><c>DockExportsMod.OnInitializeMelon()</c> calls <see cref="SetModInstance"/> to store mod reference</item>
    /// <item>Harmony's auto-patching discovers all methods with <c>[HarmonyPatch]</c> attributes in this class</item>
    /// <item>Patch methods execute when their target game methods are called</item>
    /// <item>Patches access mod instance via <see cref="_modInstance"/> field</item>
    /// </list>
    /// <para><strong>Current Usage:</strong></para>
    /// <para>
    /// This class currently has no active patches. All patches have been moved to <see cref="PhoneAppInjector"/>,
    /// which uses a self-contained pattern. This class remains available for future patches that may
    /// benefit from centralized mod instance management.
    /// </para>
    /// <para><strong>When to Use This vs PhoneAppInjector:</strong></para>
    /// <list type="bullet">
    /// <item>Use <c>HarmonyPatches</c>: For general-purpose patches that need mod instance access</item>
    /// <item>Use <c>PhoneAppInjector</c>: For phone UI-specific patches with their own state</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// Adding a new patch to this container:
    /// <code>
    /// [HarmonyPatch]
    /// public static class HarmonyPatches
    /// {
    ///     private static DockExportsMod? _modInstance;
    ///
    ///     // Example patch: Hook into player level-up event
    ///     [HarmonyPatch(typeof(LevelManager), "OnLevelUp")]
    ///     [HarmonyPostfix]
    ///     public static void OnPlayerLevelUp_Postfix()
    ///     {
    ///         if (_modInstance == null) return;
    ///
    ///         // Check if player just reached required rank
    ///         _modInstance.CheckUnlockConditions();
    ///     }
    /// }
    /// </code>
    /// </example>
    [HarmonyPatch]
    public static class HarmonyPatches
    {
        /// <summary>
        /// Cached reference to the mod instance for use in patch methods.
        /// </summary>
        /// <remarks>
        /// <para><strong>Null Safety:</strong></para>
        /// <para>
        /// This field is nullable (<c>DockExportsMod?</c>) and must be null-checked before use
        /// in patch methods. It will be <c>null</c> until <see cref="SetModInstance"/> is called
        /// during mod initialization.
        /// </para>
        /// <para><strong>Thread Safety:</strong></para>
        /// <para>
        /// This is a static field accessed from Harmony patches (which run on the game's main thread).
        /// No thread synchronization is needed for single-threaded Unity games, but be aware if
        /// future patches run on background threads.
        /// </para>
        /// <para><strong>Lifecycle:</strong></para>
        /// <para>
        /// Set once during <c>OnInitializeMelon()</c> and remains valid for the lifetime of the mod.
        /// Never set to <c>null</c> after initialization.
        /// </para>
        /// </remarks>
        /// <example>
        /// Safe usage pattern in patch methods:
        /// <code>
        /// [HarmonyPostfix]
        /// public static void SomePatch()
        /// {
        ///     if (_modInstance == null)
        ///     {
        ///         MelonLogger.Warning("Patch called before mod initialization!");
        ///         return;
        ///     }
        ///
        ///     // Safe to use _modInstance here
        ///     _modInstance.DoSomething();
        /// }
        /// </code>
        /// </example>
        private static DockExportsMod? _modInstance;

        /// <summary>
        /// Initializes the mod instance reference for patch callbacks.
        /// </summary>
        /// <param name="modInstance">The DockExportsMod instance to store for patch access</param>
        /// <remarks>
        /// <para><strong>Initialization Pattern:</strong></para>
        /// <para>
        /// This method must be called from <c>DockExportsMod.OnInitializeMelon()</c> before any
        /// patches execute. It establishes the connection between the static patch methods and
        /// the mod's instance data.
        /// </para>
        /// <para><strong>Lifecycle:</strong></para>
        /// <list type="number">
        /// <item>MelonLoader loads the mod DLL</item>
        /// <item><c>DockExportsMod</c> constructor runs</item>
        /// <item><c>OnInitializeMelon()</c> calls <see cref="SetModInstance"/></item>
        /// <item>Harmony auto-applies patches in this class</item>
        /// <item>Game methods are patched and can access <see cref="_modInstance"/></item>
        /// </list>
        /// <para><strong>Error Handling:</strong></para>
        /// <para>
        /// No validation is performed on the <paramref name="modInstance"/> parameter. The caller
        /// (DockExportsMod) is responsible for passing a valid instance. Passing <c>null</c> would
        /// cause all patch methods to early-return, effectively disabling all patches.
        /// </para>
        /// </remarks>
        /// <example>
        /// Called from DockExportsMod initialization:
        /// <code>
        /// public class DockExportsMod : MelonMod
        /// {
        ///     public override void OnInitializeMelon()
        ///     {
        ///         // Initialize patch container with mod instance
        ///         HarmonyPatches.SetModInstance(this);
        ///
        ///         // Now all patches in HarmonyPatches class can access this instance
        ///         LoggerInstance.Msg("Harmony patches initialized");
        ///     }
        /// }
        /// </code>
        /// </example>
        public static void SetModInstance(DockExportsMod  modInstance)
        {
            _modInstance = modInstance;
        }
    }
}

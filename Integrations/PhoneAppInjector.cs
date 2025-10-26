/// <summary>
/// Direct phone app injection via Harmony patching.
/// </summary>
/// <remarks>
/// <para>
/// This file contains Harmony patches that inject the Dock Exports phone app directly into
/// the game's phone UI system, bypassing S1API's PhoneApp infrastructure (which doesn't exist
/// in the available S1API versions or was found to be incompatible).
/// </para>
/// <para><strong>Contents:</strong></para>
/// <list type="bullet">
/// <item><see cref="PhoneAppInjector"/> - Static class with Harmony patches for phone integration</item>
/// </list>
/// <para><strong>Architecture Pattern:</strong> Manual Harmony Patching + UI Cloning</para>
/// <para>
/// Instead of using S1API's PhoneApp base class, this implementation:
/// </para>
/// <list type="number">
/// <item>Patches <c>HomeScreen.Start</c> to inject a custom app icon</item>
/// <item>Clones an existing app icon GameObject as a template</item>
/// <item>Replaces text labels and image sprites with custom values</item>
/// <item>Adds click listener to open the custom app UI</item>
/// <item>Uses reflection to call <see cref="DockExportsApp.OnCreatedUI"/> manually</item>
/// </list>
/// <para><strong>Why This Approach:</strong></para>
/// <para>
/// S1API v2.4.2 Forked may not include PhoneApp infrastructure, or the available version
/// proved incompatible during development. This direct approach provides full control over
/// UI integration at the cost of more manual setup code.
/// </para>
/// <para><strong>Trade-offs:</strong></para>
/// <list type="bullet">
/// <item>✓ Works regardless of S1API PhoneApp availability</item>
/// <item>✓ Full control over icon placement and UI rendering</item>
/// <item>✗ More verbose than S1API's automatic registration</item>
/// <item>✗ Requires maintenance if game's phone UI structure changes</item>
/// </list>
/// </remarks>
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using System;
using System.Linq;
using System.Reflection;

#if IL2CPP
using Il2CppScheduleOne.UI.Phone;
using Il2CppTMPro;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
#else
using ScheduleOne.UI.Phone;
using TMPro;
#endif

namespace S1DockExports.Integrations
{
    /// <summary>
    /// Harmony patches to inject the Dock Exports phone app into the game's phone system.
    /// </summary>
    /// <remarks>
    /// <para><strong>Design Pattern: Direct Harmony Patching</strong></para>
    /// <para>
    /// This class uses Harmony to patch the game's <c>HomeScreen.Start</c> method, injecting
    /// a custom app icon by cloning an existing one and modifying it. When the icon is clicked,
    /// <see cref="OpenDockExportsApp"/> manually creates the app's UI container and invokes
    /// <see cref="DockExportsApp.OnCreatedUI"/> via reflection.
    /// </para>
    /// <para><strong>Harmony Patch Lifecycle:</strong></para>
    /// <list type="number">
    /// <item>Game starts → MelonLoader loads mod → Harmony applies patches</item>
    /// <item>Player opens phone → <c>HomeScreen.Start</c> executes → <see cref="InjectAppIcon"/> runs</item>
    /// <item>Icon cloned, customized, listener added → Icon appears on home screen</item>
    /// <item>Player clicks icon → <see cref="OpenDockExportsApp"/> creates UI</item>
    /// <item>Player returns to menu → <see cref="ResetInjection"/> clears state</item>
    /// </list>
    /// <para><strong>State Management:</strong></para>
    /// <list type="bullet">
    /// <item><see cref="_injected"/> - Prevents re-injection on subsequent HomeScreen.Start calls</item>
    /// <item><see cref="_iconSprite"/> - Caches loaded icon sprite (loaded once, reused)</item>
    /// <item><see cref="_currentAppContainer"/> - Tracks active app UI for cleanup</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// Harmony automatically applies patches when the mod loads:
    /// <code>
    /// // In DockExportsMod.cs:
    /// public override void OnApplicationStart()
    /// {
    ///     var harmony = new HarmonyLib.Harmony("com.kaisoxp.dockexports");
    ///     harmony.PatchAll(Assembly.GetExecutingAssembly());
    ///     // PhoneAppInjector patches are now active
    /// }
    /// </code>
    /// </example>
    [HarmonyPatch]
    public static class PhoneAppInjector
    {
        /// <summary>
        /// Prevents re-injecting the app icon on subsequent HomeScreen.Start calls.
        /// </summary>
        /// <remarks>
        /// Set to true after successful injection in <see cref="InjectAppIcon"/>.
        /// Reset to false in <see cref="ResetInjection"/> when returning to menu.
        /// </remarks>
        private static bool _injected = false;

        /// <summary>
        /// Cached icon sprite loaded from embedded resources.
        /// </summary>
        /// <remarks>
        /// Loaded once by <see cref="LoadIconSprite"/> and reused for all subsequent icon injections.
        /// </remarks>
        private static Sprite? _iconSprite = null;

        /// <summary>
        /// Reference to the currently active app UI container.
        /// </summary>
        /// <remarks>
        /// Created by <see cref="OpenDockExportsApp"/> when the app is opened.
        /// Destroyed when opening a new instance (prevents duplicate UIs) or when
        /// resetting injection state.
        /// </remarks>
        private static GameObject? _currentAppContainer = null;

        /// <summary>
        /// Loads the embedded icon sprite from assembly resources (lazy-loaded and cached).
        /// </summary>
        /// <returns>Sprite if successfully loaded, null otherwise</returns>
        /// <remarks>
        /// <para><strong>Lazy Loading Pattern:</strong></para>
        /// <para>
        /// Checks <see cref="_iconSprite"/> cache first. If already loaded, returns immediately.
        /// Otherwise, loads from embedded resources and caches for future use.
        /// </para>
        /// <para><strong>Resource Name Resolution:</strong></para>
        /// <para>
        /// Tries multiple possible resource names to handle different folder structures:
        /// </para>
        /// <list type="bullet">
        /// <item><c>S1DockExports.DE.png</c> - File in project root</item>
        /// <item><c>S1DockExports.Assets.DE.png</c> - File in Assets folder</item>
        /// <item><c>DE.png</c> - Fallback</item>
        /// </list>
        /// <para><strong>Il2Cpp Compatibility:</strong></para>
        /// <para>
        /// Uses conditional compilation (<c>#if IL2CPP</c>) to handle Il2Cpp's special array types.
        /// Mono builds use standard <c>byte[]</c>, while Il2Cpp builds require <c>Il2CppStructArray&lt;byte&gt;</c>.
        /// </para>
        /// <para><strong>Texture to Sprite Conversion:</strong></para>
        /// <para>
        /// Creates a <c>Texture2D</c> (2×2 placeholder size), loads PNG data via <c>ImageConversion.LoadImage</c>
        /// (which resizes the texture), then wraps it in a <c>Sprite</c> with centered pivot.
        /// </para>
        /// <para><strong>Error Handling:</strong></para>
        /// <para>
        /// If loading fails, logs all available embedded resource names to help debug naming issues.
        /// Returns null on failure - callers should handle gracefully (app will work without custom icon).
        /// </para>
        /// </remarks>
        /// <example>
        /// Usage in InjectAppIcon:
        /// <code>
        /// var iconSprite = LoadIconSprite();
        /// if (iconSprite != null)
        /// {
        ///     // Replace all Image components with custom sprite
        ///     foreach (var img in imageComponents)
        ///     {
        ///         img.sprite = iconSprite;
        ///     }
        /// }
        /// else
        /// {
        ///     MelonLogger.Warning("Using default icon");
        /// }
        /// </code>
        /// </example>
        private static Sprite? LoadIconSprite()
        {
            if (_iconSprite != null)
                return _iconSprite;

            try
            {
                var assembly = Assembly.GetExecutingAssembly();

                // Try multiple possible resource names
                string[] possibleNames = {
                    "S1DockExports.DE.png",
                    "S1DockExports.Assets.DE.png",
                    "DE.png"
                };

                foreach (string resourceName in possibleNames)
                {
                    using var stream = assembly.GetManifestResourceStream(resourceName);
                    if (stream != null)
                    {
                        MelonLogger.Msg($"[DockExports] Found icon resource: {resourceName}");

                        // Read the stream into a byte array
                        byte[] data = new byte[stream.Length];
                        stream.Read(data, 0, data.Length);

                        // Create Texture2D manually
                        Texture2D texture = new Texture2D(2, 2); // Size will be replaced by LoadImage

#if IL2CPP
                        // Convert byte[] to Il2Cpp byte array
                        var il2cppArray = new Il2CppStructArray<byte>(data);
                        bool loaded = UnityEngine.ImageConversion.LoadImage(texture, il2cppArray);
#else
                        bool loaded = UnityEngine.ImageConversion.LoadImage(texture, data);
#endif

                        if (loaded)
                        {
                            // Create sprite from texture
                            _iconSprite = Sprite.Create(texture,
                                new Rect(0, 0, texture.width, texture.height),
                                new Vector2(0.5f, 0.5f)); // Pivot at center

                            MelonLogger.Msg($"[DockExports] ✓ Icon sprite loaded successfully ({texture.width}x{texture.height})");
                            return _iconSprite;
                        }
                        else
                        {
                            MelonLogger.Warning($"[DockExports] ⚠️ ImageConversion.LoadImage failed for {resourceName}");
                        }
                    }
                }

                // Log available resources if not found
                var names = assembly.GetManifestResourceNames();
                MelonLogger.Warning($"[DockExports] Icon resource 'DE.png' not found. Available: {string.Join(", ", names)}");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[DockExports] ❌ Failed to load icon sprite: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Harmony Postfix patch on <c>HomeScreen.Start</c> to inject the Dock Exports app icon.
        /// </summary>
        /// <param name="__instance">The HomeScreen instance being initialized</param>
        /// <remarks>
        /// <para><strong>Harmony Patch Attributes:</strong></para>
        /// <list type="bullet">
        /// <item><c>[HarmonyPatch(typeof(HomeScreen), "Start")]</c> - Target method to patch</item>
        /// <item><c>[HarmonyPostfix]</c> - Run after the original method completes</item>
        /// </list>
        /// <para><strong>Why Postfix:</strong></para>
        /// <para>
        /// We wait for HomeScreen.Start to finish initializing the phone UI, then inject our icon.
        /// If we used Prefix, the HomeScreen wouldn't be fully initialized yet.
        /// </para>
        /// <para><strong>Injection Process:</strong></para>
        /// <list type="number">
        /// <item>Find icon container (child with 7+ children, one per app)</item>
        /// <item>Clone first icon as template (<c>Instantiate</c>)</item>
        /// <item>Replace all text components (both Text and TextMeshProUGUI)</item>
        /// <item>Replace all Image sprites with custom icon</item>
        /// <item>Remove old Button listeners, add new listener pointing to <see cref="OpenDockExportsApp"/></item>
        /// <item>Activate icon and mark <see cref="_injected"/> = true</item>
        /// </list>
        /// <para><strong>UI Cloning Pattern:</strong></para>
        /// <para>
        /// Instead of creating a new GameObject from scratch, we clone an existing app icon.
        /// This ensures our icon has the same structure, components, layout, and styling as
        /// native icons. We then modify only the parts that need customization.
        /// </para>
        /// <para><strong>Defensive Logging:</strong></para>
        /// <para>
        /// Extensively logs the HomeScreen structure (children, icons, components) to help
        /// debug if the game's UI structure changes in future updates.
        /// </para>
        /// <para><strong>Early Return:</strong></para>
        /// <para>
        /// Checks <see cref="_injected"/> flag first - if already injected, skips.
        /// This prevents duplicate icons if HomeScreen.Start is called multiple times.
        /// </para>
        /// </remarks>
        /// <example>
        /// Harmony automatically invokes this patch when HomeScreen.Start executes:
        /// <code>
        /// // Game code (Schedule I):
        /// public class HomeScreen : MonoBehaviour
        /// {
        ///     void Start()
        ///     {
        ///         // Initialize phone UI...
        ///         // After this line, Harmony calls InjectAppIcon(this)
        ///     }
        /// }
        /// </code>
        /// </example>
        [HarmonyPatch(typeof(HomeScreen), "Start")]
        [HarmonyPostfix]
        public static void InjectAppIcon(HomeScreen __instance)
        {
            if (_injected)
            {
                MelonLogger.Msg("[DockExports] 📱 App already injected, skipping");
                return;
            }

            try
            {
                MelonLogger.Msg("[DockExports] 📱 Injecting Dock Exports icon into HomeScreen...");

                var transform = __instance.transform;

                // Find the app icons container on the HomeScreen
                MelonLogger.Msg($"[DockExports] HomeScreen has {transform.childCount} children:");
                Transform iconContainer = null;

                for (int i = 0; i < transform.childCount; i++)
                {
                    var child = transform.GetChild(i);
                    MelonLogger.Msg($"[DockExports]   Child {i}: {child.name} ({child.childCount} children)");

                    // The icon container likely has multiple children (one for each app icon)
                    if (child.childCount >= 7) // We know there are 7 apps
                    {
                        MelonLogger.Msg($"[DockExports]   ^ This looks like the icon container!");
                        iconContainer = child;

                        // Log the icons
                        for (int j = 0; j < child.childCount; j++)
                        {
                            var icon = child.GetChild(j);
                            MelonLogger.Msg($"[DockExports]     Icon {j}: {icon.name}");
                        }
                    }
                }

                if (iconContainer == null)
                {
                    MelonLogger.Warning("[DockExports] Could not find icon container on HomeScreen!");
                    return;
                }

                // Clone the first icon as a template
                var templateIcon = iconContainer.GetChild(0);
                MelonLogger.Msg($"[DockExports] Cloning icon template: {templateIcon.name}");

                // Clone it into the icon container
                var ourIcon = UnityEngine.Object.Instantiate(templateIcon.gameObject, iconContainer);
                ourIcon.name = "DockExportsIcon";

                MelonLogger.Msg($"[DockExports] Created icon GameObject: {ourIcon.name}");

                // Find and modify ALL text components (both Text and TextMeshPro)
                var textComponents = ourIcon.GetComponentsInChildren<UnityEngine.UI.Text>();
                MelonLogger.Msg($"[DockExports] Found {textComponents.Length} Text components on icon");
                foreach (var text in textComponents)
                {
                    MelonLogger.Msg($"[DockExports] Found Text: '{text.text}' -> changing to 'Dock Exports'");
                    text.text = "Dock Exports";
                }

                // Also check for TextMeshPro components (the icon label is likely TextMeshPro)
                var tmpComponents = ourIcon.GetComponentsInChildren<TextMeshProUGUI>();
                MelonLogger.Msg($"[DockExports] Found {tmpComponents.Length} TextMeshPro components on icon");
                foreach (var tmp in tmpComponents)
                {
                    MelonLogger.Msg($"[DockExports] Found TextMeshPro: '{tmp.text}' -> changing to 'Dock Exports'");
                    tmp.text = "Dock Exports";
                }

                // Replace the icon image with our custom sprite
                var iconSprite = LoadIconSprite();
                if (iconSprite != null)
                {
                    var imageComponents = ourIcon.GetComponentsInChildren<UnityEngine.UI.Image>(true); // Include inactive
                    MelonLogger.Msg($"[DockExports] 🖼️ Found {imageComponents.Length} Image components on cloned icon");

                    // Log ALL Image components for diagnostics
                    for (int i = 0; i < imageComponents.Length; i++)
                    {
                        var img = imageComponents[i];
                        string spriteName = img.sprite != null ? img.sprite.name : "null";
                        MelonLogger.Msg($"[DockExports]   [{i}] GameObject: '{img.gameObject.name}' | Sprite: '{spriteName}' | Enabled: {img.enabled}");
                    }

                    // Replace ALL Image sprites with our custom icon
                    // (We'll refine targeting after seeing the logs)
                    int replacedCount = 0;
                    foreach (var img in imageComponents)
                    {
                        if (img.sprite != null) // Only replace if there's an existing sprite
                        {
                            img.sprite = iconSprite;
                            replacedCount++;
                            MelonLogger.Msg($"[DockExports]   ✓ Replaced sprite on '{img.gameObject.name}'");
                        }
                    }

                    MelonLogger.Msg($"[DockExports] 📝 Replaced {replacedCount}/{imageComponents.Length} sprite(s)");
                }
                else
                {
                    MelonLogger.Warning("[DockExports] ⚠️ Could not load custom icon sprite, using default");
                }

                // Find and log the Button component
                var button = ourIcon.GetComponent<UnityEngine.UI.Button>();
                if (button != null)
                {
                    MelonLogger.Msg("[DockExports] Found Button component on icon, adding onClick listener");

                    // Remove existing listeners and add ours
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(new Action(() =>
                    {
                        MelonLogger.Msg("[DockExports] 🎉 DOCK EXPORTS ICON CLICKED!");
                        OpenDockExportsApp();
                    }));
                }

                // Make sure it's active
                ourIcon.SetActive(true);

                _injected = true;
                MelonLogger.Msg("[DockExports] ✅ Dock Exports app successfully injected!");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[DockExports] ❌ Failed to inject app: {ex.Message}");
                MelonLogger.Error($"[DockExports] Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Opens the Dock Exports app UI when the icon is clicked.
        /// </summary>
        /// <remarks>
        /// <para><strong>Manual UI Creation:</strong></para>
        /// <para>
        /// Since we're bypassing S1API's PhoneApp system, we manually create the app UI:
        /// </para>
        /// <list type="number">
        /// <item>Destroy previous container if it exists (prevents duplicate UIs)</item>
        /// <item>Find the AppsCanvas GameObject (where app UIs are rendered)</item>
        /// <item>Create new container GameObject as child of AppsCanvas</item>
        /// <item>Set up RectTransform to fill parent (full-screen app)</item>
        /// <item>Use reflection to call <see cref="DockExportsApp.OnCreatedUI"/> (protected method)</item>
        /// </list>
        /// <para><strong>Why Reflection:</strong></para>
        /// <para>
        /// <see cref="DockExportsApp.OnCreatedUI"/> is a protected method (from S1API PhoneApp base class).
        /// We can't call it directly, so we use <c>BindingFlags.NonPublic</c> reflection to invoke it.
        /// </para>
        /// <para><strong>Container Management:</strong></para>
        /// <para>
        /// Stores the container in <see cref="_currentAppContainer"/> for cleanup.
        /// If the icon is clicked again, the old container is destroyed before creating a new one.
        /// </para>
        /// <para><strong>Error Handling:</strong></para>
        /// <para>
        /// Logs inner exceptions from reflection calls, which helps debug TargetInvocationException
        /// errors (where the actual error is in the invoked method).
        /// </para>
        /// </remarks>
        /// <example>
        /// Called from Button.onClick listener in InjectAppIcon:
        /// <code>
        /// button.onClick.AddListener(new Action(() => {
        ///     MelonLogger.Msg("Icon clicked!");
        ///     OpenDockExportsApp();
        /// }));
        /// </code>
        /// </example>
        private static void OpenDockExportsApp()
        {
            try
            {
                MelonLogger.Msg("[DockExports] 📱 Opening DockExportsApp...");

                // Destroy old container if it exists
                if (_currentAppContainer != null)
                {
                    MelonLogger.Msg("[DockExports] Destroying previous app container");
                    UnityEngine.Object.Destroy(_currentAppContainer);
                    _currentAppContainer = null;
                }

                // Find the phone's AppsCanvas
                var appsCanvas = GameObject.Find("AppsCanvas");
                if (appsCanvas == null)
                {
                    MelonLogger.Warning("[DockExports] ⚠️ Could not find AppsCanvas");
                    return;
                }

                MelonLogger.Msg($"[DockExports] Found AppsCanvas: {appsCanvas.name}");

                // Create DockExportsApp instance and build its UI
                var app = new DockExportsApp(DockExportsMod.Instance);

                // Create a container for the app UI
                _currentAppContainer = new GameObject("DockExportsAppContainer");
                _currentAppContainer.transform.SetParent(appsCanvas.transform, false);

                // Set up RectTransform to fill parent
                var rectTransform = _currentAppContainer.AddComponent<RectTransform>();
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;

                // Call the app's OnCreatedUI to build the interface
                var onCreatedUIMethod = typeof(DockExportsApp).GetMethod("OnCreatedUI",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                if (onCreatedUIMethod != null)
                {
                    MelonLogger.Msg("[DockExports] Calling OnCreatedUI...");
                    onCreatedUIMethod.Invoke(app, new object[] { _currentAppContainer });
                    MelonLogger.Msg("[DockExports] ✓ DockExportsApp UI created successfully!");
                }
                else
                {
                    MelonLogger.Error("[DockExports] ❌ Could not find OnCreatedUI method");
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[DockExports] ❌ Failed to open app: {ex.Message}");
                MelonLogger.Error($"[DockExports] Stack trace: {ex.StackTrace}");

                // Log inner exception for reflection errors
                if (ex.InnerException != null)
                {
                    MelonLogger.Error($"[DockExports] ❌ Inner exception: {ex.InnerException.Message}");
                    MelonLogger.Error($"[DockExports] Inner stack trace: {ex.InnerException.StackTrace}");
                }
            }
        }

        /// <summary>
        /// Resets injection state when returning to the main menu (cleanup).
        /// </summary>
        /// <remarks>
        /// <para><strong>When Called:</strong></para>
        /// <para>
        /// Called by <see cref="DockExportsMod.OnSceneWasUnloaded"/> when the main game scene
        /// unloads and the player returns to the menu. This ensures clean state for the next game session.
        /// </para>
        /// <para><strong>Cleanup Actions:</strong></para>
        /// <list type="number">
        /// <item>Reset <see cref="_injected"/> flag to false (allows re-injection in next game session)</item>
        /// <item>Destroy <see cref="_currentAppContainer"/> if it exists (cleanup app UI)</item>
        /// </list>
        /// <para><strong>Why Reset:</strong></para>
        /// <para>
        /// When the player starts a new game or loads a different save, HomeScreen.Start will
        /// run again. If <see cref="_injected"/> were still true, we'd skip injection and the
        /// icon wouldn't appear. Resetting allows the injection to happen fresh each session.
        /// </para>
        /// <para><strong>Memory Management:</strong></para>
        /// <para>
        /// Destroying the app container prevents memory leaks. Unity's <c>Object.Destroy</c>
        /// queues the GameObject for destruction and garbage collection.
        /// </para>
        /// </remarks>
        /// <example>
        /// Called from mod lifecycle:
        /// <code>
        /// // In DockExportsMod.cs:
        /// public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
        /// {
        ///     if (sceneName == "MainScene")
        ///     {
        ///         PhoneAppInjector.ResetInjection();
        ///         MelonLogger.Msg("Phone app injection reset");
        ///     }
        /// }
        /// </code>
        /// </example>
        public static void ResetInjection()
        {
            _injected = false;

            // Clean up app container
            if (_currentAppContainer != null)
            {
                UnityEngine.Object.Destroy(_currentAppContainer);
                _currentAppContainer = null;
            }
        }
    }
}

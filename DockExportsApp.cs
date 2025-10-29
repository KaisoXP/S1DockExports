/// <summary>
/// Dock Exports phone app UI implementation.
/// </summary>
/// <remarks>
/// <para>
/// This file contains the in-game phone app interface for the Dock Exports system.
/// Players interact with the broker, create shipments, track active consignments,
/// and view history through this UI.
/// </para>
/// <para><strong>Contents:</strong></para>
/// <list type="bullet">
/// <item><see cref="DockExportsApp"/> - Main phone app class extending S1API.PhoneApp</item>
/// </list>
/// <para><strong>Architecture Pattern:</strong> S1API PhoneApp Integration</para>
/// <para>
/// Uses S1API's PhoneApp base class for automatic phone integration. Handles UI lifecycle
/// (creation, destruction, phone close events), embedded icon loading, and state-reactive
/// UI rebuilding (lock screen when not unlocked, full UI when unlocked).
/// </para>
/// <para><strong>UI Structure:</strong></para>
/// <para>
/// - Lock Screen: Shows unlock requirements (Rank 13 + Docks property) until unlocked<br/>
/// - Create Tab: Wholesale and consignment shipment creation buttons<br/>
/// - Active Tab: Current shipment details (quantity, payments made, total value)<br/>
/// - History Tab: Completed shipments log
/// </para>
/// </remarks>
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using S1API.PhoneApp;
using S1API.UI;
using S1API.Internal.Utils;
using S1API.Input;
using S1DockExports.Services;

namespace S1DockExports
{
    /// <summary>
    /// Phone app UI for the Dock Exports system. Displays lock screen, shipment creation,
    /// active consignments, and history.
    /// </summary>
    /// <remarks>
    /// <para><strong>Design Pattern: S1API PhoneApp</strong></para>
    /// <para>
    /// Extends <c>S1API.PhoneApp.PhoneApp</c> base class, which handles:
    /// - Automatic app registration in the in-game phone
    /// - Icon display on phone home screen
    /// - Lifecycle events (OnCreated, OnDestroyed, OnCreatedUI, OnPhoneClosed)
    /// - UI container management
    /// </para>
    /// <para><strong>State Reactivity:</strong></para>
    /// <para>
    /// The UI reacts to two key state changes:
    /// </para>
    /// <list type="number">
    /// <item><strong>Unlock State:</strong> When broker unlocks (via <see cref="OnBrokerUnlocked"/>),
    /// the UI rebuilds from lock screen to full interface</item>
    /// <item><strong>Data Loading:</strong> When shipments are loaded from save data (via <see cref="OnShipmentsLoaded"/>),
    /// the active and history panels refresh</item>
    /// </list>
    /// <para><strong>UI Tabs:</strong></para>
    /// <list type="bullet">
    /// <item><strong>Create:</strong> Wholesale/consignment creation buttons (currently debug buttons with fixed quantities)</item>
    /// <item><strong>Active:</strong> Shows current shipment progress (quantity, price, payments made)</item>
    /// <item><strong>History:</strong> Lists completed shipments with profit/loss summaries</item>
    /// </list>
    /// <para><strong>Icon Loading:</strong></para>
    /// <para>
    /// Uses embedded resource loading via <see cref="LoadEmbeddedIcon"/> and S1API's ImageUtils.
    /// The icon file (DE.png) is compiled into the DLL as an embedded resource.
    /// </para>
    /// <para><strong>Input State Management:</strong></para>
    /// <para>
    /// When the phone closes (<see cref="OnPhoneClosed"/>), input state is reset
    /// (<c>Controls.IsTyping = false</c>) to restore game controls. This prevents input
    /// lock bugs where the player can't move after closing the phone.
    /// </para>
    /// </remarks>
    /// <example>
    /// Registering the app in DockExportsMod:
    /// <code>
    /// using S1API.PhoneApp;
    ///
    /// public override void OnApplicationStart()
    /// {
    ///     var app = new DockExportsApp(this);
    ///     PhoneAppManager.Register(app);
    /// }
    ///
    /// public override void OnSceneWasInitialized(int buildIndex, string sceneName)
    /// {
    ///     if (sceneName == "MainScene")
    ///     {
    ///         PhoneAppManager.InitAll(LoggerInstance);
    ///     }
    /// }
    /// </code>
    /// </example>
    public class DockExportsApp : PhoneApp
    {
        /// <summary>
        /// Convenience accessor for the mod instance. Throws if mod not initialized.
        /// </summary>
        private DockExportsMod Mod => DockExportsMod.Instance ?? throw new InvalidOperationException("DockExportsMod instance not ready");

        /// <summary>
        /// Static singleton instance for event callbacks from outside the app.
        /// </summary>
        /// <remarks>
        /// Used by <see cref="OnBrokerUnlocked"/> to rebuild the UI when unlock state changes.
        /// </remarks>
        private static DockExportsApp? _instance;

        /// <summary>
        /// Root UI container GameObject, stored for rebuilding UI when unlock state changes.
        /// </summary>
        private GameObject? _rootContainer;

        /// <summary>
        /// Internal app identifier (must be unique across all phone apps).
        /// </summary>
        protected override string AppName => "DockExports";

        /// <summary>
        /// Display title shown in the app's header bar.
        /// </summary>
        protected override string AppTitle => "Dock Exports";

        /// <summary>
        /// Short label displayed on the phone home screen icon (2-3 characters recommended).
        /// </summary>
        protected override string IconLabel => "DE";

        /// <summary>
        /// Icon file name for disk loading. Empty string means use embedded resource via <see cref="LoadEmbeddedIcon"/>.
        /// </summary>
        /// <remarks>
        /// <para><strong>Important:</strong></para>
        /// <para>
        /// Leaving this empty prevents S1API from attempting to load from the UserData folder.
        /// Instead, we manually load the embedded icon in <see cref="OnCreated"/> and set it
        /// via <c>SetIconSprite()</c>.
        /// </para>
        /// </remarks>
        protected override string IconFileName => string.Empty;

        /// <summary>
        /// Create tab panel GameObject (shipment creation UI).
        /// </summary>
        private GameObject _createPanel;

        /// <summary>
        /// Active tab panel GameObject (current shipment status UI).
        /// </summary>
        private GameObject _activePanel;

        /// <summary>
        /// History tab panel GameObject (completed shipments log UI).
        /// </summary>
        private GameObject _historyPanel;

        /// <summary>
        /// Lock screen panel GameObject (shown when broker is not unlocked).
        /// </summary>
        private GameObject _lockScreen;

        /// <summary>
        /// Called by S1API when the app is first created (before UI is built).
        /// </summary>
        /// <remarks>
        /// <para><strong>Lifecycle Order:</strong></para>
        /// <para>
        /// OnCreated → OnCreatedUI → (app runs) → OnDestroyed
        /// </para>
        /// <para><strong>Responsibilities:</strong></para>
        /// <list type="number">
        /// <item>Subscribe to data events (<see cref="ShipmentManager.OnShipmentsLoaded"/>) so UI refreshes when save data loads</item>
        /// <item>Load and set the embedded icon via <see cref="LoadEmbeddedIcon"/> and <c>SetIconSprite()</c></item>
        /// </list>
        /// <para><strong>Why Subscribe to Events Here?</strong></para>
        /// <para>
        /// Subscribing in OnCreated (instead of constructor) ensures the app is fully initialized.
        /// If we subscribed in the constructor and an event fired before S1API finished setup,
        /// we could crash trying to access null UI elements.
        /// </para>
        /// </remarks>
        protected override void OnCreated()
        {
            _instance = this;
            MelonLoader.MelonLogger.Msg("[DockExports] 📱 Phone app OnCreated, subscribing to events");

            // Subscribe to shipments loaded event to refresh UI when save data is loaded
            ShipmentManager.OnShipmentsLoaded += OnShipmentsLoaded;

            // Load and set the embedded icon
            MelonLoader.MelonLogger.Msg("[DockExports] Loading embedded icon...");
            var iconSprite = LoadEmbeddedIcon("DE.png");
            if (iconSprite != null)
            {
                SetIconSprite(iconSprite);
                MelonLoader.MelonLogger.Msg("[DockExports] ✓ Icon loaded and set");
            }
            else
            {
                MelonLoader.MelonLogger.Warning("[DockExports] ⚠️ Failed to load embedded icon");
            }
        }

        /// <summary>
        /// Called by S1API when the app is being destroyed (cleanup phase).
        /// </summary>
        /// <remarks>
        /// <para><strong>Cleanup Responsibility:</strong></para>
        /// <para>
        /// Unsubscribe from all events to prevent memory leaks and null reference exceptions.
        /// If we don't unsubscribe, <see cref="OnShipmentsLoaded"/> could fire after the app
        /// is destroyed, causing crashes when trying to access disposed UI elements.
        /// </para>
        /// <para><strong>Memory Leak Prevention:</strong></para>
        /// <para>
        /// Event subscriptions create strong references. If we don't unsubscribe, the garbage
        /// collector can't collect this app instance even after it's "destroyed", causing memory leaks.
        /// </para>
        /// </remarks>
        protected override void OnDestroyed()
        {
            MelonLoader.MelonLogger.Msg("[DockExports] 📱 Phone app OnDestroyed, cleaning up");
            // Unsubscribe from events
            ShipmentManager.OnShipmentsLoaded -= OnShipmentsLoaded;

            if (_instance == this)
            {
                _instance = null;
            }
        }

        /// <summary>
        /// Called by S1API when the player closes the in-game phone.
        /// </summary>
        /// <remarks>
        /// <para><strong>Critical Input State Reset:</strong></para>
        /// <para>
        /// When the phone is open, <c>Controls.IsTyping</c> may be set to <c>true</c>
        /// (for input fields, though this app doesn't currently use them). If we don't reset
        /// it to <c>false</c>, the player's game controls will remain disabled after closing
        /// the phone, making the player character unable to move.
        /// </para>
        /// <para><strong>Focus Clearing:</strong></para>
        /// <para>
        /// <c>EventSystem.current.SetSelectedGameObject(null)</c> clears any focused UI element
        /// (like buttons or input fields). This ensures no UI remains "selected" when the phone
        /// is closed, which could interfere with game input (e.g., pressing spacebar accidentally
        /// triggering the last focused button).
        /// </para>
        /// <para><strong>Reference Implementation:</strong></para>
        /// <para>
        /// This pattern is also used in S1NotesApp. Always reset input state on phone close
        /// to avoid input lock bugs.
        /// </para>
        /// </remarks>
        protected override void OnPhoneClosed()
        {
            MelonLoader.MelonLogger.Msg("[DockExports] 📱 Phone closed, resetting input state");
            // Reset typing state and clear focused UI so gameplay input resumes
            Controls.IsTyping = false;
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        }

        /// <summary>
        /// Static callback invoked by <see cref="DockExportsMod"/> when broker unlocks.
        /// </summary>
        /// <remarks>
        /// <para><strong>Design Pattern: Static Callback via Singleton</strong></para>
        /// <para>
        /// This method is called from DockExportsMod (external to this class) when unlock
        /// conditions are met. It uses the <see cref="_instance"/> singleton reference to
        /// trigger a UI rebuild on the app instance.
        /// </para>
        /// <para><strong>Why Static?</strong></para>
        /// <para>
        /// DockExportsMod doesn't store a direct reference to the app instance, so it calls
        /// this static method. The static method then accesses the singleton <see cref="_instance"/>
        /// to call the instance method <see cref="RebuildUI"/>.
        /// </para>
        /// <para><strong>State Transition:</strong></para>
        /// <para>
        /// When called, the UI transitions from showing the lock screen (requirements message +
        /// debug unlock button) to showing the full interface (Create/Active/History tabs).
        /// </para>
        /// </remarks>
        /// <example>
        /// Called from DockExportsMod after unlocking:
        /// <code>
        /// private void UnlockBroker()
        /// {
        ///     brokerUnlocked = true;
        ///     DockExportsApp.OnBrokerUnlocked(); // Triggers UI rebuild
        /// }
        /// </code>
        /// </example>
        public static void OnBrokerUnlocked()
        {
            MelonLoader.MelonLogger.Msg("[DockExports] 📱 Broker unlocked event received, rebuilding UI");
            _instance?.RebuildUI();
        }

        /// <summary>
        /// Static helper invoked by the mod when shipment data changes.
        /// </summary>
        internal static void RefreshData()
        {
            _instance?.RefreshAllPanels();
        }

        /// <summary>
        /// Completely rebuilds the phone app UI (used when unlock state changes).
        /// </summary>
        /// <remarks>
        /// <para><strong>Rebuild Process:</strong></para>
        /// <list type="number">
        /// <item>Clear all existing UI children from <see cref="_rootContainer"/></item>
        /// <item>Call <see cref="OnCreatedUI"/> with the same container to rebuild from scratch</item>
        /// <item><see cref="OnCreatedUI"/> checks <c>DockExportsMod.Instance?.BrokerUnlocked</c> and builds appropriate UI (lock screen vs full interface)</item>
        /// </list>
        /// <para><strong>Why Full Rebuild Instead of Toggling Visibility?</strong></para>
        /// <para>
        /// We could show/hide lock screen vs full UI panels, but rebuilding ensures the UI
        /// always reflects the current true state. This prevents desync bugs where unlock state
        /// changes but old UI elements remain visible.
        /// </para>
        /// <para><strong>Performance:</strong></para>
        /// <para>
        /// This only happens once (when unlock conditions are first met), so the performance
        /// cost of rebuilding is acceptable. For frequent updates (like refreshing active shipment
        /// data), we use targeted refresh methods like <see cref="RefreshActivePanel"/> instead.
        /// </para>
        /// </remarks>
        private void RebuildUI()
        {
            MelonLoader.MelonLogger.Msg("[DockExports] 🔄 Rebuilding entire phone app UI");

            if (_rootContainer != null)
            {
                // Get the parent container (phone's app container) before destroying our root
                var parentContainer = _rootContainer.transform.parent?.gameObject;

                if (parentContainer != null)
                {
                    // Destroy the old root panel completely
                    UnityEngine.Object.Destroy(_rootContainer);

                    // Recreate UI from scratch with the parent container
                    OnCreatedUI(parentContainer);

                    MelonLoader.MelonLogger.Msg("[DockExports] ✓ UI rebuild complete");
                }
                else
                {
                    MelonLoader.MelonLogger.Warning("[DockExports] ⚠️ Cannot rebuild UI: parent container is null");
                }
            }
            else
            {
                MelonLoader.MelonLogger.Warning("[DockExports] ⚠️ Cannot rebuild UI: root container is null");
            }
        }

        /// <summary>
        /// Refreshes create/active/history panels, guarding against uninitialized UI elements.
        /// </summary>
        private void RefreshAllPanels()
        {
            RefreshCreatePanel();
            RefreshActivePanel();
            RefreshHistoryPanel();
        }

        /// <summary>
        /// Event handler called when <see cref="ShipmentManager"/> finishes loading shipment data from save file.
        /// </summary>
        /// <remarks>
        /// <para><strong>When This Fires:</strong></para>
        /// <para>
        /// When the game loads a save file, ShipmentManager deserializes shipment data from JSON
        /// and invokes this event. At that point, the UI panels need to refresh to display the
        /// loaded data (active shipment + history).
        /// </para>
        /// <para><strong>Why Refresh Instead of Initial Build?</strong></para>
        /// <para>
        /// The UI is built before save data loads (OnCreatedUI runs first), so initially the
        /// panels show "No active shipment" and "No history". When this event fires, we refresh
        /// the panels to display the actual loaded data.
        /// </para>
        /// <para><strong>Subscribed In:</strong></para>
        /// <para>
        /// <see cref="OnCreated"/> (subscribed) and <see cref="OnDestroyed"/> (unsubscribed).
        /// </para>
        /// </remarks>
        private void OnShipmentsLoaded()
        {
            MelonLoader.MelonLogger.Msg("[DockExports] 📱 Shipments loaded event, refreshing UI panels");
            // Refresh the UI when shipments are loaded from save data
            RefreshCreatePanel();
            RefreshActivePanel();
            RefreshHistoryPanel();
        }

        /// <summary>
        /// Called by S1API to build the app's UI inside the provided container.
        /// </summary>
        /// <param name="container">Root UI container provided by S1API's phone system</param>
        /// <remarks>
        /// <para><strong>Lifecycle Order:</strong></para>
        /// <para>
        /// OnCreated → <strong>OnCreatedUI</strong> → (app runs) → OnDestroyed
        /// </para>
        /// <para><strong>Conditional UI Building:</strong></para>
        /// <para>
        /// Checks <c>DockExportsMod.Instance?.BrokerUnlocked</c> to determine which UI to build:
        /// </para>
        /// <list type="bullet">
        /// <item><strong>Locked:</strong> Shows <see cref="BuildLockScreen"/> (requirements + debug unlock button)</item>
        /// <item><strong>Unlocked:</strong> Shows full tabbed interface (Create/Active/History)</item>
        /// </list>
        /// <para><strong>UI Hierarchy (Unlocked):</strong></para>
        /// <code>
        /// container (provided by S1API)
        /// └─ DE_Root (background panel, vertical layout)
        ///    ├─ DE_Header (header bar with title)
        ///    ├─ DE_Tabs (button row for tab switching)
        ///    ├─ DE_Tab_Create_Shipment (create panel, initially visible)
        ///    ├─ DE_Tab_Active_Shipments (active panel, initially hidden)
        ///    └─ DE_Tab_History (history panel, initially hidden)
        /// </code>
        /// <para><strong>Container Storage:</strong></para>
        /// <para>
        /// Stores <paramref name="container"/> in <see cref="_rootContainer"/> for later rebuilds
        /// via <see cref="RebuildUI"/> when unlock state changes.
        /// </para>
        /// <para><strong>Initial State:</strong></para>
        /// <para>
        /// Default tab is "Create". After building all panels, <see cref="RefreshCreatePanel"/>
        /// is called to display current wholesale cooldown status.
        /// </para>
        /// </remarks>
        protected override void OnCreatedUI(GameObject container)
        {
            bool isUnlocked = Mod.BrokerUnlocked;
            MelonLoader.MelonLogger.Msg($"[DockExports] 📱 Building UI: unlocked={isUnlocked}");

            var root = UIFactory.Panel("DE_Root", container.transform, DockExportsConfig.Bg, fullAnchor: true);
            UIFactory.VerticalLayoutOnGO(root);

            // Store OUR root panel for rebuilding later (not the shared phone container!)
            _rootContainer = root;

            var header = UIFactory.Panel("DE_Header", root.transform, DockExportsConfig.Header);
            UIFactory.Text("DE_Title", "Dock Exports", header.transform, 20, TextAnchor.MiddleCenter);

            // If not unlocked, show lock screen
            if (!isUnlocked)
            {
                MelonLoader.MelonLogger.Msg("[DockExports] 📱 Showing lock screen");
                BuildLockScreen(root.transform);
                return;
            }

            MelonLoader.MelonLogger.Msg("[DockExports] 📱 Building full UI (unlocked)");

            var tabs = UIFactory.ButtonRow("DE_Tabs", root.transform, 6);
            _createPanel = CreateTabPanel(root.transform, "Create Shipment");
            _activePanel = CreateTabPanel(root.transform, "Active Shipments");
            _historyPanel = CreateTabPanel(root.transform, "History");

            ShowOnly(_createPanel);

            MakeTabButton("Create", tabs.transform, () => { MelonLoader.MelonLogger.Msg("[DockExports] 📱 Tab: Create"); ShowOnly(_createPanel); RefreshCreatePanel(); });
            MakeTabButton("Active", tabs.transform, () => { MelonLoader.MelonLogger.Msg("[DockExports] 📱 Tab: Active"); ShowOnly(_activePanel); RefreshActivePanel(); });
            MakeTabButton("History", tabs.transform, () => { MelonLoader.MelonLogger.Msg("[DockExports] 📱 Tab: History"); ShowOnly(_historyPanel); RefreshHistoryPanel(); });

            BuildCreatePanel(_createPanel.transform);
            BuildActivePanel(_activePanel.transform);
            BuildHistoryPanel(_historyPanel.transform);

            // Initial refresh of create panel to show cooldown status
            RefreshCreatePanel();
        }

        /// <summary>
        /// Loads an embedded icon image from assembly resources using S1API's ImageUtils.
        /// </summary>
        /// <param name="fileBaseName">Base file name (e.g., "DE.png")</param>
        /// <returns>Sprite if found, null otherwise</returns>
        /// <remarks>
        /// <para><strong>Embedded Resource Pattern:</strong></para>
        /// <para>
        /// Images can be embedded into the DLL during compilation by setting Build Action to "Embedded Resource"
        /// in the .csproj file. At runtime, we load them via <c>Assembly.GetManifestResourceStream()</c>.
        /// </para>
        /// <para><strong>Resource Name Resolution:</strong></para>
        /// <para>
        /// Embedded resource names follow the pattern: <c>Namespace.Folder.Filename</c>. Since we don't know
        /// the exact name (depends on folder structure), we try multiple possible names:
        /// </para>
        /// <list type="bullet">
        /// <item><c>S1DockExports.DE.png</c> (if file is in project root)</item>
        /// <item><c>S1DockExports.Assets.DE.png</c> (if file is in Assets folder)</item>
        /// <item><c>DE.png</c> (fallback, unlikely to work)</item>
        /// </list>
        /// <para><strong>S1API Integration:</strong></para>
        /// <para>
        /// Uses <c>ImageUtils.LoadImageRaw(byte[])</c> from S1API.Internal.Utils to convert byte array
        /// to Unity Sprite. This is more reliable than manually creating Texture2D and Sprite objects.
        /// </para>
        /// <para><strong>Error Handling:</strong></para>
        /// <para>
        /// If the icon isn't found, logs all available embedded resource names to help debug naming issues.
        /// Returns null on failure; caller should handle gracefully (app will still work, just no custom icon).
        /// </para>
        /// </remarks>
        /// <example>
        /// Embedding the icon in .csproj:
        /// <code><![CDATA[
        /// <ItemGroup>
        ///   <EmbeddedResource Include="DE.png" />
        /// </ItemGroup>
        /// ]]></code>
        /// Loading in code:
        /// <code>
        /// var sprite = LoadEmbeddedIcon("DE.png");
        /// if (sprite != null)
        ///     SetIconSprite(sprite);
        /// </code>
        /// </example>
        private Sprite? LoadEmbeddedIcon(string fileBaseName)
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var names = assembly.GetManifestResourceNames();

                // Try different possible resource names
                string[] possibleNames = {
                    $"S1DockExports.{fileBaseName}",
                    $"S1DockExports.Assets.{fileBaseName}",
                    fileBaseName
                };

                foreach (string resourceName in possibleNames)
                {
                    using var stream = assembly.GetManifestResourceStream(resourceName);
                    if (stream != null)
                    {
                        // Read the stream into a byte array
                        byte[] data = new byte[stream.Length];
                        stream.Read(data, 0, data.Length);

                        // Use S1API's ImageUtils to load the image from byte array
                        return ImageUtils.LoadImageRaw(data);
                    }
                }

                // If not found, log available resources
                MelonLoader.MelonLogger.Warning($"[DockExports] Icon resource '{fileBaseName}' not found. Available: {string.Join(", ", names)}");
            }
            catch (Exception ex)
            {
                MelonLoader.MelonLogger.Warning($"[DockExports] Failed to load embedded icon '{fileBaseName}': {ex.Message}");
            }

            return null;
        }

        #region Lock Screen

        /// <summary>
        /// Builds the lock screen UI shown when broker is not unlocked.
        /// </summary>
        /// <param name="parent">Parent transform to build UI under</param>
        /// <remarks>
        /// <para><strong>Lock Screen Contents:</strong></para>
        /// <list type="bullet">
        /// <item><strong>Lock Title:</strong> "🔒 LOCKED" (large, bold)</item>
        /// <item><strong>Requirements List:</strong> "• Hustler Rank" and "• Docks Property Ownership"</item>
        /// <item><strong>Debug Unlock Button:</strong> Allows bypassing unlock requirements for testing</item>
        /// </list>
        /// <para><strong>Debug Unlock Button:</strong></para>
        /// <para>
        /// Calls <c>DockExportsMod.Instance?.DebugForceUnlock()</c> which sets <c>brokerUnlocked = true</c> and triggers
        /// <see cref="OnBrokerUnlocked"/> to rebuild the UI. This is for development testing only -
        /// in production, players must meet the actual requirements.
        /// </para>
        /// <para><strong>UI Styling:</strong></para>
        /// <para>
        /// Uses darker background color (0.1, 0.1, 0.12) to visually distinguish lock screen from
        /// unlocked state. Padding (30px) creates breathing room around the centered content.
        /// </para>
        /// </remarks>
        private void BuildLockScreen(Transform parent)
        {
            MelonLoader.MelonLogger.Msg("[DockExports] 📱 Building lock screen UI");

            var lockPanel = UIFactory.Panel("LockScreen", parent, new Color(0.1f, 0.1f, 0.12f));
            UIFactory.VerticalLayoutOnGO(lockPanel, spacing: 20, padding: new RectOffset(30, 30, 50, 30));

            // Lock icon/title
            UIFactory.Text("LockTitle", "🔒 LOCKED", lockPanel.transform, 24, TextAnchor.MiddleCenter, FontStyle.Bold);

            // Requirements
            UIFactory.Text("Requirements", "Requirements:", lockPanel.transform, 18, TextAnchor.MiddleCenter, FontStyle.Bold);

            UIFactory.Text("Req1", "• Hustler Rank", lockPanel.transform, 16, TextAnchor.MiddleCenter);
            UIFactory.Text("Req2", "• Docks Property Ownership", lockPanel.transform, 16, TextAnchor.MiddleCenter);

            // Spacer
            UIFactory.Panel("Spacer", lockPanel.transform, Color.clear);

            // Debug unlock button (small, bottom corner)
            var debugBtn = UIFactory.RoundedButtonWithLabel("DebugUnlock", "🔓 Debug Unlock", lockPanel.transform,
                new Color(0.3f, 0.3f, 0.35f), 150, 35, 12, new Color(0.7f, 0.7f, 0.7f));

            ButtonUtils.AddListener(debugBtn.Item2, () => {
                MelonLoader.MelonLogger.Msg("[DockExports] 📱 Debug unlock button clicked");
                Mod.DebugForceUnlock();
            });

            MelonLoader.MelonLogger.Msg("[DockExports] ✓ Lock screen built");
        }

        #endregion

        #region Tab Panel Helpers

        /// <summary>
        /// Creates a tab panel GameObject (initially hidden).
        /// </summary>
        /// <param name="parent">Parent transform to create panel under</param>
        /// <param name="label">Display label for the tab (e.g., "Create Shipment")</param>
        /// <returns>Panel GameObject with vertical layout, initially inactive</returns>
        /// <remarks>
        /// <para><strong>Naming Convention:</strong></para>
        /// <para>
        /// Panel name is "DE_Tab_" + sanitized label (spaces replaced with underscores).
        /// Example: "Create Shipment" → "DE_Tab_Create_Shipment"
        /// </para>
        /// <para><strong>Initial State:</strong></para>
        /// <para>
        /// Panel is created inactive (<c>SetActive(false)</c>). Tab buttons control visibility
        /// via <see cref="ShowOnly"/> when clicked.
        /// </para>
        /// </remarks>
        private static GameObject CreateTabPanel(Transform parent, string label)
        {
            var panel = UIFactory.Panel($"DE_Tab_{label.Replace(' ', '_')}", parent, new Color(0.10f, 0.10f, 0.12f));
            panel.SetActive(false);
            UIFactory.VerticalLayoutOnGO(panel);
            return panel;
        }

        /// <summary>
        /// Creates a tab button with click handler.
        /// </summary>
        /// <param name="name">Button name/label</param>
        /// <param name="parent">Parent transform (typically button row)</param>
        /// <param name="onClick">Action to execute when clicked</param>
        /// <remarks>
        /// <para><strong>Standard Tab Button Pattern:</strong></para>
        /// <para>
        /// Tab buttons typically call <see cref="ShowOnly"/> to switch visible panel,
        /// then call the corresponding Refresh method (e.g., <see cref="RefreshCreatePanel"/>)
        /// to update the panel's content.
        /// </para>
        /// <para><strong>Styling:</strong></para>
        /// <para>
        /// Uses <see cref="DockExportsConfig.Accent"/> color (light blue) for consistency
        /// with the app's color scheme. Size: 120×40px, font size: 14pt.
        /// </para>
        /// </remarks>
        private void MakeTabButton(string name, Transform parent, Action onClick)
        {
            var (_, btn, _) = UIFactory.RoundedButtonWithLabel($"DE_Btn_{name}", name, parent, DockExportsConfig.Accent, 120, 40, 14, Color.white);
            ButtonUtils.AddListener(btn, onClick);
        }

        /// <summary>
        /// Shows only the target panel, hiding all other tab panels.
        /// </summary>
        /// <param name="target">Panel to show</param>
        /// <remarks>
        /// <para><strong>Tab Switching Logic:</strong></para>
        /// <para>
        /// Deactivates all three tab panels (_createPanel, _activePanel, _historyPanel),
        /// then activates only the <paramref name="target"/> panel. This ensures exactly
        /// one tab is visible at a time.
        /// </para>
        /// <para><strong>Alternative Approach:</strong></para>
        /// <para>
        /// Could iterate through children and toggle visibility, but hardcoding the three
        /// panels is simpler and more performant for a fixed number of tabs.
        /// </para>
        /// </remarks>
        private void ShowOnly(GameObject target)
        {
            _createPanel.SetActive(false);
            _activePanel.SetActive(false);
            _historyPanel.SetActive(false);
            target.SetActive(true);
        }

        #endregion

        #region Create Tab

        /// <summary>
        /// Text component displaying wholesale cooldown status ("✓ Available" or "⏳ Cooldown: X days").
        /// </summary>
        /// <remarks>
        /// Updated by <see cref="RefreshCreatePanel"/> to reflect current wholesale availability.
        /// Color changes based on status: <see cref="DockExportsConfig.Success"/> (green) when available,
        /// <see cref="DockExportsConfig.Warning"/> (orange) when on cooldown.
        /// </remarks>
        private Text _wholesaleCooldownText;

        /// <summary>
        /// Builds the Create tab UI (shipment creation buttons).
        /// </summary>
        /// <param name="parent">Parent transform to build UI under</param>
        /// <remarks>
        /// <para><strong>Create Tab Contents:</strong></para>
        /// <list type="bullet">
        /// <item><strong>Wholesale Section:</strong> Description, cooldown status text, "Ship Wholesale x100" button</item>
        /// <item><strong>Consignment Section:</strong> Description, details, "Ship Consignment x200" button</item>
        /// <item><strong>Help Note:</strong> Disclaimer that these are debug buttons with fixed quantities</item>
        /// </list>
        /// <para><strong>Debug Buttons:</strong></para>
        /// <para>
        /// Currently uses <c>DebugCreateWholesale(100)</c> and <c>DebugCreateConsignment(200)</c>
        /// with hardcoded quantities. Full UI (with input fields for custom quantities) is planned.
        /// </para>
        /// <para><strong>Cooldown Status:</strong></para>
        /// <para>
        /// <see cref="_wholesaleCooldownText"/> is initialized here and updated by <see cref="RefreshCreatePanel"/>.
        /// It shows "Checking availability..." initially, then updates to actual status.
        /// </para>
        /// </remarks>
        private void BuildCreatePanel(Transform parent)
        {
            UIFactory.Text("Create_Title", "Create Shipment", parent, 18, TextAnchor.MiddleLeft);

            // Wholesale section
            var wholesaleSection = UIFactory.Panel("Wholesale_Section", parent, new Color(0.15f, 0.15f, 0.17f));
            UIFactory.VerticalLayoutOnGO(wholesaleSection, spacing: 5, padding: new RectOffset(10, 10, 10, 10));

            UIFactory.Text("Wholesale_Label", "Wholesale (Safe, Instant)", wholesaleSection.transform, 16, TextAnchor.MiddleLeft);
            UIFactory.Text("Wholesale_Details", $"Cap: {DockExportsConfig.WHOLESALE_CAP} bricks | Cooldown: {DockExportsConfig.WHOLESALE_COOLDOWN_DAYS} days",
                wholesaleSection.transform, 12, TextAnchor.MiddleLeft);

            _wholesaleCooldownText = UIFactory.Text("Wholesale_Cooldown", "Checking availability...",
                wholesaleSection.transform, 13, TextAnchor.MiddleLeft).GetComponent<Text>();

            var wholesaleBtn = UIFactory.RoundedButtonWithLabel("Wholesale_Btn", "Ship Wholesale x100", wholesaleSection.transform,
                DockExportsConfig.Accent, 200, 40, 14, Color.white);
            ButtonUtils.AddListener(wholesaleBtn.Item2, () => {
                Mod.DebugCreateWholesale(100);
                RefreshCreatePanel();
            });

            // Consignment section
            var consignmentSection = UIFactory.Panel("Consignment_Section", parent, new Color(0.15f, 0.15f, 0.17f));
            UIFactory.VerticalLayoutOnGO(consignmentSection, spacing: 5, padding: new RectOffset(10, 10, 10, 10));

            UIFactory.Text("Consignment_Label", "Consignment (Risky, 1.6x Price)", consignmentSection.transform, 16, TextAnchor.MiddleLeft);
            UIFactory.Text("Consignment_Details", $"Cap: {DockExportsConfig.CONSIGNMENT_CAP} bricks | {DockExportsConfig.CONSIGNMENT_INSTALLMENTS} weekly payments",
                consignmentSection.transform, 12, TextAnchor.MiddleLeft);

            var consignmentBtn = UIFactory.RoundedButtonWithLabel("Consignment_Btn", "Ship Consignment x200", consignmentSection.transform,
                DockExportsConfig.Accent, 200, 40, 14, Color.white);
            ButtonUtils.AddListener(consignmentBtn.Item2, () => {
                Mod.DebugCreateConsignment(200);
                RefreshCreatePanel();
            });

            UIFactory.Text("Create_Help", "Note: These are debug buttons with fixed quantities. Full UI coming soon.",
                parent, 11, TextAnchor.UpperLeft);
        }

        /// <summary>
        /// Refreshes the Create tab to display current wholesale cooldown status.
        /// </summary>
        /// <remarks>
        /// <para><strong>When Called:</strong></para>
        /// <list type="bullet">
        /// <item>Initial UI build (end of <see cref="OnCreatedUI"/>)</item>
        /// <item>When "Create" tab button is clicked</item>
        /// <item>After creating a shipment (cooldown may have started)</item>
        /// </list>
        /// <para><strong>Display Logic:</strong></para>
        /// <list type="bullet">
        /// <item><strong>On Cooldown:</strong> "⏳ Cooldown: X day(s) remaining" (orange color)</item>
        /// <item><strong>Available:</strong> "✓ Available now" (green color)</item>
        /// </list>
        /// <para><strong>Null Check:</strong></para>
        /// <para>
        /// Returns early if <see cref="_wholesaleCooldownText"/> is null (not yet initialized).
        /// This can happen if RefreshCreatePanel is called before BuildCreatePanel.
        /// </para>
        /// </remarks>
        private void RefreshCreatePanel()
        {
            if (_wholesaleCooldownText == null) return;

            if (Mod.IsWholesaleOnCooldown)
            {
                int daysRemaining = Mod.WholesaleDaysRemaining;
                _wholesaleCooldownText.text = $"⏳ Cooldown: {daysRemaining} day{(daysRemaining != 1 ? "s" : "")} remaining";
                _wholesaleCooldownText.color = DockExportsConfig.Warning;
            }
            else
            {
                _wholesaleCooldownText.text = "✓ Available now";
                _wholesaleCooldownText.color = DockExportsConfig.Success;
            }
        }

        /// <summary>
        /// Creates an action button with standard styling.
        /// </summary>
        /// <param name="label">Button label text</param>
        /// <param name="parent">Parent transform</param>
        /// <param name="onClick">Action to execute when clicked</param>
        /// <remarks>
        /// <para><strong>Note:</strong></para>
        /// <para>
        /// This method is defined but not currently used in the code. It was likely created
        /// for future UI features. Similar to <see cref="MakeTabButton"/> but with different
        /// button dimensions (180×40 vs 120×40).
        /// </para>
        /// </remarks>
        private void MakeActionButton(string label, Transform parent, Action onClick)
        {
            var (_, btn, _) = UIFactory.RoundedButtonWithLabel($"DE_Action_{label.Replace(' ', '_')}", label, parent, DockExportsConfig.Accent, 180, 40, 14, Color.white);
            ButtonUtils.AddListener(btn, onClick);
        }

        #endregion

        #region Active Tab

        /// <summary>
        /// Text component displaying active shipment details.
        /// </summary>
        /// <remarks>
        /// Updated by <see cref="RefreshActivePanel"/> to show current shipment info or "No active shipment."
        /// if no shipment is active.
        /// </remarks>
        private Text _activeText;

        /// <summary>
        /// Builds the Active tab UI (current shipment status display).
        /// </summary>
        /// <param name="parent">Parent transform to build UI under</param>
        /// <remarks>
        /// <para><strong>Active Tab Contents:</strong></para>
        /// <list type="bullet">
        /// <item><strong>Title:</strong> "Active Shipments"</item>
        /// <item><strong>Body Text:</strong> Shipment details (initially "No active shipment.")</item>
        /// </list>
        /// <para><strong>Dynamic Content:</strong></para>
        /// <para>
        /// The body text (<see cref="_activeText"/>) is updated by <see cref="RefreshActivePanel"/>
        /// whenever the Active tab is opened or shipment data changes.
        /// </para>
        /// </remarks>
        private void BuildActivePanel(Transform parent)
        {
            UIFactory.Text("Active_Title", "Active Shipments", parent, 18, TextAnchor.MiddleLeft);
            _activeText = UIFactory.Text("Active_Body", "No active shipment.", parent, 14, TextAnchor.UpperLeft).GetComponent<Text>();
        }

        /// <summary>
        /// Refreshes the Active tab to display current shipment details.
        /// </summary>
        /// <remarks>
        /// <para><strong>When Called:</strong></para>
        /// <list type="bullet">
        /// <item>When "Active" tab button is clicked</item>
        /// <item>When shipments are loaded from save data (<see cref="OnShipmentsLoaded"/>)</item>
        /// </list>
        /// <para><strong>Display Format (if shipment exists):</strong></para>
        /// <code>
        /// Wholesale | Qty: 100
        /// Unit: 14,700 | Total: 1,470,000
        /// Paid: 1,470,000 | Payments: 1/1
        /// </code>
        /// <code>
        /// Consignment | Qty: 200
        /// Unit: 23,520 | Total: 4,704,000
        /// Paid: 2,352,000 | Payments: 2/4
        /// </code>
        /// <para><strong>Empty State:</strong></para>
        /// <para>
        /// If <c>DockExportsMod.Instance?.Active</c> is null, displays "No active shipment."
        /// </para>
        /// </remarks>
        private void RefreshActivePanel()
        {
            if (_activeText == null) return;

            var s = Mod.Active;
            if (!s.HasValue) { _activeText.text = "No active shipment."; return; }

            var shipment = s.Value;
            _activeText.text =
                $"{shipment.Type} | Qty: {shipment.Quantity}\n" +
                $"Unit: {shipment.UnitPrice:N0} | Total: {shipment.TotalValue:N0}\n" +
                $"Paid: {shipment.TotalPaid:N0} | Payments: {shipment.PaymentsMade}/{(shipment.Type == ShipmentType.Wholesale ? 1 : DockExportsConfig.CONSIGNMENT_INSTALLMENTS)}";
        }

        #endregion

        #region History Tab

        /// <summary>
        /// Text component displaying shipment history log.
        /// </summary>
        /// <remarks>
        /// Updated by <see cref="RefreshHistoryPanel"/> to show completed shipments or "No history."
        /// if no shipments have been completed.
        /// </remarks>
        private Text _historyText;

        /// <summary>
        /// Builds the History tab UI (completed shipments log).
        /// </summary>
        /// <param name="parent">Parent transform to build UI under</param>
        /// <remarks>
        /// <para><strong>History Tab Contents:</strong></para>
        /// <list type="bullet">
        /// <item><strong>Title:</strong> "History"</item>
        /// <item><strong>Body Text:</strong> List of completed shipments (initially "No history.")</item>
        /// </list>
        /// <para><strong>Dynamic Content:</strong></para>
        /// <para>
        /// The body text (<see cref="_historyText"/>) is updated by <see cref="RefreshHistoryPanel"/>
        /// whenever the History tab is opened or shipment data changes.
        /// </para>
        /// </remarks>
        private void BuildHistoryPanel(Transform parent)
        {
            UIFactory.Text("History_Title", "History", parent, 18, TextAnchor.MiddleLeft);
            _historyText = UIFactory.Text("History_Body", "No history.", parent, 14, TextAnchor.UpperLeft).GetComponent<Text>();
        }

        /// <summary>
        /// Refreshes the History tab to display completed shipments log.
        /// </summary>
        /// <remarks>
        /// <para><strong>When Called:</strong></para>
        /// <list type="bullet">
        /// <item>When "History" tab button is clicked</item>
        /// <item>When shipments are loaded from save data (<see cref="OnShipmentsLoaded"/>)</item>
        /// </list>
        /// <para><strong>Display Format:</strong></para>
        /// <para>
        /// Calls <c>ToString()</c> on each <see cref="ShipmentHistoryEntry"/> in <c>DockExportsMod.Instance?.History</c>.
        /// Each entry is displayed on a new line. Example:
        /// </para>
        /// <code>
        /// Wholesale x100: $1,470,000 (received $1,470,000)
        /// Consignment x200: $4,704,000 (received $4,200,000 after losses)
        /// </code>
        /// <para><strong>Empty State:</strong></para>
        /// <para>
        /// If <c>DockExportsMod.Instance?.History</c> is null or empty, displays "No history."
        /// </para>
        /// </remarks>
        private void RefreshHistoryPanel()
        {
            if (_historyText == null) return;

            var history = Mod.History;
            if (history == null || history.Count == 0) { _historyText.text = "No history."; return; }
            var sb = new System.Text.StringBuilder();
            foreach (var e in history) sb.AppendLine(e.ToString());
            _historyText.text = sb.ToString();
        }

        #endregion
    }
}

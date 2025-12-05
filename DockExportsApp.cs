/// <summary>
/// Dock Exports phone app UI implementation.
/// </summary>
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
    /// Phone app UI for the Dock Exports system.
    /// </summary>
    public class DockExportsApp : PhoneApp
    {
        /// <summary>
        /// Reference to the main mod instance for accessing shipment data and methods.
        /// </summary>
        private readonly DockExportsMod _mod;

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
        /// Initializes a new instance of the <see cref="DockExportsApp"/> class.
        /// </summary>
        /// <remarks>
        /// S1API will instantiate this app automatically. We access the mod instance
        /// via the static singleton pattern. The singleton pattern allows <see cref="OnBrokerUnlocked"/>
        /// to be called from external code (e.g., DockExportsMod) to trigger UI rebuilds.
        /// </remarks>
        public DockExportsApp()
        {
            _mod = DockExportsMod.Instance ?? throw new InvalidOperationException("DockExportsMod.Instance is null. Ensure the mod initializes before the app.");
            _instance = this;
        }

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
        protected override void OnCreated()
        {
            base.OnCreated();
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
        protected override void OnDestroyed()
        {
            MelonLoader.MelonLogger.Msg("[DockExports] 📱 Phone app OnDestroyed, cleaning up");
            // Unsubscribe from events
            ShipmentManager.OnShipmentsLoaded -= OnShipmentsLoaded;
            base.OnDestroyed();
        }

        /// <summary>
        /// Called by S1API when the player closes the in-game phone.
        /// </summary>
        protected override void OnPhoneClosed()
        {
            base.OnPhoneClosed();
            MelonLoader.MelonLogger.Msg("[DockExports] 📱 Phone closed, resetting input state");
            // Reset typing state and clear focused UI so gameplay input resumes
            Controls.IsTyping = false;
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        }

        /// <summary>
        /// Static callback invoked by DockExportsMod when broker unlocks.
        /// </summary>
        public static void OnBrokerUnlocked()
        {
            MelonLoader.MelonLogger.Msg("[DockExports] 📱 Broker unlocked event received, rebuilding UI");
            _instance?.RebuildUI();
        }

        /// <summary>
        /// Completely rebuilds the phone app UI (used when unlock state changes).
        /// </summary>
        private void RebuildUI()
        {
            MelonLoader.MelonLogger.Msg("[DockExports] 🔄 Rebuilding entire phone app UI");

            if (_rootContainer != null)
            {
                // Clear all existing UI
                UIFactory.ClearChildren(_rootContainer.transform);

                // Rebuild UI with current unlock state
                OnCreatedUI(_rootContainer);

                MelonLoader.MelonLogger.Msg("[DockExports] ✓ UI rebuild complete");
            }
            else
            {
                MelonLoader.MelonLogger.Warning("[DockExports] ⚠️ Cannot rebuild UI: root container is null");
            }
        }

        /// <summary>
        /// Event handler called when ShipmentManager finishes loading shipment data from save file.
        /// </summary>
        private void OnShipmentsLoaded()
        {
            MelonLoader.MelonLogger.Msg("[DockExports] 📱 Shipments loaded event, refreshing UI panels");
            // Refresh the UI when shipments are loaded from save data
            RefreshActivePanel();
            RefreshHistoryPanel();
        }

        /// <summary>
        /// Called by S1API to build the app's UI inside the provided container.
        /// </summary>
        protected override void OnCreatedUI(GameObject container)
        {
            // Store root container for rebuilding later
            _rootContainer = container;

            bool isUnlocked = _mod.BrokerUnlocked;
            MelonLoader.MelonLogger.Msg($"[DockExports] 📱 Building UI: unlocked={isUnlocked}");

            var root = UIFactory.Panel("DE_Root", container.transform, DockExportsConfig.Bg, fullAnchor: true);
            UIFactory.VerticalLayoutOnGO(root);

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
        /// Loads an embedded icon image from assembly resources.
        /// </summary>
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
                _mod.DebugForceUnlock();
            });

            MelonLoader.MelonLogger.Msg("[DockExports] ✓ Lock screen built");
        }

        #endregion

        #region Tab Panel Helpers

        /// <summary>
        /// Creates a tab panel GameObject (initially hidden).
        /// </summary>
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
        private void MakeTabButton(string name, Transform parent, Action onClick)
        {
            var (_, btn, _) = UIFactory.RoundedButtonWithLabel($"DE_Btn_{name}", name, parent, DockExportsConfig.Accent, 120, 40, 14, Color.white);
            ButtonUtils.AddListener(btn, onClick);
        }

        /// <summary>
        /// Shows only the target panel, hiding all other tab panels.
        /// </summary>
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
        /// Text component displaying wholesale cooldown status.
        /// </summary>
        private Text _wholesaleCooldownText;

        /// <summary>
        /// Builds the Create tab UI (shipment creation buttons).
        /// </summary>
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
                _mod.DebugCreateWholesale(100);
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
                _mod.DebugCreateConsignment(200);
                RefreshCreatePanel();
            });

            UIFactory.Text("Create_Help", "Note: These are debug buttons with fixed quantities. Full UI coming soon.",
                parent, 11, TextAnchor.UpperLeft);
        }

        /// <summary>
        /// Refreshes the Create tab to display current wholesale cooldown status.
        /// </summary>
        private void RefreshCreatePanel()
        {
            if (_wholesaleCooldownText == null) return;

            if (_mod.IsWholesaleOnCooldown)
            {
                int daysRemaining = _mod.WholesaleDaysRemaining;
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
        private Text _activeText;

        /// <summary>
        /// Builds the Active tab UI (current shipment status display).
        /// </summary>
        private void BuildActivePanel(Transform parent)
        {
            UIFactory.Text("Active_Title", "Active Shipments", parent, 18, TextAnchor.MiddleLeft);
            _activeText = UIFactory.Text("Active_Body", "No active shipment.", parent, 14, TextAnchor.UpperLeft).GetComponent<Text>();
        }

        /// <summary>
        /// Refreshes the Active tab to display current shipment details.
        /// </summary>
        private void RefreshActivePanel()
        {
            var s = _mod.Active;
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
        private Text _historyText;

        /// <summary>
        /// Builds the History tab UI (completed shipments log).
        /// </summary>
        private void BuildHistoryPanel(Transform parent)
        {
            UIFactory.Text("History_Title", "History", parent, 18, TextAnchor.MiddleLeft);
            _historyText = UIFactory.Text("History_Body", "No history.", parent, 14, TextAnchor.UpperLeft).GetComponent<Text>();
        }

        /// <summary>
        /// Refreshes the History tab to display completed shipments log.
        /// </summary>
        private void RefreshHistoryPanel()
        {
            if (_mod.History == null || _mod.History.Count == 0) { _historyText.text = "No history."; return; }
            var sb = new System.Text.StringBuilder();
            foreach (var e in _mod.History) sb.AppendLine(e.ToString());
            _historyText.text = sb.ToString();
        }

        #endregion
    }
}

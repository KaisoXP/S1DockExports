/// <summary>
/// Entry point and mod state manager for S1DockExports.
/// </summary>
/// <remarks>
/// This file contains the main mod class (DockExportsMod) which orchestrates the entire mod.
/// Responsibilities:
/// - Mod lifecycle management (initialization, updates, cleanup)
/// - Broker unlock logic (checks rank and property ownership)
/// - Friday consignment payout processing
/// - Coordination between business logic (ShipmentManager) and UI (DockExportsApp)
/// - Communication with game systems via GameAccess wrapper
///
/// Architecture Pattern: Coordinator/Orchestrator
/// - Delegates heavy lifting to specialized classes (ShipmentManager, GameAccess)
/// - Responds to MelonLoader lifecycle events
/// - Provides public API for UI layer (DockExportsApp)
///
/// See Also:
/// - ShipmentManager: Business logic for shipments
/// - GameAccess: Direct game system access
/// - DockExportsApp: Phone app UI
/// - DockExportsConfig: Configuration constants
/// </remarks>
using System;
using MelonLoader;
using S1API.Internal.Abstraction;
using S1DockExports.Services;
using S1DockExports.Integrations;
using UnityEngine;
using GameAccess = S1DockExports.Integrations.GameAccess;

// MelonLoader mod registration attributes
[assembly: MelonInfo(typeof(S1DockExports.DockExportsMod), "S1DockExports", "1.0.0", "KaisoXP")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace S1DockExports
{
    /// <summary>
    /// Main entry point for the Dock Exports mod. Manages mod lifecycle, unlock conditions,
    /// and coordinates between business logic and UI layers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class extends <see cref="MelonMod"/> and provides the main coordination logic for the mod.
    /// It does NOT contain business logic directly - that's delegated to ShipmentManager.
    /// </para>
    /// <para><strong>Key Responsibilities:</strong></para>
    /// <list type="bullet">
    /// <item>Check unlock conditions (Hustler rank + Docks property ownership)</item>
    /// <item>Process Friday consignment payouts</item>
    /// <item>Coordinate between ShipmentManager and UI</item>
    /// <item>Handle scene transitions and cleanup</item>
    /// <item>Provide public API for DockExportsApp</item>
    /// </list>
    /// <para><strong>Unlock Flow:</strong></para>
    /// <code>
    /// OnUpdate (every frame, throttled to 1/sec)
    ///   → CanUnlock() checks rank + property
    ///     → UnlockBroker() when conditions met
    ///       → brokerUnlocked = true
    ///       → SendBrokerMessage (intro SMS)
    /// </code>
    /// <para><strong>Friday Payout Flow:</strong></para>
    /// <code>
    /// OnUpdate (every frame)
    ///   → IsFriday() + ShouldProcessPayout()
    ///     → ProcessConsignmentWeek()
    ///       → ShipmentManager.ProcessConsignmentPayment()
    ///       → AddMoneyToPlayer()
    ///       → SendBrokerMessage()
    ///       → Save game
    /// </code>
    /// </remarks>
    /// <example>
    /// Accessing the mod instance from other classes:
    /// <code>
    /// var mod = DockExportsMod.Instance;
    /// if (mod != null &amp;&amp; mod.BrokerUnlocked)
    /// {
    ///     mod.DebugCreateWholesale(100);
    /// }
    /// </code>
    /// </example>
    public class DockExportsMod : MelonMod
    {
        /// <summary>
        /// Singleton instance of the mod. Accessible from anywhere in the codebase.
        /// </summary>
        /// <remarks>
        /// Set in <see cref="OnInitializeMelon"/> and cleared in <see cref="OnApplicationQuit"/>.
        /// Use this to access mod functionality from UI or other systems.
        /// </remarks>
        /// <example>
        /// <code>
        /// if (DockExportsMod.Instance != null)
        /// {
        ///     var history = DockExportsMod.Instance.History;
        /// }
        /// </code>
        /// </example>
        public static DockExportsMod? Instance { get; private set; }

        /// <summary>
        /// Internal flag tracking whether the broker has been unlocked.
        /// </summary>
        private bool brokerUnlocked = false;

        /// <summary>
        /// Gets whether the broker has been unlocked (player met rank + property requirements).
        /// </summary>
        /// <remarks>
        /// This property is used by DockExportsApp to determine whether to show the lock screen
        /// or the full phone app interface.
        /// </remarks>
        public bool BrokerUnlocked => brokerUnlocked;

        /// <summary>
        /// Timestamp of last unlock check log (used to throttle logging to once per second).
        /// </summary>
        private float _lastUnlockCheckLog = 0f;

        /// <summary>
        /// Called by MelonLoader after the mod is fully loaded. Initializes the mod instance and systems.
        /// </summary>
        /// <remarks>
        /// <para><strong>Execution Order:</strong></para>
        /// <list type="number">
        /// <item>OnApplicationStart()</item>
        /// <item>OnInitializeMelon() ← YOU ARE HERE</item>
        /// <item>OnSceneWasLoaded("Menu")</item>
        /// </list>
        /// <para>
        /// ShipmentManager (which extends Saveable) is automatically discovered by S1API v2.4.2+,
        /// so no manual registration is required here.
        /// </para>
        /// </remarks>
        public override void OnInitializeMelon()
        {
            Instance = this;
            MelonLogger.Msg("[DockExports] 🎮 Mod initialized");

            // ShipmentManager (Saveable) is auto-discovered by S1API v2.4.2+
            // No manual registration needed
            MelonLogger.Msg("[DockExports] ✓ Initialization complete");
        }

        /// <summary>
        /// Called very early in the game's startup, before game systems are initialized.
        /// </summary>
        public override void OnApplicationStart()
        {
            MelonLogger.Msg("[DockExports] 📱 Phone app registration ready (S1API handles icon + lifecycle)");
        }

        /// <summary>
        /// Called when a scene is about to be loaded. Used for pre-load setup and data cleanup.
        /// </summary>
        /// <param name="buildIndex">Unity scene build index.</param>
        /// <param name="sceneName">Name of the scene being initialized (e.g., "Menu", "Main").</param>
        /// <remarks>
        /// <para><strong>Scene Flow:</strong></para>
        /// <list type="bullet">
        /// <item>"Menu" → Main menu (clear data to prevent cross-save issues)</item>
        /// <item>"Main" → In-game scene (phone app available once unlocked)</item>
        /// </list>
        /// <para>
        /// <strong>Why clear data on Menu scene?</strong>
        /// When returning to menu (e.g., quitting to main menu), we clear all shipment data
        /// to prevent it carrying over to a different save file if the player loads another game.
        /// </para>
        /// </remarks>
        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            MelonLogger.Msg($"[DockExports] 🗺️ Scene initialized: {sceneName} (index: {buildIndex})");

            // Clear shipment data when returning to menu to prevent cross-save contamination
            if (sceneName == "Menu")
            {
                MelonLogger.Msg("[DockExports] Clearing data for menu scene");
                ShipmentManager.Instance.ClearAllData();
                brokerUnlocked = false;
            }
        }

        /// <summary>
        /// Called when a scene is fully unloaded. Used for cleanup and resource release.
        /// </summary>
        /// <param name="buildIndex">Unity scene build index.</param>
        /// <param name="sceneName">Name of the scene being unloaded.</param>
        /// <remarks>
        /// We perform additional cleanup here as a safety measure, in case
        /// OnSceneWasInitialized didn't catch the transition.
        /// </remarks>
        public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
        {
            MelonLogger.Msg($"[DockExports] 🗺️ Scene unloaded: {sceneName}");

            // Additional cleanup when leaving menu
            if (sceneName == "Menu")
            {
                MelonLogger.Msg("[DockExports] Additional menu cleanup");
                ShipmentManager.Instance.ClearAllData();
                brokerUnlocked = false;
            }
        }

        /// <summary>
        /// Called every frame (60 times per second). Checks unlock conditions and processes Friday payouts.
        /// </summary>
        /// <remarks>
        /// <para><strong>⚠️ Performance Warning:</strong></para>
        /// <para>
        /// This method runs 60 times per second (3,600 times per minute)! We use throttling
        /// to limit expensive operations:
        /// </para>
        /// <list type="bullet">
        /// <item>Unlock checks: Once per second (via _lastUnlockCheckLog)</item>
        /// <item>Friday payouts: Once per day (via ShipmentManager.LastProcessedDay)</item>
        /// </list>
        /// <para><strong>Logic Flow:</strong></para>
        /// <code>
        /// IF not unlocked:
        ///   IF 1 second since last check:
        ///     Check rank + property → Unlock if met
        ///
        /// IF unlocked AND Friday AND should process:
        ///   ProcessConsignmentWeek()
        /// </code>
        /// </remarks>
        public override void OnUpdate()
        {
            // Check unlock conditions
            if (!brokerUnlocked)
            {
                // Log unlock check every second to avoid spam
                if (Time.time - _lastUnlockCheckLog >= 1.0f)
                {
                    int playerLevel = GameAccess.GetPlayerRank();
                    bool docksOwned = GameAccess.IsPropertyOwned(DockExportsConfig.DOCKS_PROPERTY_ID);

                    MelonLogger.Msg($"[DockExports] Unlock check: Level={playerLevel}, DocksOwned={docksOwned}");
                    _lastUnlockCheckLog = Time.time;
                }

                if (CanUnlock())
                {
                    UnlockBroker();
                }
            }

            // Process Friday consignment payouts
            if (brokerUnlocked && IsFriday() && ShouldProcessPayout())
            {
                ProcessConsignmentWeek();
            }
        }

        /// <summary>
        /// Checks if the player meets unlock requirements (Hustler rank + Docks property ownership).
        /// </summary>
        /// <returns>
        /// <c>true</c> if player has reached the required broker level and owns the Docks property; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// <para><strong>Unlock Requirements:</strong></para>
        /// <list type="number">
        /// <item>Player level ≥ <see cref="DockExportsConfig.REQUIRED_RANK_LEVEL"/> (Hustler III)</item>
        /// <item>Player owns "Docks Warehouse" property</item>
        /// </list>
        /// </remarks>
        private bool CanUnlock()
        {
            int requiredLevel = DockExportsConfig.REQUIRED_RANK_LEVEL;
            int playerLevel = GameAccess.GetPlayerRank();
            bool levelOk = playerLevel >= requiredLevel;

            if (!levelOk)
            {
                if (Time.time - _lastUnlockCheckLog >= 5.0f)
                {
                    MelonLogger.Msg($"[DockExports] Level check: {playerLevel} / {requiredLevel} (needs Hustler III+)");
                }
                return false;
            }

            bool propertyOwned = GameAccess.IsPropertyOwned(DockExportsConfig.DOCKS_PROPERTY_ID);

            if (!propertyOwned && Time.time - _lastUnlockCheckLog >= 5.0f)
            {
                MelonLogger.Msg($"[DockExports] Property check: {DockExportsConfig.DOCKS_PROPERTY_ID} owned? {propertyOwned}");
            }

            return propertyOwned;
        }

        /// <summary>
        /// Checks if the current in-game day is Friday.
        /// </summary>
        /// <returns><c>true</c> if today is Friday (day index 4); otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// Day index mapping: 0=Monday, 1=Tuesday, 2=Wednesday, 3=Thursday, 4=Friday, 5=Saturday, 6=Sunday.
        /// </remarks>
        private bool IsFriday() => GameAccess.GetCurrentDayOfWeek() == 4; // Friday = 4

        /// <summary>
        /// Determines if a consignment payout should be processed today.
        /// </summary>
        /// <returns>
        /// <c>true</c> if there's an active consignment shipment and we haven't processed today yet;
        /// otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// <para><strong>Prevents double-processing:</strong></para>
        /// <para>
        /// OnUpdate runs 60 times per second, so we track the last processed day to ensure
        /// we only process once per Friday, not 216,000 times!
        /// </para>
        /// </remarks>
        private bool ShouldProcessPayout()
        {
            // Only process once per day using elapsed days tracking
            int currentDay = GameAccess.GetElapsedDays();
            if (currentDay == ShipmentManager.Instance.LastProcessedDay)
                return false;

            // Check if there's an active consignment shipment
            var active = ShipmentManager.Instance.ActiveShipment;
            return active.HasValue && active.Value.Type == ShipmentType.Consignment;
        }

        /// <summary>
        /// Unlocks the broker and triggers the intro sequence.
        /// </summary>
        /// <remarks>
        /// <para><strong>Unlock Sequence:</strong></para>
        /// <list type="number">
        /// <item>Set brokerUnlocked flag to true</item>
        /// <item>Send broker intro SMS message</item>
        /// <item>Phone app UI automatically updates to show full interface</item>
        /// </list>
        /// <para>
        /// Once unlocked, the player can access the Dock Exports phone app and create shipments.
        /// </para>
        /// </remarks>
        private void UnlockBroker()
        {
            MelonLogger.Msg("[DockExports] 🎉 BROKER UNLOCKED! Triggering intro sequence");
            brokerUnlocked = true;

            // Send broker intro message via phone call
            MelonLogger.Msg("[DockExports] Sending broker intro SMS...");
            SendBrokerMessage(BrokerMessages.INTRO_SMS);

            DockExportsApp.OnBrokerUnlocked();
            DockExportsApp.RefreshData();

            MelonLogger.Msg("[DockExports] ✓ Broker unlock complete");
        }

        /// <summary>
        /// Sends a broker message to the player (currently logged only due to S1API limitations).
        /// </summary>
        /// <param name="message">The message text to send.</param>
        /// <remarks>
        /// <para><strong>⚠️ TEMPORARILY DISABLED:</strong></para>
        /// <para>
        /// S1API v2.4.2's CallManager has a TypeLoadException when trying to load
        /// ScheduleOne.Calling.CallManager. The in-game SMS/call system is currently
        /// inaccessible via S1API.
        /// </para>
        /// <para><strong>Workaround:</strong></para>
        /// <para>
        /// Messages are logged to the console for now. Future implementations will either:
        /// </para>
        /// <list type="bullet">
        /// <item>Use direct game access (via Harmony patches)</item>
        /// <item>Wait for S1API fix</item>
        /// <item>Implement custom in-game notification system</item>
        /// </list>
        /// </remarks>
        /// <example>
        /// <code>
        /// SendBrokerMessage("Week 1 cleared. $1,176,000 released.");
        /// </code>
        /// </example>
        private void SendBrokerMessage(string message)
        {
            // TEMPORARILY DISABLED: CallManager is broken in S1API v2.4.2
            // TypeLoadException: Could not load type 'ScheduleOne.Calling.CallManager'
            string preview = message.Length > 50 ? message.Substring(0, 50) + "..." : message;
            MelonLogger.Msg($"[DockExports] 📞 Broker message (SMS disabled): \"{preview}\"");
            MelonLogger.Msg($"[DockExports] 📝 Full message: {message}");

            // TODO: Re-enable when S1API is fixed or implement direct game access for calls
            // try
            // {
            //     var call = new BrokerPhoneCall(message);
            //     CallManager.QueueCall(call);
            //     MelonLogger.Msg("[DockExports] ✓ Message queued successfully");
            // }
            // catch (Exception ex)
            // {
            //     MelonLogger.Error($"[DockExports] ✗ Failed to send broker message: {ex.Message}");
            //     MelonLogger.Error($"[DockExports] Stack trace: {ex.StackTrace}");
            // }
        }

        #region Consignment Processing

        /// <summary>
        /// Processes a Friday consignment payout with loss calculation and player compensation.
        /// </summary>
        /// <remarks>
        /// <para><strong>Processing Flow:</strong></para>
        /// <list type="number">
        /// <item>Get active shipment and calculate week number</item>
        /// <item>Call ShipmentManager.ProcessConsignmentPayment() to:
        ///   <list type="bullet">
        ///   <item>Roll for loss (25% chance of 15-60% loss)</item>
        ///   <item>Calculate actual payout after losses</item>
        ///   <item>Update shipment state</item>
        ///   <item>Archive if complete (4 weeks paid)</item>
        ///   </list>
        /// </item>
        /// <item>Mark current day as processed (prevents double-processing)</item>
        /// <item>Add money to player's account</item>
        /// <item>Send broker message (loss message or success message)</item>
        /// <item>Request game save</item>
        /// </list>
        /// <para><strong>Called by:</strong> <see cref="OnUpdate"/> when it's Friday and <see cref="ShouldProcessPayout"/> returns true.</para>
        /// <para><strong>Frequency:</strong> Once per Friday, maximum 4 times per consignment (4 weekly payments).</para>
        /// </remarks>
        /// <example>
        /// Example log output for a loss event:
        /// <code>
        /// [DockExports] 💰 Processing Friday consignment payout: Week 2/4
        /// [DockExports] ⚠️ Loss event: 37% loss, payout: $740,880 (expected: $1,176,000)
        /// [DockExports] 💵 Adding $740,880 to player
        /// [DockExports] ✓ Money added successfully
        /// [DockExports] 📞 Broker message: "Week 2: Customs flagged a container. 37% lost..."
        /// [DockExports] 💾 Requesting game save
        /// </code>
        /// </example>
        private void ProcessConsignmentWeek()
        {
            var shipment = ShipmentManager.Instance.ActiveShipment;
            int weekNumber = (shipment?.PaymentsMade ?? 0) + 1;
            int totalWeeks = DockExportsConfig.CONSIGNMENT_INSTALLMENTS;

            MelonLogger.Msg($"[DockExports] 💰 Processing Friday consignment payout: Week {weekNumber}/{totalWeeks}");

            int expectedPayout = (shipment?.TotalValue ?? 0) / DockExportsConfig.CONSIGNMENT_INSTALLMENTS;
            int actualPayout = ShipmentManager.Instance.ProcessConsignmentPayment(out int lossPercent, out int floorTopUp);
            int currentDay = GameAccess.GetElapsedDays();
            int basePayout = actualPayout - floorTopUp;

            if (lossPercent > 0)
            {
                MelonLogger.Warning($"[DockExports] ⚠️ Loss event: {lossPercent}% loss, payout: ${basePayout:N0} (expected: ${expectedPayout:N0})");
            }
            else
            {
                MelonLogger.Msg($"[DockExports] ✓ No losses, payout: ${basePayout:N0}");
            }

            if (floorTopUp > 0)
            {
                MelonLogger.Msg($"[DockExports] 🔄 Floor protection applied: broker topped up ${floorTopUp:N0} to reach wholesale floor.");
            }

            // Record that we've processed this day
            ShipmentManager.Instance.LastProcessedDay = currentDay;

            // Add money to player
            AddMoneyToPlayer(actualPayout);

            // Send broker notification(s)
            string message = lossPercent > 0
                ? BrokerMessages.GetRandomLossMessage(weekNumber, lossPercent, basePayout, expectedPayout)
                : BrokerMessages.WeekCleared(weekNumber, basePayout);

            SendBrokerMessage(message);

            if (floorTopUp > 0)
            {
                SendBrokerMessage(BrokerMessages.FloorProtection(floorTopUp));
            }

            DockExportsApp.RefreshData();

            // Request game save
            MelonLogger.Msg("[DockExports] 💾 Requesting game save");
            Saveable.RequestGameSave(true);
        }

        /// <summary>
        /// Adds money to the player's cash balance using S1API.Money.
        /// </summary>
        /// <param name="amount">The amount of money to add (in game currency).</param>
        /// <remarks>
        /// <para>
        /// Uses <c>S1API.Money.Money.ChangeCashBalance()</c> which:
        /// </para>
        /// <list type="bullet">
        /// <item>Adds the specified amount to player's cash</item>
        /// <item>Shows visual feedback on screen (visualizeChange: true)</item>
        /// <item>Plays cash register sound effect (playCashSound: true)</item>
        /// </list>
        /// <para>
        /// If the API call fails (exception), the error is logged but the game continues.
        /// The shipment remains marked as processed to prevent retry loops.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// AddMoneyToPlayer(1176000); // Adds $1,176,000 to player
        /// </code>
        /// </example>
        private void AddMoneyToPlayer(int amount)
        {
            MelonLogger.Msg($"[DockExports] 💵 Adding ${amount:N0} to player");

            // Use the Money.ChangeCashBalance API
            try
            {
                S1API.Money.Money.ChangeCashBalance(amount, visualizeChange: true, playCashSound: true);
                MelonLogger.Msg("[DockExports] ✓ Money added successfully");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[DockExports] ✗ Failed to add money: {ex.Message}");
                MelonLogger.Error($"[DockExports] Stack trace: {ex.StackTrace}");
            }
        }

        #endregion

        #region Public API for DockExportsApp

        /// <summary>
        /// Creates a wholesale shipment with instant payout. DEBUG METHOD for testing.
        /// </summary>
        /// <param name="quantity">Number of bricks to ship (will be capped at <see cref="DockExportsConfig.WHOLESALE_CAP"/>).</param>
        /// <remarks>
        /// <para><strong>Wholesale Mechanics:</strong></para>
        /// <list type="bullet">
        /// <item>Cap: 100 bricks (enforced automatically)</item>
        /// <item>Payout: Instant (fair market price)</item>
        /// <item>Cooldown: 30 in-game days</item>
        /// <item>No risk: Always get full payment</item>
        /// </list>
        /// <para><strong>Processing Steps:</strong></para>
        /// <list type="number">
        /// <item>Validate quantity against cap</item>
        /// <item>Get current brick price from PriceHelper</item>
        /// <item>Call ShipmentManager.CreateWholesaleShipment()</item>
        /// <item>Call ShipmentManager.ProcessWholesalePayment() (instant)</item>
        /// <item>Add money to player</item>
        /// <item>Send broker confirmation message</item>
        /// <item>Save game</item>
        /// </list>
        /// <para><strong>Exceptions Handled:</strong></para>
        /// <para>
        /// If ShipmentManager throws InvalidOperationException (e.g., active shipment already exists
        /// or wholesale is on cooldown), the error is logged and a broker message is sent to the player.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// DockExportsMod.Instance?.DebugCreateWholesale(100); // Ship 100 bricks wholesale
        /// </code>
        /// </example>
        public void DebugCreateWholesale(int quantity)
        {
            if (quantity > DockExportsConfig.WHOLESALE_CAP)
            {
                MelonLogger.Warning($"[DockExports] Wholesale quantity {quantity} exceeds cap of {DockExportsConfig.WHOLESALE_CAP}, capping");
                quantity = DockExportsConfig.WHOLESALE_CAP;
            }

            int currentDay = GameAccess.GetElapsedDays();
            int brickPrice = PriceHelper.GetCurrentBrickPrice();

            MelonLogger.Msg($"[DockExports] 📦 Creating wholesale shipment: qty={quantity}, price=${brickPrice:N0}/brick, day={currentDay}");

            try
            {
                ShipmentManager.Instance.CreateWholesaleShipment(quantity, brickPrice, currentDay);
                MelonLogger.Msg("[DockExports] ✓ Wholesale shipment created, processing instant payment");

                int payout = ShipmentManager.Instance.ProcessWholesalePayment(currentDay);
                MelonLogger.Msg($"[DockExports] ✓ Wholesale complete, total paid: ${payout:N0}");

                AddMoneyToPlayer(payout);
                SendBrokerMessage(BrokerMessages.WholesaleConfirmed(quantity, payout));
                DockExportsApp.RefreshData();

                MelonLogger.Msg("[DockExports] 💾 Requesting game save");
                Saveable.RequestGameSave(true);
            }
            catch (InvalidOperationException ex)
            {
                MelonLogger.Warning($"[DockExports] ⏳ Wholesale blocked: {ex.Message}");
                SendBrokerMessage($"Can't move wholesale right now. {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a consignment shipment with 4 weekly payouts. DEBUG METHOD for testing.
        /// </summary>
        /// <param name="quantity">Number of bricks to ship (will be capped at <see cref="DockExportsConfig.CONSIGNMENT_CAP"/>).</param>
        /// <remarks>
        /// <para><strong>Consignment Mechanics:</strong></para>
        /// <list type="bullet">
        /// <item>Cap: 200 bricks (enforced automatically)</item>
        /// <item>Price: 1.6x fair market price</item>
        /// <item>Payout: 25% per Friday for 4 weeks</item>
        /// <item>Risk: 25% chance per week of 15-60% loss</item>
        /// </list>
        /// <para><strong>Processing Steps:</strong></para>
        /// <list type="number">
        /// <item>Validate quantity against cap</item>
        /// <item>Get current brick price * 1.6x multiplier</item>
        /// <item>Call ShipmentManager.CreateConsignmentShipment()</item>
        /// <item>Send broker confirmation message with total value</item>
        /// <item>Save game</item>
        /// <item>Wait for Fridays (automatic via OnUpdate → ProcessConsignmentWeek)</item>
        /// </list>
        /// <para><strong>Exceptions Handled:</strong></para>
        /// <para>
        /// If ShipmentManager throws InvalidOperationException (e.g., active shipment already exists),
        /// the error is logged and a broker message is sent to the player.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// DockExportsMod.Instance?.DebugCreateConsignment(200); // Ship 200 bricks on consignment
        /// </code>
        /// </example>
        public void DebugCreateConsignment(int quantity)
        {
            if (quantity > DockExportsConfig.CONSIGNMENT_CAP)
            {
                MelonLogger.Warning($"[DockExports] Consignment quantity {quantity} exceeds cap of {DockExportsConfig.CONSIGNMENT_CAP}, capping");
                quantity = DockExportsConfig.CONSIGNMENT_CAP;
            }

            int currentDay = GameAccess.GetElapsedDays();
            int brickPrice = PriceHelper.GetCurrentBrickPrice();

            MelonLogger.Msg($"[DockExports] 📦 Creating consignment shipment: qty={quantity}, price=${brickPrice:N0}/brick, multiplier={DockExportsConfig.CONSIGNMENT_MULTIPLIER}x, day={currentDay}");

            try
            {
                ShipmentManager.Instance.CreateConsignmentShipment(quantity, brickPrice, DockExportsConfig.CONSIGNMENT_MULTIPLIER, currentDay);

                var shipment = ShipmentManager.Instance.ActiveShipment;
                if (shipment.HasValue)
                {
                    MelonLogger.Msg($"[DockExports] ✓ Consignment created: total=${shipment.Value.TotalValue:N0}, {DockExportsConfig.CONSIGNMENT_INSTALLMENTS} weekly payments");

                    SendBrokerMessage(BrokerMessages.ConsignmentLocked(
                        shipment.Value.Quantity,
                        shipment.Value.UnitPrice,
                        shipment.Value.TotalValue
                    ));
                }

                DockExportsApp.RefreshData();

                MelonLogger.Msg("[DockExports] 💾 Requesting game save");
                Saveable.RequestGameSave(true);
            }
            catch (InvalidOperationException ex)
            {
                MelonLogger.Warning($"[DockExports] ✗ Consignment blocked: {ex.Message}");
                SendBrokerMessage($"Can't create consignment right now. {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the currently active shipment (if any) for UI display.
        /// </summary>
        /// <value>
        /// The active <see cref="ShipmentData"/> if a shipment is in progress; otherwise, <c>null</c>.
        /// </value>
        /// <remarks>
        /// <para>
        /// Used by DockExportsApp to display active shipment details in the "Active" tab.
        /// Only one shipment can be active at a time (either Wholesale or Consignment).
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var active = DockExportsMod.Instance?.Active;
        /// if (active.HasValue)
        /// {
        ///     Debug.Log($"Active: {active.Value.Type}, {active.Value.Quantity} bricks");
        /// }
        /// </code>
        /// </example>
        public ShipmentData? Active => ShipmentManager.Instance.ActiveShipment;

        /// <summary>
        /// Gets the shipment history (completed shipments) for UI display.
        /// </summary>
        /// <value>
        /// Read-only list of <see cref="ShipmentHistoryEntry"/> objects representing past shipments.
        /// </value>
        /// <remarks>
        /// <para>
        /// Used by DockExportsApp to display completed shipments in the "History" tab.
        /// History persists across save/load via ShipmentManager's Saveable system.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var history = DockExportsMod.Instance?.History;
        /// foreach (var entry in history)
        /// {
        ///     Debug.Log($"{entry.Type}: {entry.Quantity} bricks, ${entry.TotalPaid:N0}");
        /// }
        /// </code>
        /// </example>
        public System.Collections.Generic.IReadOnlyList<ShipmentHistoryEntry> History => ShipmentManager.Instance.History;

        /// <summary>
        /// Gets whether wholesale shipments are currently on cooldown.
        /// </summary>
        /// <value>
        /// <c>true</c> if wholesale was used within the last 30 in-game days; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// <para>
        /// Used by DockExportsApp to show/hide the wholesale cooldown warning.
        /// Cooldown is enforced by ShipmentManager to balance the safe, instant-payout wholesale option.
        /// </para>
        /// </remarks>
        public bool IsWholesaleOnCooldown => ShipmentManager.Instance.IsWholesaleOnCooldown(GameAccess.GetElapsedDays());

        /// <summary>
        /// Gets the number of in-game days remaining until wholesale becomes available again.
        /// </summary>
        /// <value>
        /// Days remaining (0 if not on cooldown).
        /// </value>
        /// <remarks>
        /// <para>
        /// Used by DockExportsApp to display "Cooldown: X days remaining" text.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// int daysLeft = DockExportsMod.Instance?.WholesaleDaysRemaining ?? 0;
        /// if (daysLeft > 0)
        /// {
        ///     cooldownText.text = $"⏳ Cooldown: {daysLeft} days remaining";
        /// }
        /// </code>
        /// </example>
        public int WholesaleDaysRemaining => ShipmentManager.Instance.WholesaleDaysRemaining(GameAccess.GetElapsedDays());

        /// <summary>
        /// Forces the broker to unlock without checking requirements. DEBUG METHOD for testing.
        /// </summary>
        /// <remarks>
        /// <para><strong>⚠️ DEBUG ONLY:</strong></para>
        /// <para>
        /// This method bypasses the normal unlock requirements (Hustler rank + Docks ownership)
        /// and immediately unlocks the broker. Used for testing the phone app UI and shipment systems
        /// without playing through the game to meet requirements.
        /// </para>
        /// <para>
        /// Called by the "🔓 Debug Unlock" button on the phone app's lock screen.
        /// </para>
        /// </remarks>
        /// <example>
        /// Button click handler in DockExportsApp:
        /// <code>
        /// ButtonUtils.AddListener(debugBtn, () => {
        ///     DockExportsMod.Instance?.DebugForceUnlock();
        /// });
        /// </code>
        /// </example>
        public void DebugForceUnlock()
        {
            MelonLogger.Msg("[DockExports] 🔓 DEBUG: Force unlocking broker (bypassing requirements)");

            if (!brokerUnlocked)
            {
                UnlockBroker();
                MelonLogger.Msg("[DockExports] ✓ Debug unlock complete");
            }
            else
            {
                MelonLogger.Msg("[DockExports] Already unlocked, no action needed");
            }
        }

        #endregion

        /// <summary>
        /// Called when the game is quitting. Performs final cleanup.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Clears the singleton Instance reference to prevent it from persisting
        /// across domain reloads (in editor) or memory leaks.
        /// </para>
        /// <para>
        /// Any unsaved data is already persisted via Saveable.RequestGameSave() calls
        /// throughout the mod, so no manual save is needed here.
        /// </para>
        /// </remarks>
        public override void OnApplicationQuit()
        {
            MelonLogger.Msg("[DockExports] 👋 Mod shutting down");
            Instance = null;
        }
    }

    // TEMPORARILY DISABLED: CallManager is broken in S1API v2.4.2
    // /// <summary>
    // /// Simple phone call for broker messages
    // /// </summary>
    // public class BrokerPhoneCall : PhoneCallDefinition
    // {
    //     public BrokerPhoneCall(string message) : base("The Broker", null)
    //     {
    //         var stage = AddStage(message);
    //         stage.AddSystemTrigger(S1API.PhoneCalls.Constants.SystemTriggerType.StartTrigger);
    //         Completed();
    //     }
    // }
}

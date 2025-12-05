/// <summary>
/// Entry point and mod state manager for S1DockExports.
/// </summary>
using System;
using MelonLoader;
using S1API.Internal.Abstraction;
using S1API.PhoneCalls;
using S1API.PhoneApp;
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
    /// Main entry point for the Dock Exports mod.
    /// </summary>
    public class DockExportsMod : MelonMod
    {
        /// <summary>
        /// Singleton instance of the mod.
        /// </summary>
        public static DockExportsMod? Instance { get; private set; }

        /// <summary>
        /// Internal flag tracking whether the broker has been unlocked.
        /// </summary>
        private bool brokerUnlocked = false;

        /// <summary>
        /// Gets whether the broker has been unlocked.
        /// </summary>
        public bool BrokerUnlocked => brokerUnlocked;

        /// <summary>
        /// Timestamp of last unlock check log (used to throttle logging to once per second).
        /// </summary>
        private float _lastUnlockCheckLog = 0f;

        /// <summary>
        /// Called by MelonLoader after the mod is fully loaded.
        /// </summary>
        public override void OnInitializeMelon()
        {
            Instance = this;
            MelonLogger.Msg("[DockExports] 🎮 Mod initialized");

            // ShipmentManager (Saveable) is auto-discovered by S1API v2.4.2+
            // No manual registration needed
            MelonLogger.Msg("[DockExports] ✓ Initialization complete");
        }

        /// <summary>
        /// Called when a scene has fully loaded.
        /// </summary>
        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            MelonLogger.Msg($"[DockExports] 🗺️ Scene loaded: {sceneName} (index: {buildIndex})");

            // Clear shipment data when returning to menu to prevent cross-save contamination
            if (sceneName == "Menu")
            {
                MelonLogger.Msg("[DockExports] Clearing data for menu scene");
                ShipmentManager.Instance.ClearAllData();
                brokerUnlocked = false;
            }
        }

        /// <summary>
        /// Called when a scene is fully unloaded.
        /// </summary>
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
        /// Called every frame. Checks unlock conditions and processes Friday payouts.
        /// </summary>
        public override void OnUpdate()
        {
            // Check unlock conditions
            if (!brokerUnlocked)
            {
                // Log unlock check every second to avoid spam
                if (Time.time - _lastUnlockCheckLog >= 1.0f)
                {
                    int rank = GameAccess.GetPlayerRank();
                    bool docksOwned = GameAccess.IsPropertyOwned(DockExportsConfig.DOCKS_PROPERTY_ID);

                    MelonLogger.Msg($"[DockExports] Unlock check: Rank={rank}, DocksOwned={docksOwned}");
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
        /// Checks if the player meets unlock requirements.
        /// </summary>
        private bool CanUnlock()
        {
            // Check rank requirement using direct game access
            int currentRank = GameAccess.GetPlayerRank();
            bool rankOk = currentRank >= 3; // Hustler = rank 3

            if (!rankOk)
            {
                // Only log rank failure occasionally to reduce spam
                if (Time.time - _lastUnlockCheckLog >= 5.0f)
                {
                    MelonLogger.Msg($"[DockExports] Rank check: {currentRank} >= 3 (Hustler)? {rankOk}");
                }
                return false;
            }

            // Check property ownership using direct game access
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
        private bool IsFriday() => GameAccess.GetCurrentDayOfWeek() == 4; // Friday = 4

        /// <summary>
        /// Determines if a consignment payout should be processed today.
        /// </summary>
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
        private void UnlockBroker()
        {
            MelonLogger.Msg("[DockExports] 🎉 BROKER UNLOCKED! Triggering intro sequence");
            brokerUnlocked = true;

            // Send broker intro message via phone call
            MelonLogger.Msg("[DockExports] Sending broker intro SMS...");
            SendBrokerMessage(BrokerMessages.INTRO_SMS);

            // Notify phone app to rebuild UI when unlock state changes
            DockExportsApp.OnBrokerUnlocked();
            MelonLogger.Msg("[DockExports] ✓ Broker unlock complete");
        }

        /// <summary>
        /// Sends a broker message to the player (currently logged only due to S1API limitations).
        /// </summary>
        private void SendBrokerMessage(string message)
        {
            string preview = message.Length > 50 ? message.Substring(0, 50) + "..." : message;
            MelonLogger.Msg($"[DockExports] 📞 Broker message (SMS disabled): \"{preview}\"");
            MelonLogger.Msg($"[DockExports] 📝 Full message: {message}");

            try
            {
                var call = new BrokerPhoneCall(message);
                CallManager.QueueCall(call);
                MelonLogger.Msg("[DockExports] ✓ Message queued successfully");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[DockExports] ✗ Failed to send broker message: {ex.Message}");
                MelonLogger.Error($"[DockExports] Stack trace: {ex.StackTrace}");
            }
        }

        #region Consignment Processing

        /// <summary>
        /// Processes a Friday consignment payout with loss calculation and player compensation.
        /// </summary>
        private void ProcessConsignmentWeek()
        {
            var shipment = ShipmentManager.Instance.ActiveShipment;
            int weekNumber = (shipment?.PaymentsMade ?? 0) + 1;
            int totalWeeks = DockExportsConfig.CONSIGNMENT_INSTALLMENTS;

            MelonLogger.Msg($"[DockExports] 💰 Processing Friday consignment payout: Week {weekNumber}/{totalWeeks}");

            int actualPayout = ShipmentManager.Instance.ProcessConsignmentPayment(out int lossPercent);
            int currentDay = GameAccess.GetElapsedDays();
            int expectedPayout = (shipment?.TotalValue ?? 0) / DockExportsConfig.CONSIGNMENT_INSTALLMENTS;

            if (lossPercent > 0)
            {
                MelonLogger.Warning($"[DockExports] ⚠️ Loss event: {lossPercent}% loss, payout: ${actualPayout:N0} (expected: ${expectedPayout:N0})");
            }
            else
            {
                MelonLogger.Msg($"[DockExports] ✓ No losses, payout: ${actualPayout:N0}");
            }

            // Record that we've processed this day
            ShipmentManager.Instance.LastProcessedDay = currentDay;

            // Add money to player
            AddMoneyToPlayer(actualPayout);

            // Send broker notification
            string message = lossPercent > 0
                ? BrokerMessages.GetRandomLossMessage(weekNumber, lossPercent, actualPayout, expectedPayout)
                : BrokerMessages.WeekCleared(weekNumber, actualPayout);

            SendBrokerMessage(message);

            // Request game save
            MelonLogger.Msg("[DockExports] 💾 Requesting game save");
            Saveable.RequestGameSave(true);
        }

        /// <summary>
        /// Adds money to the player's cash balance using S1API.Money.
        /// </summary>
        private void AddMoneyToPlayer(int amount)
        {
            MelonLogger.Msg($"[DockExports] 💵 Adding ${amount:N0} to player");
            S1API.Money.Money.ChangeCashBalance(amount, visualizeChange: true, playCashSound: true);
            MelonLogger.Msg("[DockExports] ✓ Money added successfully");
        }

        #endregion

        #region Public API for DockExportsApp

        /// <summary>
        /// Creates a wholesale shipment with instant payout. DEBUG METHOD for testing.
        /// </summary>
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

                ShipmentManager.Instance.ProcessWholesalePayment(currentDay);

                int totalPaid = ShipmentManager.Instance.ActiveShipment?.TotalPaid ?? 0;
                MelonLogger.Msg($"[DockExports] ✓ Wholesale complete, total paid: ${totalPaid:N0}");

                AddMoneyToPlayer(totalPaid);
                SendBrokerMessage(BrokerMessages.WholesaleConfirmed(quantity, totalPaid));

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
        public ShipmentData? Active => ShipmentManager.Instance.ActiveShipment;

        /// <summary>
        /// Gets the shipment history (completed shipments) for UI display.
        /// </summary>
        public System.Collections.Generic.IReadOnlyList<ShipmentHistoryEntry> History => ShipmentManager.Instance.History;

        /// <summary>
        /// Gets whether wholesale shipments are currently on cooldown.
        /// </summary>
        public bool IsWholesaleOnCooldown => ShipmentManager.Instance.IsWholesaleOnCooldown(GameAccess.GetElapsedDays());

        /// <summary>
        /// Gets the number of in-game days remaining until wholesale becomes available again.
        /// </summary>
        public int WholesaleDaysRemaining => ShipmentManager.Instance.WholesaleDaysRemaining(GameAccess.GetElapsedDays());

        /// <summary>
        /// Forces the broker to unlock without checking requirements. DEBUG METHOD for testing.
        /// </summary>
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
        public override void OnApplicationQuit()
        {
            MelonLogger.Msg("[DockExports] 👋 Mod shutting down");
            Instance = null;
        }
    }

    /// <summary>
    /// Simple phone call for broker messages
    /// </summary>
    public class BrokerPhoneCall : PhoneCallDefinition
    {
        public BrokerPhoneCall(string message) : base("The Broker", null)
        {
            var stage = AddStage(message);
            stage.AddSystemTrigger(S1API.PhoneCalls.Constants.SystemTriggerType.StartTrigger);
            Completed();
        }
    }
}

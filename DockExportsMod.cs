#nullable enable
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using MelonLoader;
using S1API;
using S1API.Leveling;
using S1API.Property;
using S1API.PhoneApp;
using S1API.PhoneCalls;
using S1API.GameTime;
using S1API.Money;
using S1API.SaveSystem;
using UnityEngine;

[assembly: MelonInfo(typeof(S1DockExports.DockExportsMod), "S1DockExports", "1.0.0", "KaisoXP")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace S1DockExports
{
    public sealed class DockExportsMod : MelonMod
    {
        // State
        private bool brokerUnlocked = false;
        private bool introMessageSent = false;
        private DockExportsApp? appInstance;
        private ShipmentData? activeShipment;
        private int lastPayoutWeek = -1;
        private float wholesaleCooldownEnd = -1;
        private List<ShipmentHistoryEntry> shipmentHistory = new List<ShipmentHistoryEntry>();

        public override void OnInitializeMelon()
        {
            // Ensure missing runtime dependencies (for example FishNet) resolve from Il2CppAssemblies.
            AppDomain.CurrentDomain.AssemblyResolve += ResolveFromIl2CppAssemblies;

            LoggerInstance.Msg("Dock Exports initializing...");
            
            // Register save system
            SaveSystem.RegisterSaveHandler("DockExports", SaveData, LoadData);
            
            LoggerInstance.Msg("Dock Exports initialized!");
        }

        private Assembly? ResolveFromIl2CppAssemblies(object? sender, ResolveEventArgs args)
        {
            // Determine the simple assembly name.
            var requestedName = new AssemblyName(args.Name).Name ?? string.Empty;

            // Map requested assembly name to the actual Il2Cpp file shipped with the game.
            string? fileName = requestedName switch
            {
                "FishNet.Runtime" => "Il2CppFishNet.Runtime.dll",
                "com.rlabrecque.steamworks.net" => "Il2Cppcom.rlabrecque.steamworks.net.dll",
                _ => null
            };
            if (fileName is null)
                return null;

            // Do not rely on MelonEnvironment at compile time. AppContext.BaseDirectory points at the game root under ML.
            string gameDirectory = AppContext.BaseDirectory;

            string il2cppAssembliesDirectory = Path.Combine(gameDirectory, "MelonLoader", "Il2CppAssemblies");
            string probePath = Path.Combine(il2cppAssembliesDirectory, fileName);

            if (!File.Exists(probePath))
                return null;

            try
            {
                return Assembly.LoadFrom(probePath);
            }
            catch (Exception ex)
            {
                LoggerInstance.Warning($"Failed to load '{fileName}' from Il2CppAssemblies: {ex.Message}");
                return null;
            }
        }

        public override void OnUpdate()
        {
            // Check unlock conditions every frame (could be optimized to check less frequently)
            CheckUnlockConditions();
            
            // Process weekly payouts if we have an active consignment
            if (activeShipment != null && activeShipment.Type == ShipmentType.Consignment)
            {
                ProcessWeeklyPayout();
            }
        }

        private void CheckUnlockConditions()
        {
            if (brokerUnlocked) return;

            // Check if player meets requirements
            int playerLevel = Leveling.GetPlayerLevel();
            bool ownsProperty = Property.IsPropertyOwned(DockExportsConfig.DOCKS_PROPERTY_ID);

            if (playerLevel >= DockExportsConfig.REQUIRED_RANK_LEVEL && ownsProperty)
            {
                UnlockBroker();
            }
        }

        private void UnlockBroker()
        {
            brokerUnlocked = true;
            LoggerInstance.Msg("Broker unlocked! Sending intro message...");

            // Send intro SMS from Broker
            if (!introMessageSent)
            {
                SendBrokerIntroSMS();
                introMessageSent = true;
                
                // Register and show the phone app
                RegisterPhoneApp();
            }
        }

        private void SendBrokerIntroSMS()
        {
            // Using PhoneCalls API to send SMS
            PhoneCalls.SendSMS("The Broker", BrokerMessages.INTRO_SMS, () => LoggerInstance.Msg("Broker intro SMS sent"));
        }

        private void RegisterPhoneApp()
        {
            // Create and register the Dock Exports app
            appInstance = new DockExportsApp(this);
            PhoneApp.RegisterApp(appInstance);
            LoggerInstance.Msg("Dock Exports app registered");
        }

        private void ProcessWeeklyPayout()
        {
            if (activeShipment == null || activeShipment.Completed) return;

            int currentWeek = GameTime.GetCurrentWeek();
            
            // Check if it's Friday and we haven't paid this week yet
            if (GameTime.GetCurrentDayOfWeek() == DayOfWeek.Friday && currentWeek != lastPayoutWeek)
            {
                lastPayoutWeek = currentWeek;
                ProcessConsignmentWeek();
            }
        }

        private void ProcessConsignmentWeek()
        {
            if (activeShipment == null) return;

            activeShipment.WeeksPaid++;
            
            int weeklyAmount = PriceHelper.CalculateWeeklyPayout(activeShipment.TotalValue);
            int actualPayout = weeklyAmount;
            int lossPercent = 0;

            // Roll for loss
            if (UnityEngine.Random.value < DockExportsConfig.WEEKLY_LOSS_CHANCE)
            {
                lossPercent = UnityEngine.Random.Range(DockExportsConfig.LOSS_MIN_PERCENT, DockExportsConfig.LOSS_MAX_PERCENT + 1);
                actualPayout = PriceHelper.ApplyLoss(weeklyAmount, lossPercent);
            }

            // Pay the player
            Money.AddMoney(actualPayout);
            activeShipment.TotalPaid += actualPayout;

            // Send notification with varied messages
            string message = lossPercent > 0
                ? BrokerMessages.GetRandomLossMessage(activeShipment.WeeksPaid, lossPercent, actualPayout, weeklyAmount)
                : (UnityEngine.Random.value > 0.5f 
                    ? BrokerMessages.WeekCleared(activeShipment.WeeksPaid, actualPayout)
                    : BrokerMessages.WeekClearedAlt(activeShipment.WeeksPaid, actualPayout));

            PhoneCalls.SendSMS("The Broker", message, null);

            // Check if shipment is complete
            if (activeShipment.WeeksPaid >= DockExportsConfig.INSTALLMENTS)
            {
                CompleteConsignment();
            }
        }

        private void CompleteConsignment()
        {
            if (activeShipment == null) return;

            // Check floor protection
            int wholesaleEquivalent = PriceHelper.CalculateWholesaleFloor(activeShipment.Quantity, activeShipment.BrickPrice);
            if (activeShipment.TotalPaid < wholesaleEquivalent)
            {
                int topUp = wholesaleEquivalent - activeShipment.TotalPaid;
                Money.AddMoney(topUp);
                PhoneCalls.SendSMS("The Broker", BrokerMessages.FloorProtection(topUp), null);
                activeShipment.TotalPaid += topUp;
            }

            activeShipment.Completed = true;
            LoggerInstance.Msg($"Consignment completed. Total paid: ${activeShipment.TotalPaid:N0}");
            
            // Add to history
            AddToHistory(new ShipmentHistoryEntry(activeShipment));
        }

        private void AddToHistory(ShipmentHistoryEntry entry)
        {
            shipmentHistory.Add(entry);
            // Keep only last 20 entries to avoid bloat
            if (shipmentHistory.Count > 20)
            {
                shipmentHistory.RemoveAt(0);
            }
        }

        public void CreateWholesaleShipment(int quantity, int brickPrice)
        {
            if (quantity > DockExportsConfig.WHOLESALE_CAP)
            {
                LoggerInstance.Warning($"Quantity exceeds wholesale cap: {quantity} > {DockExportsConfig.WHOLESALE_CAP}");
                return;
            }

            // Check cooldown
            float currentTime = GameTime.GetCurrentGameTime();
            if (currentTime < wholesaleCooldownEnd)
            {
                LoggerInstance.Msg("Wholesale on cooldown");
                return;
            }

            int totalValue = PriceHelper.CalculateWholesalePayout(quantity, brickPrice);
            Money.AddMoney(totalValue);

            // Set cooldown (convert days to game time minutes)
            wholesaleCooldownEnd = currentTime + (DockExportsConfig.WHOLESALE_COOLDOWN_DAYS * 24 * 60);

            PhoneCalls.SendSMS("The Broker", BrokerMessages.WholesaleConfirmed(quantity, totalValue), null);
            LoggerInstance.Msg($"Wholesale shipment created: {quantity} bricks, ${totalValue:N0}");
        }

        public void CreateConsignmentShipment(int quantity, int brickPrice)
        {
            if (quantity > DockExportsConfig.CONSIGNMENT_CAP)
            {
                LoggerInstance.Warning($"Quantity exceeds consignment cap: {quantity} > {DockExportsConfig.CONSIGNMENT_CAP}");
                return;
            }

            if (activeShipment != null && !activeShipment.Completed)
            {
                LoggerInstance.Msg("Already have an active consignment");
                return;
            }

            int enhancedPrice = (int)(brickPrice * DockExportsConfig.CONSIGNMENT_MULTIPLIER);
            int totalValue = PriceHelper.CalculateConsignmentValue(quantity, brickPrice);

            activeShipment = new ShipmentData
            {
                Type = ShipmentType.Consignment,
                Quantity = quantity,
                BrickPrice = brickPrice,
                TotalValue = totalValue,
                WeeksPaid = 0,
                TotalPaid = 0,
                Completed = false,
                StartWeek = GameTime.GetCurrentWeek()
            };

            lastPayoutWeek = -1; // Reset to allow first week payout

            PhoneCalls.SendSMS("The Broker", BrokerMessages.ConsignmentLocked(quantity, enhancedPrice, totalValue), null);
            LoggerInstance.Msg($"Consignment shipment created: {quantity} bricks, ${totalValue:N0} total");
        }

        public ShipmentData? GetActiveShipment() => activeShipment;
        
        public List<ShipmentHistoryEntry> GetHistory() => shipmentHistory;
        
        public bool IsWholesaleOnCooldown() => GameTime.GetCurrentGameTime() < wholesaleCooldownEnd;
        
        public int GetWholesaleDaysRemaining()
        {
            if (!IsWholesaleOnCooldown()) return 0;
            float remaining = wholesaleCooldownEnd - GameTime.GetCurrentGameTime();
            return Mathf.CeilToInt(remaining / (24 * 60)); // Convert minutes to days
        }

        // Save/Load
        private string SaveData()
        {
            var data = new SaveData
            {
                brokerUnlocked = this.brokerUnlocked,
                introMessageSent = this.introMessageSent,
                activeShipment = this.activeShipment,
                lastPayoutWeek = this.lastPayoutWeek,
                wholesaleCooldownEnd = this.wholesaleCooldownEnd,
                shipmentHistory = this.shipmentHistory
            };
            return JsonUtility.ToJson(data);
        }

        private void LoadData(string json)
        {
            try
            {
                var data = JsonUtility.FromJson<SaveData>(json);
                this.brokerUnlocked = data.brokerUnlocked;
                this.introMessageSent = data.introMessageSent;
                this.activeShipment = data.activeShipment;
                this.lastPayoutWeek = data.lastPayoutWeek;
                this.wholesaleCooldownEnd = data.wholesaleCooldownEnd;
                this.shipmentHistory = data.shipmentHistory ?? new List<ShipmentHistoryEntry>();

                if (brokerUnlocked && appInstance == null)
                {
                    RegisterPhoneApp();
                }
                
                LoggerInstance.Msg("Dock Exports save data loaded successfully");
            }
            catch (Exception e)
            {
                LoggerInstance.Error($"Failed to load save data: {e.Message}");
            }
        }
    }

    // Data classes
    [System.Serializable]
    public class ShipmentData
    {
        public ShipmentType Type;
        public int Quantity;
        public int BrickPrice;
        public int TotalValue;
        public int WeeksPaid;
        public int TotalPaid;
        public bool Completed;
        public int StartWeek;
    }

    [System.Serializable]
    public class SaveData
    {
        public bool brokerUnlocked;
        public bool introMessageSent;
        public ShipmentData? activeShipment;
        public int lastPayoutWeek;
        public float wholesaleCooldownEnd;
        public List<ShipmentHistoryEntry>? shipmentHistory;
    }

    public enum ShipmentType
    {
        Wholesale,
        Consignment
    }
}
using System.Collections.Generic;

namespace S1DockExports
{
    /// <summary>
    /// Configuration constants for Dock Exports mod
    /// </summary>
    public static class DockExportsConfig
    {
        // Unlock Requirements
        public const string REQUIRED_RANK = "Hustler III";
        public const int REQUIRED_RANK_LEVEL = 13;
        public const string DOCKS_PROPERTY_ID = "Docks Warehouse"; 
        
        // Wholesale Settings
        public const int WHOLESALE_CAP = 100;
        public const int WHOLESALE_COOLDOWN_DAYS = 30;
        
        // Consignment Settings
        public const int CONSIGNMENT_CAP = 200;
        public const float CONSIGNMENT_MULTIPLIER = 1.6f;
        public const int INSTALLMENTS = 4;
        
        // Risk Settings
        public const float WEEKLY_LOSS_CHANCE = 0.25f; // 25%
        public const int LOSS_MIN_PERCENT = 15;
        public const int LOSS_MAX_PERCENT = 60;
        
        // Default Prices (should be fetched dynamically from game)
        public const int DEFAULT_BRICK_PRICE = 14700; // 8-mix coke at fair price (735 × 20)
        
        // UI Colors
        public static readonly UnityEngine.Color COLOR_BACKGROUND = new UnityEngine.Color(0.1f, 0.15f, 0.2f, 0.95f);
        public static readonly UnityEngine.Color COLOR_HEADER = new UnityEngine.Color(0.05f, 0.1f, 0.15f, 1f);
        public static readonly UnityEngine.Color COLOR_ACCENT = new UnityEngine.Color(0.7f, 0.85f, 1f, 1f);
        public static readonly UnityEngine.Color COLOR_SUCCESS = new UnityEngine.Color(0.4f, 1f, 0.4f, 1f);
        public static readonly UnityEngine.Color COLOR_WARNING = new UnityEngine.Color(1f, 0.5f, 0.3f, 1f);
        public static readonly UnityEngine.Color COLOR_TAB_ACTIVE = new UnityEngine.Color(0.3f, 0.5f, 0.7f, 1f);
        public static readonly UnityEngine.Color COLOR_TAB_INACTIVE = new UnityEngine.Color(0.15f, 0.2f, 0.25f, 1f);
    }

    /// <summary>
    /// Broker dialogue messages
    /// </summary>
    public static class BrokerMessages
    {
        // Intro
        public const string INTRO_SMS = "Heard you control the docks. I move product overseas. Check your phone for details.";
        
        // Wholesale
        public static string WholesaleConfirmed(int quantity, int totalValue) =>
            $"Wholesale shipment confirmed. {quantity} bricks moved. ${totalValue:N0} transferred instantly. Next slot in 30 days.";
        
        // Consignment
        public static string ConsignmentLocked(int quantity, int pricePerBrick, int totalValue) =>
            $"Consignment locked. {quantity} bricks @ ${pricePerBrick:N0} each = ${totalValue:N0}. First payment Friday. 4 weeks total.";
        
        // Weekly Payouts
        public static string WeekCleared(int week, int payout) =>
            $"Week {week} cleared. ${payout:N0} released.";
        
        public static string WeekClearedAlt(int week, int payout) =>
            $"Week {week}: Shipment arrived without issue. ${payout:N0} sent.";
        
        // Loss Events
        public static string CustomsLoss(int week, int lossPercent, int actualPayout, int expectedPayout) =>
            $"Week {week}: Customs flagged a container. {lossPercent}% of your shipment didn't make it. You received ${actualPayout:N0} instead of ${expectedPayout:N0}.";
        
        public static string TransitDelayLoss(int week, int lossPercent, int actualPayout, int expectedPayout) =>
            $"Week {week}: Shipment delayed in transit. {lossPercent}% of product spoiled. You received ${actualPayout:N0} instead of ${expectedPayout:N0}.";
        
        public static string InspectionLoss(int week, int lossPercent, int actualPayout, int expectedPayout) =>
            $"Week {week}: Port inspection found discrepancies. {lossPercent}% lost. You received ${actualPayout:N0} instead of ${expectedPayout:N0}.";
        
        public static string CrewShortLoss(int week, int lossPercent, int actualPayout, int expectedPayout) =>
            $"Week {week}: Crew shorted your cut. {lossPercent}% underdelivered. You received ${actualPayout:N0} instead of ${expectedPayout:N0}.";
        
        // Floor Protection
        public static string FloorProtection(int topUpAmount) =>
            $"Tough month, but I made sure you didn't take a total loss. Added ${topUpAmount:N0} to match wholesale. Let's call it even.";
        
        // Get random loss message
        private static readonly List<System.Func<int, int, int, int, string>> LossMessages = new List<System.Func<int, int, int, int, string>>
        {
            CustomsLoss,
            TransitDelayLoss,
            InspectionLoss,
            CrewShortLoss
        };
        
        public static string GetRandomLossMessage(int week, int lossPercent, int actualPayout, int expectedPayout)
        {
            int index = UnityEngine.Random.Range(0, LossMessages.Count);
            return LossMessages[index](week, lossPercent, actualPayout, expectedPayout);
        }
    }

    /// <summary>
    /// Shipment history entry for tracking past deals
    /// </summary>
    [System.Serializable]
    public class ShipmentHistoryEntry
    {
        public ShipmentType Type;
        public int Quantity;
        public int TotalValue;
        public int TotalPaid;
        public int WeeksCompleted;
        public string CompletionDate;
        public bool HadLosses;
        public int TotalLossPercent;
        
        public ShipmentHistoryEntry(ShipmentData shipment)
        {
            Type = shipment.Type;
            Quantity = shipment.Quantity;
            TotalValue = shipment.TotalValue;
            TotalPaid = shipment.TotalPaid;
            WeeksCompleted = shipment.WeeksPaid;
            CompletionDate = System.DateTime.Now.ToString("MMM dd, yyyy");
            
            // Calculate total loss
            int expectedTotal = TotalValue;
            int actualTotal = TotalPaid;
            if (actualTotal < expectedTotal)
            {
                HadLosses = true;
                TotalLossPercent = 100 - (int)((actualTotal / (float)expectedTotal) * 100);
            }
            else
            {
                HadLosses = false;
                TotalLossPercent = 0;
            }
        }
        
        public override string ToString()
        {
            string typeStr = Type == ShipmentType.Wholesale ? "WHOLESALE" : "CONSIGNMENT";
            string lossStr = HadLosses ? $" ({TotalLossPercent}% loss)" : " (no losses)";
            return $"[{CompletionDate}] {typeStr}\n" +
                   $"  {Quantity} bricks → ${TotalPaid:N0}{lossStr}";
        }
    }

    /// <summary>
    /// Helper methods for price calculations
    /// </summary>
    public static class PriceHelper
    {
        /// <summary>
        /// Gets the current brick price from the game (placeholder)
        /// TODO: Integrate with S1API.Items to get actual current market price
        /// </summary>
        public static int GetCurrentBrickPrice()
        {
            // This should query the game's item system
            // For now, return the default 8-mix coke price
            return DockExportsConfig.DEFAULT_BRICK_PRICE;
        }
        
        /// <summary>
        /// Calculate wholesale payout
        /// </summary>
        public static int CalculateWholesalePayout(int quantity, int brickPrice)
        {
            return quantity * brickPrice;
        }
        
        /// <summary>
        /// Calculate consignment total value with multiplier
        /// </summary>
        public static int CalculateConsignmentValue(int quantity, int brickPrice)
        {
            return (int)(quantity * brickPrice * DockExportsConfig.CONSIGNMENT_MULTIPLIER);
        }
        
        /// <summary>
        /// Calculate expected weekly payout (25% of total)
        /// </summary>
        public static int CalculateWeeklyPayout(int totalValue)
        {
            return totalValue / DockExportsConfig.INSTALLMENTS;
        }
        
        /// <summary>
        /// Calculate actual payout after loss
        /// </summary>
        public static int ApplyLoss(int basePayout, int lossPercent)
        {
            return (int)(basePayout * (1f - lossPercent / 100f));
        }
        
        /// <summary>
        /// Calculate wholesale equivalent for floor protection
        /// </summary>
        public static int CalculateWholesaleFloor(int quantity, int brickPrice)
        {
            return quantity * brickPrice;
        }
    }
}
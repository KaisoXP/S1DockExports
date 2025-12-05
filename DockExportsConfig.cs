/// <summary>
/// Configuration constants and settings for the Dock Exports mod.
/// </summary>
using System.Collections.Generic;

namespace S1DockExports
{
    /// <summary>
    /// Central configuration class containing all tunable constants.
    /// </summary>
    public static class DockExportsConfig
    {
        #region Unlock Requirements

        /// <summary>
        /// Required player rank name for unlocking the broker (display only, not used in logic).
        /// </summary>
        public const string REQUIRED_RANK = "Hustler III";

        /// <summary>
        /// Required player rank level for unlocking the broker (Hustler = level 13).
        /// </summary>
        public const int REQUIRED_RANK_LEVEL = 13;

        /// <summary>
        /// Required property ID for unlocking the broker.
        /// </summary>
        public const string DOCKS_PROPERTY_ID = "Docks Warehouse";

        #endregion

        #region Wholesale Settings

        /// <summary>
        /// Maximum number of bricks allowed in a single wholesale shipment.
        /// </summary>
        public const int WHOLESALE_CAP = 100;

        /// <summary>
        /// Number of in-game days before wholesale can be used again.
        /// </summary>
        public const int WHOLESALE_COOLDOWN_DAYS = 30;

        #endregion

        #region Consignment Settings

        /// <summary>
        /// Maximum number of bricks allowed in a single consignment shipment.
        /// </summary>
        public const int CONSIGNMENT_CAP = 200;

        /// <summary>
        /// Price multiplier for consignment shipments (1.6x = 60% price boost).
        /// </summary>
        public const float CONSIGNMENT_MULTIPLIER = 1.6f;

        /// <summary>
        /// Number of weekly payments for consignment shipments (1 payment every Friday for 4 weeks).
        /// </summary>
        public const int CONSIGNMENT_INSTALLMENTS = 4;

        #endregion

        #region Risk Settings

        /// <summary>
        /// Probability of loss occurring each week for consignment shipments (0.25 = 25% chance).
        /// </summary>
        public const float WEEKLY_LOSS_CHANCE = 0.25f;

        /// <summary>
        /// Minimum loss percentage when a loss event occurs (15%).
        /// </summary>
        public const int LOSS_MIN_PERCENT = 15;

        /// <summary>
        /// Maximum loss percentage when a loss event occurs (60%).
        /// </summary>
        public const int LOSS_MAX_PERCENT = 60;

        #endregion

        #region Default Prices

        /// <summary>
        /// Default brick price used when dynamic pricing is unavailable.
        /// </summary>
        public const int DEFAULT_BRICK_PRICE = 14700;

        #endregion

        #region UI Colors

        /// <summary>
        /// Background color for the phone app (dark blue-gray with 95% opacity).
        /// </summary>
        public static readonly UnityEngine.Color Bg = new UnityEngine.Color(0.1f, 0.15f, 0.2f, 0.95f);

        /// <summary>
        /// Header bar color for the phone app (darker blue-gray, fully opaque).
        /// </summary>
        public static readonly UnityEngine.Color Header = new UnityEngine.Color(0.05f, 0.1f, 0.15f, 1f);

        /// <summary>
        /// Accent color for buttons and highlights (light blue).
        /// </summary>
        public static readonly UnityEngine.Color Accent = new UnityEngine.Color(0.7f, 0.85f, 1f, 1f);

        /// <summary>
        /// Default text color (white).
        /// </summary>
        public static readonly UnityEngine.Color Text = UnityEngine.Color.white;

        /// <summary>
        /// Success/positive state color (bright green).
        /// </summary>
        public static readonly UnityEngine.Color Success = new UnityEngine.Color(0.4f, 1f, 0.4f, 1f);

        /// <summary>
        /// Warning/caution state color (orange-red).
        /// </summary>
        public static readonly UnityEngine.Color Warning = new UnityEngine.Color(1f, 0.5f, 0.3f, 1f);

        #endregion
    }

    /// <summary>
    /// Broker dialogue message templates for SMS notifications.
    /// </summary>
    public static class BrokerMessages
    {
        /// <summary>
        /// Initial SMS sent when broker unlocks.
        /// </summary>
        public const string INTRO_SMS = "Heard you control the docks. I move product overseas. Check your phone for details.";
        
        /// <summary>
        /// Wholesale shipment confirmation message.
        /// </summary>
        public static string WholesaleConfirmed(int quantity, int totalValue) =>
            $"Wholesale shipment confirmed. {quantity} bricks moved. ${totalValue:N0} transferred instantly. Next slot in 30 days.";

        /// <summary>
        /// Consignment shipment lock-in confirmation message.
        /// </summary>
        public static string ConsignmentLocked(int quantity, int pricePerBrick, int totalValue) =>
            $"Consignment locked. {quantity} bricks @ ${pricePerBrick:N0} each = ${totalValue:N0}. First payment Friday. 4 weeks total.";

        /// <summary>
        /// Standard weekly payout message (no losses).
        /// </summary>
        public static string WeekCleared(int week, int payout) =>
            $"Week {week} cleared. ${payout:N0} released.";

        /// <summary>
        /// Alternative weekly payout message with more detail (no losses).
        /// </summary>
        public static string WeekClearedAlt(int week, int payout) =>
            $"Week {week}: Shipment arrived without issue. ${payout:N0} sent.";
        
        /// <summary>
        /// Loss event message: Customs seizure scenario.
        /// </summary>
        public static string CustomsLoss(int week, int lossPercent, int actualPayout, int expectedPayout) =>
            $"Week {week}: Customs flagged a container. {lossPercent}% of your shipment didn't make it. You received ${actualPayout:N0} instead of ${expectedPayout:N0}.";

        /// <summary>
        /// Loss event message: Transit delay/spoilage scenario.
        /// </summary>
        public static string TransitDelayLoss(int week, int lossPercent, int actualPayout, int expectedPayout) =>
            $"Week {week}: Shipment delayed in transit. {lossPercent}% of product spoiled. You received ${actualPayout:N0} instead of ${expectedPayout:N0}.";

        /// <summary>
        /// Loss event message: Port inspection scenario.
        /// </summary>
        public static string InspectionLoss(int week, int lossPercent, int actualPayout, int expectedPayout) =>
            $"Week {week}: Port inspection found discrepancies. {lossPercent}% lost. You received ${actualPayout:N0} instead of ${expectedPayout:N0}.";

        /// <summary>
        /// Loss event message: Crew betrayal scenario.
        /// </summary>
        public static string CrewShortLoss(int week, int lossPercent, int actualPayout, int expectedPayout) =>
            $"Week {week}: Crew shorted your cut. {lossPercent}% underdelivered. You received ${actualPayout:N0} instead of ${expectedPayout:N0}.";

        /// <summary>
        /// Floor protection message sent when consignment underperforms wholesale.
        /// </summary>
        public static string FloorProtection(int topUpAmount) =>
            $"Tough month, but I made sure you didn't take a total loss. Added ${topUpAmount:N0} to match wholesale. Let's call it even.";
        
        /// <summary>
        /// List of all available loss event message templates for randomization.
        /// </summary>
        private static readonly List<System.Func<int, int, int, int, string>> LossMessages = new List<System.Func<int, int, int, int, string>>
        {
            CustomsLoss,
            TransitDelayLoss,
            InspectionLoss,
            CrewShortLoss
        };

        /// <summary>
        /// Returns a randomly selected loss event message from the available templates.
        /// </summary>
        public static string GetRandomLossMessage(int week, int lossPercent, int actualPayout, int expectedPayout)
        {
            int index = UnityEngine.Random.Range(0, LossMessages.Count);
            return LossMessages[index](week, lossPercent, actualPayout, expectedPayout);
        }
    }


    /// <summary>
    /// Helper utilities for shipment price calculations and payout computations.
    /// </summary>
    public static class PriceHelper
    {
        /// <summary>
        /// Gets the current market price for a brick of cocaine.
        /// </summary>
        public static int GetCurrentBrickPrice()
        {
            // This should query the game's item system
            // For now, return the default 8-mix coke price
            return DockExportsConfig.DEFAULT_BRICK_PRICE;
        }

        /// <summary>
        /// Calculates total wholesale shipment payout (instant, no multiplier).
        /// </summary>
        public static int CalculateWholesalePayout(int quantity, int brickPrice)
        {
            return quantity * brickPrice;
        }

        /// <summary>
        /// Calculates total consignment shipment value with price multiplier.
        /// </summary>
        public static int CalculateConsignmentValue(int quantity, int brickPrice)
        {
            return (int)(quantity * brickPrice * DockExportsConfig.CONSIGNMENT_MULTIPLIER);
        }

        /// <summary>
        /// Calculates expected payout for a single week of consignment (1/4 of total).
        /// </summary>
        public static int CalculateWeeklyPayout(int totalValue)
        {
            return totalValue / DockExportsConfig.CONSIGNMENT_INSTALLMENTS;
        }

        /// <summary>
        /// Applies loss percentage to a base payout amount.
        /// </summary>
        public static int ApplyLoss(int basePayout, int lossPercent)
        {
            return (int)(basePayout * (1f - lossPercent / 100f));
        }

        /// <summary>
        /// Calculates the wholesale equivalent floor for floor protection.
        /// </summary>
        public static int CalculateWholesaleFloor(int quantity, int brickPrice)
        {
            return quantity * brickPrice;
        }
    }
}
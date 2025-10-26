/// <summary>
/// Configuration constants and settings for the Dock Exports mod.
/// </summary>
/// <remarks>
/// <para>
/// This file contains all tunable parameters, constants, and configuration values used throughout the mod.
/// Centralizing configuration makes it easy to adjust game balance, UI styling, and requirements
/// without hunting through code.
/// </para>
/// <para><strong>Contents:</strong></para>
/// <list type="bullet">
/// <item><see cref="DockExportsConfig"/> - Core configuration constants</item>
/// <item><see cref="BrokerMessages"/> - Dialogue message templates</item>
/// <item><see cref="PriceHelper"/> - Price calculation utilities</item>
/// </list>
/// <para><strong>Architecture Pattern:</strong> Static Configuration Class</para>
/// <para>
/// All values are `const` or `readonly static`, meaning they're compile-time or initialization-time
/// constants. No instances of these classes are ever created.
/// </para>
/// </remarks>
using System.Collections.Generic;

namespace S1DockExports
{
    /// <summary>
    /// Central configuration class containing all tunable constants for game balance, unlock requirements,
    /// and UI styling.
    /// </summary>
    /// <remarks>
    /// <para><strong>Design Principle: Single Source of Truth</strong></para>
    /// <para>
    /// ALL magic numbers and settings are defined here, not scattered throughout the codebase.
    /// Changing a value here (e.g., WHOLESALE_CAP from 100 to 150) updates it everywhere automatically.
    /// </para>
    /// <para><strong>Organized by Category:</strong></para>
    /// <list type="bullet">
    /// <item>Unlock Requirements - What the player needs to unlock the broker</item>
    /// <item>Wholesale Settings - Safe, instant-payout mode configuration</item>
    /// <item>Consignment Settings - Risky, high-reward mode configuration</item>
    /// <item>Risk Settings - Loss chance and severity</item>
    /// <item>Default Prices - Fallback brick prices</item>
    /// <item>UI Colors - Phone app color scheme</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// Using configuration constants:
    /// <code>
    /// if (quantity > DockExportsConfig.WHOLESALE_CAP)
    /// {
    ///     quantity = DockExportsConfig.WHOLESALE_CAP;
    /// }
    /// </code>
    /// </example>
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
        /// <remarks>
        /// Used by <see cref="DockExportsMod.CanUnlock"/> to check if the player is high enough rank.
        /// Rank 13 is mid-game, ensuring the player has progressed before accessing this system.
        /// </remarks>
        public const int REQUIRED_RANK_LEVEL = 13;

        /// <summary>
        /// Required property ID for unlocking the broker.
        /// </summary>
        /// <remarks>
        /// The player must own the "Docks Warehouse" property to access the Dock Exports system.
        /// This ensures thematic consistency (you need to control the docks to export via docks).
        /// </remarks>
        public const string DOCKS_PROPERTY_ID = "Docks Warehouse";

        #endregion

        #region Wholesale Settings

        /// <summary>
        /// Maximum number of bricks allowed in a single wholesale shipment.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Wholesale is the safe option: instant payout at fair price, but limited quantity and cooldown.
        /// </para>
        /// <para><strong>Balance Consideration:</strong></para>
        /// <para>
        /// 100 bricks × $14,700 = $1,470,000 instant payout (safe but limited).
        /// </para>
        /// </remarks>
        public const int WHOLESALE_CAP = 100;

        /// <summary>
        /// Number of in-game days before wholesale can be used again.
        /// </summary>
        /// <remarks>
        /// After creating a wholesale shipment, the player must wait 30 days before using wholesale again.
        /// This prevents spamming the safe option and encourages using consignment for larger profits.
        /// </remarks>
        public const int WHOLESALE_COOLDOWN_DAYS = 30;

        #endregion

        #region Consignment Settings

        /// <summary>
        /// Maximum number of bricks allowed in a single consignment shipment.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Consignment is the risky option: 2x the quantity of wholesale, 1.6x price multiplier,
        /// but paid over 4 weeks with 25% weekly chance of 15-60% loss.
        /// </para>
        /// <para><strong>Balance Consideration:</strong></para>
        /// <para>
        /// 200 bricks × $23,520 (1.6x) = $4,704,000 total (if no losses) vs $1,470,000 wholesale.
        /// Expected value after losses: ~$4,260,000 (still ~2.9x wholesale).
        /// </para>
        /// </remarks>
        public const int CONSIGNMENT_CAP = 200;

        /// <summary>
        /// Price multiplier for consignment shipments (1.6x = 60% price boost).
        /// </summary>
        /// <remarks>
        /// Consignment prices are 60% higher than wholesale to compensate for:
        /// <list type="bullet">
        /// <item>Payment delays (4 weeks vs instant)</item>
        /// <item>Loss risk (25% weekly chance)</item>
        /// <item>Uncertainty (unknown final payout)</item>
        /// </list>
        /// </remarks>
        public const float CONSIGNMENT_MULTIPLIER = 1.6f;

        /// <summary>
        /// Number of weekly payments for consignment shipments (1 payment every Friday for 4 weeks).
        /// </summary>
        /// <remarks>
        /// Total value is split evenly: 25% per week. Each Friday, a payment is processed with
        /// a loss roll. After 4 payments, the shipment is complete and archived to history.
        /// </remarks>
        public const int CONSIGNMENT_INSTALLMENTS = 4;

        #endregion

        #region Risk Settings

        /// <summary>
        /// Probability of loss occurring each week for consignment shipments (0.25 = 25% chance).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Each Friday, a random roll determines if a loss event occurs for that week's payment.
        /// 25% chance means, on average, 1 out of 4 weeks will have losses.
        /// </para>
        /// <para><strong>Expected Losses:</strong></para>
        /// <para>
        /// With 4 weekly payments, expect ~1 loss event per consignment on average,
        /// reducing total payout by ~10-15% (depending on loss severity).
        /// </para>
        /// </remarks>
        public const float WEEKLY_LOSS_CHANCE = 0.25f;

        /// <summary>
        /// Minimum loss percentage when a loss event occurs (15%).
        /// </summary>
        /// <remarks>
        /// If a loss event happens, the loss will be between 15% and 60% of that week's payment.
        /// 15% is the "lucky" outcome - minor disruption.
        /// </remarks>
        public const int LOSS_MIN_PERCENT = 15;

        /// <summary>
        /// Maximum loss percentage when a loss event occurs (60%).
        /// </summary>
        /// <remarks>
        /// 60% is the "worst case" for a single week - major disruption (customs seizure, etc.).
        /// Even with maximum losses every week, consignment is usually still profitable vs wholesale.
        /// </remarks>
        public const int LOSS_MAX_PERCENT = 60;

        #endregion

        #region Default Prices

        /// <summary>
        /// Default brick price used when dynamic pricing is unavailable (735 × 20 = 14,700).
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is the fair market price for 8-mix cocaine (735 purity × $20 per purity point).
        /// </para>
        /// <para><strong>TODO:</strong></para>
        /// <para>
        /// Integrate with game's item system to get actual current market price dynamically.
        /// Currently, <see cref="PriceHelper.GetCurrentBrickPrice"/> returns this constant.
        /// </para>
        /// </remarks>
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
        /// <remarks>
        /// Used for: "Wholesale Available", "No losses", positive status indicators.
        /// </remarks>
        public static readonly UnityEngine.Color Success = new UnityEngine.Color(0.4f, 1f, 0.4f, 1f);

        /// <summary>
        /// Warning/caution state color (orange-red).
        /// </summary>
        /// <remarks>
        /// Used for: "Cooldown: X days remaining", loss event warnings, error states.
        /// </remarks>
        public static readonly UnityEngine.Color Warning = new UnityEngine.Color(1f, 0.5f, 0.3f, 1f);

        #endregion
    }

    /// <summary>
    /// Broker dialogue message templates for SMS notifications.
    /// </summary>
    /// <remarks>
    /// <para><strong>Design Pattern: Message Templates</strong></para>
    /// <para>
    /// Uses static methods returning formatted strings for dynamic messages (e.g., "Week 2 cleared. $300,000 released")
    /// and const strings for fixed messages (e.g., intro SMS). This allows compile-time optimization for
    /// static messages while supporting runtime formatting for dynamic values.
    /// </para>
    /// <para><strong>Message Categories:</strong></para>
    /// <list type="bullet">
    /// <item>Intro - Initial broker contact message</item>
    /// <item>Confirmations - Shipment creation confirmations (wholesale/consignment)</item>
    /// <item>Payouts - Weekly payment notifications</item>
    /// <item>Loss Events - Randomized loss event messages with multiple variants</item>
    /// <item>Protection - Floor protection notification when consignment underperforms</item>
    /// </list>
    /// <para><strong>Tone and Style:</strong></para>
    /// <para>
    /// Messages use terse, professional criminal jargon ("product moved", "released", "flagged")
    /// to match the broker's character. Numbers are formatted with thousands separators
    /// (e.g., "$1,470,000") for readability.
    /// </para>
    /// </remarks>
    /// <example>
    /// Sending a weekly payout message:
    /// <code>
    /// string message = BrokerMessages.WeekCleared(weekNumber: 2, payout: 300000);
    /// // Result: "Week 2 cleared. $300,000 released."
    /// SendSMS("The Broker", message);
    /// </code>
    /// </example>
    /// <example>
    /// Sending a random loss event message:
    /// <code>
    /// string message = BrokerMessages.GetRandomLossMessage(
    ///     week: 3,
    ///     lossPercent: 45,
    ///     actualPayout: 165000,
    ///     expectedPayout: 300000
    /// );
    /// // Result (random): "Week 3: Customs flagged a container. 45% of your shipment didn't make it. You received $165,000 instead of $300,000."
    /// SendSMS("The Broker", message);
    /// </code>
    /// </example>
    public static class BrokerMessages
    {
        /// <summary>
        /// Initial SMS sent when broker unlocks (Rank 13 + Docks ownership).
        /// </summary>
        /// <remarks>
        /// Sent once when unlock conditions are met. Directs player to check phone for the new app.
        /// </remarks>
        public const string INTRO_SMS = "Heard you control the docks. I move product overseas. Check your phone for details.";
        
        /// <summary>
        /// Wholesale shipment confirmation message.
        /// </summary>
        /// <param name="quantity">Number of bricks shipped</param>
        /// <param name="totalValue">Total payout amount (instant)</param>
        /// <returns>Formatted confirmation message with cooldown reminder</returns>
        /// <remarks>
        /// Sent immediately after creating a wholesale shipment. Confirms instant payout
        /// and reminds player of 30-day cooldown before next wholesale shipment.
        /// </remarks>
        public static string WholesaleConfirmed(int quantity, int totalValue) =>
            $"Wholesale shipment confirmed. {quantity} bricks moved. ${totalValue:N0} transferred instantly. Next slot in 30 days.";

        /// <summary>
        /// Consignment shipment lock-in confirmation message.
        /// </summary>
        /// <param name="quantity">Number of bricks shipped</param>
        /// <param name="pricePerBrick">Consignment price per brick (1.6x wholesale)</param>
        /// <param name="totalValue">Total expected value over 4 weeks (before losses)</param>
        /// <returns>Formatted confirmation with payment schedule details</returns>
        /// <remarks>
        /// Sent when consignment is created. Shows the locked-in price (prices don't fluctuate
        /// during the 4 weeks) and sets expectations for weekly Friday payments.
        /// </remarks>
        public static string ConsignmentLocked(int quantity, int pricePerBrick, int totalValue) =>
            $"Consignment locked. {quantity} bricks @ ${pricePerBrick:N0} each = ${totalValue:N0}. First payment Friday. 4 weeks total.";

        /// <summary>
        /// Standard weekly payout message (no losses).
        /// </summary>
        /// <param name="week">Week number (1-4)</param>
        /// <param name="payout">Payment amount for this week</param>
        /// <returns>Terse confirmation of successful week</returns>
        /// <remarks>
        /// Sent on Friday when no loss event occurred. Brief and to-the-point.
        /// </remarks>
        public static string WeekCleared(int week, int payout) =>
            $"Week {week} cleared. ${payout:N0} released.";

        /// <summary>
        /// Alternative weekly payout message with more detail (no losses).
        /// </summary>
        /// <param name="week">Week number (1-4)</param>
        /// <param name="payout">Payment amount for this week</param>
        /// <returns>Detailed confirmation of successful week</returns>
        /// <remarks>
        /// Same as <see cref="WeekCleared"/> but with more explanation. Currently unused
        /// in the codebase - only WeekCleared is called. Could be used for variety.
        /// </remarks>
        public static string WeekClearedAlt(int week, int payout) =>
            $"Week {week}: Shipment arrived without issue. ${payout:N0} sent.";
        
        /// <summary>
        /// Loss event message: Customs seizure scenario.
        /// </summary>
        /// <param name="week">Week number (1-4)</param>
        /// <param name="lossPercent">Percentage of shipment lost (15-60%)</param>
        /// <param name="actualPayout">Reduced payment received</param>
        /// <param name="expectedPayout">What the payment would have been without losses</param>
        /// <returns>Customs-themed loss event message</returns>
        /// <remarks>
        /// One of four randomized loss event messages. Implies government interference
        /// (customs officials flagged the shipment). Maintains criminal realism.
        /// </remarks>
        public static string CustomsLoss(int week, int lossPercent, int actualPayout, int expectedPayout) =>
            $"Week {week}: Customs flagged a container. {lossPercent}% of your shipment didn't make it. You received ${actualPayout:N0} instead of ${expectedPayout:N0}.";

        /// <summary>
        /// Loss event message: Transit delay/spoilage scenario.
        /// </summary>
        /// <param name="week">Week number (1-4)</param>
        /// <param name="lossPercent">Percentage of shipment lost (15-60%)</param>
        /// <param name="actualPayout">Reduced payment received</param>
        /// <param name="expectedPayout">What the payment would have been without losses</param>
        /// <returns>Transit-delay-themed loss event message</returns>
        /// <remarks>
        /// One of four randomized loss event messages. Implies logistical problems
        /// (shipment delayed, product degraded). Emphasizes risk of time-sensitive cargo.
        /// </remarks>
        public static string TransitDelayLoss(int week, int lossPercent, int actualPayout, int expectedPayout) =>
            $"Week {week}: Shipment delayed in transit. {lossPercent}% of product spoiled. You received ${actualPayout:N0} instead of ${expectedPayout:N0}.";

        /// <summary>
        /// Loss event message: Port inspection scenario.
        /// </summary>
        /// <param name="week">Week number (1-4)</param>
        /// <param name="lossPercent">Percentage of shipment lost (15-60%)</param>
        /// <param name="actualPayout">Reduced payment received</param>
        /// <param name="expectedPayout">What the payment would have been without losses</param>
        /// <returns>Inspection-themed loss event message</returns>
        /// <remarks>
        /// One of four randomized loss event messages. Implies regulatory interference
        /// (discrepancies found during port inspection). Similar to customs but more bureaucratic.
        /// </remarks>
        public static string InspectionLoss(int week, int lossPercent, int actualPayout, int expectedPayout) =>
            $"Week {week}: Port inspection found discrepancies. {lossPercent}% lost. You received ${actualPayout:N0} instead of ${expectedPayout:N0}.";

        /// <summary>
        /// Loss event message: Crew betrayal scenario.
        /// </summary>
        /// <param name="week">Week number (1-4)</param>
        /// <param name="lossPercent">Percentage of shipment lost (15-60%)</param>
        /// <param name="actualPayout">Reduced payment received</param>
        /// <param name="expectedPayout">What the payment would have been without losses</param>
        /// <returns>Crew-betrayal-themed loss event message</returns>
        /// <remarks>
        /// One of four randomized loss event messages. Implies internal betrayal
        /// (crew stole or shorted the shipment). Emphasizes the "honor among thieves" risk.
        /// </remarks>
        public static string CrewShortLoss(int week, int lossPercent, int actualPayout, int expectedPayout) =>
            $"Week {week}: Crew shorted your cut. {lossPercent}% underdelivered. You received ${actualPayout:N0} instead of ${expectedPayout:N0}.";

        /// <summary>
        /// Floor protection message sent when consignment underperforms wholesale.
        /// </summary>
        /// <param name="topUpAmount">Additional money added to match wholesale floor</param>
        /// <returns>Broker's explanation of floor protection top-up</returns>
        /// <remarks>
        /// <para>
        /// If cumulative losses reduce consignment total below wholesale equivalent,
        /// the broker "tops up" the final payment to match wholesale value. This prevents
        /// consignment from being objectively worse than wholesale choice.
        /// </para>
        /// <para><strong>Design Intent:</strong></para>
        /// <para>
        /// Floor protection ensures consignment risk is never "unfair" - even with bad luck,
        /// the player breaks even with the safe choice. This encourages experimentation.
        /// </para>
        /// </remarks>
        public static string FloorProtection(int topUpAmount) =>
            $"Tough month, but I made sure you didn't take a total loss. Added ${topUpAmount:N0} to match wholesale. Let's call it even.";
        
        /// <summary>
        /// List of all available loss event message templates for randomization.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Contains function references to all four loss message methods. Used by
        /// <see cref="GetRandomLossMessage"/> to randomly select which scenario to present.
        /// </para>
        /// <para><strong>Why Randomization?</strong></para>
        /// <para>
        /// Variety in messaging prevents repetition and makes each loss event feel unique.
        /// Different scenarios (customs, transit, inspection, crew) suggest different causes
        /// even though the mechanical effect is identical (X% loss).
        /// </para>
        /// </remarks>
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
        /// <param name="week">Week number (1-4)</param>
        /// <param name="lossPercent">Percentage of shipment lost (15-60%)</param>
        /// <param name="actualPayout">Reduced payment received</param>
        /// <param name="expectedPayout">What the payment would have been without losses</param>
        /// <returns>One of four randomly chosen loss event messages</returns>
        /// <remarks>
        /// <para>
        /// Uses <c>UnityEngine.Random.Range()</c> to select from the <see cref="LossMessages"/> list.
        /// Each of the four message templates has equal probability (25% each).
        /// </para>
        /// <para><strong>Usage:</strong></para>
        /// <para>
        /// Call this instead of calling the individual loss message methods directly.
        /// Ensures variety in player-facing messaging even when losses use the same underlying mechanics.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Random result - could be any of the 4 loss message templates
        /// string msg = BrokerMessages.GetRandomLossMessage(2, 30, 210000, 300000);
        /// // Possible results:
        /// // "Week 2: Customs flagged a container. 30% of your shipment didn't make it..."
        /// // "Week 2: Shipment delayed in transit. 30% of product spoiled..."
        /// // "Week 2: Port inspection found discrepancies. 30% lost..."
        /// // "Week 2: Crew shorted your cut. 30% underdelivered..."
        /// </code>
        /// </example>
        public static string GetRandomLossMessage(int week, int lossPercent, int actualPayout, int expectedPayout)
        {
            int index = UnityEngine.Random.Range(0, LossMessages.Count);
            return LossMessages[index](week, lossPercent, actualPayout, expectedPayout);
        }
    }


    /// <summary>
    /// Helper utilities for shipment price calculations and payout computations.
    /// </summary>
    /// <remarks>
    /// <para><strong>Design Pattern: Pure Functions</strong></para>
    /// <para>
    /// All methods are static, stateless, and deterministic (except <see cref="GetCurrentBrickPrice"/>).
    /// Given the same inputs, they always return the same output. This makes testing and reasoning
    /// about pricing logic simple.
    /// </para>
    /// <para><strong>Calculation Hierarchy:</strong></para>
    /// <list type="number">
    /// <item>Get brick price: <see cref="GetCurrentBrickPrice"/> (currently returns constant)</item>
    /// <item>Calculate shipment value: <see cref="CalculateWholesalePayout"/> or <see cref="CalculateConsignmentValue"/></item>
    /// <item>Calculate weekly payment: <see cref="CalculateWeeklyPayout"/> (consignment only)</item>
    /// <item>Apply losses: <see cref="ApplyLoss"/> (consignment only, if loss event occurs)</item>
    /// <item>Check floor protection: <see cref="CalculateWholesaleFloor"/> (final week safety net)</item>
    /// </list>
    /// <para><strong>Current Limitation:</strong></para>
    /// <para>
    /// Brick price is hardcoded to <see cref="DockExportsConfig.DEFAULT_BRICK_PRICE"/>. Future integration
    /// with S1API.Items would allow dynamic pricing based on game economy.
    /// </para>
    /// </remarks>
    /// <example>
    /// Complete consignment payout calculation:
    /// <code>
    /// // Setup
    /// int brickPrice = PriceHelper.GetCurrentBrickPrice(); // $14,700
    /// int quantity = 100;
    ///
    /// // Total value calculation
    /// int totalValue = PriceHelper.CalculateConsignmentValue(quantity, brickPrice);
    /// // = 100 × $14,700 × 1.6 = $2,352,000
    ///
    /// // Weekly payment calculation
    /// int weeklyBase = PriceHelper.CalculateWeeklyPayout(totalValue);
    /// // = $2,352,000 / 4 = $588,000 per week
    ///
    /// // Apply loss (if loss event occurred)
    /// int lossPercent = 35; // 35% loss
    /// int actualPayout = PriceHelper.ApplyLoss(weeklyBase, lossPercent);
    /// // = $588,000 × (1 - 0.35) = $382,200
    ///
    /// // Floor protection check (after all 4 weeks)
    /// int wholesaleFloor = PriceHelper.CalculateWholesaleFloor(quantity, brickPrice);
    /// // = 100 × $14,700 = $1,470,000 (minimum guaranteed payout)
    /// </code>
    /// </example>
    public static class PriceHelper
    {
        /// <summary>
        /// Gets the current market price for a brick of cocaine.
        /// </summary>
        /// <returns>Price in dollars (currently always <see cref="DockExportsConfig.DEFAULT_BRICK_PRICE"/>)</returns>
        /// <remarks>
        /// <para><strong>⚠️ TODO: Dynamic Pricing</strong></para>
        /// <para>
        /// Currently returns a hardcoded constant ($14,700 for 8-mix cocaine at 735 purity × $20/purity).
        /// Future versions should integrate with S1API.Items to query the actual current market price:
        /// </para>
        /// <code>
        /// var cocaineDef = (DrugDefinition)ItemManager.GetItemDefinition("cocaine_8mix");
        /// return (int)cocaineDef.BasePrice; // Get actual market price
        /// </code>
        /// <para>
        /// This would allow shipment values to reflect real-time economy changes in the game.
        /// </para>
        /// </remarks>
        public static int GetCurrentBrickPrice()
        {
            // This should query the game's item system
            // For now, return the default 8-mix coke price
            return DockExportsConfig.DEFAULT_BRICK_PRICE;
        }

        /// <summary>
        /// Calculates total wholesale shipment payout (instant, no multiplier).
        /// </summary>
        /// <param name="quantity">Number of bricks shipped</param>
        /// <param name="brickPrice">Current brick price</param>
        /// <returns>Total payout amount</returns>
        /// <remarks>
        /// <para>
        /// Wholesale is the safe option: fair market value, instant payout, no risk.
        /// Simple multiplication with no modifiers.
        /// </para>
        /// <para><strong>Example:</strong></para>
        /// <para>
        /// 100 bricks × $14,700 = $1,470,000 instant payout.
        /// </para>
        /// </remarks>
        public static int CalculateWholesalePayout(int quantity, int brickPrice)
        {
            return quantity * brickPrice;
        }

        /// <summary>
        /// Calculates total consignment shipment value with price multiplier.
        /// </summary>
        /// <param name="quantity">Number of bricks shipped</param>
        /// <param name="brickPrice">Current brick price</param>
        /// <returns>Total expected value over 4 weeks (before losses)</returns>
        /// <remarks>
        /// <para>
        /// Consignment offers a <see cref="DockExportsConfig.CONSIGNMENT_MULTIPLIER"/> (1.6x) price boost
        /// in exchange for delayed payment and loss risk. This total is divided into 4 weekly payments.
        /// </para>
        /// <para><strong>Example:</strong></para>
        /// <para>
        /// 100 bricks × $14,700 × 1.6 = $2,352,000 total (if no losses occur).
        /// This is paid as 4 × $588,000 weekly payments.
        /// </para>
        /// <para><strong>Why 1.6x?</strong></para>
        /// <para>
        /// The multiplier compensates for payment delay, uncertainty, and average expected losses
        /// (~10-15% total). Even with typical losses, consignment usually outperforms wholesale.
        /// </para>
        /// </remarks>
        public static int CalculateConsignmentValue(int quantity, int brickPrice)
        {
            return (int)(quantity * brickPrice * DockExportsConfig.CONSIGNMENT_MULTIPLIER);
        }

        /// <summary>
        /// Calculates expected payout for a single week of consignment (1/4 of total).
        /// </summary>
        /// <param name="totalValue">Total consignment value (from <see cref="CalculateConsignmentValue"/>)</param>
        /// <returns>Base payout for one week (before loss roll)</returns>
        /// <remarks>
        /// <para>
        /// Consignment total is split evenly over <see cref="DockExportsConfig.CONSIGNMENT_INSTALLMENTS"/> (4) weeks.
        /// Each Friday, this base amount is paid out (unless a loss event occurs).
        /// </para>
        /// <para><strong>Example:</strong></para>
        /// <para>
        /// $2,352,000 total / 4 weeks = $588,000 per week.
        /// </para>
        /// </remarks>
        public static int CalculateWeeklyPayout(int totalValue)
        {
            return totalValue / DockExportsConfig.CONSIGNMENT_INSTALLMENTS;
        }

        /// <summary>
        /// Applies loss percentage to a base payout amount.
        /// </summary>
        /// <param name="basePayout">Expected payout before loss</param>
        /// <param name="lossPercent">Loss percentage (15-60%)</param>
        /// <returns>Reduced payout after applying loss</returns>
        /// <remarks>
        /// <para>
        /// When a loss event occurs (25% weekly chance), this method calculates the reduced payout.
        /// Loss percentage is randomly rolled between <see cref="DockExportsConfig.LOSS_MIN_PERCENT"/> (15%)
        /// and <see cref="DockExportsConfig.LOSS_MAX_PERCENT"/> (60%).
        /// </para>
        /// <para><strong>Example:</strong></para>
        /// <para>
        /// Base payout: $588,000<br/>
        /// Loss event: 35%<br/>
        /// Actual payout: $588,000 × (1 - 0.35) = $382,200<br/>
        /// Player loses: $205,800 this week
        /// </para>
        /// </remarks>
        public static int ApplyLoss(int basePayout, int lossPercent)
        {
            return (int)(basePayout * (1f - lossPercent / 100f));
        }

        /// <summary>
        /// Calculates the wholesale equivalent floor for floor protection.
        /// </summary>
        /// <param name="quantity">Number of bricks shipped (consignment quantity)</param>
        /// <param name="brickPrice">Current brick price</param>
        /// <returns>Minimum guaranteed total payout (wholesale value)</returns>
        /// <remarks>
        /// <para><strong>Floor Protection System:</strong></para>
        /// <para>
        /// If cumulative consignment payouts (after all losses) fall below what wholesale would have paid,
        /// the broker "tops up" the final payment to match wholesale. This ensures consignment risk
        /// is never strictly worse than the safe choice.
        /// </para>
        /// <para><strong>Example Scenario:</strong></para>
        /// <para>
        /// 100 bricks shipped on consignment:<br/>
        /// - Expected total: $2,352,000 (1.6x × $14,700)<br/>
        /// - Wholesale floor: $1,470,000 (100 × $14,700)<br/>
        /// - Actual received after heavy losses: $1,200,000<br/>
        /// - Top-up applied: $270,000 to reach $1,470,000 floor<br/>
        /// </para>
        /// <para>
        /// Player still receives wholesale equivalent despite bad luck, but misses out on the
        /// potential 60% bonus. Floor protection prevents consignment from being a trap.
        /// </para>
        /// </remarks>
        public static int CalculateWholesaleFloor(int quantity, int brickPrice)
        {
            return quantity * brickPrice;
        }
    }
}
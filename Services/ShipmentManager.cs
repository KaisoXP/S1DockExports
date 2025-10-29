/// <summary>
/// Shipment management and save/load system.
/// </summary>
/// <remarks>
/// <para>
/// This file contains all shipment business logic and data persistence for the Dock Exports system.
/// Uses S1API's Saveable base class for automatic save/load integration with the game's save system.
/// </para>
/// <para><strong>Contents:</strong></para>
/// <list type="bullet">
/// <item><see cref="ShipmentManager"/> - Main manager class (extends S1API.Saveables.Saveable)</item>
/// <item><see cref="ShipmentType"/> - Enum for wholesale vs consignment</item>
/// <item><see cref="ShipmentData"/> - Active shipment state structure</item>
/// <item><see cref="ShipmentHistoryEntry"/> - Completed shipment record</item>
/// </list>
/// <para><strong>Architecture Pattern:</strong> S1API Saveable with Singleton</para>
/// <para>
/// Extends <c>Saveable</c> base class from S1API, which provides automatic save/load via
/// the <c>[SaveableField]</c> attribute. The singleton pattern (<see cref="ShipmentManager.Instance"/>)
/// provides global access from other classes (DockExportsMod, DockExportsApp).
/// </para>
/// <para><strong>Key Responsibilities:</strong></para>
/// <list type="bullet">
/// <item>Track active shipment state (quantity, type, payments made)</item>
/// <item>Process weekly consignment payments with loss rolls</item>
/// <item>Manage wholesale cooldown timer</item>
/// <item>Archive completed shipments to history</item>
/// <item>Persist all data to game save file</item>
/// </list>
/// </remarks>
using S1API.Internal.Abstraction;
using S1API.Saveables;
using System;
using System.Collections.Generic;
using System.Linq;

namespace S1DockExports.Services
{
    /// <summary>
    /// Manages shipment data and persistence using S1API's Saveable system.
    /// </summary>
    /// <remarks>
    /// <para><strong>Design Pattern: Saveable Singleton</strong></para>
    /// <para>
    /// Extends <c>S1API.Saveables.Saveable</c> for automatic save/load integration.
    /// Uses singleton pattern (<see cref="Instance"/>) for global access.
    /// </para>
    /// <para><strong>S1API Saveable System:</strong></para>
    /// <para>
    /// Fields marked with <c>[SaveableField("key")]</c> are automatically serialized to
    /// the game's save file. When the game loads a save, S1API deserializes these fields
    /// and calls <see cref="OnLoaded"/>. For new saves, <see cref="OnCreated"/> is called instead.
    /// </para>
    /// <para><strong>Automatic Registration:</strong></para>
    /// <para>
    /// S1API v2.4.2+ automatically discovers classes that inherit from <c>Saveable</c>.
    /// No manual <c>ModSaveableRegistry.Register()</c> call is required.
    /// </para>
    /// <para><strong>Saved Data:</strong></para>
    /// <list type="bullet">
    /// <item><see cref="_activeShipment"/> - Current shipment in progress (null if none)</item>
    /// <item><see cref="_history"/> - List of completed shipments</item>
    /// <item><see cref="_lastProcessedDay"/> - Last day we processed a payout (prevents double-processing)</item>
    /// <item><see cref="_wholesaleCooldownEndDay"/> - Day when wholesale becomes available again</item>
    /// </list>
    /// <para><strong>Lifecycle:</strong></para>
    /// <para>
    /// Constructor → OnCreated (new save) or OnLoaded (existing save) → Normal operation
    /// </para>
    /// </remarks>
    /// <example>
    /// Accessing the singleton from other code:
    /// <code>
    /// // Check if wholesale is on cooldown
    /// bool onCooldown = ShipmentManager.Instance.IsWholesaleOnCooldown(currentDay);
    ///
    /// // Get active shipment
    /// ShipmentData? active = ShipmentManager.Instance.ActiveShipment;
    ///
    /// // Subscribe to data loaded event (for UI refreshing)
    /// ShipmentManager.OnShipmentsLoaded += () => {
    ///     MelonLogger.Msg("Shipments loaded, refreshing UI...");
    /// };
    /// </code>
    /// </example>
    public class ShipmentManager : Saveable
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ShipmentManager"/>.
        /// </summary>
        /// <remarks>
        /// <para><strong>Singleton Registration:</strong></para>
        /// <para>
        /// Sets <see cref="Instance"/> to this instance. Called both when creating a new manager
        /// and when S1API deserializes from save data.
        /// </para>
        /// <para><strong>Important:</strong></para>
        /// <para>
        /// S1API may create multiple instances during save/load. Always use <see cref="Instance"/>
        /// to access the current active manager.
        /// </para>
        /// </remarks>
        public ShipmentManager()
        {
            Instance = this;
        }

        /// <summary>
        /// Active shipment currently in progress (null if no active shipment).
        /// </summary>
        /// <remarks>
        /// <para><strong>Saved to:</strong> "ActiveShipment" key in save file</para>
        /// <para>
        /// When wholesale is created, this is set with PaymentsMade = 0, TotalPaid = 0.
        /// When consignment is created, same initial state. As payments are processed,
        /// TotalPaid and PaymentsMade are incremented. When complete, this is set to null
        /// and the shipment is archived to <see cref="_history"/>.
        /// </para>
        /// </remarks>
        [SaveableField("ActiveShipment")]
        private ShipmentData? _activeShipment = null;

        /// <summary>
        /// List of completed shipments (archived when shipments finish).
        /// </summary>
        /// <remarks>
        /// <para><strong>Saved to:</strong> "ShipmentHistory" key in save file</para>
        /// <para>
        /// New entries are added when shipments complete via <see cref="CompleteActiveShipment"/>.
        /// Displayed in the History tab of the phone app. Never cleared (permanent record).
        /// </para>
        /// </remarks>
        [SaveableField("ShipmentHistory")]
        private List<ShipmentHistoryEntry> _history = new List<ShipmentHistoryEntry>();

        /// <summary>
        /// Last in-game day that a payout was processed (prevents double-processing).
        /// </summary>
        /// <remarks>
        /// <para><strong>Saved to:</strong> "LastProcessedDay" key in save file</para>
        /// <para>
        /// Updated by DockExportsMod after processing Friday payouts. If the current day
        /// matches this value, payout processing is skipped. This prevents processing the
        /// same Friday multiple times if the game is saved/loaded on Friday.
        /// </para>
        /// <para>
        /// -1 means no payouts have been processed yet.
        /// </para>
        /// </remarks>
        [SaveableField("LastProcessedDay")]
        private int _lastProcessedDay = -1;

        /// <summary>
        /// In-game day when wholesale cooldown expires (-1 if no cooldown).
        /// </summary>
        /// <remarks>
        /// <para><strong>Saved to:</strong> "WholesaleCooldownEndDay" key in save file</para>
        /// <para>
        /// Set when wholesale shipment is processed (currentDay + <see cref="DockExportsConfig.WHOLESALE_COOLDOWN_DAYS"/>).
        /// Checked by <see cref="IsWholesaleOnCooldown"/> to prevent creating new wholesale shipments too soon.
        /// </para>
        /// <para>
        /// -1 means no cooldown (wholesale is available).
        /// </para>
        /// </remarks>
        [SaveableField("WholesaleCooldownEndDay")]
        private int _wholesaleCooldownEndDay = -1;

        /// <summary>
        /// Singleton instance of the ShipmentManager.
        /// </summary>
        /// <remarks>
        /// <para><strong>Global Access Pattern:</strong></para>
        /// <para>
        /// Use <c>ShipmentManager.Instance</c> to access the manager from anywhere in the code.
        /// The instance is updated in the constructor, <see cref="OnLoaded"/>, and <see cref="OnCreated"/>.
        /// </para>
        /// <para><strong>Thread Safety:</strong></para>
        /// <para>
        /// Not thread-safe, but Unity/MelonLoader runs on a single thread so this is fine.
        /// </para>
        /// </remarks>
        public static ShipmentManager Instance { get; private set; } = new ShipmentManager();

        /// <summary>
        /// Event fired when shipment data is loaded from save file.
        /// </summary>
        /// <remarks>
        /// <para><strong>When Invoked:</strong></para>
        /// <para>
        /// Called at the end of <see cref="OnLoaded"/> after S1API has deserialized all
        /// <c>[SaveableField]</c> fields from the save file.
        /// </para>
        /// <para><strong>Subscribers:</strong></para>
        /// <para>
        /// <c>DockExportsApp.OnCreated()</c> subscribes to this event to refresh the UI
        /// (Active and History tabs) when save data loads.
        /// </para>
        /// <para><strong>Null-Safety:</strong></para>
        /// <para>
        /// Uses null-conditional operator (<c>?.</c>) when invoking to prevent crashes if no subscribers.
        /// </para>
        /// </remarks>
        /// <example>
        /// Subscribing to the event:
        /// <code>
        /// ShipmentManager.OnShipmentsLoaded += () => {
        ///     MelonLogger.Msg("Shipments loaded!");
        ///     RefreshActivePanel();
        ///     RefreshHistoryPanel();
        /// };
        /// </code>
        /// </example>
        public static event Action? OnShipmentsLoaded;

        /// <summary>
        /// Called by S1API after loading data from save file.
        /// </summary>
        /// <remarks>
        /// <para><strong>Lifecycle:</strong> Constructor → S1API deserializes fields → <strong>OnLoaded</strong></para>
        /// <para>
        /// At this point, all <c>[SaveableField]</c> fields have been deserialized from the save file.
        /// <see cref="_activeShipment"/>, <see cref="_history"/>, <see cref="_lastProcessedDay"/>,
        /// and <see cref="_wholesaleCooldownEndDay"/> contain the saved data.
        /// </para>
        /// <para><strong>Responsibilities:</strong></para>
        /// <list type="number">
        /// <item>Update <see cref="Instance"/> to this loaded instance</item>
        /// <item>Log loaded data summary for debugging</item>
        /// <item>Invoke <see cref="OnShipmentsLoaded"/> event to notify subscribers (e.g., UI)</item>
        /// </list>
        /// <para><strong>Logging:</strong></para>
        /// <para>
        /// Logs a summary: "Active: Consignment, 2/4 payments, History: 3 entries, Cooldown: 15 days"
        /// This helps verify save/load is working correctly during development.
        /// </para>
        /// </remarks>
        protected override void OnLoaded()
        {
            Instance = this;
            MelonLoader.MelonLogger.Msg("[DockExports] 📂 Shipments loaded from save");

            // Log loaded data summary
            string activeInfo = _activeShipment.HasValue
                ? $"{_activeShipment.Value.Type}, {_activeShipment.Value.PaymentsMade}/{(_activeShipment.Value.Type == ShipmentType.Wholesale ? 1 : 4)} payments"
                : "none";

            MelonLoader.MelonLogger.Msg($"[DockExports] Active: {activeInfo}, History: {_history.Count} entries, Cooldown: {(_wholesaleCooldownEndDay > 0 ? _wholesaleCooldownEndDay + " days" : "none")}");

            // Backfill base unit prices for saves created before this field existed
            if (_activeShipment.HasValue)
            {
                var shipment = _activeShipment.Value;
                if (shipment.BaseUnitPrice <= 0)
                {
                    shipment.BaseUnitPrice = shipment.Type == ShipmentType.Consignment
                        ? CalculateBasePrice(shipment.UnitPrice)
                        : shipment.UnitPrice;
                    _activeShipment = shipment;
                }
            }

            foreach (var entry in _history)
            {
                if (entry.BaseUnitPrice <= 0)
                {
                    entry.BaseUnitPrice = entry.Type == ShipmentType.Consignment
                        ? CalculateBasePrice(entry.UnitPrice)
                        : entry.UnitPrice;
                }
            }

            // Notify UI that shipments have been loaded
            OnShipmentsLoaded?.Invoke();
        }

        /// <summary>
        /// Called by S1API when creating a new save file (no existing data to load).
        /// </summary>
        /// <remarks>
        /// <para><strong>Lifecycle:</strong> Constructor → <strong>OnCreated</strong> (no OnLoaded)</para>
        /// <para>
        /// When the game creates a new save, S1API calls this instead of <see cref="OnLoaded"/>.
        /// All <c>[SaveableField]</c> fields have their default values (initialized in field declarations).
        /// </para>
        /// <para><strong>Responsibilities:</strong></para>
        /// <list type="number">
        /// <item>Update <see cref="Instance"/> to this new instance</item>
        /// <item>Log creation for debugging</item>
        /// </list>
        /// <para><strong>No Event Invocation:</strong></para>
        /// <para>
        /// Unlike <see cref="OnLoaded"/>, this does NOT invoke <see cref="OnShipmentsLoaded"/>
        /// because there's no data to load - everything is at default values.
        /// </para>
        /// </remarks>
        protected override void OnCreated()
        {
            Instance = this;
            MelonLoader.MelonLogger.Msg("[DockExports] 📂 ShipmentManager created (new save)");
        }

        // Note: Manual registration is no longer required in S1API v2.4.2+
        // Classes that inherit from Saveable are automatically discovered

        #region Public Properties

        /// <summary>
        /// Gets the currently active shipment (null if no active shipment).
        /// </summary>
        /// <value>Nullable <see cref="ShipmentData"/> struct containing shipment state</value>
        /// <remarks>
        /// <para>
        /// Use <c>.HasValue</c> to check if a shipment exists, then <c>.Value</c> to access it.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var active = ShipmentManager.Instance.ActiveShipment;
        /// if (active.HasValue)
        /// {
        ///     MelonLogger.Msg($"Active: {active.Value.Type}, {active.Value.PaymentsMade} payments made");
        /// }
        /// </code>
        /// </example>
        public ShipmentData? ActiveShipment => _activeShipment;

        /// <summary>
        /// Gets the read-only list of completed shipments.
        /// </summary>
        /// <value>Read-only list of <see cref="ShipmentHistoryEntry"/> objects</value>
        /// <remarks>
        /// <para>
        /// Returns an <c>IReadOnlyList</c> wrapper around <see cref="_history"/> to prevent
        /// external code from modifying the history. Only <see cref="CompleteActiveShipment"/>
        /// can add entries.
        /// </para>
        /// </remarks>
        public IReadOnlyList<ShipmentHistoryEntry> History => _history.AsReadOnly();

        /// <summary>
        /// Gets or sets the last in-game day that a payout was processed.
        /// </summary>
        /// <value>Day number, or -1 if no payouts have been processed yet</value>
        /// <remarks>
        /// <para><strong>Usage:</strong></para>
        /// <para>
        /// DockExportsMod checks this before processing Friday payouts. If <c>currentDay == LastProcessedDay</c>,
        /// skip processing to prevent double payouts if the game is saved/loaded multiple times on Friday.
        /// </para>
        /// <para><strong>Example:</strong></para>
        /// <code>
        /// if (TimeManager.ElapsedDays != ShipmentManager.Instance.LastProcessedDay)
        /// {
        ///     ProcessFridayPayout();
        ///     ShipmentManager.Instance.LastProcessedDay = TimeManager.ElapsedDays;
        /// }
        /// </code>
        /// </remarks>
        public int LastProcessedDay
        {
            get => _lastProcessedDay;
            set => _lastProcessedDay = value;
        }

        /// <summary>
        /// Checks if wholesale is currently on cooldown.
        /// </summary>
        /// <param name="currentDay">Current in-game day number</param>
        /// <returns>True if cooldown is active, false if wholesale is available</returns>
        /// <remarks>
        /// <para>
        /// Wholesale has a <see cref="DockExportsConfig.WHOLESALE_COOLDOWN_DAYS"/> (30 days) cooldown
        /// after each use. This method compares <paramref name="currentDay"/> to <see cref="_wholesaleCooldownEndDay"/>
        /// to determine availability.
        /// </para>
        /// <para><strong>Logic:</strong></para>
        /// <para>
        /// If currentDay &lt; _wholesaleCooldownEndDay, cooldown is active (return true).
        /// Otherwise, wholesale is available (return false).
        /// </para>
        /// </remarks>
        public bool IsWholesaleOnCooldown(int currentDay)
        {
            return currentDay < _wholesaleCooldownEndDay;
        }

        /// <summary>
        /// Gets the number of days remaining until wholesale cooldown expires.
        /// </summary>
        /// <param name="currentDay">Current in-game day number</param>
        /// <returns>Days remaining (0 if no cooldown)</returns>
        /// <remarks>
        /// <para>
        /// Used by the phone app UI to display "Cooldown: X days remaining" message.
        /// </para>
        /// <para><strong>Calculation:</strong></para>
        /// <para>
        /// If on cooldown: <c>_wholesaleCooldownEndDay - currentDay</c><br/>
        /// If not on cooldown: 0
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// int daysLeft = ShipmentManager.Instance.WholesaleDaysRemaining(currentDay);
        /// if (daysLeft > 0)
        ///     UI.ShowMessage($"Wholesale available in {daysLeft} days");
        /// else
        ///     UI.ShowMessage("Wholesale available now!");
        /// </code>
        /// </example>
        public int WholesaleDaysRemaining(int currentDay)
        {
            if (!IsWholesaleOnCooldown(currentDay))
                return 0;
            return _wholesaleCooldownEndDay - currentDay;
        }

        #endregion

        #region Shipment Creation

        /// <summary>
        /// Creates a new wholesale shipment.
        /// </summary>
        /// <param name="quantity">Number of bricks to ship</param>
        /// <param name="brickPrice">Current brick price (fair market value)</param>
        /// <param name="currentDay">Current in-game day number</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if an active shipment already exists or wholesale is on cooldown
        /// </exception>
        /// <remarks>
        /// <para><strong>Wholesale Characteristics:</strong></para>
        /// <list type="bullet">
        /// <item>Safe: Instant payout, no risk of losses</item>
        /// <item>Limited: Max <see cref="DockExportsConfig.WHOLESALE_CAP"/> (100) bricks</item>
        /// <item>Cooldown: <see cref="DockExportsConfig.WHOLESALE_COOLDOWN_DAYS"/> (30) days after use</item>
        /// </list>
        /// <para><strong>Validation:</strong></para>
        /// <para>
        /// Checks that no active shipment exists and wholesale is not on cooldown.
        /// Throws <see cref="InvalidOperationException"/> if validation fails.
        /// </para>
        /// <para><strong>Created Shipment State:</strong></para>
        /// <para>
        /// Type = Wholesale, TotalPaid = 0, PaymentsMade = 0, TotalValue = quantity × brickPrice
        /// </para>
        /// <para><strong>Payment Processing:</strong></para>
        /// <para>
        /// After creation, call <see cref="ProcessWholesalePayment"/> to complete the shipment
        /// (instant payout, set cooldown, archive to history).
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// try
        /// {
        ///     ShipmentManager.Instance.CreateWholesaleShipment(100, 14700, currentDay);
        ///     MelonLogger.Msg("Wholesale shipment created!");
        /// }
        /// catch (InvalidOperationException ex)
        /// {
        ///     MelonLogger.Warning($"Cannot create wholesale: {ex.Message}");
        /// }
        /// </code>
        /// </example>
        public void CreateWholesaleShipment(int quantity, int brickPrice, int currentDay)
        {
            if (_activeShipment.HasValue)
                throw new InvalidOperationException("Cannot create new shipment while one is active");

            if (IsWholesaleOnCooldown(currentDay))
            {
                int daysRemaining = WholesaleDaysRemaining(currentDay);
                throw new InvalidOperationException($"Wholesale is on cooldown. {daysRemaining} days remaining.");
            }

            int totalValue = quantity * brickPrice;

            _activeShipment = new ShipmentData
            {
                Type = ShipmentType.Wholesale,
                Quantity = quantity,
                UnitPrice = brickPrice,
                BaseUnitPrice = brickPrice,
                TotalValue = totalValue,
                TotalPaid = 0,
                PaymentsMade = 0,
                CreatedDay = currentDay
            };
        }

        /// <summary>
        /// Creates a new consignment shipment with price multiplier.
        /// </summary>
        /// <param name="quantity">Number of bricks to ship</param>
        /// <param name="brickPrice">Base brick price (before multiplier)</param>
        /// <param name="multiplier">Price multiplier (typically <see cref="DockExportsConfig.CONSIGNMENT_MULTIPLIER"/> = 1.6)</param>
        /// <param name="currentDay">Current in-game day number</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if an active shipment already exists
        /// </exception>
        /// <remarks>
        /// <para><strong>Consignment Characteristics:</strong></para>
        /// <list type="bullet">
        /// <item>Risky: 25% weekly chance of 15-60% loss per payment</item>
        /// <item>High reward: 1.6x price multiplier (60% bonus)</item>
        /// <item>Delayed payout: 4 weekly payments (Fridays)</item>
        /// <item>Larger capacity: Max <see cref="DockExportsConfig.CONSIGNMENT_CAP"/> (200) bricks</item>
        /// </list>
        /// <para><strong>Price Calculation:</strong></para>
        /// <para>
        /// Enhanced price = brickPrice × multiplier<br/>
        /// Total value = quantity × enhanced price<br/>
        /// Example: 200 bricks × ($14,700 × 1.6) = $4,704,000 total
        /// </para>
        /// <para><strong>Created Shipment State:</strong></para>
        /// <para>
        /// Type = Consignment, TotalPaid = 0, PaymentsMade = 0, UnitPrice = enhanced price
        /// </para>
        /// <para><strong>Payment Processing:</strong></para>
        /// <para>
        /// Call <see cref="ProcessConsignmentPayment"/> each Friday to process weekly payments.
        /// After 4 payments (<see cref="DockExportsConfig.CONSIGNMENT_INSTALLMENTS"/>), shipment
        /// is automatically completed and archived.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// try
        /// {
        ///     ShipmentManager.Instance.CreateConsignmentShipment(
        ///         quantity: 200,
        ///         brickPrice: 14700,
        ///         multiplier: DockExportsConfig.CONSIGNMENT_MULTIPLIER,
        ///         currentDay: currentDay
        ///     );
        ///     MelonLogger.Msg("Consignment shipment created!");
        /// }
        /// catch (InvalidOperationException ex)
        /// {
        ///     MelonLogger.Warning($"Cannot create consignment: {ex.Message}");
        /// }
        /// </code>
        /// </example>
        public void CreateConsignmentShipment(int quantity, int brickPrice, float multiplier, int currentDay)
        {
            if (_activeShipment.HasValue)
                throw new InvalidOperationException("Cannot create new shipment while one is active");

            int enhancedPrice = (int)(brickPrice * multiplier);
            int totalValue = quantity * enhancedPrice;

            _activeShipment = new ShipmentData
            {
                Type = ShipmentType.Consignment,
                Quantity = quantity,
                UnitPrice = enhancedPrice,
                BaseUnitPrice = brickPrice,
                TotalValue = totalValue,
                TotalPaid = 0,
                PaymentsMade = 0,
                CreatedDay = currentDay
            };
        }

        #endregion

        #region Shipment Processing

        /// <summary>
        /// Processes the wholesale shipment payment (instant, full payout).
        /// </summary>
        /// <param name="currentDay">Current in-game day number</param>
        /// <returns>Total payout transferred to the player.</returns>
        /// <remarks>
        /// <para><strong>Wholesale Payment Logic:</strong></para>
        /// <list type="number">
        /// <item>Pay full TotalValue instantly (TotalPaid = TotalValue)</item>
        /// <item>Mark as complete (PaymentsMade = 1)</item>
        /// <item>Start cooldown (endDay = currentDay + 30)</item>
        /// <item>Archive to history via <see cref="CompleteActiveShipment"/></item>
        /// <item>Clear active shipment (_activeShipment = null)</item>
        /// </list>
        /// <para><strong>Validation:</strong></para>
        /// <para>
        /// Returns early (no-op) if no active shipment or active shipment is not wholesale.
        /// </para>
        /// <para><strong>Cooldown:</strong></para>
        /// <para>
        /// Sets <see cref="_wholesaleCooldownEndDay"/> to prevent creating another wholesale
        /// shipment for <see cref="DockExportsConfig.WHOLESALE_COOLDOWN_DAYS"/> (30) days.
        /// </para>
        /// </remarks>
        /// <example>
        /// Called by DockExportsMod after wholesale shipment is created:
        /// <code>
        /// // Create wholesale shipment
        /// ShipmentManager.Instance.CreateWholesaleShipment(100, 14700, currentDay);
        ///
        /// // Process payment immediately
        /// int payout = ShipmentManager.Instance.ProcessWholesalePayment(currentDay);
        ///
        /// // Player receives payout instantly (e.g., $1,470,000)
        /// // Wholesale is now on cooldown for 30 days
        /// </code>
        /// </example>
        public int ProcessWholesalePayment(int currentDay)
        {
            if (!_activeShipment.HasValue || _activeShipment.Value.Type != ShipmentType.Wholesale)
                return 0;

            var shipment = _activeShipment.Value;
            shipment.TotalPaid = shipment.TotalValue;
            shipment.PaymentsMade = 1;
            _activeShipment = shipment;

            // Set cooldown
            _wholesaleCooldownEndDay = currentDay + DockExportsConfig.WHOLESALE_COOLDOWN_DAYS;

            // Complete and archive
            CompleteActiveShipment();

             return shipment.TotalValue;
        }

        /// <summary>
        /// Processes one weekly consignment payment with loss roll.
        /// </summary>
        /// <param name="lossPercent">Out parameter: Loss percentage (0-60%, or 0 if no loss)</param>
        /// <param name="floorTopUp">Out parameter: Additional funds added to match wholesale floor on final week.</param>
        /// <returns>Actual payout amount after losses (including any floor top-up)</returns>
        /// <remarks>
        /// <para><strong>Consignment Payment Logic:</strong></para>
        /// <list type="number">
        /// <item>Calculate expected payout (TotalValue / 4 = 25% per week)</item>
        /// <item>Roll for loss (<see cref="DockExportsConfig.WEEKLY_LOSS_CHANCE"/> = 25% chance)</item>
        /// <item>If loss occurs, roll loss severity (<see cref="DockExportsConfig.LOSS_MIN_PERCENT"/> to <see cref="DockExportsConfig.LOSS_MAX_PERCENT"/> = 15-60%)</item>
        /// <item>Calculate actual payout (expected × (1 - lossPercent / 100))</item>
        /// <item>Update shipment (TotalPaid += actual, PaymentsMade++)</item>
        /// <item>If 4 payments complete, archive to history via <see cref="CompleteActiveShipment"/></item>
        /// </list>
        /// <para><strong>Loss Randomization:</strong></para>
        /// <para>
        /// Uses <c>UnityEngine.Random</c> for both loss occurrence roll and severity roll.
        /// Each Friday has independent 25% chance of loss. Loss severity is random between 15-60%.
        /// </para>
        /// <para><strong>Validation:</strong></para>
        /// <para>
        /// Returns 0 (no-op) if no active shipment or active shipment is not consignment.
        /// </para>
        /// <para><strong>Auto-Completion:</strong></para>
        /// <para>
        /// After the 4th payment, automatically calls <see cref="CompleteActiveShipment"/> to
        /// archive the shipment and clear active state.
        /// </para>
        /// </remarks>
        /// <example>
        /// Called by DockExportsMod every Friday:
        /// <code>
        /// int actualPayout = ShipmentManager.Instance.ProcessConsignmentPayment(out int lossPercent, out int floorTopUp);
        ///
        /// if (lossPercent > 0)
        /// {
        ///     // Loss occurred
        ///     string msg = BrokerMessages.GetRandomLossMessage(weekNum, lossPercent, actualPayout - floorTopUp, expectedPayout);
        ///     SendSMS("The Broker", msg);
        /// }
        /// else
        /// {
        ///     // No losses
        ///     string msg = BrokerMessages.WeekCleared(weekNum, actualPayout);
        ///     SendSMS("The Broker", msg);
        /// }
        ///
        /// AddMoneyToPlayer(actualPayout);
        /// </code>
        /// </example>
        public int ProcessConsignmentPayment(out int lossPercent, out int floorTopUp)
        {
            if (!_activeShipment.HasValue || _activeShipment.Value.Type != ShipmentType.Consignment)
            {
                lossPercent = 0;
                floorTopUp = 0;
                return 0;
            }

            var shipment = _activeShipment.Value;
            int expectedPayout = shipment.TotalValue / DockExportsConfig.CONSIGNMENT_INSTALLMENTS;

            // Roll for loss
            lossPercent = UnityEngine.Random.value < DockExportsConfig.WEEKLY_LOSS_CHANCE
                ? UnityEngine.Random.Range(DockExportsConfig.LOSS_MIN_PERCENT, DockExportsConfig.LOSS_MAX_PERCENT + 1)
                : 0;

            int actualPayout = (int)(expectedPayout * (1f - lossPercent / 100f));

            shipment.TotalPaid += actualPayout;
            shipment.PaymentsMade++;

            floorTopUp = 0;
            bool finalPayment = shipment.PaymentsMade >= DockExportsConfig.CONSIGNMENT_INSTALLMENTS;
            if (finalPayment)
            {
                int floorValue = PriceHelper.CalculateWholesaleFloor(shipment.Quantity, shipment.BaseUnitPrice);
                if (shipment.TotalPaid < floorValue)
                {
                    floorTopUp = floorValue - shipment.TotalPaid;
                    shipment.TotalPaid += floorTopUp;
                    actualPayout += floorTopUp;
                }
            }

            _activeShipment = shipment;

            if (finalPayment)
            {
                CompleteActiveShipment();
            }

            return actualPayout;
        }

        /// <summary>
        /// Completes the active shipment and archives it to history.
        /// </summary>
        /// <remarks>
        /// <para><strong>Completion Process:</strong></para>
        /// <list type="number">
        /// <item>Create <see cref="ShipmentHistoryEntry"/> from active shipment data</item>
        /// <item>Add entry to <see cref="_history"/> list</item>
        /// <item>Clear active shipment (_activeShipment = null)</item>
        /// </list>
        /// <para><strong>When Called:</strong></para>
        /// <list type="bullet">
        /// <item><see cref="ProcessWholesalePayment"/> - After instant payout</item>
        /// <item><see cref="ProcessConsignmentPayment"/> - After 4th weekly payment</item>
        /// </list>
        /// <para><strong>Validation:</strong></para>
        /// <para>
        /// Returns early (no-op) if no active shipment exists.
        /// </para>
        /// <para><strong>History Entry:</strong></para>
        /// <para>
        /// Includes timestamp (<c>DateTime.UtcNow</c>) for completed date, along with
        /// all shipment details (type, quantity, unit price, total value, total paid).
        /// </para>
        /// </remarks>
        private void CompleteActiveShipment()
        {
            if (!_activeShipment.HasValue)
                return;

            var shipment = _activeShipment.Value;
            _history.Add(new ShipmentHistoryEntry
            {
                Type = shipment.Type,
                Quantity = shipment.Quantity,
                UnitPrice = shipment.UnitPrice,
                BaseUnitPrice = shipment.BaseUnitPrice,
                TotalValue = shipment.TotalValue,
                TotalPaid = shipment.TotalPaid,
                CompletedDate = DateTime.UtcNow
            });

            _activeShipment = null;
        }

        #endregion

        private static int CalculateBasePrice(int unitPrice)
        {
            if (unitPrice <= 0)
                return 0;

            double multiplier = DockExportsConfig.CONSIGNMENT_MULTIPLIER;
            if (multiplier <= double.Epsilon)
                return unitPrice;

            return Math.Max(1, (int)Math.Round(unitPrice / multiplier));
        }

        #region Data Management

        /// <summary>
        /// Clears all shipment data (used for debugging/testing).
        /// </summary>
        /// <remarks>
        /// <para><strong>⚠️ Warning:</strong></para>
        /// <para>
        /// This method resets ALL shipment data to default state:
        /// </para>
        /// <list type="bullet">
        /// <item>Active shipment: null</item>
        /// <item>History: empty list</item>
        /// <item>Last processed day: -1</item>
        /// <item>Wholesale cooldown: -1 (no cooldown)</item>
        /// </list>
        /// <para>
        /// Intended for development/testing purposes. Be careful using this in production
        /// as it will erase all player progress.
        /// </para>
        /// <para><strong>Save Persistence:</strong></para>
        /// <para>
        /// Changes will persist to save file when the game saves (S1API handles this automatically).
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Reset all data for testing
        /// ShipmentManager.Instance.ClearAllData();
        /// MelonLogger.Msg("All shipment data cleared!");
        /// </code>
        /// </example>
        public void ClearAllData()
        {
            _activeShipment = null;
            _history.Clear();
            _lastProcessedDay = -1;
            _wholesaleCooldownEndDay = -1;
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Shipment type enum (Wholesale vs Consignment).
    /// </summary>
    /// <remarks>
    /// <para><strong>Wholesale:</strong></para>
    /// <list type="bullet">
    /// <item>Instant payout, no risk</item>
    /// <item>Max 100 bricks</item>
    /// <item>30-day cooldown</item>
    /// </list>
    /// <para><strong>Consignment:</strong></para>
    /// <list type="bullet">
    /// <item>1.6x price multiplier</item>
    /// <item>Max 200 bricks</item>
    /// <item>4 weekly payments</item>
    /// <item>25% weekly chance of 15-60% loss</item>
    /// </list>
    /// </remarks>
    public enum ShipmentType
    {
        /// <summary>
        /// Wholesale shipment (safe, instant, limited quantity, cooldown).
        /// </summary>
        Wholesale,

        /// <summary>
        /// Consignment shipment (risky, delayed, higher reward, larger quantity).
        /// </summary>
        Consignment
    }

    /// <summary>
    /// Active shipment state data structure.
    /// </summary>
    /// <remarks>
    /// <para><strong>Serialization:</strong></para>
    /// <para>
    /// Marked with <c>[Serializable]</c> so S1API can save/load it via <see cref="ShipmentManager._activeShipment"/>.
    /// </para>
    /// <para><strong>Nullable:</strong></para>
    /// <para>
    /// Stored as <c>ShipmentData?</c> (nullable struct) in ShipmentManager. Null means no active shipment.
    /// </para>
    /// <para><strong>Immutability:</strong></para>
    /// <para>
    /// Structs are value types. To modify a field, you must copy the struct, modify the copy, and reassign:
    /// </para>
    /// <code>
    /// var shipment = _activeShipment.Value;  // Copy
    /// shipment.TotalPaid += payout;           // Modify copy
    /// _activeShipment = shipment;             // Reassign
    /// </code>
    /// </remarks>
    [Serializable]
    public struct ShipmentData
    {
        /// <summary>
        /// Shipment type (Wholesale or Consignment).
        /// </summary>
        public ShipmentType Type;

        /// <summary>
        /// Number of bricks shipped.
        /// </summary>
        public int Quantity;

        /// <summary>
        /// Price per brick (wholesale: base price, consignment: enhanced price).
        /// </summary>
        public int UnitPrice;

        /// <summary>
        /// Original base price per brick (used for floor protection calculations).
        /// Matches <see cref="UnitPrice"/> for wholesale shipments and the pre-multiplier price for consignments.
        /// </summary>
        public int BaseUnitPrice;

        /// <summary>
        /// Total expected value of the shipment.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Wholesale: Quantity × UnitPrice<br/>
        /// Consignment: Quantity × (UnitPrice × 1.6)
        /// </para>
        /// <para>
        /// This is the theoretical maximum payout before losses.
        /// </para>
        /// </remarks>
        public int TotalValue;

        /// <summary>
        /// Total amount paid so far (accumulates with each payment).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Wholesale: Set to TotalValue on completion (instant full payout)<br/>
        /// Consignment: Increments each Friday with weekly payout (may be less than TotalValue due to losses)
        /// </para>
        /// </remarks>
        public int TotalPaid;

        /// <summary>
        /// Number of payments made so far.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Wholesale: 0 → 1 (complete)<br/>
        /// Consignment: 0 → 1 → 2 → 3 → 4 (complete)
        /// </para>
        /// </remarks>
        public int PaymentsMade;

        /// <summary>
        /// In-game day when the shipment was created.
        /// </summary>
        /// <remarks>
        /// Used for tracking shipment age and debugging. Not currently used in game logic.
        /// </remarks>
        public int CreatedDay;
    }

    /// <summary>
    /// Completed shipment history entry (archived when shipment finishes).
    /// </summary>
    /// <remarks>
    /// <para><strong>Serialization:</strong></para>
    /// <para>
    /// Marked with <c>[Serializable]</c> so S1API can save/load the history list.
    /// </para>
    /// <para><strong>Display:</strong></para>
    /// <para>
    /// Shown in the History tab of the phone app. <see cref="ToString"/> provides formatted output.
    /// </para>
    /// <para><strong>Loss Calculation:</strong></para>
    /// <para>
    /// <see cref="LossPercent"/> property calculates total loss percentage from TotalValue vs TotalPaid.
    /// </para>
    /// </remarks>
    [Serializable]
    public class ShipmentHistoryEntry
    {
        /// <summary>
        /// Shipment type (Wholesale or Consignment).
        /// </summary>
        public ShipmentType Type;

        /// <summary>
        /// Number of bricks shipped.
        /// </summary>
        public int Quantity;

        /// <summary>
        /// Price per brick at shipment creation.
        /// </summary>
        public int UnitPrice;

        /// <summary>
        /// Base price per brick (used for consignment floor comparisons).
        /// </summary>
        public int BaseUnitPrice;

        /// <summary>
        /// Total expected value (before losses).
        /// </summary>
        public int TotalValue;

        /// <summary>
        /// Total amount actually received (after losses).
        /// </summary>
        public int TotalPaid;

        /// <summary>
        /// UTC timestamp when shipment was completed.
        /// </summary>
        public DateTime CompletedDate;

        /// <summary>
        /// Calculated total loss percentage for this shipment.
        /// </summary>
        /// <value>
        /// Percentage loss (0-100), calculated as: 100 - ((TotalPaid / TotalValue) × 100)
        /// </value>
        /// <remarks>
        /// <para><strong>Examples:</strong></para>
        /// <list type="bullet">
        /// <item>TotalValue = $4,704,000, TotalPaid = $4,200,000 → 11% loss</item>
        /// <item>TotalValue = $1,470,000, TotalPaid = $1,470,000 → 0% loss (wholesale, no losses)</item>
        /// </list>
        /// <para><strong>Zero-Division Protection:</strong></para>
        /// <para>
        /// Returns 0 if TotalValue is 0 (should never happen in normal usage).
        /// </para>
        /// </remarks>
        public int LossPercent
        {
            get
            {
                if (TotalValue == 0) return 0;
                return 100 - (int)((TotalPaid / (float)TotalValue) * 100);
            }
        }

        /// <summary>
        /// Formats the history entry as a human-readable string.
        /// </summary>
        /// <returns>Multi-line formatted string for display in History tab</returns>
        /// <remarks>
        /// <para><strong>Format:</strong></para>
        /// <code>
        /// [MMM dd, yyyy] WHOLESALE/CONSIGNMENT
        ///   {Quantity} bricks → ${TotalPaid:N0} (X% loss) OR (no losses)
        /// </code>
        /// <para><strong>Examples:</strong></para>
        /// <code>
        /// [Jan 15, 2024] WHOLESALE
        ///   100 bricks → $1,470,000 (no losses)
        ///
        /// [Feb 12, 2024] CONSIGNMENT
        ///   200 bricks → $4,200,000 (11% loss)
        /// </code>
        /// </remarks>
        public override string ToString()
        {
            string typeStr = Type == ShipmentType.Wholesale ? "WHOLESALE" : "CONSIGNMENT";
            string lossStr = LossPercent > 0 ? $" ({LossPercent}% loss)" : " (no losses)";
            return $"[{CompletedDate:MMM dd, yyyy}] {typeStr}\n" +
                   $"  {Quantity} bricks → ${TotalPaid:N0}{lossStr}";
        }
    }

    #endregion
}

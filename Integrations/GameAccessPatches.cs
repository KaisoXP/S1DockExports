/// <summary>
/// Direct game system access workarounds for broken S1API managers.
/// </summary>
/// <remarks>
/// <para>
/// This file provides direct access to Schedule I game systems, bypassing S1API's manager
/// abstractions which are broken or incomplete in S1API v2.4.2 Forked.
/// </para>
/// <para><strong>Contents:</strong></para>
/// <list type="bullet">
/// <item><see cref="GameAccess"/> - Static utility class with direct game access methods</item>
/// </list>
/// <para><strong>Architecture Pattern:</strong> Direct NetworkSingleton Access</para>
/// <para>
/// Instead of using S1API managers (LevelManager, PropertyManager, TimeManager, etc.),
/// this implementation directly accesses the game's <c>NetworkSingleton&lt;T&gt;</c> instances.
/// </para>
/// <para><strong>Why This Exists:</strong></para>
/// <para>
/// S1API v2.4.2 Forked has several broken or unimplemented managers:
/// </para>
/// <list type="bullet">
/// <item><c>LevelManager.Rank</c> - TypeLoadException: Cannot find NetworkSingleton&lt;T&gt;</item>
/// <item><c>PropertyManager.FindPropertyByName()</c> - Same NetworkSingleton issue</item>
/// <item><c>TimeManager.CurrentDay/ElapsedDays</c> - Same NetworkSingleton issue</item>
/// <item><c>CallManager.QueueCall()</c> - TypeLoadException: ScheduleOne.Calling.CallManager not found</item>
/// </list>
/// <para>
/// However, the underlying game types <em>do exist</em> and are accessible via direct references
/// (proven by S1FuelMod, which successfully uses these types). S1API's abstractions fail due to
/// incorrect type resolution or missing Assembly-CSharp references.
/// </para>
/// <para><strong>Trade-offs:</strong></para>
/// <list type="bullet">
/// <item>✓ Works reliably - direct access to game types</item>
/// <item>✓ No dependency on S1API's broken managers</item>
/// <item>✗ Less portable - tightly coupled to game's internal structure</item>
/// <item>✗ May break if game updates change internal types</item>
/// </list>
/// </remarks>
using HarmonyLib;
using System;
using System.Collections;
using System.Reflection;
using MelonLoader;

#if IL2CPP
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Levelling;
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.Property;
using Il2CppScheduleOne.Money;
#else
using ScheduleOne.DevUtilities;
using ScheduleOne.Levelling;
using ScheduleOne.GameTime;
using ScheduleOne.Property;
using ScheduleOne.Money;
#endif

namespace S1DockExports.Integrations
{
    /// <summary>
    /// Direct game system access utilities (workaround for broken S1API managers).
    /// </summary>
    /// <remarks>
    /// <para><strong>Design Pattern: Static Utility Class</strong></para>
    /// <para>
    /// Provides static methods for accessing game systems that S1API v2.4.2 cannot access.
    /// All methods directly use <c>NetworkSingleton&lt;T&gt;.Instance</c> from the game's types.
    /// </para>
    /// <para><strong>S1API Issues Documented:</strong></para>
    /// <list type="number">
    /// <item><c>LevelManager.Rank</c> → TypeLoadException (NetworkSingleton not found)</item>
    /// <item><c>PropertyManager.FindPropertyByName()</c> → Not yet implemented in S1API</item>
    /// <item><c>TimeManager.CurrentDay/ElapsedDays</c> → TypeLoadException (NetworkSingleton not found)</item>
    /// <item><c>CallManager.QueueCall()</c> → TypeLoadException (ScheduleOne.Calling.CallManager not found)</item>
    /// </list>
    /// <para><strong>NetworkSingleton Pattern:</strong></para>
    /// <para>
    /// Schedule I uses <c>NetworkSingleton&lt;T&gt;</c> for many game systems. Access pattern:
    /// </para>
    /// <code>
    /// if (NetworkSingleton&lt;LevelManager&gt;.InstanceExists)
    /// {
    ///     var manager = NetworkSingleton&lt;LevelManager&gt;.Instance;
    ///     int rank = (int)manager.Rank;
    /// }
    /// </code>
    /// <para><strong>Error Handling:</strong></para>
    /// <para>
    /// All methods check <c>InstanceExists</c> before accessing <c>Instance</c> to prevent
    /// null reference exceptions. Returns safe defaults (0, false, default price) on failure.
    /// </para>
    /// </remarks>
    /// <example>
    /// Using GameAccess instead of S1API:
    /// <code>
    /// // S1API (broken):
    /// // int rank = (int)LevelManager.Rank; // TypeLoadException!
    ///
    /// // GameAccess (working):
    /// int rank = GameAccess.GetPlayerRank(); // Works!
    ///
    /// // S1API (broken):
    /// // int day = TimeManager.ElapsedDays; // TypeLoadException!
    ///
    /// // GameAccess (working):
    /// int day = GameAccess.GetElapsedDays(); // Works!
    /// </code>
    /// </example>
    public static class GameAccess
    {
        /// <summary>
        /// Gets the current player rank (0-10).
        /// </summary>
        /// <returns>Player rank as integer (0 = Street Rat, 10 = Kingpin), or 0 on failure</returns>
        /// <remarks>
        /// <para><strong>Replaces:</strong> <c>S1API.Leveling.LevelManager.Rank</c></para>
        /// <para><strong>S1API Issue:</strong> TypeLoadException - Cannot find NetworkSingleton&lt;T&gt;</para>
        /// <para><strong>Direct Access:</strong></para>
        /// <para>
        /// Directly accesses <c>NetworkSingleton&lt;LevelManager&gt;.Instance.Rank</c> from the game's types.
        /// </para>
        /// <para><strong>Rank Values:</strong></para>
        /// <list type="bullet">
        /// <item>0 = Street Rat</item>
        /// <item>1 = Hoodlum I, II, III</item>
        /// <item>2 = Peddler I, II, III</item>
        /// <item>3 = Hustler I, II, III (Hustler III = rank 13 total, required for this mod)</item>
        /// <item>4-10 = Higher ranks</item>
        /// </list>
        /// <para><strong>Error Handling:</strong></para>
        /// <para>
        /// Returns 0 if LevelManager doesn't exist or access fails. Safe default (lowest rank).
        /// </para>
        /// </remarks>
        /// <example>
        /// Checking unlock requirement:
        /// <code>
        /// int playerRank = GameAccess.GetPlayerRank();
        /// if (playerRank >= DockExportsConfig.REQUIRED_RANK_LEVEL)
        /// {
        ///     // Player meets rank requirement
        ///     UnlockBroker();
        /// }
        /// </code>
        /// </example>
        public static int GetPlayerRank()
        {
            try
            {
                if (!NetworkSingleton<LevelManager>.InstanceExists)
                    return 0;

                var levelManager = NetworkSingleton<LevelManager>.Instance;

                // Prefer explicit player level values
                int? level = TryGetInt(levelManager, "CurrentLevel", "Level", "PlayerLevel", "RankLevel", "TotalLevel");
                if (level.HasValue && level.Value > 0)
                    return level.Value;

                // Attempt parameterless getter methods (naming differs between builds)
                level = TryInvokeInt(levelManager, "GetCurrentLevel", "GetLevel", "GetPlayerLevel");
                if (level.HasValue && level.Value > 0)
                    return level.Value;

                // Fall back to enum-based rank (may be coarse-grained, but better than zero)
                level = TryGetInt(levelManager, "Rank");
                if (level.HasValue)
                    return level.Value;

                return 0;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[DockExports] Failed to get player rank: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Checks if the player owns a property by name.
        /// </summary>
        /// <param name="propertyName">Property name (e.g., "Docks Warehouse")</param>
        /// <returns><c>true</c> if the player owns the property; otherwise <c>false</c></returns>
        /// <remarks>
        /// <para><strong>Replaces:</strong> <c>S1API.Property.PropertyManager.FindPropertyByName().IsOwned</c></para>
        /// <para>
        /// Uses reflection to query the underlying PropertyManager singleton directly, ensuring compatibility
        /// even when S1API facades are out of sync with the current game build.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// bool ownsDocks = GameAccess.IsPropertyOwned("Docks Warehouse");
        /// if (ownsDocks)
        /// {
        ///     UnlockBroker();
        /// }
        /// </code>
        /// </example>
        public static bool IsPropertyOwned(string propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                return false;

            try
            {
                if (!NetworkSingleton<PropertyManager>.InstanceExists)
                {
                    MelonLogger.Warning("[DockExports] PropertyManager singleton missing; cannot verify ownership.");
                    return false;
                }

                var manager = NetworkSingleton<PropertyManager>.Instance;
                string normalized = propertyName.Trim();

                object? property = TryInvokePropertyLookup(manager, normalized)
                                   ?? FindPropertyByEnumeration(manager, normalized);

                if (property == null)
                {
                    MelonLogger.Warning($"[DockExports] Property '{normalized}' not found.");
                    return false;
                }

                bool? owned = TryGetBool(property, "IsOwned", "Owned", "IsPurchased", "OwnedByPlayer", "IsOwnedByPlayer");
                if (owned.HasValue)
                    return owned.Value;

                var ownerValue = GetMemberValue(property, "OwnerId") ?? GetMemberValue(property, "Owner");
                if (ownerValue != null)
                {
                    if (ownerValue is string ownerString)
                        return !string.IsNullOrEmpty(ownerString);
                    if (ownerValue is int ownerInt)
                        return ownerInt != 0;
                    if (ownerValue is Enum ownerEnum)
                        return Convert.ToInt32(ownerEnum) != 0;
                }

                MelonLogger.Warning($"[DockExports] Unable to determine ownership for property '{normalized}'. Assuming not owned.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[DockExports] Failed to check property ownership for '{propertyName}': {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Gets the current day of the week (0 = Monday, 4 = Friday, 6 = Sunday).
        /// </summary>
        /// <returns>Day of week (0-6), or 0 on failure</returns>
        /// <remarks>
        /// <para><strong>Replaces:</strong> <c>S1API.GameTime.TimeManager.CurrentDay</c></para>
        /// <para><strong>S1API Issue:</strong> TypeLoadException - Cannot find NetworkSingleton&lt;T&gt;</para>
        /// <para><strong>Direct Access:</strong></para>
        /// <para>
        /// Directly accesses <c>NetworkSingleton&lt;TimeManager&gt;.Instance.DayIndex</c> and calculates
        /// day of week using modulo 7.
        /// </para>
        /// <para><strong>Day Values:</strong></para>
        /// <list type="bullet">
        /// <item>0 = Monday</item>
        /// <item>1 = Tuesday</item>
        /// <item>2 = Wednesday</item>
        /// <item>3 = Thursday</item>
        /// <item>4 = Friday (when consignment payouts are processed)</item>
        /// <item>5 = Saturday</item>
        /// <item>6 = Sunday</item>
        /// </list>
        /// <para><strong>⚠️ Calculation Assumption:</strong></para>
        /// <para>
        /// Assumes Day 0 starts on Monday. If the game starts on a different day, this may be off.
        /// Requires testing to verify accuracy.
        /// </para>
        /// </remarks>
        /// <example>
        /// Checking if it's Friday:
        /// <code>
        /// int dayOfWeek = GameAccess.GetCurrentDayOfWeek();
        /// if (dayOfWeek == 4) // Friday
        /// {
        ///     ProcessConsignmentPayout();
        /// }
        /// </code>
        /// </example>
        public static int GetCurrentDayOfWeek()
        {
            try
            {
                if (NetworkSingleton<TimeManager>.InstanceExists)
                {
                    var timeManager = NetworkSingleton<TimeManager>.Instance;
                    // TODO: Find correct property - CurrentDayOfWeek doesn't exist
                    // For now, calculate from DayIndex: Day 0 = Monday, Day 4 = Friday, etc.
                    int dayOfWeek = timeManager.DayIndex % 7;
                    return dayOfWeek;
                }
                return 0;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[DockExports] Failed to get current day: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Gets the total number of elapsed days since game start.
        /// </summary>
        /// <returns>Elapsed days (0-based), or 0 on failure</returns>
        /// <remarks>
        /// <para><strong>Replaces:</strong> <c>S1API.GameTime.TimeManager.ElapsedDays</c></para>
        /// <para><strong>S1API Issue:</strong> TypeLoadException - Cannot find NetworkSingleton&lt;T&gt;</para>
        /// <para><strong>Direct Access:</strong></para>
        /// <para>
        /// Directly accesses <c>NetworkSingleton&lt;TimeManager&gt;.Instance.DayIndex</c>.
        /// </para>
        /// <para><strong>Usage:</strong></para>
        /// <para>
        /// Used for tracking cooldown timers (wholesale) and preventing double-processing of
        /// Friday payouts (via <see cref="ShipmentManager.LastProcessedDay"/>).
        /// </para>
        /// <para><strong>Example Values:</strong></para>
        /// <list type="bullet">
        /// <item>Day 0 = Game start</item>
        /// <item>Day 7 = One week later</item>
        /// <item>Day 30 = One month later</item>
        /// </list>
        /// </remarks>
        /// <example>
        /// Tracking last processed day:
        /// <code>
        /// int currentDay = GameAccess.GetElapsedDays();
        /// if (currentDay != ShipmentManager.Instance.LastProcessedDay)
        /// {
        ///     ProcessPayout();
        ///     ShipmentManager.Instance.LastProcessedDay = currentDay;
        /// }
        /// </code>
        /// </example>
        public static int GetElapsedDays()
        {
            try
            {
                if (NetworkSingleton<TimeManager>.InstanceExists)
                {
                    var timeManager = NetworkSingleton<TimeManager>.Instance;
                    return timeManager.DayIndex;
                }
                return 0;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[DockExports] Failed to get elapsed days: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Gets the current brick price from the market.
        /// </summary>
        /// <returns>Brick price in dollars (currently always <see cref="DockExportsConfig.DEFAULT_BRICK_PRICE"/>)</returns>
        /// <remarks>
        /// <para><strong>⚠️ NOT IMPLEMENTED:</strong></para>
        /// <para>
        /// Returns a hardcoded default price ($14,700 for 8-mix cocaine). Dynamic pricing
        /// based on the game's item system is not yet implemented.
        /// </para>
        /// <para><strong>TODO:</strong></para>
        /// <para>
        /// Find the correct product/item manager API in Assembly-CSharp to get real-time brick prices.
        /// Likely involves accessing an ItemManager or ProductManager singleton.
        /// </para>
        /// <para><strong>Current Behavior:</strong></para>
        /// <para>
        /// Always returns <see cref="DockExportsConfig.DEFAULT_BRICK_PRICE"/> (14,700).
        /// </para>
        /// </remarks>
        /// <example>
        /// Getting brick price for shipment:
        /// <code>
        /// int brickPrice = GameAccess.GetBrickPrice(); // Always 14,700
        /// int totalValue = quantity * brickPrice;
        /// </code>
        /// </example>
        public static int GetBrickPrice()
        {
            // TODO: Find correct product manager API
            // For now, return configured default
            return DockExportsConfig.DEFAULT_BRICK_PRICE;
        }

        #region Reflection helpers

        private static int? TryInvokeInt(object instance, params string[] methodNames)
        {
            foreach (var name in methodNames)
            {
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                try
                {
                    var method = instance.GetType().GetMethod(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase, Type.DefaultBinder, Type.EmptyTypes, null);
                    if (method == null)
                        continue;

                    var result = method.Invoke(instance, Array.Empty<object>());
                    var converted = ConvertToNullableInt(result);
                    if (converted.HasValue)
                        return converted;
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning($"[DockExports] Failed invoking '{name}': {ex.Message}");
                }
            }

            return null;
        }

        private static int? TryGetInt(object instance, params string[] memberNames)
        {
            foreach (var member in memberNames)
            {
                if (string.IsNullOrWhiteSpace(member))
                    continue;

                var value = GetMemberValue(instance, member);
                var converted = ConvertToNullableInt(value);
                if (converted.HasValue)
                    return converted;
            }
            return null;
        }

        private static bool? TryGetBool(object instance, params string[] memberNames)
        {
            foreach (var member in memberNames)
            {
                if (string.IsNullOrWhiteSpace(member))
                    continue;

                var value = GetMemberValue(instance, member);
                var converted = ConvertToNullableBool(value);
                if (converted.HasValue)
                    return converted;
            }
            return null;
        }

        private static string? TryGetString(object instance, params string[] memberNames)
        {
            foreach (var member in memberNames)
            {
                if (string.IsNullOrWhiteSpace(member))
                    continue;

                var value = GetMemberValue(instance, member);
                if (value != null)
                    return value.ToString();
            }
            return null;
        }

        private static object? GetMemberValue(object instance, string memberName)
        {
            if (instance == null || string.IsNullOrWhiteSpace(memberName))
                return null;

            try
            {
                var type = instance.GetType();

                var property = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (property != null)
                    return property.GetValue(instance);

                var field = type.GetField(memberName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (field != null)
                    return field.GetValue(instance);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[DockExports] Reflection read failed for '{memberName}': {ex.Message}");
            }

            return null;
        }

        private static int? ConvertToNullableInt(object? value)
        {
            if (value == null)
                return null;

            switch (value)
            {
                case int i:
                    return i;
                case long l:
                    return (int)l;
                case short s:
                    return s;
                case byte b:
                    return b;
                case float f:
                    return (int)f;
                case double d:
                    return (int)d;
                case Enum e:
                    return Convert.ToInt32(e);
            }

            if (int.TryParse(value.ToString(), out int parsed))
                return parsed;

            return null;
        }

        private static bool? ConvertToNullableBool(object? value)
        {
            if (value == null)
                return null;

            switch (value)
            {
                case bool b:
                    return b;
                case int i:
                    return i != 0;
                case long l:
                    return l != 0;
                case Enum e:
                    return Convert.ToInt32(e) != 0;
            }

            if (bool.TryParse(value.ToString(), out bool parsed))
                return parsed;

            return null;
        }

        private static object? TryInvokePropertyLookup(object manager, string propertyName)
        {
            var lookup = manager.GetType().GetMethod("FindPropertyByName", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                         ?? manager.GetType().GetMethod("GetPropertyByName", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (lookup == null)
                return null;

            try
            {
                return lookup.Invoke(manager, new object[] { propertyName });
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[DockExports] Property lookup for '{propertyName}' failed: {ex.Message}");
                return null;
            }
        }

        private static object? FindPropertyByEnumeration(object manager, string propertyName)
        {
            var collection = GetMemberValue(manager, "AllProperties")
                           ?? GetMemberValue(manager, "Properties")
                           ?? GetMemberValue(manager, "PropertyDefinitions");

            foreach (var candidate in EnumerateCollection(collection))
            {
                string? name = TryGetString(candidate, "PropertyName", "Name", "DisplayName", "Title", "InternalName");
                if (string.IsNullOrEmpty(name))
                    continue;

                if (string.Equals(name, propertyName, StringComparison.OrdinalIgnoreCase))
                    return candidate;
            }

            return null;
        }

        private static IEnumerable EnumerateCollection(object? collection)
        {
            if (collection == null)
                yield break;

            if (collection is IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                    if (item != null)
                        yield return item;
                yield break;
            }

            var type = collection.GetType();
            var getEnumerator = type.GetMethod("GetEnumerator", BindingFlags.Public | BindingFlags.Instance);
            if (getEnumerator == null)
                yield break;

            var enumerator = getEnumerator.Invoke(collection, Array.Empty<object>());
            if (enumerator == null)
                yield break;

            var enumeratorType = enumerator.GetType();
            var moveNext = enumeratorType.GetMethod("MoveNext", BindingFlags.Public | BindingFlags.Instance);
            var currentProperty = enumeratorType.GetProperty("Current", BindingFlags.Public | BindingFlags.Instance);
            if (moveNext == null || currentProperty == null)
                yield break;

            while (moveNext.Invoke(enumerator, Array.Empty<object>()) is bool hasNext && hasNext)
            {
                var current = currentProperty.GetValue(enumerator);
                if (current != null)
                    yield return current;
            }
        }

        #endregion
    }
}

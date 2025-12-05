/// <summary>
/// Direct game system access workarounds for broken S1API managers.
/// </summary>
using HarmonyLib;
using System;
using MelonLoader;
using S1API.Property;

#if IL2CPP
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Levelling;
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.Money;
#else
using ScheduleOne.DevUtilities;
using ScheduleOne.Levelling;
using ScheduleOne.GameTime;
using ScheduleOne.Money;
#endif

namespace S1DockExports.Integrations
{
    /// <summary>
    /// Direct game system access utilities (workaround for broken S1API managers).
    /// </summary>
    public static class GameAccess
    {
        /// <summary>
        /// Gets the current player rank (0-10).
        /// </summary>
        public static int GetPlayerRank()
        {
            if (NetworkSingleton<LevelManager>.InstanceExists)
            {
                var levelManager = NetworkSingleton<LevelManager>.Instance;
                return (int)levelManager.Rank;
            }
            return 0;
        }

        /// <summary>
        /// Checks if the player owns a property by name.
        /// </summary>
        public static bool IsPropertyOwned(string propertyName)
        {
            var business = BusinessManager.FindBusinessByName(propertyName);
            return business?.IsOwned ?? false;
        }

        /// <summary>
        /// Gets the current day of the week (0 = Monday, 4 = Friday, 6 = Sunday).
        /// </summary>
        public static int GetCurrentDayOfWeek()
        {
            if (NetworkSingleton<TimeManager>.InstanceExists)
            {
                var timeManager = NetworkSingleton<TimeManager>.Instance;
                return timeManager.DayIndex % 7;
            }
            return 0;
        }

        /// <summary>
        /// Gets the total number of elapsed days since game start.
        /// </summary>
        public static int GetElapsedDays()
        {
            if (NetworkSingleton<TimeManager>.InstanceExists)
            {
                var timeManager = NetworkSingleton<TimeManager>.Instance;
                return timeManager.DayIndex;
            }
            return 0;
        }

        /// <summary>
        /// Gets the current brick price from the market. Returns hardcoded default for now.
        /// </summary>
        public static int GetBrickPrice()
        {
            // TODO: Find correct product manager API
            // For now, return configured default
            return DockExportsConfig.DEFAULT_BRICK_PRICE;
        }
    }
}

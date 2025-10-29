#if CROSS_COMPAT
namespace S1DockExports.Integrations
{
    /// <summary>
    /// Minimal no-op implementation used for the CrossCompat configuration so the project can build
    /// without referencing Schedule I game assemblies.
    /// </summary>
    public static class GameAccess
    {
        public static int GetPlayerRank() => 0;
        public static bool IsPropertyOwned(string propertyName) => false;
        public static int GetCurrentDayOfWeek() => 0;
        public static int GetElapsedDays() => 0;
        public static int GetBrickPrice() => DockExportsConfig.DEFAULT_BRICK_PRICE;
    }
}
#endif

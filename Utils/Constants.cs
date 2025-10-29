namespace S1DockExports.Utils
{
    public static class Constants
    {
        /// <summary>
        /// Mod information
        /// </summary>
        public const string MOD_NAME = "S1DockExports";
        public const string MOD_VERSION = "0.0.1";
        public const string MOD_AUTHOR = "KaisoXP";
        public const string MOD_DESCRIPTION = "We Goin' Global...";

        /// <summary>
        /// MelonPreferences configuration
        /// </summary>
        public const string PREFERENCES_CATEGORY = MOD_NAME;

        /// <summary>
        /// Default preference values
        /// </summary>
        public static class Defaults
        {
            public const bool BOOLEAN_DEFAULT = false;
        }

        /// <summary>
        /// Preference value constraints
        /// </summary>
        public static class Constraints
        {
            public const float MIN_CONSTRAINT = 0f;
            public const float MAX_CONSTRAINT = 100f;
        }

        /// <summary>
        /// Game-related constants
        /// </summary>
        public static class Game
        {
            public const string GAME_STUDIO = "TVGS";
            public const string GAME_NAME = "Schedule I";
        }

    }
}

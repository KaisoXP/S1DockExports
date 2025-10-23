#nullable enable
using System;
using System.IO;
using System.Reflection;
using MelonLoader;

[assembly: MelonInfo(typeof(S1DockExports.Core), "S1DockExports", "0.0.1", "KaisoXP")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace S1DockExports
{
    public sealed class Core : MelonMod
    {
        public override void OnInitializeMelon()
        {
            // Ensure missing runtime dependencies (for example FishNet) resolve from Il2CppAssemblies.
            AppDomain.CurrentDomain.AssemblyResolve += ResolveFromIl2CppAssemblies;

            MelonLogger.Msg("S1DockExports initialized.");
        }

        private Assembly? ResolveFromIl2CppAssemblies(object? sender, ResolveEventArgs args)
        {
            // Determine the simple assembly name.
            var requestedName = new AssemblyName(args.Name).Name ?? string.Empty;

            // Map requested assembly name to the actual Il2Cpp file shipped with the game.
            string? fileName = requestedName switch
            {
                "FishNet.Runtime" => "Il2CppFishNet.Runtime.dll",
                // Uncomment if you later rely on Steamworks.NET types that S1API uses at runtime.
                // "com.rlabrecque.steamworks.net" => "Il2Cppcom.rlabrecque.steamworks.net.dll",
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
                MelonLogger.Warning($"Failed to load '{fileName}' from Il2CppAssemblies: {ex.Message}");
                return null;
            }
        }
    }
}

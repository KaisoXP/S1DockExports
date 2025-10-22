using System;
using System.IO;
using System.Reflection;
using MelonLoader;

[assembly: MelonInfo(typeof(S1DockExports.Core), "S1DockExports", "0.0.1", "KaisoXP")]
[assembly: MelonGame(null, "Schedule I")]

namespace S1DockExports
{
    public sealed class Core : MelonMod
    {
        public override void OnInitializeMelon()
        {
            // Ensure any missing runtime deps (like FishNet) resolve from Il2CppAssemblies
            AppDomain.CurrentDomain.AssemblyResolve += ResolveFromIl2CppAssemblies;

            MelonLogger.Msg("S1DockExports initialized.");
        }

        private Assembly? ResolveFromIl2CppAssemblies(object? sender, ResolveEventArgs args)
        {
            // Map requested name -> exact Il2Cpp filename(s) that exist in the game
            string name = new AssemblyName(args.Name).Name ?? string.Empty;

            // Only intercept FishNet (and Steamworks if needed in future)
            string? file = name switch
            {
                "FishNet.Runtime" => "Il2CppFishNet.Runtime.dll",
                // "com.rlabrecque.steamworks.net" => "Il2Cppcom.rlabrecque.steamworks.net.dll",
                _ => null
            };
            if (file == null) return null;

            // Build the path to MelonLoader\Il2CppAssemblies
            // (Use the same path you reference in your .csproj)
            var gameDir = MelonEnvironment.GameRootDirectory; // e.g., ...\Schedule I
            var probe = Path.Combine(gameDir, "MelonLoader", "Il2CppAssemblies", file);
            if (File.Exists(probe))
            {
                try { return Assembly.LoadFrom(probe); }
                catch (Exception ex)
                {
                    MelonLogger.Warning($"Failed to load '{file}' from Il2CppAssemblies: {ex.Message}");
                }
            }
            return null; // let default probing continue
        }
    }
}

# Repository Guidelines

## Project Structure & Module Organization
- Core mod coordination lives in `DockExportsMod.cs`, with business logic in `Services/ShipmentManager.cs` and player-facing UI in `DockExportsApp.cs`.
- Harmony and S1API integrations are isolated under `Integrations/` (`GameAccessPatches.cs`, `PhoneAppInjector.cs`, `HarmonyPatches.cs`) so new hooks stay contained.
- Shared constants and config defaults belong in `Utils/Constants.cs` and `DockExportsConfig.cs`. Keep assets such as `DE.png` alongside the project root to satisfy the `.csproj` embedded resource.
- Reference tutorials and technical notes in `Tutorials/` before introducing new subsystems; many patterns (save data, Harmony patching, project layout) are already documented there.

## Build, Test, and Development Commands
- `dotnet build -c Il2cpp` — primary target that compiles against live game assemblies, runs `VerifyGameFiles`, and drops `S1DockExports_Il2cpp.dll` into the configured `Mods` directory.
- `dotnet build -c CrossCompat` — lighter compile for API-only validation when you do not have the game folder mounted.
- `dotnet clean` — clear intermediate output; use before switching MelonLoader or game versions.
- Set `S1_GAME="D:\SteamLibrary\steamapps\common\Schedule I"` (or your path) to override the default game location before building.

## Coding Style & Naming Conventions
- Follow existing C# conventions: four-space indent, XML documentation (`///`) for public APIs, `PascalCase` for types/methods, `camelCase` for locals/fields, and `ALL_CAPS` only for constants defined in `Constants.cs`.
- Prefer explicit types when clarity matters (e.g., `MelonMod` instances) and use `var` only for obvious assignments.
- Keep Harmony patch classes partial-free and co-locate helper structs with their owning feature to avoid namespace sprawl.

## Testing Guidelines
- No automated test harness exists; every change must be validated in-game. After an `Il2cpp` build, launch Schedule I and exercise both Wholesale and Consignment flows, checking Broker unlock messaging, payouts, and loss recovery.
- Use `MelonLogger.Msg` for temporary diagnostics and remove or downgrade to verbose logging before opening a PR.
- When touching save data, verify load/save cycles by exiting to main menu and reloading a profile at least once.

## Commit & Pull Request Guidelines
- Mirror the repository’s short, action-oriented commit format (`verb change`, e.g., `add shipment cooldown logging`). Squash fixups locally before opening a PR.
- PRs must describe gameplay impact, manual test notes, and list any new configuration toggles. Link relevant S1API or Harmony issues when applicable.
- Include screenshots or short clips for UI, dialogue, or phone app updates so reviewers can validate presentation without rebuilding.

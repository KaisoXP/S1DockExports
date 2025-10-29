# Understanding the .csproj Build File

> **A Complete Line-by-Line Guide to S1DockExports.csproj**

**Author:** Claude Code + KaisoXP
**Date:** October 25, 2025
**Project:** S1DockExports
**Audience:** Developers who want to understand the build system

---

## Table of Contents

1. [What is a .csproj File?](#what-is-a-csproj-file)
2. [Why Two Configurations?](#why-two-configurations)
3. [File Structure Overview](#file-structure-overview)
4. [Section-by-Section Breakdown](#section-by-section-breakdown)
5. [Assembly References Explained](#assembly-references-explained)
6. [Build Targets Deep Dive](#build-targets-deep-dive)
7. [Troubleshooting Build Issues](#troubleshooting-build-issues)
8. [Customizing for Your Project](#customizing-for-your-project)

---

## What is a .csproj File?

### The Simple Explanation

A `.csproj` file is like a **recipe for building your mod**. It tells the build system (`dotnet build`):
- What C# files to compile
- What libraries (DLLs) your code needs
- How to package the output
- What to do after building (copy files, launch game, etc.)

**Analogy:** Think of it like a restaurant recipe:
- **Ingredients** = Assembly references (DLLs)
- **Instructions** = Build targets (compile, copy, launch)
- **Variations** = Configurations (Il2cpp vs CrossCompat)

### XML Format

`.csproj` files use XML (eXtensible Markup Language):

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
  </PropertyGroup>
</Project>
```

**Structure:**
- `<TagName>` = Opening tag
- `</TagName>` = Closing tag
- `<TagName>Value</TagName>` = Element with value
- `<TagName Attribute="Value">` = Element with attribute
- `<!-- Comment -->` = Comments (ignored by build system)

---

## Why Two Configurations?

S1DockExports has **two build configurations**: `Il2cpp` and `CrossCompat`.

### Il2cpp Configuration (REQUIRED)

**What it is:** References the game's actual code (`Assembly-CSharp.dll`).

**When to use:** **ALWAYS** for S1DockExports (and most "Schedule I" mods).

**Why:**
- S1API managers (TimeManager, LevelManager, etc.) need game types
- Direct game access requires Assembly-CSharp
- Your mod interacts deeply with game systems

**Output:** `S1DockExports_Il2cpp.dll`

**Build command:**
```bash
dotnet build -c Il2cpp
```

### CrossCompat Configuration (NOT USED)

**What it is:** References only Unity/MelonLoader assemblies, NOT the game's code.

**When to use:** Simple mods that don't need game-specific types.

**Why it exists:**
- Avoids shipping game code (legal concerns)
- Works across multiple games
- Example: A generic FPS counter mod

**Output:** `S1DockExports.dll`

**⚠️ WARNING:** S1DockExports will **crash** if built with CrossCompat!

```
TypeLoadException: Could not load type 'ScheduleOne.DevUtilities.NetworkSingleton`1'
```

This is because `S1API.Leveling.LevelManager` expects `Assembly-CSharp` to be loaded.

---

## File Structure Overview

The `.csproj` file has this structure:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <!-- 1. Embedded Resources (icon) -->
  <ItemGroup>
    <EmbeddedResource Include="DE.png" />
  </ItemGroup>

  <!-- 2. Project-Wide Settings -->
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <RootNamespace>S1DockExports</RootNamespace>
    <!-- ... more settings ... -->
  </PropertyGroup>

  <!-- 3. Shared Path Definitions -->
  <PropertyGroup>
    <Il2CppAssembliesPath>$(GamePath)\MelonLoader\Il2CppAssemblies</Il2CppAssembliesPath>
    <!-- ... more paths ... -->
  </PropertyGroup>

  <!-- 4. CrossCompat Configuration -->
  <PropertyGroup Condition="'$(Configuration)'=='CrossCompat'">
    <!-- CrossCompat-specific settings -->
  </PropertyGroup>
  <ItemGroup Condition="'$(Configuration)'=='CrossCompat'">
    <!-- CrossCompat assembly references -->
  </ItemGroup>

  <!-- 5. Il2cpp Configuration -->
  <PropertyGroup Condition="'$(Configuration)'=='Il2cpp'">
    <!-- Il2cpp-specific settings -->
  </PropertyGroup>
  <ItemGroup Condition="'$(Configuration)'=='Il2cpp'">
    <!-- Il2cpp assembly references -->
  </ItemGroup>

  <!-- 6. S1API Reference (shared by both configs) -->
  <ItemGroup>
    <Reference Include="S1API">
      <HintPath>$(ModsPath)\S1API.dll</HintPath>
    </Reference>
  </ItemGroup>

  <!-- 7. Custom Build Targets -->
  <Target Name="VerifyGameFiles" BeforeTargets="Build">
    <!-- Verify paths exist before building -->
  </Target>

  <Target Name="CopyToMods_Whitelist" AfterTargets="Build">
    <!-- Copy mod DLL to game's Mods folder -->
  </Target>

  <Target Name="RelaunchGame" AfterTargets="CopyToMods_Whitelist">
    <!-- Kill and restart the game -->
  </Target>

</Project>
```

---

## Section-by-Section Breakdown

### Section 1: Embedded Resources

```xml
<ItemGroup>
  <EmbeddedResource Include="DE.png">
    <!-- Build action: Embedded Resource -->
  </EmbeddedResource>
</ItemGroup>
```

**What it does:**
- Embeds `DE.png` directly into the compiled DLL
- The image becomes part of your mod's assembly
- Accessible via `Assembly.GetManifestResourceStream("S1DockExports.DE.png")`

**Why:**
- No external files needed (just one DLL)
- Icon can't get "lost" or separated from mod
- Easier distribution

**How to add more:**
```xml
<EmbeddedResource Include="Assets/BrokerPortrait.png" />
<EmbeddedResource Include="Sounds/notification.wav" />
```

### Section 2: Project-Wide Settings

```xml
<PropertyGroup>
  <TargetFramework>netstandard2.1</TargetFramework>
  <ImplicitUsings>enable</ImplicitUsings>
  <RootNamespace>S1DockExports</RootNamespace>
  <LangVersion>default</LangVersion>
  <NeutralLanguage>en-US</NeutralLanguage>
  <Configurations>Il2cpp;CrossCompat</Configurations>
  <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
  <EnableDefaultCompileItems>true</EnableDefaultCompileItems>

  <!-- Per-machine override: $env:S1_GAME="D:\SteamLibrary\..." -->
  <GamePath Condition="'$(GamePath)'=='' and '$(S1_GAME)'!=''">$(S1_GAME)</GamePath>
  <!-- Default path (edit if different) -->
  <GamePath Condition="'$(GamePath)'==''">C:\Program Files (x86)\Steam\steamapps\common\Schedule I</GamePath>
</PropertyGroup>
```

#### Line-by-Line Explanation

**`<TargetFramework>netstandard2.1</TargetFramework>`**
- Targets .NET Standard 2.1 (compatible with Unity's scripting backend)
- Unity uses .NET Standard, not .NET Core or .NET Framework
- Must match what the game expects

**`<ImplicitUsings>enable</ImplicitUsings>`**
- Automatically adds common `using` statements
- Example: `System`, `System.Collections.Generic`, `System.Linq`
- You don't need to write `using System;` at the top of every file

**`<RootNamespace>S1DockExports</RootNamespace>`**
- Default namespace for new files
- When you create `NewClass.cs`, it starts in `namespace S1DockExports { }`

**`<LangVersion>default</LangVersion>`**
- Uses the default C# language version for the target framework
- For .NET Standard 2.1, this is C# 8.0

**`<NeutralLanguage>en-US</NeutralLanguage>`**
- Default language for resources and messages
- Affects localization (not used in this mod)

**`<Configurations>Il2cpp;CrossCompat</Configurations>`**
- Defines available build configurations
- Semicolon-separated list
- Enables switching with `-c` flag: `dotnet build -c Il2cpp`

**`<AllowUnsafeBlocks>true</AllowUnsafeBlocks>`**
- Allows `unsafe` code (pointers, unmanaged memory)
- Needed for some Il2Cpp interop scenarios
- Not currently used in S1DockExports, but safe to enable

**`<EnableDefaultCompileItems>true</EnableDefaultCompileItems>`**
- Automatically includes all `.cs` files in project
- You don't need to manually list every source file
- Any `.cs` file in the project folder is compiled

**`<GamePath>` (3 lines)**

This clever setup allows overriding the game path:

1. **Environment variable:** `$env:S1_GAME="D:\SteamLibrary\..."`
   - If set, uses this path
   - Great for developers with non-default Steam location

2. **Default fallback:**
   - If no environment variable, uses `C:\Program Files (x86)\Steam\...`
   - Most users have default Steam location

**Usage:**
```powershell
# PowerShell - set for this session
$env:S1_GAME="D:\Games\Steam\steamapps\common\Schedule I"

# Permanently set (System Properties → Environment Variables)
# Name: S1_GAME
# Value: D:\Games\Steam\steamapps\common\Schedule I
```

### Section 3: Shared Path Definitions

```xml
<PropertyGroup>
  <Il2CppAssembliesPath>$(GamePath)\MelonLoader\Il2CppAssemblies</Il2CppAssembliesPath>
  <MelonLoaderNet35>$(GamePath)\MelonLoader\net35</MelonLoaderNet35>
  <MelonLoaderNet6>$(GamePath)\MelonLoader\net6</MelonLoaderNet6>
  <ModsPath>$(GamePath)\Mods</ModsPath>
</PropertyGroup>
```

**What it does:**
- Defines reusable path variables
- `$(Variable)` syntax allows referencing: `$(Il2CppAssembliesPath)\UnityEngine.CoreModule.dll`

**Paths explained:**

| Variable | Path | Contains |
|----------|------|----------|
| `Il2CppAssembliesPath` | `GamePath\MelonLoader\Il2CppAssemblies` | Game's Unity DLLs, Assembly-CSharp.dll |
| `MelonLoaderNet35` | `GamePath\MelonLoader\net35` | MelonLoader.dll, 0Harmony.dll |
| `MelonLoaderNet6` | `GamePath\MelonLoader\net6` | Il2CppInterop.Runtime.dll |
| `ModsPath` | `GamePath\Mods` | Where compiled mods go |

**Why this approach:**
- Change `GamePath` once → all paths update
- Easier to read than full paths everywhere
- Consistent across the file

### Section 4: CrossCompat Configuration

```xml
<PropertyGroup Condition="'$(Configuration)'=='CrossCompat'">
  <DefineConstants>CROSS_COMPAT</DefineConstants>
  <AssemblyName>S1DockExports</AssemblyName>
</PropertyGroup>
```

**`Condition="'$(Configuration)'=='CrossCompat'"`**
- Only applies when building with `dotnet build -c CrossCompat`
- Think of it like an `if` statement

**`<DefineConstants>CROSS_COMPAT</DefineConstants>`**
- Defines the `CROSS_COMPAT` preprocessor symbol
- Allows conditional compilation in C# code:

```csharp
#if CROSS_COMPAT
    // CrossCompat-specific code
#else
    // Il2cpp-specific code
#endif
```

**`<AssemblyName>S1DockExports</AssemblyName>`**
- Output DLL name: `S1DockExports.dll`

**Assembly References (CrossCompat):**

```xml
<ItemGroup Condition="'$(Configuration)'=='CrossCompat'">
  <!-- Unity + TextMeshPro (from Il2CppAssemblies) -->
  <Reference Include="UnityEngine.CoreModule">
    <HintPath>$(Il2CppAssembliesPath)\UnityEngine.CoreModule.dll</HintPath>
  </Reference>
  <!-- ... more Unity DLLs ... -->

  <!-- MelonLoader compile-time stubs -->
  <Reference Include="MelonLoader">
    <HintPath>$(MelonLoaderNet35)\MelonLoader.dll</HintPath>
  </Reference>
  <!-- ... more MelonLoader DLLs ... -->
</ItemGroup>
```

**Pattern:**
```xml
<Reference Include="DllName">
  <HintPath>Full\Path\To\DllName.dll</HintPath>
</Reference>
```

- `Include="DllName"` - Name used in code (`using UnityEngine;`)
- `<HintPath>` - Where to find the DLL at compile time

**Note:** CrossCompat references Unity DLLs but **NOT** `Assembly-CSharp.dll`.

### Section 5: Il2cpp Configuration (CRITICAL)

```xml
<PropertyGroup Condition="'$(Configuration)'=='Il2cpp'">
  <DefineConstants>IL2CPP</DefineConstants>
  <AssemblyName>S1DockExports_Il2cpp</AssemblyName>
</PropertyGroup>
```

**`<DefineConstants>IL2CPP</DefineConstants>`**
- Defines `IL2CPP` preprocessor symbol
- Allows conditional compilation:

```csharp
#if IL2CPP
using Il2CppScheduleOne;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
#else
using ScheduleOne;
#endif
```

**`<AssemblyName>S1DockExports_Il2cpp</AssemblyName>`**
- Output DLL name: `S1DockExports_Il2cpp.dll`
- Different name than CrossCompat (prevents confusion)

**Assembly References (Il2cpp):**

```xml
<ItemGroup Condition="'$(Configuration)'=='Il2cpp'">
  <Reference Include="Assembly-CSharp">
    <HintPath>$(Il2CppAssembliesPath)\Assembly-CSharp.dll</HintPath>
  </Reference>
  <!-- ... Unity DLLs ... -->
  <Reference Include="UnityEngine.ImageConversionModule">
    <HintPath>$(Il2CppAssembliesPath)\UnityEngine.ImageConversionModule.dll</HintPath>
  </Reference>
  <!-- ... more DLLs ... -->
</ItemGroup>
```

**Key difference:** References `Assembly-CSharp.dll` (the game's code).

**Why `ImageConversionModule` is here:**
- Only Il2cpp config needs it (for loading embedded icons)
- Contains `ImageConversion.LoadImage()` method
- Required for `PhoneAppInjector.cs:LoadIconSprite()`

### Section 6: S1API Reference (Shared)

```xml
<ItemGroup>
  <Reference Include="S1API">
    <HintPath>$(ModsPath)\S1API.dll</HintPath>
  </Reference>
</ItemGroup>
```

**Why not conditional?**
- Both configurations need S1API
- No `Condition=` means "always include"

**Why reference from ModsPath?**
- S1API is already in the game's `Mods/` folder
- Ensures we use the same version the game loads
- Avoids version mismatches

### Section 7: Build Verification Target

```xml
<Target Name="VerifyGameFiles" BeforeTargets="Build">
  <Error Condition="!Exists('$(Il2CppAssembliesPath)\UnityEngine.CoreModule.dll')"
         Text="UnityEngine.CoreModule.dll not found at '$(Il2CppAssembliesPath)'. Check GamePath." />
  <Error Condition="!Exists('$(MelonLoaderNet35)\MelonLoader.dll')"
         Text="MelonLoader.dll not found in net35. Check GamePath or MelonLoader install." />
  <Error Condition="!Exists('$(MelonLoaderNet6)\Il2CppInterop.Runtime.dll')"
         Text="Il2CppInterop.Runtime.dll not found in net6. Check GamePath or MelonLoader install." />
</Target>
```

**What it does:**
- Runs **before** the build starts (`BeforeTargets="Build"`)
- Checks if required DLLs exist
- **Stops the build immediately** if any are missing
- Shows a helpful error message

**Why this is awesome:**
- **Fail fast** - No cryptic errors 10 seconds into the build
- **Clear error messages** - "Check GamePath" tells you exactly what's wrong
- **Saves time** - Doesn't waste time compiling before failing

**Example error:**
```
error : UnityEngine.CoreModule.dll not found at 'C:\Wrong\Path\MelonLoader\Il2CppAssemblies'.
Check GamePath.
```

Immediately you know: "Oh, my `GamePath` is wrong!"

### Section 8: File Copy Target

```xml
<Target Name="CopyToMods_Whitelist" AfterTargets="Build">
  <ItemGroup>
    <!-- Your mod -->
    <_Ship Include="$(TargetPath)" />
    <!-- S1API is already in Mods folder, don't copy it -->
    <!-- FishNet runtime (only this one Il2Cpp DLL, to satisfy S1API's probe) -->
    <_Ship Include="$(Il2CppAssembliesPath)\Il2CppFishNet.Runtime.dll"
           Condition="Exists('$(Il2CppAssembliesPath)\Il2CppFishNet.Runtime.dll')" />
  </ItemGroup>
  <Copy SourceFiles="@(_Ship)" DestinationFolder="$(ModsPath)" SkipUnchangedFiles="true" />
</Target>
```

**What it does:**
- Runs **after** the build completes (`AfterTargets="Build"`)
- Copies specific files to the game's `Mods/` folder
- Only copies **if the file changed** (`SkipUnchangedFiles="true"`)

**Files copied:**

1. **`$(TargetPath)`** - Your compiled mod DLL
   - `S1DockExports_Il2cpp.dll` (for Il2cpp config)
   - `S1DockExports.dll` (for CrossCompat config)

2. **`Il2CppFishNet.Runtime.dll`** - Required by S1API
   - S1API needs FishNet for networking
   - The game already has FishNet, but S1API probes for this specific DLL

**What's NOT copied:**

- **S1API.dll** - Already in `Mods/` folder, don't overwrite it
- **Unity DLLs** - Game already has them in `Il2CppAssemblies/`
- **MelonLoader DLLs** - Game already has them in `MelonLoader/`

**Why whitelist approach:**
- Prevents accidentally shipping 100+ Unity DLLs
- Keeps `Mods/` folder clean
- Only the essentials

**`SkipUnchangedFiles="true"`:**
- If the DLL didn't change since last build, don't copy it
- Faster builds
- Prevents unnecessary file system writes

### Section 9: Game Relaunch Target

```xml
<Target Name="RelaunchGame" AfterTargets="CopyToMods_Whitelist">
  <Exec Command="(tasklist | findstr /I &quot;Schedule.*I.exe&quot; &gt;nul &amp;&amp; taskkill /F /IM &quot;Schedule I.exe&quot; &amp;&amp; timeout /t 3 /nobreak &gt;nul) || echo Game not running&#x0D;&#x0A;START &quot;&quot; &quot;$(GamePath)\Schedule I.exe&quot;" />
</Target>
```

**What it does:**
- Runs **after** files are copied (`AfterTargets="CopyToMods_Whitelist"`)
- Kills the game if running
- Waits 3 seconds
- Relaunches the game

**Command breakdown:**

```batch
(
  tasklist | findstr /I "Schedule.*I.exe" >nul     # Check if game is running
  &&                                                # If yes, then...
  taskkill /F /IM "Schedule I.exe"                 # Kill it forcefully
  &&                                                # Then...
  timeout /t 3 /nobreak >nul                       # Wait 3 seconds silently
)
||                                                  # If game wasn't running...
echo Game not running                              # Just say so
START "" "$(GamePath)\Schedule I.exe"              # Launch the game
```

**Why this is amazing:**
- **Instant iteration** - Build → automatically test in-game
- **No manual steps** - Press F5 in VS Code, done!
- **Graceful handling** - Works whether game is running or not

**⚠️ WARNING:** The game will close **without warning**. Save your game before building!

---

## Assembly References Explained

### Why So Many DLLs?

Unity games are **modular**. Each feature is in a separate DLL:

| DLL | Purpose |
|-----|---------|
| `UnityEngine.CoreModule` | Core Unity classes (GameObject, Transform, Component) |
| `UnityEngine.UI` | UI system (Button, Image, Text, Canvas) |
| `UnityEngine.UIModule` | Additional UI features |
| `UnityEngine.TextRenderingModule` | Text rendering (fonts, glyphs) |
| `Unity.TextMeshPro` | Advanced text rendering (TextMeshProUGUI) |
| `UnityEngine.ImageConversionModule` | Image loading (`ImageConversion.LoadImage`) |

**If you don't reference them, you get compile errors:**

```
error CS0246: The type or namespace name 'Button' could not be found
(are you missing a using directive or an assembly reference?)
```

### How to Find Which DLL You Need

**Problem:** Your code uses `SomeClass` but the compiler can't find it.

**Solution:**

1. **Google:** "Unity SomeClass documentation"
   - Unity docs show which DLL contains the class
   - Example: https://docs.unity3d.com/ScriptReference/UI.Button.html
   - Says "UnityEngine.UI" → need `UnityEngine.UI.dll`

2. **Use dnSpy:**
   - Open `Il2CppAssemblies/*.dll` in dnSpy
   - Search for the class name
   - See which DLL contains it

3. **Check existing mods:**
   - S1NotesApp, S1FuelMod, etc.
   - See what they reference

### Il2Cpp-Specific DLLs

These are unique to Il2Cpp builds:

| DLL | Purpose |
|-----|---------|
| `Il2CppInterop.Runtime` | Bridge between managed (C#) and unmanaged (C++) |
| `Il2CppSystem`, `Il2Cppmscorlib` | Il2Cpp versions of .NET base libraries |
| `Il2CppNewtonsoft.Json` | JSON serialization (Il2Cpp version) |
| `Il2CppFishNet.Runtime` | Networking library (used by game and S1API) |

**When you need them:**
- Using game types (requires `Assembly-CSharp`)
- Converting arrays (`Il2CppStructArray<byte>`)
- Game-specific networking

---

## Build Targets Deep Dive

### What are Build Targets?

**Build targets** are custom actions that run at specific points in the build process.

**Analogy:** Like hooks in a restaurant kitchen:
- **Before cooking** (BeforeTargets) - "Check if ingredients are fresh"
- **After cooking** (AfterTargets) - "Plate the dish and serve it"

### Target Execution Order

```
┌─────────────────────────────────────┐
│  dotnet build -c Il2cpp             │
└────────────────┬────────────────────┘
                 ↓
┌─────────────────────────────────────┐
│  VerifyGameFiles (BeforeTargets)    │ ← Check paths exist
└────────────────┬────────────────────┘
                 ↓
┌─────────────────────────────────────┐
│  Build (Compile C# → DLL)           │ ← MSBuild compiles your code
└────────────────┬────────────────────┘
                 ↓
┌─────────────────────────────────────┐
│  CopyToMods_Whitelist (AfterTargets)│ ← Copy DLL to Mods/
└────────────────┬────────────────────┘
                 ↓
┌─────────────────────────────────────┐
│  RelaunchGame (AfterTargets)        │ ← Kill and restart game
└─────────────────────────────────────┘
```

### Creating Custom Targets

**Example:** Add a target that zips the mod for distribution.

```xml
<Target Name="PackageForRelease" AfterTargets="CopyToMods_Whitelist">
  <!-- Only run in Release configuration -->
  <PropertyGroup Condition="'$(Configuration)'=='Il2cpp'">
    <ZipFileName>S1DockExports_$(Version).zip</ZipFileName>
  </PropertyGroup>

  <!-- Create zip file -->
  <ZipDirectory
    SourceDirectory="$(ModsPath)"
    DestinationFile="$(ProjectDir)Releases\$(ZipFileName)"
    Overwrite="true" />

  <Message Text="✅ Release package created: $(ZipFileName)" Importance="high" />
</Target>
```

Now `dotnet build -c Il2cpp` also creates a release zip!

---

## Troubleshooting Build Issues

### Issue 1: "UnityEngine.CoreModule.dll not found"

**Error:**
```
error : UnityEngine.CoreModule.dll not found at 'C:\...\Il2CppAssemblies'.
Check GamePath.
```

**Cause:** `GamePath` is wrong or MelonLoader isn't installed.

**Solution:**

1. **Verify game path:**
   ```powershell
   dir "C:\Program Files (x86)\Steam\steamapps\common\Schedule I"
   ```
   Should show `Schedule I.exe`, `MelonLoader/`, `Mods/` folders.

2. **Set correct path:**
   ```powershell
   $env:S1_GAME="D:\Your\Actual\Path\Schedule I"
   ```

3. **Install MelonLoader** if missing:
   - Download from https://melonwiki.xyz/
   - Run installer, point it to game directory

### Issue 2: "The type or namespace name 'X' could not be found"

**Error:**
```
error CS0246: The type or namespace name 'TextMeshProUGUI' could not be found
```

**Cause:** Missing assembly reference.

**Solution:**

1. **Find which DLL contains the type** (Google "Unity TextMeshProUGUI")
   - Answer: `Unity.TextMeshPro.dll`

2. **Add reference to `.csproj`:**
   ```xml
   <Reference Include="Unity.TextMeshPro">
     <HintPath>$(Il2CppAssembliesPath)\Unity.TextMeshPro.dll</HintPath>
   </Reference>
   ```

3. **Rebuild:**
   ```bash
   dotnet build -c Il2cpp
   ```

### Issue 3: "Access to the path '...' is denied"

**Error:**
```
error MSB3021: Unable to copy file "bin\Il2cpp\S1DockExports_Il2cpp.dll"
to "C:\...\Mods\S1DockExports_Il2cpp.dll". Access to the path is denied.
```

**Cause:** Game is running and has the DLL loaded.

**Solution:**

1. **Close the game** before building
2. Or, **comment out the RelaunchGame target temporarily:**
   ```xml
   <!-- <Target Name="RelaunchGame" AfterTargets="CopyToMods_Whitelist"> -->
   <!--   ... -->
   <!-- </Target> -->
   ```

3. **Rebuild:**
   ```bash
   dotnet build -c Il2cpp
   ```

### Issue 4: "Could not load file or assembly 'S1API'"

**Error (in-game log):**
```
FileNotFoundException: Could not load file or assembly 'S1API, Version=2.4.2.0'
```

**Cause:** `S1API.dll` is missing from `Mods/` folder.

**Solution:**

1. **Download S1API** from releases
2. **Copy to `Mods/` folder:**
   ```powershell
   copy S1API.dll "C:\Program Files (x86)\Steam\steamapps\common\Schedule I\Mods\"
   ```

3. **Rebuild mod:**
   ```bash
   dotnet build -c Il2cpp
   ```

---

## Customizing for Your Project

### Change Output Name

**Current:**
```xml
<AssemblyName>S1DockExports_Il2cpp</AssemblyName>
```

**Change to:**
```xml
<AssemblyName>MyMod</AssemblyName>
```

**Output:** `MyMod.dll`

### Add Version Number

```xml
<PropertyGroup>
  <Version>1.0.0</Version>
  <AssemblyVersion>$(Version)</AssemblyVersion>
  <FileVersion>$(Version)</FileVersion>
</PropertyGroup>
```

**Access in code:**
```csharp
var version = Assembly.GetExecutingAssembly().GetName().Version;
MelonLogger.Msg($"Mod version: {version}");
```

### Remove Game Relaunch (Manual Testing)

**Comment out the target:**
```xml
<!-- Disable auto-relaunch for manual testing -->
<!--
<Target Name="RelaunchGame" AfterTargets="CopyToMods_Whitelist">
  <Exec Command="..." />
</Target>
-->
```

Now builds don't restart the game.

### Add Multiple Embedded Resources

```xml
<ItemGroup>
  <EmbeddedResource Include="Assets/*.png" />
  <EmbeddedResource Include="Sounds/*.wav" />
  <EmbeddedResource Include="Data/config.json" />
</ItemGroup>
```

Wildcards (`*`) include all matching files.

### Change Target Framework

**⚠️ WARNING:** Only do this if you know what you're doing!

```xml
<!-- Change from netstandard2.1 to net472 -->
<TargetFramework>net472</TargetFramework>
```

**When to change:**
- Different game uses different framework
- S1API requires different framework

**Most Unity games use `netstandard2.1`** - don't change unless necessary!

---

## Summary

### Key Takeaways

✅ **.csproj is the build recipe** - Tells compiler what to do
✅ **Two configs for different scenarios** - Il2cpp (with game code) vs CrossCompat (without)
✅ **Use Il2cpp for S1DockExports** - Required for S1API managers
✅ **Paths are customizable** - Use `$env:S1_GAME` for your setup
✅ **Build targets automate workflow** - Verify → Build → Copy → Relaunch
✅ **Reference only what you need** - Unity is modular, add DLLs as needed

### Build Command Cheat Sheet

```bash
# Build Il2cpp (RECOMMENDED)
dotnet build -c Il2cpp

# Build CrossCompat (NOT SUPPORTED for S1DockExports)
dotnet build -c CrossCompat

# Clean build artifacts
dotnet clean

# Rebuild everything from scratch
dotnet clean && dotnet build -c Il2cpp

# Build without auto-relaunch (if target is disabled)
dotnet build -c Il2cpp

# Set custom game path (PowerShell)
$env:S1_GAME="D:\Games\Steam\steamapps\common\Schedule I"
dotnet build -c Il2cpp
```

### Next Steps

- **Read the code** - See how classes use the assemblies referenced here
- **Experiment** - Try adding a new DLL reference and using it
- **Customize** - Adapt the build targets to your workflow

---

**See Also:**
- [MODDING_TUTORIAL.md](./MODDING_TUTORIAL.md) - Learn modding fundamentals
- [PROJECT_STRUCTURE.md](./PROJECT_STRUCTURE.md) - Understand code organization
- [LOGGING_GUIDE.md](./LOGGING_GUIDE.md) - Master debugging

**Questions?** Open an issue on GitHub or ask the community!

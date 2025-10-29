# Loading Custom Icons in Unity Il2Cpp Mods

A comprehensive guide to loading embedded PNG images as sprites in MelonLoader/Il2Cpp Unity mods, based on solving icon loading challenges in the S1DockExports project.

## Table of Contents

1. [The Goal](#the-goal)
2. [Initial Challenges](#initial-challenges)
3. [Root Cause Analysis](#root-cause-analysis)
4. [The Solution](#the-solution)
5. [Implementation Steps](#implementation-steps)
6. [Debugging Process](#debugging-process)
7. [Key Takeaways](#key-takeaways)

---

## The Goal

Load a custom 256x256 PNG image from embedded assembly resources and display it as a phone app icon in the game "Schedule I" using Harmony patches and Il2Cpp interop.

**Requirements:**
- Load image from embedded resource (not disk)
- Convert to Unity Sprite
- Replace cloned icon's Image components
- Work with Il2Cpp managed/unmanaged interop

---

## Initial Challenges

### Attempt 1: Using S1API's ImageUtils

**Code:**
```csharp
using S1API.Internal.Utils;

var iconSprite = ImageUtils.LoadImageRaw(data);
```

**Result:**
```
❌ Failed to load icon sprite: Method not found:
'Boolean UnityEngine.ImageConversion.LoadImage(UnityEngine.Texture2D, Byte[])'
```

**Problem:** S1API's `ImageUtils.LoadImageRaw()` calls `UnityEngine.ImageConversion.LoadImage()`, which doesn't exist in this Unity version or isn't properly exposed through Il2Cpp interop.

### Attempt 2: Direct Texture2D.LoadImage()

**Code:**
```csharp
Texture2D texture = new Texture2D(2, 2);
texture.LoadImage(data); // Tried to call as instance method
```

**Result:**
```
error CS1061: 'Texture2D' does not contain a definition for 'LoadImage'
```

**Problem:** In Il2Cpp, `LoadImage` is in `UnityEngine.ImageConversion` static class, not an instance method on `Texture2D`.

---

## Root Cause Analysis

### Understanding Unity Image Loading APIs

Unity has multiple ways to load images:

1. **Texture2D.LoadImage()** (Legacy, Mono only)
   - Instance method on Texture2D
   - Not available in Il2Cpp builds

2. **UnityEngine.ImageConversion.LoadImage()** (Modern)
   - Static method in ImageConversionModule
   - Requires proper assembly reference
   - Needs Il2Cpp array conversion

### Il2Cpp Interop Challenge

Il2Cpp uses a different type system than Mono. Managed arrays (`byte[]`) must be converted to Il2Cpp arrays (`Il2CppStructArray<byte>`) when calling Unity APIs.

**Managed (C#):**
```csharp
byte[] data = new byte[1024];
```

**Il2Cpp (Unity):**
```csharp
Il2CppStructArray<byte> il2cppData = new Il2CppStructArray<byte>(data);
```

---

## The Solution

### Overview

1. Add `UnityEngine.ImageConversionModule` assembly reference
2. Load embedded resource as `byte[]`
3. Convert to `Il2CppStructArray<byte>` (Il2Cpp builds only)
4. Call `ImageConversion.LoadImage()` with proper parameters
5. Create `Sprite` from loaded `Texture2D`

### Required Assembly References

Add to `.csproj` for Il2Cpp configuration:

```xml
<ItemGroup Condition="'$(Configuration)'=='Il2cpp'">
  <Reference Include="UnityEngine.ImageConversionModule">
    <HintPath>$(Il2CppAssembliesPath)\UnityEngine.ImageConversionModule.dll</HintPath>
  </Reference>
</ItemGroup>
```

### Required Usings

```csharp
using UnityEngine;
using System.Reflection;
using MelonLoader;

#if IL2CPP
using Il2CppInterop.Runtime.InteropTypes.Arrays;
#endif
```

---

## Implementation Steps

### Step 1: Embed the Image Resource

In your `.csproj`:

```xml
<ItemGroup>
  <EmbeddedResource Include="DE.png">
    <!-- Build action: Embedded Resource -->
  </EmbeddedResource>
</ItemGroup>
```

### Step 2: Load Embedded Resource

```csharp
private static Sprite? LoadIconSprite()
{
    try
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Try multiple possible resource names
        string[] possibleNames = {
            "S1DockExports.DE.png",
            "S1DockExports.Assets.DE.png",
            "DE.png"
        };

        foreach (string resourceName in possibleNames)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                MelonLogger.Msg($"Found icon resource: {resourceName}");

                // Read stream into byte array
                byte[] data = new byte[stream.Length];
                stream.Read(data, 0, data.Length);

                // Continue to Step 3...
                return LoadTextureFromBytes(data);
            }
        }

        // Debug: Log available resources if not found
        var names = assembly.GetManifestResourceNames();
        MelonLogger.Warning($"Icon resource not found. Available: {string.Join(", ", names)}");
    }
    catch (Exception ex)
    {
        MelonLogger.Error($"Failed to load icon sprite: {ex.Message}");
    }

    return null;
}
```

### Step 3: Convert Bytes to Texture2D

```csharp
private static Sprite? LoadTextureFromBytes(byte[] data)
{
    // Create empty texture (size will be replaced by LoadImage)
    Texture2D texture = new Texture2D(2, 2);

#if IL2CPP
    // Convert managed byte[] to Il2Cpp array
    var il2cppArray = new Il2CppStructArray<byte>(data);
    bool loaded = UnityEngine.ImageConversion.LoadImage(texture, il2cppArray);
#else
    // Mono builds can use managed arrays directly
    bool loaded = UnityEngine.ImageConversion.LoadImage(texture, data);
#endif

    if (!loaded)
    {
        MelonLogger.Warning("ImageConversion.LoadImage failed");
        return null;
    }

    MelonLogger.Msg($"Icon sprite loaded successfully ({texture.width}x{texture.height})");

    // Continue to Step 4...
    return CreateSpriteFromTexture(texture);
}
```

### Step 4: Create Sprite from Texture

```csharp
private static Sprite CreateSpriteFromTexture(Texture2D texture)
{
    // Create sprite covering entire texture
    Sprite sprite = Sprite.Create(
        texture,
        new Rect(0, 0, texture.width, texture.height), // Full texture
        new Vector2(0.5f, 0.5f)  // Pivot at center
    );

    return sprite;
}
```

### Step 5: Replace Icon Sprites

```csharp
public static void ReplaceIconSprites(GameObject iconGameObject, Sprite customSprite)
{
    // Find all Image components (including inactive)
    var imageComponents = iconGameObject.GetComponentsInChildren<UnityEngine.UI.Image>(true);

    MelonLogger.Msg($"Found {imageComponents.Length} Image components");

    // Log all components for debugging
    for (int i = 0; i < imageComponents.Length; i++)
    {
        var img = imageComponents[i];
        string spriteName = img.sprite != null ? img.sprite.name : "null";
        MelonLogger.Msg($"  [{i}] GameObject: '{img.gameObject.name}' | Sprite: '{spriteName}'");
    }

    // Replace all sprites
    int replacedCount = 0;
    foreach (var img in imageComponents)
    {
        if (img.sprite != null) // Only replace existing sprites
        {
            img.sprite = customSprite;
            replacedCount++;
            MelonLogger.Msg($"  ✓ Replaced sprite on '{img.gameObject.name}'");
        }
    }

    MelonLogger.Msg($"Replaced {replacedCount}/{imageComponents.Length} sprite(s)");
}
```

---

## Debugging Process

### 1. Verify Embedded Resource

Check if resource is actually embedded:

```csharp
var assembly = Assembly.GetExecutingAssembly();
var names = assembly.GetManifestResourceNames();
foreach (var name in names)
{
    MelonLogger.Msg($"Resource: {name}");
}
```

**Expected output:** `S1DockExports.DE.png`

### 2. Log Image Loading Steps

Add detailed logging at each step:

```csharp
MelonLogger.Msg($"Found icon resource: {resourceName}");
MelonLogger.Msg($"Read {data.Length} bytes");
MelonLogger.Msg($"Created texture: {texture != null}");
MelonLogger.Msg($"LoadImage result: {loaded}");
MelonLogger.Msg($"Texture size: {texture.width}x{texture.height}");
```

### 3. Inspect Icon GameObject Hierarchy

Log all components to understand structure:

```csharp
var imageComponents = icon.GetComponentsInChildren<UnityEngine.UI.Image>(true);
for (int i = 0; i < imageComponents.Length; i++)
{
    var img = imageComponents[i];
    MelonLogger.Msg($"  [{i}] Name: '{img.gameObject.name}'");
    MelonLogger.Msg($"       Sprite: '{img.sprite?.name ?? "null"}'");
    MelonLogger.Msg($"       Enabled: {img.enabled}");
    MelonLogger.Msg($"       Parent: '{img.transform.parent?.name ?? "root"}'");
}
```

**Our output:**
```
[0] GameObject: 'Outline' | Sprite: 'UISprite'
[1] GameObject: 'Mask' | Sprite: 'UI_Phone_IconBack'
[2] GameObject: 'Image' | Sprite: 'Icon_Messages'  ← Main icon
[3] GameObject: 'Notifications' | Sprite: 'UI_Phone_Notif'
```

### 4. Common Errors and Solutions

| Error | Cause | Solution |
|-------|-------|----------|
| `Method not found: ImageConversion.LoadImage` | Missing assembly reference | Add `UnityEngine.ImageConversionModule` to csproj |
| `Texture2D.LoadImage not found` | Wrong API (Mono vs Il2Cpp) | Use `ImageConversion.LoadImage` static method |
| `Type mismatch` errors | Managed/Il2Cpp array mismatch | Convert to `Il2CppStructArray<byte>` |
| Icon shows but wrong image | Sprite not replaced | Verify Image components targeted |
| Resource not found | Wrong embedded resource name | Log `GetManifestResourceNames()` |

---

## Key Takeaways

### 1. API Compatibility Matters

Different Unity versions and build types (Mono vs Il2Cpp) have different APIs:
- **Mono**: `Texture2D.LoadImage()` instance method
- **Il2Cpp**: `ImageConversion.LoadImage()` static method

Always check which APIs are available in your target environment.

### 2. Il2Cpp Requires Type Conversion

Managed arrays must be converted when crossing the managed/unmanaged boundary:

```csharp
byte[] managedArray = ...;
var il2cppArray = new Il2CppStructArray<byte>(managedArray);
```

### 3. Use Conditional Compilation

Support both Mono and Il2Cpp builds:

```csharp
#if IL2CPP
    var il2cppArray = new Il2CppStructArray<byte>(data);
    bool loaded = ImageConversion.LoadImage(texture, il2cppArray);
#else
    bool loaded = ImageConversion.LoadImage(texture, data);
#endif
```

### 4. Log Everything During Development

Comprehensive logging saved hours of debugging:
- Resource discovery
- Loading steps
- GameObject hierarchy
- Success/failure states

### 5. Don't Trust Helper Libraries Blindly

S1API's `ImageUtils.LoadImageRaw()` failed because it used an incompatible API. When helper libraries fail, implement the solution directly.

### 6. Embedded Resource Naming

Resource names follow this pattern:
```
<AssemblyName>.<Folder>.<FileName>
```

Examples:
- `S1DockExports.DE.png` (root)
- `S1DockExports.Assets.DE.png` (Assets folder)

Always try multiple variations and log `GetManifestResourceNames()` to discover the actual name.

---

## Complete Working Example

Here's the final, working implementation:

```csharp
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using System;
using System.Reflection;

#if IL2CPP
using Il2CppInterop.Runtime.InteropTypes.Arrays;
#endif

namespace S1DockExports.Integrations
{
    [HarmonyPatch]
    public static class PhoneAppInjector
    {
        private static Sprite? _iconSprite = null;

        /// <summary>
        /// Load the embedded icon sprite on first use
        /// </summary>
        private static Sprite? LoadIconSprite()
        {
            if (_iconSprite != null)
                return _iconSprite;

            try
            {
                var assembly = Assembly.GetExecutingAssembly();

                // Try multiple possible resource names
                string[] possibleNames = {
                    "S1DockExports.DE.png",
                    "S1DockExports.Assets.DE.png",
                    "DE.png"
                };

                foreach (string resourceName in possibleNames)
                {
                    using var stream = assembly.GetManifestResourceStream(resourceName);
                    if (stream != null)
                    {
                        MelonLogger.Msg($"Found icon resource: {resourceName}");

                        // Read the stream into a byte array
                        byte[] data = new byte[stream.Length];
                        stream.Read(data, 0, data.Length);

                        // Create Texture2D manually
                        Texture2D texture = new Texture2D(2, 2);

#if IL2CPP
                        // Convert byte[] to Il2Cpp byte array
                        var il2cppArray = new Il2CppStructArray<byte>(data);
                        bool loaded = UnityEngine.ImageConversion.LoadImage(texture, il2cppArray);
#else
                        bool loaded = UnityEngine.ImageConversion.LoadImage(texture, data);
#endif

                        if (loaded)
                        {
                            // Create sprite from texture
                            _iconSprite = Sprite.Create(texture,
                                new Rect(0, 0, texture.width, texture.height),
                                new Vector2(0.5f, 0.5f));

                            MelonLogger.Msg($"✓ Icon sprite loaded successfully ({texture.width}x{texture.height})");
                            return _iconSprite;
                        }
                        else
                        {
                            MelonLogger.Warning($"⚠️ ImageConversion.LoadImage failed for {resourceName}");
                        }
                    }
                }

                // Log available resources if not found
                var names = assembly.GetManifestResourceNames();
                MelonLogger.Warning($"Icon resource not found. Available: {string.Join(", ", names)}");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"❌ Failed to load icon sprite: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Replace icon sprites on a cloned GameObject
        /// </summary>
        public static void ReplaceIconImage(GameObject iconGameObject)
        {
            var iconSprite = LoadIconSprite();
            if (iconSprite == null)
            {
                MelonLogger.Warning("⚠️ Could not load custom icon sprite, using default");
                return;
            }

            // Find all Image components (including inactive)
            var imageComponents = iconGameObject.GetComponentsInChildren<UnityEngine.UI.Image>(true);
            MelonLogger.Msg($"🖼️ Found {imageComponents.Length} Image components on cloned icon");

            // Log all components for diagnostics
            for (int i = 0; i < imageComponents.Length; i++)
            {
                var img = imageComponents[i];
                string spriteName = img.sprite != null ? img.sprite.name : "null";
                MelonLogger.Msg($"  [{i}] GameObject: '{img.gameObject.name}' | Sprite: '{spriteName}' | Enabled: {img.enabled}");
            }

            // Replace all sprites with our custom icon
            int replacedCount = 0;
            foreach (var img in imageComponents)
            {
                if (img.sprite != null) // Only replace if there's an existing sprite
                {
                    img.sprite = iconSprite;
                    replacedCount++;
                    MelonLogger.Msg($"  ✓ Replaced sprite on '{img.gameObject.name}'");
                }
            }

            MelonLogger.Msg($"📝 Replaced {replacedCount}/{imageComponents.Length} sprite(s)");
        }
    }
}
```

**Result:**
```
[13:08:12.344] 📱 Injecting Dock Exports icon into HomeScreen...
[13:08:12.366] ✓ Icon sprite loaded successfully (256x256)
[13:08:12.368] 🖼️ Found 4 Image components on cloned icon
[13:08:12.374]   ✓ Replaced sprite on 'Outline'
[13:08:12.374]   ✓ Replaced sprite on 'Mask'
[13:08:12.375]   ✓ Replaced sprite on 'Image'
[13:08:12.376]   ✓ Replaced sprite on 'Notifications'
[13:08:12.376] 📝 Replaced 4/4 sprite(s)
```

---

## Summary

Loading custom images in Unity Il2Cpp mods requires:

1. ✅ Proper assembly references (`UnityEngine.ImageConversionModule`)
2. ✅ Correct API usage (`ImageConversion.LoadImage` static method)
3. ✅ Il2Cpp type conversion (`Il2CppStructArray<byte>`)
4. ✅ Conditional compilation for cross-platform support
5. ✅ Comprehensive logging for debugging

By following these steps, you can reliably load and display custom images in your Unity Il2Cpp mods.

---

**Author:** Claude Code + KaisoXP
**Date:** October 25, 2025
**Project:** S1DockExports
**Unity Version:** 2021.3.x (Il2Cpp)

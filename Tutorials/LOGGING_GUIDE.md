# The Complete Logging Guide for Unity Il2Cpp Modding

> **Your Most Powerful Tool for Exploring Unknown Games**

**Author:** Claude Code + KaisoXP
**Date:** October 25, 2025
**Project:** S1DockExports
**Audience:** Complete beginners (no programming knowledge assumed)

---

## Table of Contents

1. [What is Logging?](#what-is-logging)
2. [Why Logging is Your Superpower](#why-logging-is-your-superpower)
3. [MelonLogger Basics](#melonlogger-basics)
4. [Where Do Logs Appear?](#where-do-logs-appear)
5. [Strategic Logging Placement](#strategic-logging-placement)
6. [Reverse-Engineering with Logs](#reverse-engineering-with-logs)
7. [Real-World Examples from S1DockExports](#real-world-examples)
8. [Log Reading Skills](#log-reading-skills)
9. [Advanced Logging Patterns](#advanced-logging-patterns)
10. [Common Pitfalls & Solutions](#common-pitfalls--solutions)
11. [Quick Reference](#quick-reference)

---

## What is Logging?

### The Simple Explanation

Imagine you're exploring a dark cave with a flashlight. **Logging is like leaving glowing breadcrumbs** behind you so you can:
- See where you've been
- Know which paths you took
- Remember what you found at each location
- Trace your way back if something goes wrong

In programming, logging means **writing messages to a log file or console** to track:
- What your code is doing
- What values variables have
- What functions are being called
- Where errors occur

### The Modding Context

When you mod a game like "Schedule I," you're working with:
- **Closed-source code** (you can't read the original game code)
- **No documentation** (no manual telling you how things work)
- **Black box systems** (you see inputs and outputs, but not what's inside)

**Logging is how you shine a light into that black box.** It's like being a detective leaving notes at every clue you find.

### Real-World Analogy

Think of logging like a scientist's lab notebook:

```
❌ WITHOUT LOGGING:
"I mixed some chemicals and something exploded. Not sure which ones or when."

✅ WITH LOGGING:
"10:32 AM - Added 5ml of Chemical A to beaker
 10:33 AM - Temperature: 22°C
 10:34 AM - Added 3ml of Chemical B
 10:34 AM - Temperature spiked to 85°C
 10:35 AM - Reaction occurred: yellow precipitate formed"
```

The second scientist can **reproduce the experiment** and **understand what happened**.

---

## Why Logging is Your Superpower

### 1. **The Game Doesn't Come with Instructions**

You can't open "Schedule I" and see how the phone system works. But with logging, you can:

```csharp
MelonLogger.Msg($"Found GameObject: {obj.name}");
MelonLogger.Msg($"It has {obj.transform.childCount} children");
```

Output:
```
[10:23:45.123] Found GameObject: HomeScreen
[10:23:45.124] It has 8 children
```

Now you know there's a "HomeScreen" object with 8 children!

### 2. **You Can't Use a Debugger**

In normal programming, you'd use a debugger to pause code and inspect variables. **With Unity mods, that's much harder.** Logging is your replacement debugger.

### 3. **Things Happen Fast**

The game runs at 60 frames per second. Without logs, you'd miss what's happening. Logs let you **freeze time** and examine what occurred.

### 4. **You Learn by Experimenting**

Modding is like being a scientist in an alien laboratory:
- You don't know what buttons do
- You press them and observe what happens
- Logs are your observations

```csharp
MelonLogger.Msg("Before calling mystery function");
MysteryGameFunction();
MelonLogger.Msg("After calling mystery function - we survived!");
```

---

## MelonLogger Basics

### The Three Main Log Levels

MelonLoader provides three types of log messages:

#### 1. **`MelonLogger.Msg()`** - Regular Information

Use this for **normal events** you want to track.

```csharp
MelonLogger.Msg("Player opened the phone");
MelonLogger.Msg($"Current rank: {playerRank}");
```

**When to use:**
- Tracking what your code is doing
- Reporting successful operations
- Showing variable values
- Confirming something happened

#### 2. **`MelonLogger.Warning()`** - Something Unexpected

Use this when something is **odd but not broken**.

```csharp
MelonLogger.Warning("Could not find icon resource, using default");
MelonLogger.Warning($"Player rank is {rank}, expected at least 3");
```

**When to use:**
- Something failed but you have a fallback
- Values are outside expected range but code continues
- Something's missing but not critical
- Configuration issues

#### 3. **`MelonLogger.Error()`** - Something Broke

Use this when something **failed and it matters**.

```csharp
MelonLogger.Error("Failed to load save data!");
MelonLogger.Error($"Exception: {ex.Message}");
```

**When to use:**
- Exceptions occurred
- Critical operations failed
- Data corruption detected
- The mod can't continue normally

### Basic Syntax

```csharp
// Simple message
MelonLogger.Msg("Hello from the mod!");

// With a variable
int playerLevel = 5;
MelonLogger.Msg($"Player is level {playerLevel}");

// Multiple variables
string name = "The Broker";
int amount = 1000;
MelonLogger.Msg($"{name} paid you ${amount}");

// With formatting
float price = 14700.5f;
MelonLogger.Msg($"Price: ${price:N0}"); // Output: Price: $14,701
```

### The `$` String Interpolation

The `$` before a string makes it an **interpolated string**, which lets you embed variables:

```csharp
int x = 10;
int y = 20;

// Without interpolation (old way)
MelonLogger.Msg("X is " + x + " and Y is " + y);

// With interpolation (modern way)
MelonLogger.Msg($"X is {x} and Y is {y}");
```

**Much easier to read!**

---

## Where Do Logs Appear?

### 1. **MelonLoader Console Window**

When the game launches with MelonLoader, a **black console window** opens alongside the game:

```
[10:15:32.456] [DockExports] Mod initialized
[10:15:32.789] [DockExports] Found icon resource: S1DockExports.DE.png
[10:15:32.790] [DockExports] ✓ Icon sprite loaded successfully (256x256)
```

This is **real-time** - messages appear as they're logged.

### 2. **Log Files**

Logs are also saved to files in the game directory:

```
C:\Program Files (x86)\Steam\steamapps\common\Schedule I\
    MelonLoader\
        Latest.log          ← Most recent session
        2025-10-25_10-15.log  ← Timestamped logs
```

**When to use files:**
- Game crashes (console disappears)
- Need to review past sessions
- Sharing logs with others for help
- Searching for patterns across multiple runs

### 3. **Filtering Your Logs**

Notice the `[DockExports]` prefix in the logs above? That's your mod's identifier:

```csharp
MelonLogger.Msg("[DockExports] My message here");
```

This helps you **find your logs** among all the other mods and game messages:

```
[10:15:30.123] [SomeOtherMod] Other mod's message
[10:15:31.456] [Unity] Game engine message
[10:15:32.789] [DockExports] YOUR MESSAGE IS HERE!  ← Easy to spot!
[10:15:33.012] [Game] Game system message
```

---

## Strategic Logging Placement

### Where to Put Logs

Think of logs as **checkpoints in your code**. Here are the most valuable places:

#### 1. **Entry Points** - "We Started"

```csharp
public override void OnInitializeMelon()
{
    MelonLogger.Msg("[DockExports] 🎮 Mod initialized");
    // ... rest of your code
}
```

**Why:** Confirms your mod loaded successfully.

#### 2. **Exit Points** - "We Finished"

```csharp
public void ProcessPayment()
{
    MelonLogger.Msg("[DockExports] Processing payment...");

    // ... payment logic ...

    MelonLogger.Msg("[DockExports] ✓ Payment processed successfully");
}
```

**Why:** If you see "Processing" but not "successfully," you know it crashed in between.

#### 3. **Before API Calls** - "Calling the Game"

```csharp
MelonLogger.Msg("[DockExports] About to call NetworkSingleton<TimeManager>.Instance");
var timeManager = NetworkSingleton<TimeManager>.Instance;
MelonLogger.Msg("[DockExports] ✓ Successfully got TimeManager instance");
```

**Why:** If the second log never appears, the API call crashed.

#### 4. **After API Calls with Results** - "What Did We Get?"

```csharp
int dayIndex = timeManager.DayIndex;
MelonLogger.Msg($"[DockExports] DayIndex value: {dayIndex}");
```

**Why:** You discover what values the game API returns.

#### 5. **Conditional Branches** - "Which Path Did We Take?"

```csharp
if (playerRank >= 3)
{
    MelonLogger.Msg("[DockExports] ✓ Rank requirement met");
    UnlockBroker();
}
else
{
    MelonLogger.Msg($"[DockExports] ✗ Rank too low: {playerRank} < 3");
}
```

**Why:** You see which conditions are true/false.

#### 6. **Loop Iterations** - "How Many Times?"

```csharp
for (int i = 0; i < children.Count; i++)
{
    var child = children[i];
    MelonLogger.Msg($"[DockExports] Child {i}: {child.name}");
}
```

**Why:** You discover all items in a collection.

**⚠️ CAUTION:** Loops can spam logs! See "Throttled Logging" below.

#### 7. **Exception Handlers** - "What Went Wrong?"

```csharp
try
{
    LoadIconSprite();
}
catch (Exception ex)
{
    MelonLogger.Error($"[DockExports] ❌ Failed to load icon: {ex.Message}");
    MelonLogger.Error($"[DockExports] Stack trace: {ex.StackTrace}");
}
```

**Why:** You see exactly what error occurred and where.

---

## Reverse-Engineering with Logs

### The Scientific Method for Modding

When you don't have documentation, you use **logging to explore**:

1. **Form a Hypothesis**: "I think phone icons are in a GameObject called 'IconContainer'"
2. **Design an Experiment**: Log all GameObjects to find it
3. **Run the Experiment**: Build the mod and launch the game
4. **Observe Results**: Read the logs
5. **Form Conclusions**: "IconContainer doesn't exist, but I found 'HomeScreen' instead"
6. **Repeat**: Refine your hypothesis and try again

### Pattern 1: Discovering GameObject Hierarchies

**Goal:** Find where phone app icons are stored.

**Code:**
```csharp
[HarmonyPatch(typeof(HomeScreen), "Start")]
[HarmonyPostfix]
public static void ExploreHomeScreen(HomeScreen __instance)
{
    MelonLogger.Msg("[DockExports] 📱 Exploring HomeScreen structure...");

    var transform = __instance.transform;
    MelonLogger.Msg($"[DockExports] HomeScreen has {transform.childCount} children:");

    for (int i = 0; i < transform.childCount; i++)
    {
        var child = transform.GetChild(i);
        MelonLogger.Msg($"[DockExports]   Child {i}: '{child.name}' ({child.childCount} children)");

        // Log grandchildren for suspicious children
        if (child.childCount >= 7)
        {
            MelonLogger.Msg($"[DockExports]   ^ This looks promising! Logging its children:");

            for (int j = 0; j < child.childCount; j++)
            {
                var grandchild = child.GetChild(j);
                MelonLogger.Msg($"[DockExports]     Icon {j}: '{grandchild.name}'");
            }
        }
    }
}
```

**Output:**
```
[10:23:12.345] [DockExports] 📱 Exploring HomeScreen structure...
[10:23:12.346] [DockExports] HomeScreen has 8 children:
[10:23:12.347] [DockExports]   Child 0: 'Background' (0 children)
[10:23:12.348] [DockExports]   Child 1: 'StatusBar' (3 children)
[10:23:12.349] [DockExports]   Child 2: 'AppIconsGrid' (7 children)
[10:23:12.350] [DockExports]   ^ This looks promising! Logging its children:
[10:23:12.351] [DockExports]     Icon 0: 'MessagesIcon'
[10:23:12.352] [DockExports]     Icon 1: 'ContactsIcon'
[10:23:12.353] [DockExports]     Icon 2: 'NotesIcon'
...
```

**Discovery:** Icons are in `AppIconsGrid`! We found it!

### Pattern 2: Discovering Component Types

**Goal:** What components are on the icon GameObjects?

**Code:**
```csharp
var icon = iconContainer.GetChild(0); // First icon
MelonLogger.Msg($"[DockExports] Investigating icon: {icon.name}");

// Get ALL components
var allComponents = icon.GetComponents<Component>();
MelonLogger.Msg($"[DockExports] Found {allComponents.Length} components:");

foreach (var component in allComponents)
{
    MelonLogger.Msg($"[DockExports]   - {component.GetType().Name}");
}
```

**Output:**
```
[10:25:30.123] [DockExports] Investigating icon: MessagesIcon
[10:25:30.124] [DockExports] Found 5 components:
[10:25:30.125] [DockExports]   - Transform
[10:25:30.126] [DockExports]   - RectTransform
[10:25:30.127] [DockExports]   - CanvasRenderer
[10:25:30.128] [DockExports]   - Image
[10:25:30.129] [DockExports]   - Button  ← AH HA! There's a Button component!
```

**Discovery:** Icons have a `Button` component we can hook into!

### Pattern 3: Discovering Properties and Fields

**Goal:** What properties does `TimeManager` have?

**Code:**
```csharp
var timeManager = NetworkSingleton<TimeManager>.Instance;
MelonLogger.Msg("[DockExports] 🕐 Exploring TimeManager properties...");

// Try common time-related properties
try
{
    MelonLogger.Msg($"[DockExports] DayIndex: {timeManager.DayIndex}");
}
catch
{
    MelonLogger.Warning("[DockExports] DayIndex property doesn't exist");
}

try
{
    MelonLogger.Msg($"[DockExports] CurrentDay: {timeManager.CurrentDay}");
}
catch
{
    MelonLogger.Warning("[DockExports] CurrentDay property doesn't exist");
}

try
{
    MelonLogger.Msg($"[DockExports] CurrentTime: {timeManager.CurrentTime}");
}
catch
{
    MelonLogger.Warning("[DockExports] CurrentTime property doesn't exist");
}
```

**Output:**
```
[10:30:15.123] [DockExports] 🕐 Exploring TimeManager properties...
[10:30:15.124] [DockExports] DayIndex: 42
[10:30:15.125] [DockExports] CurrentDay property doesn't exist
[10:30:15.126] [DockExports] CurrentTime: 1430
```

**Discovery:** `DayIndex` and `CurrentTime` exist, but `CurrentDay` doesn't!

### Pattern 4: Tracking Values Over Time

**Goal:** How does DayIndex change when days pass?

**Code:**
```csharp
private int lastLoggedDay = -1;

public override void OnUpdate()
{
    var timeManager = NetworkSingleton<TimeManager>.Instance;
    int currentDay = timeManager.DayIndex;

    // Log only when day changes (not every frame!)
    if (currentDay != lastLoggedDay)
    {
        int dayOfWeek = currentDay % 7;
        MelonLogger.Msg($"[DockExports] 📅 Day changed: DayIndex={currentDay}, DayOfWeek={dayOfWeek}");
        lastLoggedDay = currentDay;
    }
}
```

**Output:**
```
[10:32:10.456] [DockExports] 📅 Day changed: DayIndex=42, DayOfWeek=0 (Monday)
[10:35:22.789] [DockExports] 📅 Day changed: DayIndex=43, DayOfWeek=1 (Tuesday)
[10:38:45.012] [DockExports] 📅 Day changed: DayIndex=44, DayOfWeek=2 (Wednesday)
```

**Discovery:** `DayIndex` increments by 1 each day, and `% 7` gives us day of week!

---

## Real-World Examples from S1DockExports

Let me show you **actual logs from developing this mod** and what we learned from them.

### Example 1: Finding the Icon Container

**The Problem:** Where are phone app icons stored in the game?

**The Investigation:**
```csharp
[HarmonyPatch(typeof(HomeScreen), "Start")]
[HarmonyPostfix]
public static void InjectAppIcon(HomeScreen __instance)
{
    MelonLogger.Msg("[DockExports] 📱 Injecting Dock Exports icon into HomeScreen...");

    var transform = __instance.transform;
    MelonLogger.Msg($"[DockExports] HomeScreen has {transform.childCount} children:");

    Transform iconContainer = null;

    for (int i = 0; i < transform.childCount; i++)
    {
        var child = transform.GetChild(i);
        MelonLogger.Msg($"[DockExports]   Child {i}: {child.name} ({child.childCount} children)");

        // We know there are 7 app icons, so look for container with 7+ children
        if (child.childCount >= 7)
        {
            MelonLogger.Msg($"[DockExports]   ^ This looks like the icon container!");
            iconContainer = child;

            // Log all the icons
            for (int j = 0; j < child.childCount; j++)
            {
                var icon = child.GetChild(j);
                MelonLogger.Msg($"[DockExports]     Icon {j}: {icon.name}");
            }
        }
    }

    if (iconContainer == null)
    {
        MelonLogger.Warning("[DockExports] Could not find icon container!");
        return;
    }

    MelonLogger.Msg("[DockExports] ✅ Found icon container!");
}
```

**The Logs:**
```
[13:08:10.123] [DockExports] 📱 Injecting Dock Exports icon into HomeScreen...
[13:08:10.124] [DockExports] HomeScreen has 5 children:
[13:08:10.125] [DockExports]   Child 0: 'Panel' (1 children)
[13:08:10.126] [DockExports]   Child 1: 'StatusBar' (3 children)
[13:08:10.127] [DockExports]   Child 2: 'Grid' (7 children)
[13:08:10.128] [DockExports]   ^ This looks like the icon container!
[13:08:10.129] [DockExports]     Icon 0: 'Icon_Messages'
[13:08:10.130] [DockExports]     Icon 1: 'Icon_Contacts'
[13:08:10.131] [DockExports]     Icon 2: 'Icon_Bank'
[13:08:10.132] [DockExports]     Icon 3: 'Icon_Notes'
[13:08:10.133] [DockExports]     Icon 4: 'Icon_Pager'
[13:08:10.134] [DockExports]     Icon 5: 'Icon_Map'
[13:08:10.135] [DockExports]     Icon 6: 'Icon_Settings'
[13:08:10.136] [DockExports] ✅ Found icon container!
```

**What We Learned:**
- Icon container is at `HomeScreen → Grid`
- It has exactly 7 children (the 7 default apps)
- Icons are named with pattern `Icon_<AppName>`

### Example 2: Discovering Image Components

**The Problem:** How do we replace the icon's image with our custom one?

**The Investigation:**
```csharp
var templateIcon = iconContainer.GetChild(0); // Clone the Messages icon
var ourIcon = UnityEngine.Object.Instantiate(templateIcon.gameObject, iconContainer);

MelonLogger.Msg($"[DockExports] Created icon GameObject: {ourIcon.name}");

// Find ALL Image components (including inactive ones)
var imageComponents = ourIcon.GetComponentsInChildren<UnityEngine.UI.Image>(true);
MelonLogger.Msg($"[DockExports] 🖼️ Found {imageComponents.Length} Image components on cloned icon");

// Log details about each one
for (int i = 0; i < imageComponents.Length; i++)
{
    var img = imageComponents[i];
    string spriteName = img.sprite != null ? img.sprite.name : "null";
    MelonLogger.Msg($"[DockExports]   [{i}] GameObject: '{img.gameObject.name}' | Sprite: '{spriteName}' | Enabled: {img.enabled}");
}
```

**The Logs:**
```
[13:08:12.366] [DockExports] Created icon GameObject: Icon_Messages(Clone)
[13:08:12.368] [DockExports] 🖼️ Found 4 Image components on cloned icon
[13:08:12.370] [DockExports]   [0] GameObject: 'Outline' | Sprite: 'UISprite' | Enabled: true
[13:08:12.371] [DockExports]   [1] GameObject: 'Mask' | Sprite: 'UI_Phone_IconBack' | Enabled: true
[13:08:12.372] [DockExports]   [2] GameObject: 'Image' | Sprite: 'Icon_Messages' | Enabled: true
[13:08:12.373] [DockExports]   [3] GameObject: 'Notifications' | Sprite: 'UI_Phone_Notif' | Enabled: false
```

**What We Learned:**
- Icons have **4 Image components**, not 1!
- Main icon image is at index [2], GameObject named 'Image'
- There's also an Outline, Mask, and Notifications badge
- We need to replace ALL sprites to fully customize the icon

### Example 3: Debugging Icon Loading Failure

**The Problem:** Custom icon isn't showing up. Why?

**The Investigation:**
```csharp
private static Sprite? LoadIconSprite()
{
    try
    {
        var assembly = Assembly.GetExecutingAssembly();
        MelonLogger.Msg("[DockExports] Loading embedded icon...");

        // Log ALL available resources
        var names = assembly.GetManifestResourceNames();
        MelonLogger.Msg($"[DockExports] Available embedded resources ({names.Length}):");
        foreach (var name in names)
        {
            MelonLogger.Msg($"[DockExports]   - {name}");
        }

        // Try to load it
        string resourceName = "S1DockExports.DE.png";
        using var stream = assembly.GetManifestResourceStream(resourceName);

        if (stream == null)
        {
            MelonLogger.Error($"[DockExports] ❌ Resource '{resourceName}' not found!");
            return null;
        }

        MelonLogger.Msg($"[DockExports] ✓ Found resource: {resourceName} ({stream.Length} bytes)");

        // Read into byte array
        byte[] data = new byte[stream.Length];
        stream.Read(data, 0, data.Length);
        MelonLogger.Msg($"[DockExports] ✓ Read {data.Length} bytes from stream");

        // Create texture
        Texture2D texture = new Texture2D(2, 2);
        MelonLogger.Msg("[DockExports] Created Texture2D, loading image...");

        var il2cppArray = new Il2CppStructArray<byte>(data);
        bool loaded = UnityEngine.ImageConversion.LoadImage(texture, il2cppArray);

        if (!loaded)
        {
            MelonLogger.Error("[DockExports] ❌ ImageConversion.LoadImage returned false!");
            return null;
        }

        MelonLogger.Msg($"[DockExports] ✓ Image loaded: {texture.width}x{texture.height}");

        // Create sprite
        var sprite = Sprite.Create(texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f));

        MelonLogger.Msg("[DockExports] ✅ Icon sprite created successfully!");
        return sprite;
    }
    catch (Exception ex)
    {
        MelonLogger.Error($"[DockExports] ❌ Exception loading icon: {ex.Message}");
        MelonLogger.Error($"[DockExports] Stack trace: {ex.StackTrace}");
        return null;
    }
}
```

**The Logs (First Attempt):**
```
[12:45:10.123] [DockExports] Loading embedded icon...
[12:45:10.124] [DockExports] Available embedded resources (1):
[12:45:10.125] [DockExports]   - S1DockExports.DE.png
[12:45:10.126] [DockExports] ✓ Found resource: S1DockExports.DE.png (65536 bytes)
[12:45:10.127] [DockExports] ✓ Read 65536 bytes from stream
[12:45:10.128] [DockExports] Created Texture2D, loading image...
[12:45:10.129] [DockExports] ❌ ImageConversion.LoadImage returned false!
```

**What We Learned:**
- Resource IS embedded correctly ✓
- We CAN read it ✓
- But `ImageConversion.LoadImage()` is returning false ✗

**The Fix:** We needed to reference `UnityEngine.ImageConversionModule.dll` in the csproj!

**After adding the reference:**
```
[13:08:12.123] [DockExports] Loading embedded icon...
[13:08:12.124] [DockExports] Available embedded resources (1):
[13:08:12.125] [DockExports]   - S1DockExports.DE.png
[13:08:12.126] [DockExports] ✓ Found resource: S1DockExports.DE.png (65536 bytes)
[13:08:12.127] [DockExports] ✓ Read 65536 bytes from stream
[13:08:12.128] [DockExports] Created Texture2D, loading image...
[13:08:12.344] [DockExports] ✓ Image loaded: 256x256
[13:08:12.345] [DockExports] ✅ Icon sprite created successfully!
```

**Success!** The detailed logging helped us pinpoint the exact failure point.

---

## Log Reading Skills

### How to Parse Log Output

Logs can be overwhelming. Here's how to read them effectively:

#### 1. **Read Timestamps to Understand Sequence**

```
[10:15:30.123] [DockExports] Starting operation A
[10:15:30.456] [DockExports] Starting operation B
[10:15:31.789] [DockExports] Operation A completed  ← Took 1.6 seconds
[10:15:32.012] [DockExports] Operation B completed  ← Took 1.5 seconds
```

Operations A and B ran **concurrently** (overlapping).

#### 2. **Spot Errors vs Warnings**

```
[10:15:30.123] [DockExports] Attempting to load config...
[10:15:30.124] [DockExports] ⚠️ Config file not found, using defaults  ← Warning: not ideal but OK
[10:15:30.125] [DockExports] ✓ Config loaded
[10:15:30.126] [DockExports] Attempting to load save data...
[10:15:30.127] [DockExports] ❌ Save data corrupted!  ← Error: this is broken
```

Warnings are yellow flags. Errors are red flags.

#### 3. **Look for Missing Completion Logs**

```
[10:15:30.123] [DockExports] Starting complex operation...
[10:15:30.124] [DockExports] Step 1 complete
[10:15:30.125] [DockExports] Step 2 complete
[10:15:30.126] [DockExports] Step 3 starting...
[NOTHING AFTER THIS]
```

**Step 3 crashed!** The code never reached Step 4.

#### 4. **Trace Execution Flow**

Number your log messages to track execution order:

```csharp
MelonLogger.Msg("[DockExports] [1] Entering ProcessPayment()");
MelonLogger.Msg("[DockExports] [2] Validated shipment");
MelonLogger.Msg("[DockExports] [3] Calculated payout");
MelonLogger.Msg("[DockExports] [4] Added money to player");
MelonLogger.Msg("[DockExports] [5] Exiting ProcessPayment()");
```

If logs show `[1] → [2] → [5]`, steps 3-4 were skipped (maybe an early return?).

#### 5. **Search for Patterns**

Use Ctrl+F in the log file to search for:
- Your mod name: `[DockExports]`
- Error keywords: `Exception`, `Error`, `Failed`, `❌`
- Specific operations: `ProcessPayment`, `UnlockBroker`, etc.

---

## Advanced Logging Patterns

### Pattern 1: Throttled Logging (Prevent Spam)

**Problem:** Logging every frame creates thousands of duplicate messages.

**Solution:** Log only when values change or at intervals.

```csharp
private float lastLogTime = 0f;
private int lastLoggedRank = -1;

public override void OnUpdate()
{
    int currentRank = GetPlayerRank();

    // Method 1: Log only when value changes
    if (currentRank != lastLoggedRank)
    {
        MelonLogger.Msg($"[DockExports] Rank changed: {lastLoggedRank} → {currentRank}");
        lastLoggedRank = currentRank;
    }

    // Method 2: Log once per second maximum
    if (Time.time - lastLogTime >= 1.0f)
    {
        MelonLogger.Msg($"[DockExports] Current rank: {currentRank}");
        lastLogTime = Time.time;
    }
}
```

### Pattern 2: Conditional Logging (Debug Flags)

**Problem:** You want detailed logs during development but not in release.

**Solution:** Use a debug flag.

```csharp
public class DockExportsMod : MelonMod
{
    private const bool DEBUG_MODE = true; // Set to false for release

    private void DebugLog(string message)
    {
        if (DEBUG_MODE)
        {
            MelonLogger.Msg($"[DockExports] [DEBUG] {message}");
        }
    }

    public void SomeMethod()
    {
        DebugLog("Entering SomeMethod"); // Only logs if DEBUG_MODE = true

        // ... method code ...

        DebugLog("Exiting SomeMethod");
    }
}
```

### Pattern 3: Pretty-Printing Complex Objects

**Problem:** Logging objects directly shows useless type names.

```csharp
var shipment = GetActiveShipment();
MelonLogger.Msg($"Shipment: {shipment}");
// Output: Shipment: S1DockExports.ShipmentData
//         ↑ Useless!
```

**Solution:** Log individual properties.

```csharp
var shipment = GetActiveShipment();
MelonLogger.Msg("[DockExports] Active Shipment:");
MelonLogger.Msg($"  Type: {shipment.Type}");
MelonLogger.Msg($"  Quantity: {shipment.Quantity}");
MelonLogger.Msg($"  Unit Price: ${shipment.UnitPrice:N0}");
MelonLogger.Msg($"  Total Value: ${shipment.TotalValue:N0}");
MelonLogger.Msg($"  Total Paid: ${shipment.TotalPaid:N0}");
MelonLogger.Msg($"  Payments Made: {shipment.PaymentsMade}/4");
```

**Output:**
```
[DockExports] Active Shipment:
  Type: Consignment
  Quantity: 200
  Unit Price: $23,520
  Total Value: $4,704,000
  Total Paid: $2,352,000
  Payments Made: 2/4
```

Much better!

### Pattern 4: Logging Collections

**Problem:** How do you log arrays or lists?

**Solution:** Loop and log each item.

```csharp
var history = GetShipmentHistory();
MelonLogger.Msg($"[DockExports] Shipment History ({history.Count} entries):");

for (int i = 0; i < history.Count; i++)
{
    var entry = history[i];
    MelonLogger.Msg($"[DockExports]   [{i}] {entry.Type} | {entry.Quantity} bricks | ${entry.TotalPaid:N0}");
}
```

**Output:**
```
[DockExports] Shipment History (3 entries):
[DockExports]   [0] Wholesale | 100 bricks | $1,470,000
[DockExports]   [1] Consignment | 200 bricks | $4,010,000
[DockExports]   [2] Wholesale | 100 bricks | $1,470,000
```

### Pattern 5: Structured Logging (For Searching)

**Problem:** Hard to find specific events later.

**Solution:** Use consistent prefixes and structure.

```csharp
// Bad: Inconsistent formatting
MelonLogger.Msg("Payment of $1000");
MelonLogger.Msg("Player got 1000 dollars");
MelonLogger.Msg("Added money: 1000");

// Good: Consistent structure
MelonLogger.Msg("[DockExports] [PAYMENT] Added $1,000 to player");
MelonLogger.Msg("[DockExports] [PAYMENT] Added $2,500 to player");
MelonLogger.Msg("[DockExports] [PAYMENT] Added $1,176,000 to player");
```

Now you can search for `[PAYMENT]` to find all payment events!

**Common prefixes:**
- `[INIT]` - Initialization
- `[UI]` - UI operations
- `[SAVE]` - Save/load operations
- `[PAYMENT]` - Money transactions
- `[UNLOCK]` - Unlock events
- `[ERROR]` - Errors
- `[DEBUG]` - Debug-only messages

---

## Common Pitfalls & Solutions

### Pitfall 1: Logging Too Much

**Problem:**
```csharp
public override void OnUpdate() // Runs 60 times per second!
{
    MelonLogger.Msg($"[DockExports] Update frame, player position: {playerPos}");
}
```

**Result:** 3,600 log messages per minute. The log file becomes unusable.

**Solution:** Use throttled logging (see above) or only log when values change.

### Pitfall 2: Logging Too Little

**Problem:**
```csharp
public void ComplexOperation()
{
    // 50 lines of code with no logs

    MelonLogger.Msg("[DockExports] Operation complete");
}
```

**Result:** When it crashes, you have no idea where.

**Solution:** Add logs at key checkpoints throughout the method.

### Pitfall 3: Not Logging Exceptions Properly

**Problem:**
```csharp
try
{
    DangerousOperation();
}
catch (Exception ex)
{
    MelonLogger.Error("Something failed"); // No details!
}
```

**Result:** You know it failed but not why.

**Solution:** Always log the exception message and stack trace.

```csharp
catch (Exception ex)
{
    MelonLogger.Error($"[DockExports] ❌ Operation failed: {ex.Message}");
    MelonLogger.Error($"[DockExports] Stack trace: {ex.StackTrace}");
}
```

### Pitfall 4: Logging Sensitive Data

**Problem:**
```csharp
MelonLogger.Msg($"[DockExports] Player password: {password}");
```

**Result:** Sensitive data in log files that might be shared publicly.

**Solution:** Never log passwords, API keys, or personal information.

### Pitfall 5: Forgetting to Remove Debug Logs

**Problem:**
```csharp
MelonLogger.Msg("[DockExports] [DEBUG] Testing button click"); // Left in release
```

**Result:** Cluttered logs for end users.

**Solution:** Use the debug flag pattern (shown above) or remove before release.

### Pitfall 6: Not Using Descriptive Messages

**Problem:**
```csharp
MelonLogger.Msg("Here"); // Where is "here"?
MelonLogger.Msg("Value: 42"); // Which value?
MelonLogger.Msg("Done"); // What's done?
```

**Result:** Logs are cryptic and useless later.

**Solution:** Be specific.

```csharp
MelonLogger.Msg("[DockExports] Entering ProcessConsignmentPayment()");
MelonLogger.Msg($"[DockExports] Calculated expected payout: ${expectedPayout:N0}");
MelonLogger.Msg("[DockExports] ✓ Consignment payment processing complete");
```

---

## Quick Reference

### Log Level Cheat Sheet

| Log Level | When to Use | Example |
|-----------|-------------|---------|
| `MelonLogger.Msg()` | Normal operation, tracking, info | `"Player opened phone"` |
| `MelonLogger.Warning()` | Unexpected but handled | `"Config file missing, using defaults"` |
| `MelonLogger.Error()` | Something broke | `"Failed to load save data!"` |

### Essential Logging Locations

✅ **Always log these:**
- Mod initialization (`OnInitializeMelon`)
- Scene changes (`OnSceneWasLoaded`)
- Method entry/exit for critical operations
- Before/after game API calls
- Exception catch blocks
- Conditional branches (if/else outcomes)

❌ **Avoid logging in:**
- `OnUpdate()` without throttling (runs 60 FPS!)
- Tight loops without limits
- Every frame of an animation
- Third-party library internals

### Formatting Tips

```csharp
// Currency
int money = 1500000;
MelonLogger.Msg($"Amount: ${money:N0}"); // Output: Amount: $1,500,000

// Percentages
float percent = 0.25f;
MelonLogger.Msg($"Chance: {percent:P0}"); // Output: Chance: 25%

// Decimals
float price = 12.34567f;
MelonLogger.Msg($"Price: ${price:F2}"); // Output: Price: $12.35

// Booleans
bool isActive = true;
MelonLogger.Msg($"Active: {(isActive ? "✓" : "✗")}"); // Output: Active: ✓
```

### Emoji Guide (Optional but Helpful!)

```csharp
MelonLogger.Msg("📱 Phone event");
MelonLogger.Msg("💰 Money transaction");
MelonLogger.Msg("✅ Success");
MelonLogger.Error("❌ Failure");
MelonLogger.Warning("⚠️ Warning");
MelonLogger.Msg("🔍 Investigating...");
MelonLogger.Msg("💾 Save operation");
MelonLogger.Msg("🎮 Game event");
MelonLogger.Msg("🖼️ UI operation");
```

Makes logs easier to scan visually!

---

## Final Thoughts

**Logging is not optional for modding.** It's your primary tool for:
- Understanding how the game works
- Debugging when things break
- Communicating with yourself across debugging sessions
- Proving your code actually ran

Think of every log statement as **leaving notes for Future You** who's debugging at 2 AM and can't remember what Past You was thinking.

### The Golden Rule

**If you're not sure whether to log something, LOG IT.** You can always ignore logs, but you can't recover information you never logged.

### Next Steps

Now that you understand logging, practice by:

1. **Add logging to an existing method** in your mod
2. **Run the game** and observe what appears in the console
3. **Try breaking something intentionally** and see how logs help you find the problem
4. **Explore a new game system** using the patterns from "Reverse-Engineering with Logs"

Happy logging! 🎮📝

---

**See Also:**
- [MODDING_TUTORIAL.md](./MODDING_TUTORIAL.md) - Complete modding guide
- [ICON_LOADING_TUTORIAL.md](./ICON_LOADING_TUTORIAL.md) - Image loading specifics
- [PROJECT_STRUCTURE.md](./PROJECT_STRUCTURE.md) - Code organization

**Questions?** Add an issue to the GitHub repo or check the MelonLoader documentation.

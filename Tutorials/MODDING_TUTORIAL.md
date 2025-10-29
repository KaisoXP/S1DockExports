# The Complete Guide to Unity Il2Cpp Modding

> **From Zero to Hero: Learn to Mod Games Like "Schedule I"**

**Author:** Claude Code + KaisoXP
**Date:** October 25, 2025
**Project:** S1DockExports
**Audience:** Complete beginners (no game development or advanced programming knowledge assumed)

---

## Table of Contents

1. [What is Modding?](#what-is-modding)
2. [The Technology Stack](#the-technology-stack)
3. [How Mods Actually Work](#how-mods-actually-work)
4. [The Scientific Method for Modding](#the-scientific-method-for-modding)
5. [Setting Up Your Development Environment](#setting-up-your-development-environment)
6. [Your First Mod: "Hello World"](#your-first-mod-hello-world)
7. [Understanding the Mod Lifecycle](#understanding-the-mod-lifecycle)
8. [Harmony: Patching Game Code](#harmony-patching-game-code)
9. [Working with Unity GameObjects](#working-with-unity-gameobjects)
10. [Save Systems and Persistence](#save-systems-and-persistence)
11. [Building a Complete Feature](#building-a-complete-feature)
12. [Common Patterns and Recipes](#common-patterns-and-recipes)
13. [Debugging and Troubleshooting](#debugging-and-troubleshooting)
14. [Best Practices](#best-practices)
15. [Going Further](#going-further)

---

## What is Modding?

### The Simple Explanation

**Modding** = **Mod**ifying a game to add features, change behavior, or fix issues.

Think of a game like a house:

- The **game developers** built the house
- You (the **modder**) are adding new rooms, changing the wallpaper, or installing smart lighting
- The **game engine** (Unity) is like the house's foundation and structure
- **Mods** are your additions and changes

### Why Mod?

People mod games to:

- **Add features** they wish existed (like our Dock Exports system)
- **Fix bugs** or improve quality of life
- **Extend gameplay** with new content
- **Learn** how games work under the hood
- **Have fun** building something creative

### What Makes "Schedule I" Moddable?

"Schedule I" uses:

- **Unity** game engine (very common, well-understood)
- **Il2Cpp** compilation (converts C# to C++ for performance)
- **MelonLoader** (allows loading external code into the game)

This combination means we can:
✅ Load our custom C# code into the game
✅ Access game objects and systems
✅ Patch (modify) game methods at runtime
✅ Save custom data
✅ Add new UI elements

---

## The Technology Stack

Let's understand the **layers** of technology, from bottom to top:

```
┌─────────────────────────────────────────┐
│     YOUR MOD (S1DockExports.dll)        │  ← Your custom code
├─────────────────────────────────────────┤
│     S1API (Abstraction Layer)           │  ← Helper library
├─────────────────────────────────────────┤
│     Harmony (Patching Library)          │  ← Modifies game code
├─────────────────────────────────────────┤
│     MelonLoader (Mod Loader)            │  ← Injects mods into game
├─────────────────────────────────────────┤
│     Il2Cpp Runtime                      │  ← Manages C#/C++ interop
├─────────────────────────────────────────┤
│     Unity Engine                        │  ← Game engine
├─────────────────────────────────────────┤
│     Schedule I (Game Code)              │  ← The actual game
└─────────────────────────────────────────┘
```

Let's explain each layer using analogies:

### Layer 1: Schedule I (The Game)

**Analogy:** The restaurant's kitchen.

The game is a fully functional restaurant kitchen. You can't go in and rearrange it (it's closed-source), but you can observe what comes out and figure out how dishes are made.

### Layer 2: Unity Engine

**Analogy:** The building's infrastructure (plumbing, electrical, HVAC).

Unity provides:

- **GameObjects** (the "objects" in the game world)
- **Components** (behaviors attached to objects)
- **Scenes** (different areas/levels)
- **UI system** (buttons, text, images)
- **Physics, rendering, audio, etc.**

You use Unity's systems even though you're not Unity developers - just like you use a building's electricity without being an electrician.

### Layer 3: Il2Cpp Runtime

**Analogy:** A translator between two languages.

Unity games are written in C#, but Il2Cpp converts them to C++ for better performance. This creates a **bridge** between:

- **Managed world** (your C# mod code)
- **Unmanaged world** (the game's C++ code)

Sometimes you need special code to cross this bridge (like converting arrays).

### Layer 4: MelonLoader

**Analogy:** A backstage pass to the restaurant kitchen.

MelonLoader is a framework that:

- Launches the game with modifications
- Loads your mod DLLs into the game's memory
- Provides a console for log messages
- Manages mod initialization and lifecycle

**Without MelonLoader, your code can't run inside the game.**

### Layer 5: Harmony

**Analogy:** A GPS that reroutes traffic.

Harmony is a library that **patches methods at runtime**. It's like saying:

> "When the game tries to call function `OpenPhone()`, run my custom code first/after/instead."

This lets you:

- Hook into game events
- Modify behavior without changing game files
- Add new functionality to existing systems

### Layer 6: S1API

**Analogy:** A convenience toolkit.

S1API is a helper library created by the modding community that provides:

- Easy access to game systems (time, money, properties)
- UI building helpers
- Save system abstractions
- Phone app framework

**Think of it as a "mod developer's SDK"** (Software Development Kit).

### Layer 7: Your Mod

**Analogy:** Your custom recipes and additions to the restaurant.

This is your C# code that:

- Uses all the layers below
- Adds new features (like Dock Exports)
- Responds to game events
- Persists data across save/load

---

## How Mods Actually Work

### The Boot Sequence

Let's trace what happens from launching the game to your mod running:

![sequence Diagram Click "Play Schedule I](https://github.com/KaisoXP/S1DockExports/blob/main/Tutorials/MODDING_TUTORIAL_1.png "sequence Diagram Click "Play Schedule I")

**Key takeaways:**

1. MelonLoader runs **before** the game fully starts
2. Your mod initializes **during** game startup
3. Harmony patches are applied **before** game code runs
4. Your mod responds to **game events** (scenes loading, updates, etc.)

### The Runtime Loop

While the game is running:

![Game Flow Chart](https://github.com/KaisoXP/S1DockExports/blob/main/Tutorials/MODDING_TUTORIAL_2.png "Game Flow Chart")

Your mod is **always running in the background**, checking conditions and responding to events.  
**NOTICE:** This workflow is incorrect and expensive and redone in [Optimized_Architecture_CSharp_Guide](https://github.com/KaisoXP/S1DockExports/blob/main/Tutorials/Optimized_Architecture_CSharp_Guide.md)

### Harmony Patches: Code Interception

Here's how Harmony patches work:

**Without Harmony:**

```
Player clicks icon → Game's OpenApp() runs → App opens
```

**With Harmony patch:**

```
Player clicks icon
  ↓
Harmony intercepts the call
  ↓
Your [Prefix] code runs (before game code)
  ↓
Game's OpenApp() runs
  ↓
Your [Postfix] code runs (after game code)
  ↓
App opens
```

**Analogy:** Harmony is like a security guard who checks people before they enter a building, and logs them after they leave.

---

## The Scientific Method for Modding

When you mod a **closed-source game** (no source code access), you become a **scientist exploring unknown territory**.

### The Process

![Observe Game Behaviour](https://github.com/KaisoXP/S1DockExports/blob/main/Tutorials/MODDING_TUTORIAL_3.png "Observe Game Behaviour")

### Example: Finding Phone App Icons

**1. Observe:**

- The game has a phone with app icons
- Icons appear when you open the phone
- Icons are clickable

**2. Hypothesis:**

- "Icons must be GameObjects in the scene"
- "They're probably children of some parent container"

**3. Experiment Design:**

- Patch the phone's initialization method
- Log all GameObjects in the scene hierarchy
- Look for containers with multiple children

**4. Write Code:**

```csharp
[HarmonyPatch(typeof(HomeScreen), "Start")]
[HarmonyPostfix]
public static void ExploreHomeScreen(HomeScreen __instance)
{
    MelonLogger.Msg("[DockExports] 📱 Exploring HomeScreen...");

    var transform = __instance.transform;
    MelonLogger.Msg($"HomeScreen has {transform.childCount} children:");

    for (int i = 0; i < transform.childCount; i++)
    {
        var child = transform.GetChild(i);
        MelonLogger.Msg($"  Child {i}: {child.name} ({child.childCount} children)");
    }
}
```

**5. Run & Read Logs:**

```
[DockExports] 📱 Exploring HomeScreen...
[DockExports] HomeScreen has 5 children:
[DockExports]   Child 0: Panel (1 children)
[DockExports]   Child 1: StatusBar (3 children)
[DockExports]   Child 2: Grid (7 children)  ← Found it!
```

**6. Analyze:**

- "Grid" has 7 children (matches the 7 apps!)
- This is probably the icon container

**7. Success!**

- Hypothesis confirmed
- Document the finding
- Use this knowledge to inject our custom icon

### Key Mindset Shifts

❌ **Don't think:** "I need the documentation to tell me how this works"
✅ **Do think:** "I'll use logging to discover how this works"

❌ **Don't think:** "This should work" (hope-driven development)
✅ **Do think:** "Let me verify this works" (evidence-driven development)

❌ **Don't think:** "It crashed, I give up"
✅ **Do think:** "It crashed, what do the logs tell me about where?"

---

## Setting Up Your Development Environment

### Required Software

1. **Visual Studio Code** or **Visual Studio 2022**

   - IDE for writing C# code
   - Free: https://code.visualstudio.com/

2. **.NET SDK 7.0+**

   - Required to compile C# projects
   - Free: https://dotnet.microsoft.com/download

3. **MelonLoader**

   - Install into your game directory
   - Instructions: https://melonwiki.xyz/#/

4. **Schedule I** (the game, obviously!)
   - Must own on Steam

### Project Setup

1. **Clone or create a mod template:**

```bash
git clone https://github.com/KaisoXP/S1DockExports.git
cd S1DockExports
```

2. **Set your game path** (if not default location):

```bash
# Windows PowerShell
$env:S1_GAME="D:\SteamLibrary\steamapps\common\Schedule I"

# Or edit S1DockExports.csproj directly
<GamePath>YOUR_PATH_HERE</GamePath>
```

3. **Build the project:**

```bash
dotnet build -c Il2cpp
```

4. **Verify build output:**

```
C:\Program Files (x86)\Steam\steamapps\common\Schedule I\
    Mods\
        S1DockExports_Il2cpp.dll  ← Your mod!
        S1API.dll
        Il2CppFishNet.Runtime.dll
```

5. **Launch the game:**

- A console window should open (MelonLoader)
- Check for `[DockExports] Mod initialized` in the logs

---

## Your First Mod: "Hello World"

Let's create the simplest possible mod to verify everything works.

### Step 1: Create the Mod File

Create `HelloWorldMod.cs`:

```csharp
using MelonLoader;

// Tell MelonLoader about your mod
[assembly: MelonInfo(typeof(HelloWorldMod.HelloMod), "HelloWorld", "1.0.0", "YourName")]
[assembly: MelonGame("TVGS", "Schedule I")] // Must match game

namespace HelloWorldMod
{
    public class HelloMod : MelonMod
    {
        // Called when mod is first loaded
        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("🎮 Hello World! My first mod is loaded!");
        }

        // Called every frame (60 times per second)
        public override void OnUpdate()
        {
            // Check if player presses F5
            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.F5))
            {
                MelonLogger.Msg("🎉 You pressed F5! The mod detected it!");
            }
        }

        // Called when game is closing
        public override void OnApplicationQuit()
        {
            MelonLogger.Msg("👋 Goodbye from HelloWorld mod!");
        }
    }
}
```

### Step 2: Build and Test

```bash
dotnet build
```

### Step 3: Launch Game

When the game starts with MelonLoader, you should see:

```
[10:15:32.123] [HelloWorld] 🎮 Hello World! My first mod is loaded!
```

Press **F5** in-game:

```
[10:16:45.678] [HelloWorld] 🎉 You pressed F5! The mod detected it!
```

**Congratulations!** You just:

- Created a mod from scratch
- Detected game initialization
- Responded to player input
- Logged messages to the console

This proves your entire development environment works!

---

## Understanding the Mod Lifecycle

Your mod has several "lifecycle methods" that MelonLoader calls at specific times:

![Game Start](https://github.com/KaisoXP/S1DockExports/blob/main/Tutorials/MODDING_TUTORIAL_4.png "Game Start")

### Key Methods Explained

#### `OnApplicationStart()`

**When:** Very first thing, before game fully initializes
**Use for:** One-time setup that doesn't depend on game state
**Example:**

```csharp
public override void OnApplicationStart()
{
    MelonLogger.Msg("Mod is loading before game starts");
    // Register phone apps, set up Harmony patches, etc.
}
```

#### `OnInitializeMelon()`

**When:** After `OnApplicationStart()`, when your mod is fully loaded
**Use for:** Main initialization logic
**Example:**

```csharp
public override void OnInitializeMelon()
{
    Instance = this;
    MelonLogger.Msg("Mod initialized and ready");

    // Initialize systems
    ShipmentManager.Initialize();
}
```

#### `OnSceneWasInitialized(string sceneName)`

**When:** A new scene is about to be loaded
**Use for:** Preparing for scene load, clearing data
**Example:**

```csharp
public override void OnSceneWasInitialized(int buildIndex, string sceneName)
{
    MelonLogger.Msg($"Scene initializing: {sceneName}");

    if (sceneName == "Menu")
    {
        // Clear data when going back to menu
        ClearSessionData();
    }
}
```

#### `OnSceneWasLoaded(string sceneName)`

**When:** A new scene has fully loaded
**Use for:** Accessing scene GameObjects, injecting UI
**Example:**

```csharp
public override void OnSceneWasLoaded(int buildIndex, string sceneName)
{
    MelonLogger.Msg($"Scene loaded: {sceneName}");

    if (sceneName == "Main")
    {
        // Main game scene loaded, inject our phone app
        InjectPhoneApp();
    }
}
```

#### `OnUpdate()`

**When:** Every frame (60 times per second)
**Use for:** Checking conditions, polling state
**⚠️ WARNING:** Runs very frequently! Use throttling!
**Example:**

```csharp
private float lastCheck = 0f;

public override void OnUpdate()
{
    // Only check once per second, not 60 times!
    if (Time.time - lastCheck >= 1.0f)
    {
        CheckUnlockConditions();
        lastCheck = Time.time;
    }
}
```

#### `OnLateUpdate()`

**When:** After all `OnUpdate()` calls finish
**Use for:** Operations that need to run after game logic updates
**Example:**

```csharp
public override void OnLateUpdate()
{
    // Update UI to reflect changes made in OnUpdate
    UpdateUIIfNeeded();
}
```

#### `OnFixedUpdate()`

**When:** Fixed time intervals (default: 50 times per second)
**Use for:** Physics-related code (rare in UI mods)
**Example:**

```csharp
public override void OnFixedUpdate()
{
    // Physics calculations here
}
```

#### `OnApplicationQuit()`

**When:** Game is closing
**Use for:** Cleanup, final saves
**Example:**

```csharp
public override void OnApplicationQuit()
{
    MelonLogger.Msg("Saving data before quit...");
    SaveData();
    Instance = null;
}
```

---

## Harmony: Patching Game Code

Harmony is your **secret weapon** for modding. It lets you hook into the game's code without modifying the game files.

### The Three Types of Patches

#### 1. Prefix (Before)

Runs **before** the game's method.

**Use cases:**

- Prevent a method from running
- Modify method parameters
- Run setup code

**Example:**

```csharp
[HarmonyPatch(typeof(Phone), "Open")]
[HarmonyPrefix]
public static bool BeforePhoneOpens(Phone __instance)
{
    MelonLogger.Msg("[Mod] Phone is about to open");

    // Return true = continue to original method
    // Return false = skip original method entirely
    return true;
}
```

**Diagram:**

```
Player clicks phone icon
  ↓
[YOUR PREFIX RUNS HERE] ← Can cancel the call!
  ↓
Game's Phone.Open() method runs
  ↓
Phone opens
```

#### 2. Postfix (After)

Runs **after** the game's method.

**Use cases:**

- React to method completion
- Modify return values
- Log results

**Example:**

```csharp
[HarmonyPatch(typeof(Phone), "Open")]
[HarmonyPostfix]
public static void AfterPhoneOpens(Phone __instance)
{
    MelonLogger.Msg("[Mod] Phone just opened, injecting our app icon");
    InjectCustomAppIcon();
}
```

**Diagram:**

```
Player clicks phone icon
  ↓
Game's Phone.Open() method runs
  ↓
[YOUR POSTFIX RUNS HERE]
  ↓
Phone opens with your additions
```

#### 3. Transpiler (During)

**Advanced:** Modifies the **IL code** (instructions) of the method.

**Use cases:**

- Change method logic internally
- Very advanced, rarely needed

**We won't cover this** (95% of mods never need it).

### Anatomy of a Harmony Patch

Let's break down a real patch from S1DockExports:

```csharp
[HarmonyPatch]  // 1. Tells Harmony this class contains patches
public static class PhoneAppInjector
{
    // 2. Target the method to patch
    [HarmonyPatch(typeof(HomeScreen), "Start")]
    // 3. Specify patch type
    [HarmonyPostfix]
    // 4. The patch method
    public static void InjectAppIcon(HomeScreen __instance)
    {
        // __instance = the HomeScreen object the game is working with

        MelonLogger.Msg("HomeScreen.Start() just ran, adding our icon");

        // Access the HomeScreen's transform
        var transform = __instance.transform;

        // Find icon container and add our icon
        // ... implementation ...
    }
}
```

**Breakdown:**

1. `[HarmonyPatch]` - Marks the class as containing patches
2. `[HarmonyPatch(typeof(HomeScreen), "Start")]` - Target `HomeScreen.Start()` method
3. `[HarmonyPostfix]` - Run **after** `Start()` completes
4. `__instance` parameter - The `HomeScreen` object the method was called on

### Special Parameter Names

Harmony uses **special parameter names** to pass you information:

| Parameter                | What It Is                                   |
| ------------------------ | -------------------------------------------- |
| `__instance`             | The object the method was called on (`this`) |
| `__result`               | The return value (for Postfix patches)       |
| `__0`, `__1`, `__2`, ... | Method parameters by index                   |
| Any other name           | Match method parameter names exactly         |

**Example:**

```csharp
// Game has: public void ProcessPayment(int amount, string reason)

[HarmonyPatch(typeof(Bank), "ProcessPayment")]
[HarmonyPrefix]
public static void BeforePayment(
    Bank __instance,     // The Bank object
    int amount,          // First parameter (matched by name)
    string reason        // Second parameter (matched by name)
)
{
    MelonLogger.Msg($"Processing ${amount} for: {reason}");
}
```

### Finding Methods to Patch

**How do you know what methods exist if the game is closed-source?**

1. **Use dnSpy or ILSpy** (decompilers)

   - Open `Assembly-CSharp.dll` from game directory
   - Browse classes and methods
   - See method signatures

2. **Look at S1API source code**

   - It already figured out many useful methods

3. **Experiment with logging**
   - Patch a class's constructor
   - Log all method calls in that class

**Example exploration:**

```csharp
[HarmonyPatch(typeof(HomeScreen), "Start")]
[HarmonyPostfix]
public static void LogHomeScreenMethods(HomeScreen __instance)
{
    var methods = typeof(HomeScreen).GetMethods();
    MelonLogger.Msg($"HomeScreen has {methods.Length} methods:");

    foreach (var method in methods)
    {
        MelonLogger.Msg($"  - {method.Name}");
    }
}
```

---

## Working with Unity GameObjects

Unity games are built from **GameObjects** - think of them as LEGO bricks that make up the game world.

### GameObject Hierarchy

GameObjects are organized in a **tree structure** (like folders on your computer):

```
Scene
├── HomeScreen (GameObject)
│   ├── Panel (child GameObject)
│   ├── StatusBar (child)
│   │   ├── TimeText (grandchild)
│   │   ├── BatteryIcon (grandchild)
│   │   └── SignalIcon (grandchild)
│   ├── Grid (child)
│   │   ├── Icon_Messages (grandchild)
│   │   ├── Icon_Contacts (grandchild)
│   │   └── Icon_Bank (grandchild)
│   └── Footer (child)
```

### Finding GameObjects

#### Method 1: By Name (Simple but Slow)

```csharp
// Find anywhere in scene
GameObject phone = GameObject.Find("PhoneObject");

if (phone != null)
{
    MelonLogger.Msg($"Found phone: {phone.name}");
}
else
{
    MelonLogger.Warning("Phone object not found!");
}
```

**⚠️ WARNING:** This searches the entire scene and is slow! Don't use in `OnUpdate()`.

#### Method 2: Through Hierarchy (Fast and Reliable)

```csharp
// Start from a known object (from Harmony patch)
Transform homeScreen = __instance.transform;

// Get child by index
Transform firstChild = homeScreen.GetChild(0);

// Get child by name (only searches direct children)
Transform statusBar = homeScreen.Find("StatusBar");

// Iterate all children
for (int i = 0; i < homeScreen.childCount; i++)
{
    Transform child = homeScreen.GetChild(i);
    MelonLogger.Msg($"Child {i}: {child.name}");
}
```

#### Method 3: By Component (Find Objects with Specific Component)

```csharp
// Find all objects with Button component
Button[] allButtons = GameObject.FindObjectsOfType<Button>();

MelonLogger.Msg($"Found {allButtons.Length} buttons in scene");

foreach (var button in allButtons)
{
    MelonLogger.Msg($"  Button on: {button.gameObject.name}");
}
```

### Components: GameObject Behaviors

Every GameObject has **components** attached that define what it does:

```csharp
GameObject icon = ...; // Some icon GameObject

// Get a component
UnityEngine.UI.Image imageComponent = icon.GetComponent<Image>();
if (imageComponent != null)
{
    MelonLogger.Msg($"Icon has an Image component");
    // Modify the image
    imageComponent.sprite = myCustomSprite;
}

// Get component in children (recursive search)
Button button = icon.GetComponentInChildren<Button>();
if (button != null)
{
    button.onClick.AddListener(() => {
        MelonLogger.Msg("Icon clicked!");
    });
}

// Get ALL components
Component[] allComponents = icon.GetComponents<Component>();
MelonLogger.Msg($"Icon has {allComponents.Length} components:");
foreach (var comp in allComponents)
{
    MelonLogger.Msg($"  - {comp.GetType().Name}");
}
```

### Common Unity Components

| Component       | Purpose                   | Common Operations                                 |
| --------------- | ------------------------- | ------------------------------------------------- |
| `Transform`     | Position, rotation, scale | `transform.position`, `GetChild()`, `SetParent()` |
| `RectTransform` | UI positioning            | `anchorMin`, `anchorMax`, `sizeDelta`             |
| `Image`         | Display sprites           | `sprite`, `color`                                 |
| `Text`          | Display text              | `text`, `fontSize`, `color`                       |
| `Button`        | Clickable UI              | `onClick.AddListener()`                           |
| `ScrollRect`    | Scrollable area           | `verticalScrollbar`, `content`                    |
| `Canvas`        | UI rendering root         | `renderMode`                                      |

### Creating New GameObjects

```csharp
// Create empty GameObject
GameObject myObject = new GameObject("MyCustomObject");

// Add a component
var image = myObject.AddComponent<Image>();
image.sprite = myCustomSprite;

// Set parent (puts it in hierarchy)
myObject.transform.SetParent(parentTransform, false);
// false = don't preserve world position (use parent's coordinate space)

// Position it
var rectTransform = myObject.GetComponent<RectTransform>();
rectTransform.anchoredPosition = new Vector2(100, 50);

// Make sure it's visible
myObject.SetActive(true);
```

### Cloning Existing GameObjects

**Best practice:** Clone an existing game object as a template.

```csharp
// Get a template object
GameObject templateIcon = iconContainer.GetChild(0).gameObject;

// Clone it
GameObject ourIcon = UnityEngine.Object.Instantiate(
    templateIcon,      // What to clone
    iconContainer,     // Where to put it (parent)
    false              // Don't preserve world position
);

// Rename it
ourIcon.name = "CustomDockExportsIcon";

// Now modify it
var image = ourIcon.GetComponentInChildren<Image>();
image.sprite = myCustomSprite;
```

**Why clone?**

- Keeps the same structure
- Inherits all components
- Already properly configured
- Much easier than building from scratch

---

## Save Systems and Persistence

Your mod needs to **remember data** across:

- Game restarts
- Save/load
- Scene changes

### S1API Saveable System

S1API provides a `Saveable` base class that handles saving/loading automatically.

**How it works:**

1. Extend `Saveable` class
2. Mark fields with `[SaveableField]` attribute
3. S1API automatically serializes them to JSON
4. Data is saved alongside game saves

### Example: ShipmentManager

```csharp
using S1API.Saveables;

public class ShipmentManager : Saveable
{
    // This field will be automatically saved!
    [SaveableField("ActiveShipment")]
    private ShipmentData? _activeShipment = null;

    [SaveableField("ShipmentHistory")]
    private List<ShipmentHistoryEntry> _history = new List<ShipmentHistoryEntry>();

    [SaveableField("LastProcessedDay")]
    private int _lastProcessedDay = -1;

    // Singleton instance
    public static ShipmentManager Instance { get; private set; } = new ShipmentManager();

    // Called when data is loaded from save
    protected override void OnLoaded()
    {
        Instance = this;
        MelonLogger.Msg("📂 Shipments loaded from save");

        // Log what was loaded
        string activeInfo = _activeShipment.HasValue
            ? $"{_activeShipment.Value.Type}"
            : "none";
        MelonLogger.Msg($"Active: {activeInfo}, History: {_history.Count} entries");
    }

    // Called when creating a new save
    protected override void OnCreated()
    {
        Instance = this;
        MelonLogger.Msg("📂 ShipmentManager created (new save)");
    }

    // Regular methods work as normal
    public void CreateNewShipment(int quantity)
    {
        _activeShipment = new ShipmentData
        {
            Quantity = quantity,
            // ... other fields ...
        };

        // Trigger a save
        Saveable.RequestGameSave(immediate: true);
    }
}
```

### Data Structures for Saving

Use **simple, serializable types**:

✅ **Works:**

- Primitives: `int`, `float`, `bool`, `string`
- Structs with simple fields
- Lists and arrays of simple types
- Nullable types: `int?`, `ShipmentData?`

❌ **Doesn't Work:**

- Unity types: `GameObject`, `Transform`, `Sprite`
- Complex classes with circular references
- Delegates/events

**Example struct:**

```csharp
[Serializable]  // Required for JSON serialization
public struct ShipmentData
{
    public ShipmentType Type;    // Enum is fine
    public int Quantity;         // Primitive is fine
    public int TotalValue;       // Primitive is fine
    public int CreatedDay;       // Primitive is fine
    // Don't add: public GameObject UI; ← This would break!
}

public enum ShipmentType
{
    Wholesale,
    Consignment
}
```

### Requesting Saves

```csharp
// After modifying saveable data
Saveable.RequestGameSave(immediate: true);
```

**When to save:**

- After creating a shipment
- After processing a payment
- After unlocking a feature
- After any important state change

### Debugging Save Data

Want to see what's being saved?

```csharp
protected override void OnLoaded()
{
    MelonLogger.Msg("=== LOADED SAVE DATA ===");
    MelonLogger.Msg($"Active Shipment: {_activeShipment}");
    MelonLogger.Msg($"History Count: {_history.Count}");

    foreach (var entry in _history)
    {
        MelonLogger.Msg($"  - {entry.Type}: {entry.Quantity} bricks, ${entry.TotalPaid}");
    }
    MelonLogger.Msg("========================");
}
```

---

## Building a Complete Feature

Let's walk through building a complete feature **from scratch**: detecting when it's Friday and processing a weekly payout.

### Step 1: Understand the Goal

**Feature:** Every in-game Friday, process a consignment payment with a chance of loss.

**Requirements:**

- Detect when it's Friday
- Only process once per Friday (not 60 times per second!)
- Roll for loss (25% chance)
- Calculate actual payout
- Add money to player
- Send a broker message

### Step 2: Research (The Scientific Method)

**What we need to find:**

1. How to detect the current day of the week
2. How to add money to the player
3. How to send messages

**Research approach:**

```csharp
// Experiment 1: Find the TimeManager
[HarmonyPatch(typeof(TimeManager), "Awake")]
[HarmonyPostfix]
public static void ExploreTimeManager(TimeManager __instance)
{
    MelonLogger.Msg("🕐 TimeManager initialized, exploring...");

    // Try different properties
    try
    {
        MelonLogger.Msg($"DayIndex: {__instance.DayIndex}");
    }
    catch (Exception ex)
    {
        MelonLogger.Error($"DayIndex failed: {ex.Message}");
    }

    // ... test other properties ...
}
```

**Discovery:** `TimeManager.DayIndex` exists and increments each day!

### Step 3: Design the Solution

![Design the Solution](https://github.com/KaisoXP/S1DockExports/blob/main/Tutorials/MODDING_TUTORIAL_5.png "Design the Solution")

### Step 4: Implement (With Logging!)

```csharp
public class DockExportsMod : MelonMod
{
    private int lastProcessedDay = -1;
    private float lastCheckTime = 0f;

    public override void OnUpdate()
    {
        // Throttle to once per second
        if (Time.time - lastCheckTime < 1.0f)
            return;

        lastCheckTime = Time.time;

        // Check if it's Friday and we should process
        if (IsFriday() && ShouldProcessPayout())
        {
            MelonLogger.Msg("[DockExports] 📅 It's Friday and we have an active consignment!");
            ProcessConsignmentWeek();
        }
    }

    private bool IsFriday()
    {
        try
        {
            var timeManager = NetworkSingleton<TimeManager>.Instance;
            int dayOfWeek = timeManager.DayIndex % 7;
            return dayOfWeek == 4; // Friday = 4 (0=Monday)
        }
        catch (Exception ex)
        {
            MelonLogger.Error($"[DockExports] Failed to check day: {ex.Message}");
            return false;
        }
    }

    private bool ShouldProcessPayout()
    {
        // Get current day
        int currentDay = GetElapsedDays();

        // Already processed today?
        if (currentDay == lastProcessedDay)
        {
            return false; // Already did it today
        }

        // Is there an active consignment?
        var active = ShipmentManager.Instance.ActiveShipment;
        if (!active.HasValue || active.Value.Type != ShipmentType.Consignment)
        {
            return false; // No active consignment
        }

        return true; // All conditions met!
    }

    private void ProcessConsignmentWeek()
    {
        MelonLogger.Msg("[DockExports] 💰 Processing Friday consignment payout...");

        // Get shipment info
        var shipment = ShipmentManager.Instance.ActiveShipment.Value;
        int weekNumber = shipment.PaymentsMade + 1;
        int expectedPayout = shipment.TotalValue / 4; // 25% of total

        MelonLogger.Msg($"[DockExports] Week {weekNumber}/4, expected payout: ${expectedPayout:N0}");

        // Roll for loss (25% chance)
        int lossPercent = 0;
        if (UnityEngine.Random.value < 0.25f) // 25% chance
        {
            lossPercent = UnityEngine.Random.Range(15, 61); // 15-60%
            MelonLogger.Warning($"[DockExports] ⚠️ Loss event! {lossPercent}% lost");
        }
        else
        {
            MelonLogger.Msg("[DockExports] ✓ No losses this week");
        }

        // Calculate actual payout
        int actualPayout = (int)(expectedPayout * (1f - lossPercent / 100f));
        MelonLogger.Msg($"[DockExports] Actual payout: ${actualPayout:N0}");

        // Update shipment
        ShipmentManager.Instance.ProcessConsignmentPayment(out int _);

        // Mark as processed
        int currentDay = GetElapsedDays();
        lastProcessedDay = currentDay;

        // Add money to player
        AddMoneyToPlayer(actualPayout);

        // Send broker message
        string message = lossPercent > 0
            ? $"Week {weekNumber}: Customs flagged a container. {lossPercent}% lost. Received ${actualPayout:N0} instead of ${expectedPayout:N0}."
            : $"Week {weekNumber}: Shipment arrived clean. ${actualPayout:N0} released.";

        SendBrokerMessage(message);

        // Save
        Saveable.RequestGameSave(true);

        MelonLogger.Msg("[DockExports] ✅ Friday payout complete");
    }

    private void AddMoneyToPlayer(int amount)
    {
        MelonLogger.Msg($"[DockExports] 💵 Adding ${amount:N0} to player");

        try
        {
            S1API.Money.Money.ChangeCashBalance(amount, visualizeChange: true, playCashSound: true);
            MelonLogger.Msg("[DockExports] ✓ Money added");
        }
        catch (Exception ex)
        {
            MelonLogger.Error($"[DockExports] ❌ Failed to add money: {ex.Message}");
        }
    }

    private void SendBrokerMessage(string message)
    {
        MelonLogger.Msg($"[DockExports] 📞 Broker message: {message}");
        // TODO: Implement actual in-game message when API is available
    }

    private int GetElapsedDays()
    {
        try
        {
            return NetworkSingleton<TimeManager>.Instance.DayIndex;
        }
        catch
        {
            return 0;
        }
    }
}
```

### Step 5: Test

**Test cases:**

1. ✅ Non-Friday: Log should show checks but no processing
2. ✅ Friday with no active shipment: No processing
3. ✅ Friday with active consignment: Process payment
4. ✅ Process only once: Second check should skip (already processed)
5. ✅ Next Friday: Should process again

**Test logs:**

```
[10:15:30.123] [DockExports] 📅 Current day: 0 (Monday), not Friday
[10:15:31.456] [DockExports] 📅 Current day: 1 (Tuesday), not Friday
...
[10:18:45.789] [DockExports] 📅 Current day: 4 (Friday), checking shipments
[10:18:45.790] [DockExports] 📅 It's Friday and we have an active consignment!
[10:18:45.791] [DockExports] 💰 Processing Friday consignment payout...
[10:18:45.792] [DockExports] Week 1/4, expected payout: $1,176,000
[10:18:45.793] [DockExports] ✓ No losses this week
[10:18:45.794] [DockExports] Actual payout: $1,176,000
[10:18:45.795] [DockExports] 💵 Adding $1,176,000 to player
[10:18:45.796] [DockExports] ✓ Money added
[10:18:45.797] [DockExports] 📞 Broker message: Week 1: Shipment arrived clean. $1,176,000 released.
[10:18:45.798] [DockExports] ✅ Friday payout complete
```

**Success!** The feature works end-to-end.

### Step 6: Refine

Based on testing, we might add:

- Better error handling
- Visual feedback (UI notification)
- Sound effects
- Achievements/stats tracking

---

## Common Patterns and Recipes

Here are reusable patterns you'll use frequently:

### Pattern 1: Singleton Instance

**Purpose:** Only one instance of your manager should exist.

```csharp
public class MyManager
{
    // Singleton instance
    public static MyManager Instance { get; private set; }

    public MyManager()
    {
        Instance = this;
    }

    public void DoSomething()
    {
        // ...
    }
}

// Usage anywhere:
MyManager.Instance.DoSomething();
```

### Pattern 2: Event System

**Purpose:** Notify other code when something happens.

```csharp
public class ShipmentManager : Saveable
{
    // Define an event
    public static event Action? OnShipmentsLoaded;

    protected override void OnLoaded()
    {
        // Trigger the event
        OnShipmentsLoaded?.Invoke();
    }
}

// Subscribe to the event:
public class DockExportsApp : PhoneApp
{
    protected override void OnCreated()
    {
        // Subscribe
        ShipmentManager.OnShipmentsLoaded += OnShipmentsLoaded;
    }

    protected override void OnDestroyed()
    {
        // Unsubscribe (important!)
        ShipmentManager.OnShipmentsLoaded -= OnShipmentsLoaded;
    }

    private void OnShipmentsLoaded()
    {
        MelonLogger.Msg("Shipments loaded, refreshing UI");
        RefreshUI();
    }
}
```

### Pattern 3: UI Factory

**Purpose:** Create UI elements programmatically.

```csharp
// Create a button
var (buttonGameObject, buttonComponent, textComponent) =
    UIFactory.RoundedButtonWithLabel(
        name: "MyButton",
        label: "Click Me",
        parent: parentTransform,
        bgColor: new Color(0.3f, 0.5f, 0.7f),
        width: 200,
        height: 40,
        fontSize: 14,
        textColor: Color.white
    );

// Add click handler
ButtonUtils.AddListener(buttonComponent, () => {
    MelonLogger.Msg("Button clicked!");
});
```

### Pattern 4: Conditional Compilation

**Purpose:** Different code for Mono vs Il2Cpp builds.

```csharp
#if IL2CPP
using Il2CppScheduleOne;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
#else
using ScheduleOne;
#endif

public void LoadImage(byte[] data)
{
#if IL2CPP
    // Il2Cpp needs array conversion
    var il2cppArray = new Il2CppStructArray<byte>(data);
    ImageConversion.LoadImage(texture, il2cppArray);
#else
    // Mono can use byte[] directly
    ImageConversion.LoadImage(texture, data);
#endif
}
```

### Pattern 5: Try-Catch with Logging

**Purpose:** Gracefully handle errors.

```csharp
public void RiskyOperation()
{
    try
    {
        MelonLogger.Msg("[Mod] Starting risky operation");

        // Risky code here
        var result = GameAPI.DoSomethingDangerous();

        MelonLogger.Msg($"[Mod] ✓ Operation succeeded: {result}");
    }
    catch (Exception ex)
    {
        MelonLogger.Error($"[Mod] ❌ Operation failed: {ex.Message}");
        MelonLogger.Error($"[Mod] Stack trace: {ex.StackTrace}");

        // Optionally: fallback behavior
        UseFallback();
    }
}
```

---

## Debugging and Troubleshooting

### Common Issues and Solutions

#### Issue: Mod Doesn't Load

**Symptoms:**

- No `[YourMod]` messages in console
- MelonLoader doesn't mention your mod

**Causes:**

1. DLL not in `Mods/` folder
2. Wrong MelonInfo attribute
3. Build failed

**Solution:**

```bash
# Check build output
dotnet build -c Il2cpp

# Verify DLL exists
dir "C:\Program Files (x86)\Steam\steamapps\common\Schedule I\Mods"

# Check MelonLoader logs for errors
type "C:\Program Files (x86)\Steam\steamapps\common\Schedule I\MelonLoader\Latest.log"
```

#### Issue: NullReferenceException

**Symptoms:**

```
NullReferenceException: Object reference not set to an instance of an object
```

**Cause:** Trying to use an object that doesn't exist.

**Solution:** Check for null before using:

```csharp
// Bad
var timeManager = NetworkSingleton<TimeManager>.Instance;
int day = timeManager.DayIndex; // Crashes if timeManager is null!

// Good
if (NetworkSingleton<TimeManager>.InstanceExists)
{
    var timeManager = NetworkSingleton<TimeManager>.Instance;
    int day = timeManager.DayIndex;
}
else
{
    MelonLogger.Warning("[Mod] TimeManager not available yet");
}
```

#### Issue: TypeLoadException

**Symptoms:**

```
TypeLoadException: Could not load type 'SomeType' from assembly 'Assembly-CSharp'
```

**Cause:** Missing assembly reference or wrong build configuration.

**Solution:**

1. Make sure you're building with `Il2cpp` configuration (not CrossCompat)
2. Add required DLL reference to `.csproj`

```xml
<ItemGroup Condition="'$(Configuration)'=='Il2cpp'">
  <Reference Include="Assembly-CSharp">
    <HintPath>$(Il2CppAssembliesPath)\Assembly-CSharp.dll</HintPath>
  </Reference>
</ItemGroup>
```

#### Issue: Harmony Patch Not Running

**Symptoms:**

- Logs before patch appear
- Logs inside patch method don't appear

**Causes:**

1. Wrong class/method name in `[HarmonyPatch]`
2. Wrong parameter types
3. Method doesn't exist in game

**Solution:**

```csharp
// Add logging to verify patch application
[HarmonyPatch(typeof(HomeScreen), "Start")]
[HarmonyPostfix]
public static void MyPatch(HomeScreen __instance)
{
    // First line: prove the patch ran
    MelonLogger.Msg("✅ PATCH EXECUTED: HomeScreen.Start");

    // Rest of code...
}
```

If you never see "✅ PATCH EXECUTED", the patch isn't being applied. Check:

- Class name spelling: `typeof(HomeScreen)` must match exactly
- Method name spelling: `"Start"` must match exactly (case-sensitive)
- Use dnSpy/ILSpy to verify method exists

---

## Best Practices

### Do's ✅

1. **Log extensively during development**

   - You can always remove logs later
   - Future You will thank Past You

2. **Use constants for magic numbers**

   ```csharp
   // Bad
   if (rank >= 3)  // What is 3?

   // Good
   private const int HUSTLER_RANK = 3;
   if (rank >= HUSTLER_RANK)
   ```

3. **Check for null before using objects**

   ```csharp
   if (instance != null)
   {
       instance.DoSomething();
   }
   ```

4. **Throttle high-frequency operations**

   ```csharp
   private float lastCheck = 0f;

   public override void OnUpdate()
   {
       if (Time.time - lastCheck < 1.0f)
           return;

       lastCheck = Time.time;
       // ... check stuff ...
   }
   ```

5. **Unsubscribe from events**

   ```csharp
   protected override void OnDestroyed()
   {
       SomeEvent -= Handler; // Prevent memory leaks!
   }
   ```

6. **Use meaningful names**

   ```csharp
   // Bad
   int x = GetData();

   // Good
   int playerRank = GetPlayerRank();
   ```

### Don'ts ❌

1. **Don't log every frame without throttling**

   ```csharp
   // This creates 3,600 logs per minute!
   public override void OnUpdate()
   {
       MelonLogger.Msg("Update frame"); // BAD!
   }
   ```

2. **Don't ignore exceptions**

   ```csharp
   try
   {
       DoSomething();
   }
   catch
   {
       // Silent failure = debugging nightmare!
   }
   ```

3. **Don't modify game files**

   - Use Harmony patches instead
   - Game updates will break your changes

4. **Don't trust data without validation**

   ```csharp
   // Bad
   int quantity = userInput;
   CreateShipment(quantity); // Could be negative!

   // Good
   int quantity = Math.Max(1, Math.Min(userInput, MAX_CAP));
   CreateShipment(quantity);
   ```

5. **Don't hardcode paths**

   ```csharp
   // Bad
   string path = "C:\\Program Files\\...\\Mods\\";

   // Good
   string gamePath = Environment.GetEnvironmentVariable("S1_GAME");
   string modsPath = Path.Combine(gamePath, "Mods");
   ```

---

## Going Further

### Next Steps

Now that you understand the fundamentals:

1. **Study existing mods**

   - S1NotesApp (reference implementation)
   - S1FuelMod (direct game access patterns)
   - S1API source code (framework understanding)

2. **Experiment with features**

   - Add a new phone app
   - Create a time-based system
   - Implement a notification system
   - Add achievements/stats

3. **Contribute to community**
   - Share your findings
   - Help others debug
   - Create tutorials
   - Improve S1API

### Resources

**Documentation:**

- [LOGGING_GUIDE.md](./LOGGING_GUIDE.md) - Master logging for debugging
- [ICON_LOADING_TUTORIAL.md](./ICON_LOADING_TUTORIAL.md) - Embedded resource images
- [PROJECT_STRUCTURE.md](./PROJECT_STRUCTURE.md) - Code organization
- [CSPROJ_EXPLAINED.md](./CSPROJ_EXPLAINED.md) - Build system deep dive

**External:**

- [MelonLoader Wiki](https://melonwiki.xyz/) - Official MelonLoader docs
- [Harmony Documentation](https://harmony.pardeike.net/) - Patching guide
- [Unity Scripting API](https://docs.unity3d.com/ScriptReference/) - Unity classes reference

**Tools:**

- [dnSpy](https://github.com/dnSpy/dnSpy) - .NET decompiler (explore game code)
- [ILSpy](https://github.com/icsharpcode/ILSpy) - Alternative decompiler
- [Visual Studio Code](https://code.visualstudio.com/) - Lightweight code editor

### Community

- **Discord:** Schedule I Modding community
- **GitHub:** Share your mods and collaborate
- **Reddit:** r/scheduleone (if it exists)

---

## Final Thoughts

**Modding is equal parts:**

- 🔬 **Science** (experimenting and discovering)
- 🎨 **Art** (designing features players will enjoy)
- 🛠️ **Engineering** (building robust, maintainable code)

You now have the foundation to:

- ✅ Understand how mods load and execute
- ✅ Use Harmony to patch game code
- ✅ Work with Unity GameObjects and components
- ✅ Persist data across sessions
- ✅ Debug issues systematically
- ✅ Build complete features from scratch

**The best way to learn is by doing.** Pick a small feature idea and build it step by step, logging everything along the way.

Welcome to the modding community! 🎮✨

---

**Questions or feedback?** Open an issue on the GitHub repo or reach out to the community.

Happy modding! 🚀

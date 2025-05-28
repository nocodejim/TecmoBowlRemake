# Getting Started with Unity Tecmo Bowl Development

## Welcome to Game Development! 🎮

This guide will take you from complete beginner to building your own Tecmo Bowl remake. If you're comfortable with Visual Studio 2022 but new to Unity, you're in the perfect place to start your game development journey.

## Table of Contents

1. [Initial Setup](#initial-setup)
2. [Understanding Unity Basics](#understanding-unity-basics)
3. [Your First Unity Scene](#your-first-unity-scene)
4. [Visual Studio + Unity Integration](#visual-studio--unity-integration)
5. [Building the Tecmo Bowl Foundation](#building-the-tecmo-bowl-foundation)
6. [Testing Your First Build](#testing-your-first-build)
7. [Next Steps](#next-steps)

---

## Initial Setup

### Prerequisites Check
Before we begin, ensure you have:
- ✅ Windows 10 or 11 (64-bit)
- ✅ Visual Studio 2022 Professional
- ✅ At least 8GB RAM (16GB recommended)
- ✅ 5GB free disk space for Unity + project
- ✅ Admin rights on your computer

### Step 1: Run the Setup Script

1. **Open PowerShell as Administrator**
   - Press `Win + X` and select "Windows PowerShell (Admin)"
   - Or search "PowerShell" in Start Menu, right-click, "Run as administrator"

2. **Navigate to your project folder**
   ```powershell
   cd C:\GameDev\TecmoBowlRemake
   ```

3. **Run the setup script**
   ```powershell
   .\setup-vs2022-gamedev.ps1
   ```

4. **Wait for completion** (20-30 minutes)
   - The script installs Unity, configures Visual Studio, and sets up your project structure
   - ☕ Perfect time for a coffee break!

5. **Restart your computer** when prompted
   - This ensures all PATH changes take effect

### Step 2: Verify Installation

After restarting:

1. **Check Unity Hub**
   - Find "Unity Hub" in your Start Menu
   - Sign in with a Unity ID (create free account if needed)
   - Verify Unity 2022.3.45f1 is installed

2. **Check Visual Studio**
   - Open Visual Studio 2022
   - Go to Tools > Get Tools and Features
   - Verify "Game development with Unity" workload is installed

---

## Understanding Unity Basics

### Unity vs Visual Studio: Key Concepts

If you're coming from traditional C# development, here are the key differences:

| Concept | Visual Studio Project | Unity Project |
|---------|----------------------|---------------|
| **Entry Point** | `Main()` method | `Start()` method on MonoBehaviour |
| **Project Structure** | Solution with projects | Single Unity project with scenes |
| **Code Organization** | Namespaces and classes | GameObjects with Component scripts |
| **Runtime** | Console/Windows app | Game engine with render loop |
| **Debugging** | Standard debugger | Unity console + Visual Studio debugger |

### The Unity Way of Thinking

**Everything is a GameObject with Components**

Think of Unity like this:
- **GameObject** = An empty container (like a `<div>` in HTML)
- **Component** = Functionality attached to that container (like CSS classes)
- **Scene** = A collection of GameObjects (like a complete web page)

Example: A football player in Tecmo Bowl
```
Player GameObject
├── Transform Component (position, rotation, scale)
├── SpriteRenderer Component (visual appearance)
├── Rigidbody2D Component (physics)
├── Collider2D Component (collision detection)
└── TecmoBowlPlayerController Component (our custom script)
```

### Unity's Component System vs OOP

Instead of deep inheritance trees, Unity uses composition:

**Traditional OOP Approach:**
```csharp
public class Entity { }
public class MovableEntity : Entity { }
public class Player : MovableEntity { }
public class FootballPlayer : Player { }
```

**Unity Component Approach:**
```csharp
// One GameObject with multiple components
GameObject player = new GameObject("Player");
player.AddComponent<Transform>();      // Position
player.AddComponent<Movement>();       // Can move
player.AddComponent<PlayerController>(); // Player-specific logic
player.AddComponent<FootballPlayer>(); // Football-specific behavior
```

---

## Your First Unity Scene

### Step 1: Create the Unity Project

1. **Open Unity Hub**
2. **Click "New Project"**
3. **Select "2D Core" template**
4. **Project Settings:**
   - Project name: `TecmoBowlRemake`
   - Location: `C:\GameDev\TecmoBowlRemake\TecmoBowlUnity`
5. **Click "Create Project"**

### Step 2: Understanding the Unity Interface

When Unity opens, you'll see several panels:

**Hierarchy Panel (left)**
- Shows all GameObjects in your current scene
- Think of it as the "Solution Explorer" for your scene

**Scene View (center)**
- Visual representation of your game world
- Where you arrange and position objects

**Game View (center, tab)**
- What players will actually see
- Click "Play" to test your game

**Inspector Panel (right)**
- Shows properties of selected GameObject
- Like the "Properties" window in Visual Studio

**Project Panel (bottom)**
- Shows all files in your project
- Like "Solution Explorer" for your entire project

**Console Panel (bottom, tab)**
- Shows debug messages, errors, warnings
- Your new best friend for debugging!

### Step 3: Create Your First GameObject

Let's create a simple football field:

1. **Right-click in Hierarchy**
2. **Select "2D Object" > "Sprite"**
3. **Rename it to "FootballField"**
4. **In Inspector, set Transform:**
   - Position: X=0, Y=0, Z=0
   - Scale: X=10, Y=6, Z=1

You now have a white rectangle that represents your field!

### Step 4: Add a Background Color

1. **Right-click in Hierarchy**
2. **Select "2D Object" > "Sprite"**
3. **Rename to "FieldBackground"**
4. **In Inspector:**
   - Set Color to green (click the white square next to "Color")
   - Set Scale to X=12, Y=8, Z=1

Now you have a green football field!

---

## Visual Studio + Unity Integration

### Setting Up the Integration

1. **In Unity: Edit > Preferences**
2. **Select "External Tools"**
3. **Set "External Script Editor" to:**
   ```
   C:\Program Files\Microsoft Visual Studio\2022\Professional\Common7\IDE\devenv.exe
   ```

### Your First Script

Let's create a simple script to understand the workflow:

1. **In Unity Project panel, create folder structure:**
   ```
   Assets/
   ├── Scripts/
   ├── Sprites/
   ├── Audio/
   └── Scenes/
   ```

2. **Right-click Scripts folder**
3. **Create > C# Script**
4. **Name it "GameManager"**

5. **Double-click the script** - Visual Studio will open!

### Understanding MonoBehaviour

Every Unity script inherits from `MonoBehaviour`, which gives you access to Unity's lifecycle methods:

```csharp
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // Called once when the script starts
    void Start()
    {
        Debug.Log("Game Manager started!");
    }
    
    // Called every frame (60 times per second)
    void Update()
    {
        // Check for input
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Space key pressed!");
        }
    }
}
```

### The Unity-Visual Studio Workflow

1. **Write code in Visual Studio**
2. **Save the file (Ctrl+S)**
3. **Switch back to Unity** - it automatically compiles
4. **Check Console panel for any errors**
5. **Attach script to GameObject by dragging it**
6. **Press Play to test**

This is different from traditional development where you manually build and run!

---

## Building the Tecmo Bowl Foundation

### Project Structure Overview

Your Tecmo Bowl project uses this architecture:

```
TecmoBowlRemake/
├── Game Manager (singleton, controls everything)
├── Team Data Manager (loads NFL teams)
├── UI Manager (handles menus and HUD)
├── Player Controllers (individual player behavior)
└── Audio Manager (sounds and music)
```

### Step 1: Import the Core Scripts

1. **Copy the provided scripts to your Scripts folder:**
   - `TecmoBowlGameManager.cs`
   - `NFLTeam.cs` and related data classes
   - `TecmoBowlPlayerController.cs`
   - `TecmoBowlUIManager.cs`

2. **In Unity, they'll automatically compile**
3. **Check Console for any errors and fix them**

### Step 2: Create the Game Manager

1. **Create empty GameObject in Hierarchy**
2. **Rename to "GameManager"**
3. **Drag `TecmoBowlGameManager.cs` script onto it**
4. **The GameObject now has the Game Manager component!**

### Step 3: Set Up the Scene Structure

Create this hierarchy in your scene:

```
Scene: MainScene
├── GameManager (TecmoBowlGameManager script)
├── UI Canvas (for all UI elements)
│   ├── MainMenu
│   ├── GameHUD
│   └── PlaySelection
├── Field
│   ├── FieldBackground
│   └── FieldMarkings
└── Players (will be created dynamically)
```

### Step 4: Understanding ScriptableObjects

Tecmo Bowl uses ScriptableObjects for team data. Think of them as "data containers" that can be created in the Unity Editor:

1. **Right-click in Project panel**
2. **Create > Tecmo Bowl > NFL Team**
3. **Name it "KansasCityChiefs"**
4. **Fill in the team data in Inspector:**
   - Team Name: "Chiefs"
   - City Name: "Kansas City"
   - Abbreviation: "KC"
   - Primary Color: Red
   - Secondary Color: Gold

This creates a data file that can be loaded by your scripts!

### Step 5: Connect the Systems

In your GameManager GameObject:
1. **Find the "Team Data Manager" field in Inspector**
2. **Drag your TeamDataManager script onto it**
3. **This "links" the systems together**

This is Unity's visual way of setting up dependencies - much more intuitive than config files!

---

## Testing Your First Build

### Debug Your Game

1. **Press Play in Unity**
2. **Watch the Console panel for debug messages**
3. **Use Debug.Log() like Console.WriteLine() for troubleshooting**

Example debugging:
```csharp
void Start()
{
    Debug.Log("Game starting...");
    if (homeTeam == null)
    {
        Debug.LogError("No home team assigned!");
    }
    else
    {
        Debug.Log($"Home team: {homeTeam.teamName}");
    }
}
```

### Build for Windows

1. **File > Build Settings**
2. **Select "PC, Mac & Linux Standalone"**
3. **Target Platform: Windows**
4. **Click "Add Open Scenes" to include current scene**
5. **Click "Build"**
6. **Choose output folder (like `C:\GameDev\TecmoBowlRemake\Build`)**
7. **Unity creates an executable file!**

### Test the Executable

1. **Navigate to your build folder**
2. **Double-click `TecmoBowlRemake.exe`**
3. **Your game runs outside of Unity!**

---

## Next Steps

### Phase 1: Basic Functionality (Week 1)
- ✅ Set up development environment
- ✅ Create basic scene structure  
- ✅ Implement core game manager
- 🔄 Add team selection menu
- 🔄 Create simple player movement

### Phase 2: Core Gameplay (Week 2-3)
- 🔄 Implement play selection system
- 🔄 Add player controllers with AI
- 🔄 Create tackle and scoring systems
- 🔄 Add authentic Tecmo Bowl mechanics

### Phase 3: Polish & Features (Week 4)
- 🔄 Add all 32 NFL teams with real data
- 🔄 Implement audio system
- 🔄 Create season mode
- 🔄 Add visual effects and animations

### Learning Resources

**Unity Official Resources:**
- [Unity Learn](https://learn.unity.com/) - Free official tutorials
- [Unity Manual](https://docs.unity3d.com/Manual/) - Complete reference
- [Unity Scripting API](https://docs.unity3d.com/ScriptReference/) - All available functions

**Recommended Learning Path:**
1. **"Create with Code" course** - Unity's beginner-friendly course
2. **"2D Game Development" tutorials** - Directly applicable to Tecmo Bowl
3. **"Unity C# Survival Guide"** - Bridging C# knowledge to Unity

**Community Resources:**
- [Unity Forum](https://forum.unity.com/) - Ask questions, get help
- [Stack Overflow Unity Tag](https://stackoverflow.com/questions/tagged/unity3d) - Technical solutions
- [Unity Discord](https://discord.gg/unity) - Real-time community help

### Common Beginner Mistakes to Avoid

1. **Don't modify scripts while Play mode is active**
   - Changes will be lost when you stop playing!

2. **Always assign references in Inspector**
   - Null reference exceptions are common without proper assignments

3. **Use Debug.Log extensively**
   - Unlike Visual Studio debugging, Unity debugging relies heavily on logging

4. **Save your scene frequently (Ctrl+S)**
   - Unlike code files, scene changes can be lost

5. **Organize your project from day one**
   - Create proper folder structures early

### Troubleshooting Common Issues

**"NullReferenceException"**
- Most common Unity error
- Usually means you forgot to assign something in Inspector
- Check all public fields in your scripts

**"Script compilation errors"**
- Fix all compilation errors before pressing Play
- Unity won't run with compilation errors

**"Objects not appearing in game"**
- Check the Camera position and field of view
- Ensure objects are within camera bounds
- Verify Scale values aren't zero

**"Input not working"**
- Make sure your game window has focus
- Check Input Manager settings (Edit > Project Settings > Input Manager)

---

## Conclusion

Congratulations! You've taken your first steps into Unity game development. The transition from traditional C# development to Unity can feel different at first, but the visual, component-based approach makes complex game systems much more manageable.

Remember:
- **Take it one step at a time** - Game development is complex but rewarding
- **Experiment and iterate** - Unity makes it easy to try ideas quickly
- **Use the community** - The Unity community is incredibly helpful
- **Have fun!** - You're building a game - enjoy the process!

Your Tecmo Bowl remake is going to be amazing. The foundation is solid, the tools are powerful, and you have all the knowledge needed to create something special.

**Ready to start coding? Let's build the best Tecmo Bowl experience ever! 🏈**

---

*Next up: [Unity Architecture Deep Dive](unity-architecture-guide.md) - Understanding how all the systems work together*
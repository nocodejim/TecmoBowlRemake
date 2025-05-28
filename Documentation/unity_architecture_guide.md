# Unity Tecmo Bowl Architecture Guide

## System Overview

This document explains the architecture of our Tecmo Bowl remake, showing how all systems work together to create an authentic football gaming experience. Understanding this architecture will help you modify, extend, and debug the game effectively.

## Table of Contents

1. [Architectural Philosophy](#architectural-philosophy)
2. [Core System Architecture](#core-system-architecture)
3. [Component Relationships](#component-relationships)
4. [Data Flow Diagrams](#data-flow-diagrams)
5. [Game State Management](#game-state-management)
6. [Event-Driven Architecture](#event-driven-architecture)
7. [Performance Considerations](#performance-considerations)
8. [Extending the System](#extending-the-system)

---

## Architectural Philosophy

### Design Principles

Our Tecmo Bowl remake follows these key architectural principles:

**1. Single Responsibility Principle**
- Each component has one clear purpose
- `GameManager` handles game flow, `PlayerController` handles player behavior
- Makes debugging and maintenance easier

**2. Event-Driven Communication**
- Systems communicate through events, not direct references
- Reduces coupling between systems
- Enables easy addition of new features

**3. Data-Driven Design**
- Team and player data stored in ScriptableObjects
- Easy to modify without changing code
- Supports modding and roster updates

**4. Unity Best Practices**
- Proper use of MonoBehaviour lifecycle methods
- Component-based architecture
- Efficient GameObject management

### Why This Architecture?

Coming from traditional software development, you might wonder why we don't use more familiar patterns like MVC or MVVM. Unity's component system is inherently different:

```
Traditional Web App          Unity Game
Controller ↔ Model ↔ View   GameObject ↔ Components ↔ Systems
```

Unity's approach is more like an **Entity Component System (ECS)** where:
- **Entity** = GameObject
- **Component** = MonoBehaviour scripts
- **System** = Manager classes that coordinate components

---

## Core System Architecture

### System Hierarchy

```
TecmoBowlGameManager (Singleton)
├── TeamDataManager (Data Layer)
├── TecmoBowlUIManager (Presentation Layer)
├── AudioManager (Audio Layer)
├── InputManager (Input Layer)
└── PlayerControllers[] (Gameplay Layer)
```

### System Responsibilities

| System | Primary Responsibility | Secondary Responsibilities |
|--------|----------------------|---------------------------|
| **GameManager** | Game state, flow control | Score tracking, timing, play execution |
| **TeamDataManager** | Team/player data loading | Roster management, team statistics |
| **UIManager** | All user interface | Menus, HUD, play selection |
| **PlayerController** | Individual player behavior | Movement, AI, collision detection |
| **AudioManager** | Sound effects and music | Audio mixing, dynamic music |

### Singleton Pattern Usage

We use singletons sparingly and only for truly global systems:

```csharp
public class TecmoBowlGameManager : MonoBehaviour
{
    public static TecmoBowlGameManager Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
```

**Why Singletons Here?**
- Game state needs to persist across scene changes
- Multiple systems need to access game state
- Alternative (static classes) wouldn't work with Unity's lifecycle

---

## Component Relationships

### GameObject Component Structure

Each major entity in our game follows this pattern:

```
Player GameObject
├── Transform (Unity built-in)
├── SpriteRenderer (Unity built-in)
├── Rigidbody2D (Unity physics)
├── Collider2D (Unity collision)
├── TecmoBowlPlayerController (our logic)
└── Animator (Unity animation)
```

### Component Communication Patterns

**1. Direct Reference (Same GameObject)**
```csharp
public class PlayerController : MonoBehaviour
{
    private Rigidbody2D rb;
    
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }
}
```

**2. Inspector Assignment (Different GameObjects)**
```csharp
public class PlayerController : MonoBehaviour
{
    [SerializeField] private Transform ballPosition;
    // Assigned in Unity Inspector
}
```

**3. Event System (Loose Coupling)**
```csharp
public static event Action<PlayerController> OnPlayerTackled;

// Trigger event
OnPlayerTackled?.Invoke(this);

// Listen for event
void Start()
{
    PlayerController.OnPlayerTackled += HandlePlayerTackled;
}
```

**4. Singleton Access (Global State)**
```csharp
void Update()
{
    var gameState = TecmoBowlGameManager.Instance.currentGameState;
}
```

### When to Use Each Pattern

| Pattern | Use When | Example |
|---------|----------|---------|
| **Direct Reference** | Same GameObject components | Getting Rigidbody2D for movement |
| **Inspector Assignment** | Related GameObjects | Player references ball position |
| **Event System** | Loose coupling needed | Player scored, notify UI and audio |
| **Singleton** | Global state access | Any system needs current game state |

---

## Data Flow Diagrams

### Game Initialization Flow

```
Application Start
       ↓
Setup Script Execution
       ↓
Unity Project Creation
       ↓
GameManager.Awake()
       ↓
TeamDataManager.LoadTeams()
       ↓
UIManager.ShowMainMenu()
       ↓
Ready for Player Input
```

### Play Execution Flow

```
PlaySelection State
       ↓
User Selects Offensive Play
       ↓
User Selects Defensive Play
       ↓
GameManager.ExecutePlay()
       ↓
PlayerControllers Activated
       ↓
Physics/Collision Detection
       ↓
Play Result Calculated
       ↓
UI Updated with Result
       ↓
Next Down or Score
```

### Data Transformation Pipeline

```
NFL Team Data (ScriptableObject)
       ↓
TeamDataManager.LoadTeam()
       ↓
Runtime Team Instance
       ↓
PlayerController.Initialize()
       ↓
Active Game Player
       ↓
UI Display/Game Logic
```

---

## Game State Management

### State Machine Design

Our game uses a finite state machine for clear flow control:

```csharp
public enum GameState
{
    MainMenu,      // Player selecting options
    TeamSelection, // Choosing teams
    CoinToss,      // Game start ceremony
    PlaySelection, // Choosing plays
    Playing,       // Active gameplay
    PlayResult,    // Showing play outcome
    Paused,        // Game paused
    GameOver       // Game finished
}
```

### State Transitions

```
MainMenu ──────────────┐
    ↓                  │
TeamSelection          │ (Return to Menu)
    ↓                  │
CoinToss              │
    ↓                  │
PlaySelection ←────────┤
    ↓                  │
Playing               │
    ↓                  │
PlayResult ────────────┤
    ↓                  │
GameOver ──────────────┘
```

### State Management Code Pattern

```csharp
public void SetGameState(GameState newState)
{
    GameState previousState = currentGameState;
    currentGameState = newState;
    
    // Notify all interested systems
    OnGameStateChanged?.Invoke(newState);
    
    // Handle state-specific logic
    switch (newState)
    {
        case GameState.PlaySelection:
            PreparePlaySelection();
            break;
        case GameState.Playing:
            StartGameClock();
            break;
        // ... other states
    }
}
```

**Why State Machines?**
- **Predictable behavior** - Always know what state you're in
- **Easy debugging** - Can log state transitions
- **Clear transitions** - Explicit rules for state changes
- **Maintainable** - Easy to add new states or modify existing ones

---

## Event-Driven Architecture

### Event System Benefits

Traditional approach (tightly coupled):
```csharp
public class PlayerController
{
    public UIManager uiManager; // Direct dependency
    public AudioManager audioManager; // Direct dependency
    
    void OnTackle()
    {
        uiManager.ShowTackleMessage(); // Tight coupling
        audioManager.PlayTackleSound(); // Tight coupling
    }
}
```

Event-driven approach (loosely coupled):
```csharp
public class PlayerController
{
    public static event Action<PlayerController> OnPlayerTackled;
    
    void OnTackle()
    {
        OnPlayerTackled?.Invoke(this); // One event, many listeners
    }
}

// UI and Audio subscribe independently
UIManager.OnEnable() => PlayerController.OnPlayerTackled += ShowTackleMessage;
AudioManager.OnEnable() => PlayerController.OnPlayerTackled += PlayTackleSound;
```

### Event Categories

**Game Events** (GameManager)
```csharp
public static event Action<GameState> OnGameStateChanged;
public static event Action<int, int> OnScoreChanged;
public static event Action<string> OnGameMessage;
```

**Player Events** (PlayerController)
```csharp
public static event Action<PlayerController> OnPlayerTackled;
public static event Action<PlayerController> OnPlayerScored;
public static event Action<PlayerController, PlayerController> OnPlayerCaughtPass;
```

**UI Events** (UIManager)
```csharp
public static event Action<TecmoPlay> OnPlaySelected;
public static event Action<NFLTeam> OnTeamSelected;
```

### Event Subscription Pattern

Always follow this pattern to avoid memory leaks:

```csharp
void OnEnable()
{
    // Subscribe to events
    PlayerController.OnPlayerTackled += HandlePlayerTackled;
}

void OnDisable()
{
    // ALWAYS unsubscribe!
    PlayerController.OnPlayerTackled -= HandlePlayerTackled;
}
```

---

## Performance Considerations

### Unity-Specific Optimizations

**1. Object Pooling for Temporary Objects**
```csharp
// Instead of creating/destroying footballs constantly
public class FootballPool : MonoBehaviour
{
    private Queue<GameObject> footballPool = new Queue<GameObject>();
    
    public GameObject GetFootball()
    {
        if (footballPool.Count > 0)
            return footballPool.Dequeue();
        else
            return Instantiate(footballPrefab);
    }
    
    public void ReturnFootball(GameObject football)
    {
        football.SetActive(false);
        footballPool.Enqueue(football);
    }
}
```

**2. Efficient Component Access**
```csharp
// Cache component references
public class PlayerController : MonoBehaviour
{
    private Rigidbody2D rb; // Cached reference
    
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>(); // Cache once
    }
    
    void Update()
    {
        rb.velocity = newVelocity; // Use cached reference
        // NOT: GetComponent<Rigidbody2D>().velocity = newVelocity;
    }
}
```

**3. Smart Update Loops**
```csharp
public class AIController : MonoBehaviour
{
    private float aiUpdateInterval = 0.2f; // Update AI 5 times per second
    private float aiTimer = 0f;
    
    void Update()
    {
        aiTimer += Time.deltaTime;
        if (aiTimer >= aiUpdateInterval)
        {
            aiTimer = 0f;
            UpdateAI(); // Expensive AI logic only runs occasionally
        }
    }
}
```

### Memory Management

**ScriptableObject Benefits**
- Team data loaded once, shared by all instances
- Reduces memory usage compared to duplicate data
- Easy to update without recompiling

**Event System Memory Leaks**
- Always unsubscribe from events in OnDisable()
- Use weak references for long-lived objects
- Consider event bus pattern for complex scenarios

### Profiling Your Game

Unity provides excellent profiling tools:

1. **Window > Analysis > Profiler**
2. **Run your game in editor**
3. **Watch for:**
   - CPU spikes during play execution
   - Memory allocation during gameplay
   - Rendering bottlenecks

---

## Extending the System

### Adding New Features

Our architecture makes it easy to add new features:

**1. Adding Weather Effects**
```csharp
// Create new component
public class WeatherSystem : MonoBehaviour
{
    public static event Action<WeatherType> OnWeatherChanged;
    
    void Start()
    {
        // Subscribe to game events
        GameManager.OnGameStateChanged += HandleGameStateChange;
    }
    
    void HandleGameStateChange(GameState state)
    {
        if (state == GameState.Playing)
        {
            ApplyWeatherEffects();
        }
    }
}

// Existing systems automatically get weather updates
PlayerController.OnEnable() => WeatherSystem.OnWeatherChanged += AdjustPlayerSpeed;
UIManager.OnEnable() => WeatherSystem.OnWeatherChanged += UpdateWeatherDisplay;
```

**2. Adding New Player Positions**
```csharp
// Extend existing enums
public enum PlayerRole
{
    Quarterback,
    RunningBack,
    Receiver,
    Linebacker,
    Defensive,
    Kicker,     // New position
    Punter      // New position
}

// PlayerController automatically handles new roles
switch (role)
{
    case PlayerRole.Kicker:
        aiBehavior = AIBehaviorType.FieldGoalTarget;
        break;
    // ... existing cases
}
```

### Modding Support

The ScriptableObject system enables easy modding:

**Team Data Modding**
1. Create new NFLTeam ScriptableObject
2. Fill in custom team data
3. Add to TeamDataManager's team list
4. Game automatically supports new team

**Play Modding**
1. Create new TecmoPlay objects
2. Assign to team playbooks
3. Custom plays work immediately

### Testing Architecture

Each system can be tested independently:

```csharp
[Test]
public void GameManager_StateTransition_UpdatesCorrectly()
{
    // Arrange
    var gameManager = CreateTestGameManager();
    var initialState = GameState.MainMenu;
    
    // Act
    gameManager.SetGameState(GameState.TeamSelection);
    
    // Assert
    Assert.AreEqual(GameState.TeamSelection, gameManager.currentGameState);
}
```

---

## Architecture Checklist

When adding new features, ensure you follow these principles:

### ✅ Component Design
- [ ] Single responsibility per component
- [ ] Cached component references
- [ ] Proper MonoBehaviour lifecycle usage
- [ ] Inspector-assignable fields where appropriate

### ✅ Event Usage
- [ ] Events for cross-system communication
- [ ] Proper subscription/unsubscription
- [ ] Meaningful event data passed
- [ ] Events documented with XML comments

### ✅ Performance
- [ ] No expensive operations in Update()
- [ ] Object pooling for frequently created/destroyed objects
- [ ] Efficient collision detection
- [ ] Minimal garbage allocation

### ✅ Maintainability
- [ ] Clear naming conventions
- [ ] XML documentation on public methods
- [ ] Logical folder organization
- [ ] Version control friendly (no large binary assets)

---

## Common Architectural Pitfalls

### ❌ Anti-Patterns to Avoid

**1. GameObject.Find() in Update()**
```csharp
// DON'T DO THIS
void Update()
{
    var player = GameObject.Find("Player"); // Expensive search every frame!
}

// DO THIS INSTEAD
void Start()
{
    player = GameObject.Find("Player"); // Search once, cache result
}
```

**2. Tight Coupling Between Systems**
```csharp
// DON'T DO THIS
public class PlayerController
{
    public UIManager uiManager; // Direct dependency
    
    void OnScore()
    {
        uiManager.ShowScoreMessage(); // Tight coupling
    }
}

// DO THIS INSTEAD
public class PlayerController
{
    public static event Action OnPlayerScored;
    
    void OnScore()
    {
        OnPlayerScored?.Invoke(); // Loose coupling via events
    }
}
```

**3. Ignoring Unity Lifecycle**
```csharp
// DON'T DO THIS
public class PlayerController : MonoBehaviour
{
    void Start()
    {
        // Assuming other objects are ready
        ball = GameObject.Find("Ball");
        ball.transform.position = transform.position; // May be null!
    }
}

// DO THIS INSTEAD
public class PlayerController : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(InitializeWhenReady());
    }
    
    IEnumerator InitializeWhenReady()
    {
        while (ball == null)
        {
            ball = GameObject.Find("Ball");
            yield return null; // Wait one frame
        }
        
        // Now safe to use ball
        ball.transform.position = transform.position;
    }
}
```

---

## Conclusion

This architecture provides a solid foundation for our Tecmo Bowl remake while remaining flexible for future enhancements. The key principles are:

1. **Component-based design** following Unity best practices
2. **Event-driven communication** for system decoupling
3. **Data-driven configuration** via ScriptableObjects
4. **Performance-conscious implementation** using Unity optimizations
5. **Extensible structure** supporting modding and new features

Understanding this architecture will help you:
- Debug issues more effectively
- Add new features confidently
- Optimize performance systematically
- Maintain code quality over time

The next step is diving into specific implementation details for each system. Happy coding! 🎮

---

*Next: [Player Controller Deep Dive](player-controller-guide.md) - Mastering Tecmo Bowl gameplay mechanics*
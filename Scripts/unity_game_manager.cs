using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System;

/// <summary>
/// Main Game Manager for Tecmo Bowl Remake
/// Handles game state, flow control, and coordinates all major systems
/// This is the central hub that controls the entire game experience
/// </summary>
public class TecmoBowlGameManager : MonoBehaviour
{
    #region Singleton Pattern
    public static TecmoBowlGameManager Instance { get; private set; }
    
    private void Awake()
    {
        // Ensure only one GameManager exists (Singleton pattern)
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeGame();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion
    
    #region Game State Management
    [Header("Game State")]
    public GameState currentGameState = GameState.MainMenu;
    public GameMode currentGameMode = GameMode.SingleGame;
    
    [Header("Game Configuration")]
    [SerializeField] private float quarterLengthMinutes = 1.5f;
    [SerializeField] private int playersPerSide = 9;
    [SerializeField] private bool acceleratedClock = true;
    
    // Game timing
    private float gameTimer;
    private float quarterTimeRemaining;
    private int currentQuarter = 1;
    private bool clockRunning = false;
    
    // Score tracking
    private int homeScore = 0;
    private int awayScore = 0;
    
    // Down system (Tecmo Bowl style)
    private int currentDown = 1;
    private int yardsToGo = 10;
    private int fieldPosition = 20; // Yard line (0-100)
    private bool homeTeamPossession = true;
    #endregion
    
    #region Team Management
    [Header("Team Data")]
    public NFLTeam homeTeam;
    public NFLTeam awayTeam;
    public TeamDataManager teamDataManager;
    
    // Current play selection
    private TecmoPlay selectedOffensivePlay;
    private TecmoPlay selectedDefensivePlay;
    private bool playSelectionComplete = false;
    #endregion
    
    #region Events
    // Game state events for UI and other systems to subscribe to
    public static event Action<GameState> OnGameStateChanged;
    public static event Action<int, int> OnScoreChanged;
    public static event Action<int, int, int> OnDownChanged; // down, yards to go, field position
    public static event Action<float> OnClockUpdated;
    public static event Action<int> OnQuarterChanged;
    public static event Action<string> OnGameMessage; // For displaying game events
    #endregion
    
    #region Unity Lifecycle
    private void Start()
    {
        // Initialize game systems
        SetGameState(GameState.MainMenu);
    }
    
    private void Update()
    {
        // Update game clock during play
        if (clockRunning && currentGameState == GameState.Playing)
        {
            UpdateGameClock();
        }
        
        // Handle input based on current game state
        HandleInput();
    }
    #endregion
    
    #region Game Initialization
    private void InitializeGame()
    {
        Debug.Log("Initializing Tecmo Bowl Remake...");
        
        // Initialize quarter time
        quarterTimeRemaining = quarterLengthMinutes * 60f;
        
        // Load team data
        if (teamDataManager == null)
        {
            teamDataManager = FindObjectOfType<TeamDataManager>();
        }
        
        Debug.Log("Game Manager initialized successfully!");
    }
    
    /// <summary>
    /// Start a new game with selected teams
    /// </summary>
    public void StartNewGame(NFLTeam home, NFLTeam away, GameMode mode = GameMode.SingleGame)
    {
        homeTeam = home;
        awayTeam = away;
        currentGameMode = mode;
        
        // Reset game state
        ResetGameState();
        
        // Load the main game scene
        SetGameState(GameState.CoinToss);
        
        OnGameMessage?.Invoke($"{awayTeam.teamName} vs {homeTeam.teamName} - Game Starting!");
        Debug.Log($"New game started: {awayTeam.teamName} @ {homeTeam.teamName}");
    }
    
    private void ResetGameState()
    {
        homeScore = 0;
        awayScore = 0;
        currentQuarter = 1;
        quarterTimeRemaining = quarterLengthMinutes * 60f;
        currentDown = 1;
        yardsToGo = 10;
        fieldPosition = 20;
        homeTeamPossession = true;
        clockRunning = false;
        
        // Notify all systems of reset
        OnScoreChanged?.Invoke(homeScore, awayScore);
        OnDownChanged?.Invoke(currentDown, yardsToGo, fieldPosition);
        OnQuarterChanged?.Invoke(currentQuarter);
        OnClockUpdated?.Invoke(quarterTimeRemaining);
    }
    #endregion
    
    #region Game State Management
    public void SetGameState(GameState newState)
    {
        GameState previousState = currentGameState;
        currentGameState = newState;
        
        Debug.Log($"Game State changed from {previousState} to {newState}");
        OnGameStateChanged?.Invoke(newState);
        
        // Handle state-specific logic
        switch (newState)
        {
            case GameState.MainMenu:
                clockRunning = false;
                break;
                
            case GameState.TeamSelection:
                // Load team selection UI
                break;
                
            case GameState.CoinToss:
                StartCoroutine(HandleCoinToss());
                break;
                
            case GameState.PlaySelection:
                clockRunning = false;
                playSelectionComplete = false;
                break;
                
            case GameState.Playing:
                // Game action is happening
                break;
                
            case GameState.PlayResult:
                clockRunning = false;
                StartCoroutine(HandlePlayResult());
                break;
                
            case GameState.GameOver:
                clockRunning = false;
                HandleGameOver();
                break;
        }
    }
    #endregion
    
    #region Game Clock Management
    private void UpdateGameClock()
    {
        if (quarterTimeRemaining > 0)
        {
            quarterTimeRemaining -= Time.deltaTime;
            OnClockUpdated?.Invoke(quarterTimeRemaining);
            
            // Check for end of quarter
            if (quarterTimeRemaining <= 0)
            {
                EndQuarter();
            }
        }
    }
    
    public void StartClock()
    {
        clockRunning = true;
        OnGameMessage?.Invoke("Clock started");
    }
    
    public void StopClock()
    {
        clockRunning = false;
        OnGameMessage?.Invoke("Clock stopped");
    }
    
    private void EndQuarter()
    {
        quarterTimeRemaining = 0;
        clockRunning = false;
        
        OnGameMessage?.Invoke($"End of Quarter {currentQuarter}");
        
        currentQuarter++;
        OnQuarterChanged?.Invoke(currentQuarter);
        
        if (currentQuarter <= 4)
        {
            // Start next quarter
            quarterTimeRemaining = quarterLengthMinutes * 60f;
            OnClockUpdated?.Invoke(quarterTimeRemaining);
            
            // Switch field sides between quarters (Tecmo Bowl tradition)
            if (currentQuarter == 2 || currentQuarter == 4)
            {
                // Switch sides but keep same possession
                fieldPosition = 100 - fieldPosition;
            }
        }
        else
        {
            // Game over
            SetGameState(GameState.GameOver);
        }
    }
    #endregion
    
    #region Play Management
    /// <summary>
    /// Called when both teams have selected their plays
    /// </summary>
    public void ExecutePlay(TecmoPlay offensivePlay, TecmoPlay defensivePlay)
    {
        selectedOffensivePlay = offensivePlay;
        selectedDefensivePlay = defensivePlay;
        
        OnGameMessage?.Invoke($"Executing: {offensivePlay.playName} vs {defensivePlay.playName}");
        
        // Start the actual play
        SetGameState(GameState.Playing);
        
        // Start game clock
        if (acceleratedClock)
        {
            StartClock();
        }
        
        // The actual play execution will be handled by PlayExecutionManager
        StartCoroutine(ExecutePlaySequence());
    }
    
    private IEnumerator ExecutePlaySequence()
    {
        // This is where the magic happens - the actual football play
        // For now, we'll simulate the play result
        yield return new WaitForSeconds(2f); // Simulate play duration
        
        // Calculate play result based on play matchup
        PlayResult result = CalculatePlayResult(selectedOffensivePlay, selectedDefensivePlay);
        
        // Apply the result
        ApplyPlayResult(result);
        
        // Move to play result state
        SetGameState(GameState.PlayResult);
    }
    
    private PlayResult CalculatePlayResult(TecmoPlay offensive, TecmoPlay defensive)
    {
        // This is simplified - real implementation would be much more complex
        PlayResult result = new PlayResult();
        
        // Check if defense guessed the play correctly
        bool defenseGuessedCorrectly = (offensive.playType == defensive.anticipatedPlay);
        
        if (defenseGuessedCorrectly)
        {
            // Defense has advantage
            result.yardsGained = UnityEngine.Random.Range(-2, 3);
            result.resultType = PlayResultType.TackledInBounds;
        }
        else
        {
            // Offense has advantage
            switch (offensive.playType)
            {
                case PlayType.Run:
                    result.yardsGained = UnityEngine.Random.Range(1, 8);
                    break;
                case PlayType.Pass:
                    result.yardsGained = UnityEngine.Random.Range(3, 15);
                    break;
            }
            result.resultType = PlayResultType.TackledInBounds;
        }
        
        // Check for special results (simplified)
        if (UnityEngine.Random.value < 0.05f) // 5% chance
        {
            result.resultType = PlayResultType.Fumble;
        }
        else if (offensive.playType == PlayType.Pass && UnityEngine.Random.value < 0.1f) // 10% chance for passes
        {
            result.resultType = PlayResultType.Interception;
        }
        
        return result;
    }
    
    private void ApplyPlayResult(PlayResult result)
    {
        OnGameMessage?.Invoke($"Result: {result.yardsGained} yards, {result.resultType}");
        
        switch (result.resultType)
        {
            case PlayResultType.TackledInBounds:
                fieldPosition += result.yardsGained;
                yardsToGo -= result.yardsGained;
                break;
                
            case PlayResultType.Fumble:
            case PlayResultType.Interception:
                // Turnover - "side change" in Tecmo Bowl terms
                homeTeamPossession = !homeTeamPossession;
                fieldPosition = 100 - fieldPosition; // Flip field position
                currentDown = 1;
                yardsToGo = 10;
                OnGameMessage?.Invoke("SIDE CHANGE!");
                break;
                
            case PlayResultType.Touchdown:
                // Score touchdown
                if (homeTeamPossession)
                    homeScore += 6;
                else
                    awayScore += 6;
                    
                OnScoreChanged?.Invoke(homeScore, awayScore);
                OnGameMessage?.Invoke("TOUCHDOWN!");
                break;
        }
        
        // Check for first down
        if (yardsToGo <= 0 && result.resultType == PlayResultType.TackledInBounds)
        {
            currentDown = 1;
            yardsToGo = 10;
            OnGameMessage?.Invoke("FIRST DOWN!");
        }
        else if (result.resultType == PlayResultType.TackledInBounds)
        {
            currentDown++;
            if (currentDown > 4)
            {
                // Turnover on downs
                homeTeamPossession = !homeTeamPossession;
                fieldPosition = 100 - fieldPosition;
                currentDown = 1;
                yardsToGo = 10;
                OnGameMessage?.Invoke("Turnover on downs!");
            }
        }
        
        // Keep field position in bounds
        fieldPosition = Mathf.Clamp(fieldPosition, 0, 100);
        
        OnDownChanged?.Invoke(currentDown, yardsToGo, fieldPosition);
    }
    
    private IEnumerator HandlePlayResult()
    {
        // Show play result for a moment
        yield return new WaitForSeconds(3f);
        
        // Check if game should continue
        if (currentGameState != GameState.GameOver)
        {
            SetGameState(GameState.PlaySelection);
        }
    }
    #endregion
    
    #region Game Events
    private IEnumerator HandleCoinToss()
    {
        OnGameMessage?.Invoke("Coin toss...");
        yield return new WaitForSeconds(2f);
        
        bool homeTeamWins = UnityEngine.Random.value > 0.5f;
        string winner = homeTeamWins ? homeTeam.teamName : awayTeam.teamName;
        
        OnGameMessage?.Invoke($"{winner} wins the toss and will receive!");
        homeTeamPossession = homeTeamWins;
        
        yield return new WaitForSeconds(2f);
        
        // Start first play
        SetGameState(GameState.PlaySelection);
    }
    
    private void HandleGameOver()
    {
        string winner = homeScore > awayScore ? homeTeam.teamName : awayTeam.teamName;
        string finalScore = $"Final Score: {awayTeam.teamName} {awayScore}, {homeTeam.teamName} {homeScore}";
        
        OnGameMessage?.Invoke($"GAME OVER! {winner} wins! {finalScore}");
        
        Debug.Log($"Game completed: {finalScore}");
    }
    #endregion
    
    #region Input Handling
    private void HandleInput()
    {
        // Handle pause menu
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (currentGameState == GameState.Playing)
            {
                SetGameState(GameState.Paused);
            }
            else if (currentGameState == GameState.Paused)
            {
                SetGameState(GameState.Playing);
            }
        }
        
        // Debug controls (remove in final build)
        if (Debug.isDebugBuild)
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                // Quick score for testing
                homeScore += 6;
                OnScoreChanged?.Invoke(homeScore, awayScore);
            }
        }
    }
    #endregion
    
    #region Public Interface
    /// <summary>
    /// Get current game time in MM:SS format
    /// </summary>
    public string GetFormattedGameTime()
    {
        int minutes = Mathf.FloorToInt(quarterTimeRemaining / 60f);
        int seconds = Mathf.FloorToInt(quarterTimeRemaining % 60f);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }
    
    /// <summary>
    /// Get current down and distance string (e.g., "1st and 10")
    /// </summary>
    public string GetDownAndDistance()
    {
        string downText = currentDown switch
        {
            1 => "1st",
            2 => "2nd", 
            3 => "3rd",
            4 => "4th",
            _ => "1st"
        };
        
        return $"{downText} and {yardsToGo}";
    }
    
    /// <summary>
    /// Get field position as string (e.g., "CHI 35")
    /// </summary>
    public string GetFieldPosition()
    {
        string teamAbbr = homeTeamPossession ? homeTeam.abbreviation : awayTeam.abbreviation;
        return $"{teamAbbr} {fieldPosition}";
    }
    
    public bool IsHomeTeamPossession()
    {
        return homeTeamPossession;
    }
    
    public NFLTeam GetPossessionTeam()
    {
        return homeTeamPossession ? homeTeam : awayTeam;
    }
    
    public NFLTeam GetDefenseTeam()
    {
        return homeTeamPossession ? awayTeam : homeTeam;
    }
    #endregion
}

#region Supporting Data Structures
/// <summary>
/// Game state enumeration for managing different phases of the game
/// </summary>
public enum GameState
{
    MainMenu,
    TeamSelection,
    CoinToss,
    PlaySelection,
    Playing,
    PlayResult,
    Paused,
    GameOver
}

/// <summary>
/// Game mode options
/// </summary>
public enum GameMode
{
    SingleGame,
    Season,
    CoachMode
}

/// <summary>
/// Result of a play execution
/// </summary>
[System.Serializable]
public class PlayResult
{
    public int yardsGained;
    public PlayResultType resultType;
    public bool clockShouldStop;
    public string description;
}

/// <summary>
/// Types of play results
/// </summary>
public enum PlayResultType
{
    TackledInBounds,
    TackledOutOfBounds, 
    Incomplete,
    Touchdown,
    FieldGoal,
    Safety,
    Fumble,
    Interception,
    Penalty
}
#endregion
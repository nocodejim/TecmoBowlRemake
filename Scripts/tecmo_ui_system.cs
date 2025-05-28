using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Main UI Manager for Tecmo Bowl Remake
/// Handles all user interface elements including the iconic play selection screen
/// Recreates the authentic Tecmo Bowl visual style with modern Unity UI
/// </summary>
public class TecmoBowlUIManager : MonoBehaviour
{
    #region Singleton
    public static TecmoBowlUIManager Instance { get; private set; }
    
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
    #endregion
    
    #region UI Panels
    [Header("Main UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject gameHudPanel;
    public GameObject playSelectionPanel;
    public GameObject teamSelectionPanel;
    public GameObject pauseMenuPanel;
    public GameObject gameOverPanel;
    
    [Header("Game HUD Elements")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI quarterText;
    public TextMeshProUGUI downAndDistanceText;
    public TextMeshProUGUI fieldPositionText;
    public TextMeshProUGUI gameMessageText;
    public Image possessionIndicator;
    
    [Header("Play Selection Elements")]
    public Transform offensivePlayContainer;
    public Transform defensivePlayContainer;
    public Button[] offensivePlayButtons = new Button[4];
    public Button[] defensivePlayButtons = new Button[4];
    public TextMeshProUGUI playSelectionTitle;
    public TextMeshProUGUI playSelectionPrompt;
    public GameObject playSelectionBackground;
    
    [Header("Team Selection Elements")]
    public Transform teamListContainer;
    public Button homeTeamConfirmButton;
    public Button awayTeamConfirmButton;
    public TextMeshProUGUI selectedTeamsText;
    
    [Header("Weather Display")]
    public Image weatherIcon;
    public TextMeshProUGUI weatherText;
    
    [Header("Player Stats Display")]
    public GameObject playerStatsPanel;
    public TextMeshProUGUI playerNameText;
    public TextMeshProUGUI playerPositionText;
    public Transform playerRatingsContainer;
    #endregion
    
    #region UI State
    private NFLTeam selectedHomeTeam;
    private NFLTeam selectedAwayTeam;
    private TecmoPlay selectedOffensivePlay;
    private TecmoPlay selectedDefensivePlay;
    private bool offensePlaySelected = false;
    private bool defensePlaySelected = false;
    private bool isPlaySelectionActive = false;
    
    // Message display
    private Queue<string> messageQueue = new Queue<string>();
    private bool isShowingMessage = false;
    #endregion
    
    #region Unity Lifecycle
    private void Start()
    {
        InitializeUI();
        SubscribeToEvents();
        ShowMainMenu();
    }
    
    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    #endregion
    
    #region Initialization
    private void InitializeUI()
    {
        // Hide all panels initially
        HideAllPanels();
        
        // Initialize play selection buttons
        SetupPlaySelectionButtons();
        
        // Setup team selection
        SetupTeamSelection();
        
        // Configure UI colors for Tecmo Bowl authenticity
        ConfigureRetroStyling();
        
        Debug.Log("UI System initialized");
    }
    
    private void SetupPlaySelectionButtons()
    {
        // Create play selection buttons if they don't exist
        if (offensivePlayContainer != null && offensivePlayButtons[0] == null)
        {
            CreatePlayButtons(offensivePlayContainer, offensivePlayButtons, true);
        }
        
        if (defensivePlayContainer != null && defensivePlayButtons[0] == null)
        {
            CreatePlayButtons(defensivePlayContainer, defensivePlayButtons, false);
        }
    }
    
    private void CreatePlayButtons(Transform container, Button[] buttonArray, bool isOffensive)
    {
        for (int i = 0; i < 4; i++)
        {
            // Create button GameObject
            GameObject buttonObj = new GameObject($"{(isOffensive ? "Offensive" : "Defensive")}Play{i + 1}");
            buttonObj.transform.SetParent(container);
            
            // Add button component
            Button button = buttonObj.AddComponent<Button>();
            buttonArray[i] = button;
            
            // Add image component for button background
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.3f, 0.8f, 0.8f); // Tecmo Bowl blue
            
            // Add text component
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform);
            TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
            buttonText.text = $"Play {i + 1}";
            buttonText.color = Color.white;
            buttonText.fontSize = 18;
            buttonText.alignment = TextAlignmentOptions.Center;
            
            // Configure button
            button.targetGraphic = buttonImage;
            button.interactable = true;
            
            // Add click listener
            int playIndex = i; // Capture for closure
            button.onClick.AddListener(() => OnPlayButtonClicked(playIndex, isOffensive));
            
            // Position button
            RectTransform rect = buttonObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 50);
            rect.anchoredPosition = new Vector2(0, -i * 60);
        }
    }
    
    private void SetupTeamSelection()
    {
        if (teamListContainer == null) return;
        
        // This would populate with all 32 NFL teams
        var teamDataManager = FindObjectOfType<TeamDataManager>();
        if (teamDataManager != null && teamDataManager.allNFLTeams.Count > 0)
        {
            CreateTeamSelectionButtons(teamDataManager.allNFLTeams);
        }
    }
    
    private void CreateTeamSelectionButtons(List<NFLTeam> teams)
    {
        foreach (var team in teams)
        {
            GameObject teamButton = new GameObject($"Team_{team.abbreviation}");
            teamButton.transform.SetParent(teamListContainer);
            
            Button button = teamButton.AddComponent<Button>();
            Image buttonImage = teamButton.AddComponent<Image>();
            
            // Set team colors
            buttonImage.color = team.primaryColor;
            
            // Add team name text
            GameObject textObj = new GameObject("TeamName");
            textObj.transform.SetParent(teamButton.transform);
            TextMeshProUGUI teamText = textObj.AddComponent<TextMeshProUGUI>();
            teamText.text = team.GetFullName();
            teamText.color = team.secondaryColor;
            teamText.fontSize = 16;
            teamText.alignment = TextAlignmentOptions.Center;
            
            // Add click listener
            button.onClick.AddListener(() => OnTeamSelected(team));
            
            // Position button (would need proper layout in real implementation)
            RectTransform rect = teamButton.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(250, 40);
        }
    }
    
    private void ConfigureRetroStyling()
    {
        // Apply Tecmo Bowl-style colors and fonts
        Color tecmoBlue = new Color(0.1f, 0.2f, 0.8f);
        Color tecmoYellow = new Color(1f, 0.9f, 0.2f);
        
        if (playSelectionBackground != null)
        {
            playSelectionBackground.GetComponent<Image>().color = tecmoBlue;
        }
        
        // Style text elements with retro feel
        var allText = FindObjectsOfType<TextMeshProUGUI>();
        foreach (var text in allText)
        {
            if (text.name.Contains("Title"))
            {
                text.color = tecmoYellow;
                text.fontSize = 24;
            }
        }
    }
    #endregion
    
    #region Event Subscription
    private void SubscribeToEvents()
    {
        TecmoBowlGameManager.OnGameStateChanged += HandleGameStateChanged;
        TecmoBowlGameManager.OnScoreChanged += UpdateScore;
        TecmoBowlGameManager.OnDownChanged += UpdateDownAndDistance;
        TecmoBowlGameManager.OnClockUpdated += UpdateGameClock;
        TecmoBowlGameManager.OnQuarterChanged += UpdateQuarter;
        TecmoBowlGameManager.OnGameMessage += ShowGameMessage;
        
        TecmoBowlPlayerController.OnPlayerTackled += HandlePlayerTackled;
        TecmoBowlPlayerController.OnPlayerScored += HandlePlayerScored;
    }
    
    private void UnsubscribeFromEvents()
    {
        TecmoBowlGameManager.OnGameStateChanged -= HandleGameStateChanged;
        TecmoBowlGameManager.OnScoreChanged -= UpdateScore;
        TecmoBowlGameManager.OnDownChanged -= UpdateDownAndDistance;
        TecmoBowlGameManager.OnClockUpdated -= UpdateGameClock;
        TecmoBowlGameManager.OnQuarterChanged -= UpdateQuarter;
        TecmoBowlGameManager.OnGameMessage -= ShowGameMessage;
        
        TecmoBowlPlayerController.OnPlayerTackled -= HandlePlayerTackled;
        TecmoBowlPlayerController.OnPlayerScored -= HandlePlayerScored;
    }
    #endregion
    
    #region Game State Handling
    private void HandleGameStateChanged(GameState newState)
    {
        Debug.Log($"UI handling state change to: {newState}");
        
        switch (newState)
        {
            case GameState.MainMenu:
                ShowMainMenu();
                break;
                
            case GameState.TeamSelection:
                ShowTeamSelection();
                break;
                
            case GameState.CoinToss:
                ShowGameHUD();
                break;
                
            case GameState.PlaySelection:
                ShowPlaySelection();
                break;
                
            case GameState.Playing:
                ShowGameHUD();
                break;
                
            case GameState.PlayResult:
                ShowGameHUD();
                break;
                
            case GameState.Paused:
                ShowPauseMenu();
                break;
                
            case GameState.GameOver:
                ShowGameOver();
                break;
        }
    }
    #endregion
    
    #region Panel Display Methods
    private void HideAllPanels()
    {
        if (mainMenuPanel) mainMenuPanel.SetActive(false);
        if (gameHudPanel) gameHudPanel.SetActive(false);
        if (playSelectionPanel) playSelectionPanel.SetActive(false);
        if (teamSelectionPanel) teamSelectionPanel.SetActive(false);
        if (pauseMenuPanel) pauseMenuPanel.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(false);
    }
    
    public void ShowMainMenu()
    {
        HideAllPanels();
        if (mainMenuPanel) mainMenuPanel.SetActive(true);
    }
    
    public void ShowTeamSelection()
    {
        HideAllPanels();
        if (teamSelectionPanel) teamSelectionPanel.SetActive(true);
    }
    
    public void ShowGameHUD()
    {
        HideAllPanels();
        if (gameHudPanel) gameHudPanel.SetActive(true);
        isPlaySelectionActive = false;
    }
    
    public void ShowPlaySelection()
    {
        HideAllPanels();
        if (playSelectionPanel) playSelectionPanel.SetActive(true);
        
        // Reset play selection state
        offensePlaySelected = false;
        defensePlaySelected = false;
        isPlaySelectionActive = true;
        
        // Update play selection UI
        UpdatePlaySelectionDisplay();
        
        Debug.Log("Play selection screen shown");
    }
    
    public void ShowPauseMenu()
    {
        if (pauseMenuPanel) pauseMenuPanel.SetActive(true);
    }
    
    public void ShowGameOver()
    {
        HideAllPanels();
        if (gameOverPanel) gameOverPanel.SetActive(true);
    }
    #endregion
    
    #region Play Selection System
    private void UpdatePlaySelectionDisplay()
    {
        if (!isPlaySelectionActive) return;
        
        var gameManager = TecmoBowlGameManager.Instance;
        if (gameManager == null) return;
        
        // Get current teams
        NFLTeam offenseTeam = gameManager.GetPossessionTeam();
        NFLTeam defenseTeam = gameManager.GetDefenseTeam();
        
        // Update title
        if (playSelectionTitle != null)
        {
            playSelectionTitle.text = "SELECT YOUR PLAY";
        }
        
        // Update prompt
        if (playSelectionPrompt != null)
        {
            string prompt = "";
            if (!offensePlaySelected)
            {
                prompt = $"{offenseTeam.teamName} - Select Offensive Play";
            }
            else if (!defensePlaySelected)
            {
                prompt = $"{defenseTeam.teamName} - Select Defensive Play";
            }
            else
            {
                prompt = "Plays Selected - Starting Play...";
            }
            playSelectionPrompt.text = prompt;
        }
        
        // Update play buttons
        UpdateOffensivePlayButtons(offenseTeam);
        UpdateDefensivePlayButtons(defenseTeam);
        
        // Check if both plays are selected
        if (offensePlaySelected && defensePlaySelected)
        {
            StartCoroutine(ExecuteSelectedPlays());
        }
    }
    
    private void UpdateOffensivePlayButtons(NFLTeam team)
    {
        if (team == null || team.offensivePlays.Count < 4) return;
        
        for (int i = 0; i < 4; i++)
        {
            if (offensivePlayButtons[i] != null)
            {
                var play = team.offensivePlays[i];
                var buttonText = offensivePlayButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = $"{i + 1}. {play.playName}";
                }
                
                // Disable if already selected or if it's defense's turn
                offensivePlayButtons[i].interactable = !offensePlaySelected;
                
                // Highlight selected play
                if (selectedOffensivePlay == play)
                {
                    offensivePlayButtons[i].GetComponent<Image>().color = Color.yellow;
                }
                else
                {
                    offensivePlayButtons[i].GetComponent<Image>().color = new Color(0.2f, 0.3f, 0.8f, 0.8f);
                }
            }
        }
    }
    
    private void UpdateDefensivePlayButtons(NFLTeam team)
    {
        if (team == null || team.defensivePlays.Count < 4) return;
        
        for (int i = 0; i < 4; i++)
        {
            if (defensivePlayButtons[i] != null)
            {
                var play = team.defensivePlays[i];
                var buttonText = defensivePlayButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = $"{i + 1}. {play.playName}";
                }
                
                // Disable if already selected or if it's offense's turn
                defensivePlayButtons[i].interactable = offensePlaySelected && !defensePlaySelected;
                
                // Highlight selected play
                if (selectedDefensivePlay == play)
                {
                    defensivePlayButtons[i].GetComponent<Image>().color = Color.red;
                }
                else
                {
                    defensivePlayButtons[i].GetComponent<Image>().color = new Color(0.8f, 0.2f, 0.2f, 0.8f);
                }
            }
        }
    }
    
    private void OnPlayButtonClicked(int playIndex, bool isOffensive)
    {
        var gameManager = TecmoBowlGameManager.Instance;
        if (gameManager == null) return;
        
        if (isOffensive && !offensePlaySelected)
        {
            var offenseTeam = gameManager.GetPossessionTeam();
            if (offenseTeam != null && playIndex < offenseTeam.offensivePlays.Count)
            {
                selectedOffensivePlay = offenseTeam.offensivePlays[playIndex];
                offensePlaySelected = true;
                
                Debug.Log($"Offensive play selected: {selectedOffensivePlay.playName}");
                
                // Play selection sound effect
                PlaySelectionSound();
            }
        }
        else if (!isOffensive && offensePlaySelected && !defensePlaySelected)
        {
            var defenseTeam = gameManager.GetDefenseTeam();
            if (defenseTeam != null && playIndex < defenseTeam.defensivePlays.Count)
            {
                selectedDefensivePlay = defenseTeam.defensivePlays[playIndex];
                defensePlaySelected = true;
                
                Debug.Log($"Defensive play selected: {selectedDefensivePlay.playName}");
                
                // Play selection sound effect
                PlaySelectionSound();
            }
        }
        
        UpdatePlaySelectionDisplay();
    }
    
    private IEnumerator ExecuteSelectedPlays()
    {
        // Brief pause to show both selections
        yield return new WaitForSeconds(1f);
        
        // Execute the plays
        var gameManager = TecmoBowlGameManager.Instance;
        if (gameManager != null)
        {
            gameManager.ExecutePlay(selectedOffensivePlay, selectedDefensivePlay);
        }
        
        // Reset selections for next play
        selectedOffensivePlay = null;
        selectedDefensivePlay = null;
        offensePlaySelected = false;
        defensePlaySelected = false;
    }
    #endregion
    
    #region HUD Updates
    private void UpdateScore(int homeScore, int awayScore)
    {
        if (scoreText != null)
        {
            var gameManager = TecmoBowlGameManager.Instance;
            if (gameManager != null)
            {
                string awayTeam = gameManager.awayTeam?.abbreviation ?? "AWAY";
                string homeTeam = gameManager.homeTeam?.abbreviation ?? "HOME";
                scoreText.text = $"{awayTeam} {awayScore} - {homeTeam} {homeScore}";
            }
            else
            {
                scoreText.text = $"AWAY {awayScore} - HOME {homeScore}";
            }
        }
    }
    
    private void UpdateGameClock(float timeRemaining)
    {
        if (timeText != null)
        {
            int minutes = Mathf.FloorToInt(timeRemaining / 60f);
            int seconds = Mathf.FloorToInt(timeRemaining % 60f);
            timeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }
    
    private void UpdateQuarter(int quarter)
    {
        if (quarterText != null)
        {
            quarterText.text = $"Q{quarter}";
        }
    }
    
    private void UpdateDownAndDistance(int down, int yardsToGo, int fieldPosition)
    {
        if (downAndDistanceText != null)
        {
            string downText = down switch
            {
                1 => "1st",
                2 => "2nd",
                3 => "3rd", 
                4 => "4th",
                _ => "1st"
            };
            downAndDistanceText.text = $"{downText} & {yardsToGo}";
        }
        
        if (fieldPositionText != null)
        {
            var gameManager = TecmoBowlGameManager.Instance;
            if (gameManager != null)
            {
                fieldPositionText.text = gameManager.GetFieldPosition();
            }
        }
    }
    
    private void ShowGameMessage(string message)
    {
        messageQueue.Enqueue(message);
        
        if (!isShowingMessage)
        {
            StartCoroutine(ProcessMessageQueue());
        }
    }
    
    private IEnumerator ProcessMessageQueue()
    {
        isShowingMessage = true;
        
        while (messageQueue.Count > 0)
        {
            string message = messageQueue.Dequeue();
            
            if (gameMessageText != null)
            {
                gameMessageText.text = message;
                gameMessageText.color = Color.yellow;
                
                // Animate message (fade in/out)
                yield return StartCoroutine(AnimateMessage());
            }
            
            yield return new WaitForSeconds(0.5f);
        }
        
        isShowingMessage = false;
    }
    
    private IEnumerator AnimateMessage()
    {
        // Fade in
        float timer = 0f;
        while (timer < 0.5f)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, timer / 0.5f);
            gameMessageText.color = new Color(1f, 1f, 0f, alpha);
            yield return null;
        }
        
        // Hold
        yield return new WaitForSeconds(2f);
        
        // Fade out
        timer = 0f;
        while (timer < 0.5f)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, timer / 0.5f);
            gameMessageText.color = new Color(1f, 1f, 0f, alpha);
            yield return null;
        }
        
        gameMessageText.text = "";
    }
    #endregion
    
    #region Team Selection
    private void OnTeamSelected(NFLTeam team)
    {
        if (selectedHomeTeam == null)
        {
            selectedHomeTeam = team;
            Debug.Log($"Home team selected: {team.GetFullName()}");
        }
        else if (selectedAwayTeam == null && team != selectedHomeTeam)
        {
            selectedAwayTeam = team;
            Debug.Log($"Away team selected: {team.GetFullName()}");
        }
        
        UpdateTeamSelectionDisplay();
    }
    
    private void UpdateTeamSelectionDisplay()
    {
        if (selectedTeamsText != null)
        {
            string homeTeam = selectedHomeTeam?.GetFullName() ?? "Select Home Team";
            string awayTeam = selectedAwayTeam?.GetFullName() ?? "Select Away Team";
            selectedTeamsText.text = $"Away: {awayTeam}\nHome: {homeTeam}";
        }
        
        // Enable confirm button if both teams selected
        if (homeTeamConfirmButton != null)
        {
            homeTeamConfirmButton.interactable = selectedHomeTeam != null && selectedAwayTeam != null;
        }
    }
    
    public void OnConfirmTeamSelection()
    {
        if (selectedHomeTeam != null && selectedAwayTeam != null)
        {
            var gameManager = TecmoBowlGameManager.Instance;
            if (gameManager != null)
            {
                gameManager.StartNewGame(selectedHomeTeam, selectedAwayTeam);
            }
        }
    }
    #endregion
    
    #region Player Events
    private void HandlePlayerTackled(TecmoBowlPlayerController player)
    {
        ShowGameMessage($"{player.playerData.playerName} tackled!");
    }
    
    private void HandlePlayerScored(TecmoBowlPlayerController player)
    {
        ShowGameMessage($"TOUCHDOWN! {player.playerData.playerName}!");
    }
    #endregion
    
    #region Audio & Effects
    private void PlaySelectionSound()
    {
        // Play authentic Tecmo Bowl selection beep
        // This would use Unity's AudioSource component
        Debug.Log("*BEEP* - Play selected sound");
    }
    
    private void PlayTackleSound()
    {
        Debug.Log("*THUD* - Tackle sound");
    }
    
    private void PlayTouchdownSound()
    {
        Debug.Log("*TOUCHDOWN MUSIC* - Celebration sound");
    }
    #endregion
    
    #region Main Menu Buttons
    public void OnStartGameClicked()
    {
        var gameManager = TecmoBowlGameManager.Instance;
        if (gameManager != null)
        {
            gameManager.SetGameState(GameState.TeamSelection);
        }
    }
    
    public void OnQuickPlayClicked()
    {
        // Start quick game with random teams
        var teamDataManager = FindObjectOfType<TeamDataManager>();
        if (teamDataManager != null && teamDataManager.allNFLTeams.Count >= 2)
        {
            var randomHome = teamDataManager.GetRandomTeam();
            NFLTeam randomAway;
            do
            {
                randomAway = teamDataManager.GetRandomTeam();
            } while (randomAway == randomHome);
            
            var gameManager = TecmoBowlGameManager.Instance;
            if (gameManager != null)
            {
                gameManager.StartNewGame(randomHome, randomAway);
            }
        }
    }
    
    public void OnExitGameClicked()
    {
        Application.Quit();
    }
    #endregion
    
    #region Pause Menu
    public void OnResumeClicked()
    {
        var gameManager = TecmoBowlGameManager.Instance;
        if (gameManager != null)
        {
            gameManager.SetGameState(GameState.Playing);
        }
    }
    
    public void OnMainMenuClicked()
    {
        var gameManager = TecmoBowlGameManager.Instance;
        if (gameManager != null)
        {
            gameManager.SetGameState(GameState.MainMenu);
        }
    }
    #endregion
    
    #region Weather System
    public void UpdateWeatherDisplay(WeatherType weather)
    {
        if (weatherText != null)
        {
            weatherText.text = weather switch
            {
                WeatherType.Clear => "CLEAR",
                WeatherType.Rain => "RAIN",
                WeatherType.Snow => "SNOW",
                _ => "CLEAR"
            };
        }
        
        // Update weather icon
        if (weatherIcon != null)
        {
            // Set appropriate weather sprite
            // This would load from Resources or be assigned in Inspector
        }
    }
    #endregion
}

#region Supporting Enums
public enum WeatherType
{
    Clear,
    Rain,
    Snow
}
#endregion
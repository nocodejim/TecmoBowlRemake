using UnityEngine;
using UnityEngine.SceneManagement; // Required for scene management
// Potentially add: using UnityEngine.UI; if you directly reference UI elements like Dropdown, Button

public class MenuManager : MonoBehaviour
{
    // Placeholder for team selection logic
    // public Dropdown team1Dropdown; // Example: Assign in Inspector
    // public Dropdown team2Dropdown; // Example: Assign in Inspector

    public static string SelectedTeam1Abbreviation { get; private set; }
    public static string SelectedTeam2Abbreviation { get; private set; }

    void Start()
    {
        // Initialize UI elements here if needed
        // e.g., Populate dropdowns with team names from TeamDataManager
        // For now, we can assume TeamDataManager will be available in the GameplayScene
        // or use placeholder values.
        Debug.Log("MenuManager started. Implement team selection and start game logic.");
    }

    public void SelectTeam1(string teamAbbreviation)
    {
        SelectedTeam1Abbreviation = teamAbbreviation;
        Debug.Log("Team 1 selected: " + teamAbbreviation);
    }

    public void SelectTeam2(string teamAbbreviation)
    {
        SelectedTeam2Abbreviation = teamAbbreviation;
        Debug.Log("Team 2 selected: " + teamAbbreviation);
    }

    public void StartGame()
    {
        // Basic validation (optional for now)
        if (string.IsNullOrEmpty(SelectedTeam1Abbreviation) || string.IsNullOrEmpty(SelectedTeam2Abbreviation))
        {
            Debug.LogError("Both teams must be selected before starting the game.");
            return;
        }

        // Load the GameplayScene
        // Ensure "GameplayScene" will be added to Build Settings
        SceneManager.LoadScene("GameplayScene");
    }
}

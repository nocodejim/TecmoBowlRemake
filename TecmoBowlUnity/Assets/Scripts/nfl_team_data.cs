using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ScriptableObject for NFL Team data - allows easy editing in Unity Inspector
/// Contains all team information, colors, and player rosters for 2024-2025 season
/// </summary>
[CreateAssetMenu(fileName = "New NFL Team", menuName = "Tecmo Bowl/NFL Team")]
public class NFLTeam : ScriptableObject
{
    [Header("Team Identity")]
    public string teamName;
    public string cityName;
    public string abbreviation;
    public int teamId; // Unique identifier
    
    [Header("Team Colors")]
    public Color primaryColor = Color.white;
    public Color secondaryColor = Color.black;
    public Color accentColor = Color.gray;
    
    [Header("Team Logo & Graphics")]
    public Sprite teamLogo;
    public Sprite helmetSprite;
    public Sprite uniformSprite;
    
    [Header("Team Statistics")]
    [Range(1, 5)] public int overallRating = 3;
    [Range(1, 5)] public int offenseRating = 3;
    [Range(1, 5)] public int defenseRating = 3;
    [Range(1, 5)] public int specialTeamsRating = 3;
    
    [Header("Playbook")]
    public List<TecmoPlay> offensivePlays = new List<TecmoPlay>(4);
    public List<TecmoPlay> defensivePlays = new List<TecmoPlay>(4);
    
    [Header("Roster - Offense")]
    public TecmoPlayer quarterback;
    public TecmoPlayer runningBack;
    public TecmoPlayer receiver1;
    public TecmoPlayer receiver2;
    
    [Header("Roster - Defense")]
    public TecmoPlayer linebacker;
    public TecmoPlayer cornerback;
    public TecmoPlayer safety;
    
    [Header("Special Teams")]
    public TecmoPlayer kicker;
    public TecmoPlayer punter;
    
    /// <summary>
    /// Get all offensive players as a list
    /// </summary>
    public List<TecmoPlayer> GetOffensivePlayers()
    {
        return new List<TecmoPlayer> { quarterback, runningBack, receiver1, receiver2 };
    }
    
    /// <summary>
    /// Get all defensive players as a list
    /// </summary>
    public List<TecmoPlayer> GetDefensivePlayers()
    {
        return new List<TecmoPlayer> { linebacker, cornerback, safety };
    }
    
    /// <summary>
    /// Get star player (highest rated offensive player)
    /// </summary>
    public TecmoPlayer GetStarPlayer()
    {
        var offensivePlayers = GetOffensivePlayers();
        return offensivePlayers.OrderByDescending(p => p.GetOverallRating()).FirstOrDefault();
    }
    
    /// <summary>
    /// Get team display name (e.g., "Kansas City Chiefs")
    /// </summary>
    public string GetFullName()
    {
        return $"{cityName} {teamName}";
    }
}

/// <summary>
/// Individual player data with Tecmo Bowl-style ratings
/// Authentic to original game's rating system (MS, BC, PC, HP, RC)
/// </summary>
[System.Serializable]
public class TecmoPlayer
{
    [Header("Player Identity")]
    public string playerName;
    public string position;
    public int jerseyNumber;
    public bool isStarPlayer = false; // Bo Jackson/Jerry Rice equivalent
    
    [Header("Tecmo Bowl Ratings (0-7 scale like original)")]
    [Range(0, 7)] public int maxSpeed = 4;           // MS - Maximum Speed
    [Range(0, 7)] public int ballControl = 4;        // BC - Ball Control (fumble resistance)
    [Range(0, 7)] public int passControl = 4;        // PC - Pass Control (throwing accuracy)
    [Range(0, 7)] public int hitPower = 4;           // HP - Hit Power (tackling/breaking tackles)
    [Range(0, 7)] public int receiving = 4;          // RC - Receiving (catching ability)
    
    [Header("Modern Additions")]
    [Range(0, 100)] public int awareness = 50;       // Modern addition for AI behavior
    [Range(0, 100)] public int clutch = 50;          // Performance in key situations
    
    [Header("Player Sprite")]
    public Sprite playerSprite;
    
    /// <summary>
    /// Calculate overall player rating (0-100 scale for modern compatibility)
    /// </summary>
    public int GetOverallRating()
    {
        // Weight the ratings based on position
        float total = 0;
        int count = 0;
        
        if (position.Contains("QB"))
        {
            total += passControl * 3 + maxSpeed * 2 + awareness * 2;
            count = 7;
        }
        else if (position.Contains("RB"))
        {
            total += maxSpeed * 3 + ballControl * 2 + hitPower * 2;
            count = 7;
        }
        else if (position.Contains("WR") || position.Contains("TE"))
        {
            total += receiving * 3 + maxSpeed * 2 + ballControl * 2;
            count = 7;
        }
        else // Defensive players
        {
            total += hitPower * 2 + maxSpeed * 2 + awareness * 2 + ballControl;
            count = 7;
        }
        
        return Mathf.RoundToInt((total / count) * (100f / 7f));
    }
    
    /// <summary>
    /// Check if player can break tackle based on hit power vs tackler
    /// </summary>
    public bool AttemptTackleBreak(TecmoPlayer tackler)
    {
        if (isStarPlayer)
        {
            // Star players get bonus (Bo Jackson effect)
            return (hitPower + 2) > tackler.hitPower + Random.Range(0, 3);
        }
        
        return hitPower > tackler.hitPower + Random.Range(0, 2);
    }
    
    /// <summary>
    /// Attempt to catch a pass based on receiving rating
    /// </summary>
    public bool AttemptCatch(int passDifficulty = 3)
    {
        int catchChance = receiving + (isStarPlayer ? 2 : 0);
        return catchChance >= passDifficulty + Random.Range(0, 3);
    }
}

/// <summary>
/// Tecmo Bowl play data - authentic 4-play system
/// Each team has 4 offensive plays and 4 defensive alignments
/// </summary>
[System.Serializable]
public class TecmoPlay
{
    [Header("Play Identity")]
    public string playName;
    public PlayType playType;
    public int playNumber; // 1-4 for Tecmo Bowl authenticity
    
    [Header("Play Characteristics")]
    public PlayDirection direction;
    public int averageYards = 5;
    public int maxYards = 15;
    [Range(0f, 1f)] public float successRate = 0.7f;
    
    [Header("Play Animation")]
    public string animationTrigger;
    public Sprite playDiagramSprite;
    
    [Header("Defensive Settings (for defensive plays)")]
    public PlayType anticipatedPlay; // What offensive play this defense is designed to stop
    
    /// <summary>
    /// Calculate play effectiveness against a defensive play
    /// Returns modifier for success (0.5 = 50% effectiveness, 1.5 = 150% effectiveness)
    /// </summary>
    public float GetEffectivenessAgainst(TecmoPlay defensivePlay)
    {
        // If defense correctly anticipated this play type, offense is less effective
        if (defensivePlay.anticipatedPlay == this.playType)
        {
            return 0.6f; // 40% penalty for being anticipated
        }
        
        // If defense anticipated wrong play type, offense gets bonus
        return 1.3f; // 30% bonus for not being anticipated
    }
}

/// <summary>
/// Play types in Tecmo Bowl
/// </summary>
public enum PlayType
{
    Run,
    Pass,
    Defense // For defensive plays
}

/// <summary>
/// Play direction
/// </summary>
public enum PlayDirection
{
    Left,
    Right,
    Center,
    Deep
}

/// <summary>
/// Manager for all NFL team data - handles loading and accessing teams
/// This is where we store all 32 NFL teams with current 2024-2025 rosters
/// </summary>
public class TeamDataManager : MonoBehaviour
{
    [Header("NFL Teams")]
    public List<NFLTeam> allNFLTeams = new List<NFLTeam>();
    
    [Header("Current Season")]
    public string seasonYear = "2024-2025";
    public NFLTeam superbowlTeam1; // Chiefs
    public NFLTeam superbowlTeam2; // Eagles
    
    private void Awake()
    {
        // Initialize teams if not already loaded
        if (allNFLTeams.Count == 0)
        {
            CreateDefaultTeams();
        }
    }
    
    /// <summary>
    /// Get team by abbreviation (e.g., "KC", "PHI")
    /// </summary>
    public NFLTeam GetTeamByAbbreviation(string abbr)
    {
        return allNFLTeams.FirstOrDefault(team => team.abbreviation.Equals(abbr, System.StringComparison.OrdinalIgnoreCase));
    }
    
    /// <summary>
    /// Get team by city name
    /// </summary>
    public NFLTeam GetTeamByCity(string city)
    {
        return allNFLTeams.FirstOrDefault(team => team.cityName.Equals(city, System.StringComparison.OrdinalIgnoreCase));
    }
    
    /// <summary>
    /// Get teams by division or conference
    /// </summary>
    public List<NFLTeam> GetTeamsByRating(int minRating = 3)
    {
        return allNFLTeams.Where(team => team.overallRating >= minRating).ToList();
    }
    
    /// <summary>
    /// Get random team for quick play
    /// </summary>
    public NFLTeam GetRandomTeam()
    {
        return allNFLTeams[Random.Range(0, allNFLTeams.Count)];
    }
    
    /// <summary>
    /// Create default team data for all 32 NFL teams
    /// This would normally be loaded from external data files
    /// </summary>
    private void CreateDefaultTeams()
    {
        Debug.Log("Creating default NFL team data...");
        
        // Sample teams - in real implementation, this would load from JSON/XML files
        CreateKansasCityChiefs();
        CreatePhiladelphiaEagles();
        CreateBuffaloBills();
        CreateDallasCowboys();
        // ... etc for all 32 teams
        
        Debug.Log($"Loaded {allNFLTeams.Count} NFL teams");
    }
    
    #region Team Creation Methods
    private void CreateKansasCityChiefs()
    {
        var chiefs = ScriptableObject.CreateInstance<NFLTeam>();
        chiefs.teamName = "Chiefs";
        chiefs.cityName = "Kansas City";
        chiefs.abbreviation = "KC";
        chiefs.teamId = 1;
        chiefs.primaryColor = new Color(0.88f, 0.1f, 0.14f); // Red
        chiefs.secondaryColor = new Color(1f, 0.84f, 0f); // Gold
        chiefs.overallRating = 5; // Elite team
        chiefs.offenseRating = 5;
        chiefs.defenseRating = 4;
        
        // Star players (2024-2025 season)
        chiefs.quarterback = new TecmoPlayer
        {
            playerName = "Patrick Mahomes",
            position = "QB",
            jerseyNumber = 15,
            isStarPlayer = true,
            maxSpeed = 5,
            ballControl = 6,
            passControl = 7, // Elite passer
            hitPower = 4,
            receiving = 0,
            awareness = 95,
            clutch = 99 // Mr. Clutch
        };
        
        chiefs.runningBack = new TecmoPlayer
        {
            playerName = "Isiah Pacheco",
            position = "RB",
            jerseyNumber = 10,
            maxSpeed = 6,
            ballControl = 5,
            hitPower = 5,
            awareness = 75
        };
        
        chiefs.receiver1 = new TecmoPlayer
        {
            playerName = "Travis Kelce",
            position = "TE",
            jerseyNumber = 87,
            isStarPlayer = true,
            maxSpeed = 4,
            ballControl = 6,
            receiving = 7, // Elite receiver
            hitPower = 5,
            awareness = 90,
            clutch = 85
        };
        
        // Create playbook
        chiefs.offensivePlays = CreateChiefsPlaybook();
        
        allNFLTeams.Add(chiefs);
        superbowlTeam1 = chiefs;
    }
    
    private void CreatePhiladelphiaEagles()
    {
        var eagles = ScriptableObject.CreateInstance<NFLTeam>();
        eagles.teamName = "Eagles";
        eagles.cityName = "Philadelphia";
        eagles.abbreviation = "PHI";
        eagles.teamId = 2;
        eagles.primaryColor = new Color(0f, 0.2f, 0.18f); // Midnight Green
        eagles.secondaryColor = Color.white;
        eagles.accentColor = new Color(0.64f, 0.64f, 0.64f); // Silver
        eagles.overallRating = 5; // Elite team
        eagles.offenseRating = 5;
        eagles.defenseRating = 4;
        
        eagles.quarterback = new TecmoPlayer
        {
            playerName = "Jalen Hurts",
            position = "QB",
            jerseyNumber = 1,
            isStarPlayer = true,
            maxSpeed = 6, // Mobile QB
            ballControl = 5,
            passControl = 5,
            hitPower = 6,
            awareness = 85,
            clutch = 80
        };
        
        eagles.runningBack = new TecmoPlayer
        {
            playerName = "Saquon Barkley",
            position = "RB",
            jerseyNumber = 26,
            isStarPlayer = true,
            maxSpeed = 7, // Elite speed
            ballControl = 6,
            hitPower = 6,
            awareness = 85,
            clutch = 75
        };
        
        eagles.receiver1 = new TecmoPlayer
        {
            playerName = "A.J. Brown",
            position = "WR",
            jerseyNumber = 11,
            isStarPlayer = true,
            maxSpeed = 6,
            ballControl = 5,
            receiving = 6,
            hitPower = 5,
            awareness = 80
        };
        
        allNFLTeams.Add(eagles);
        superbowlTeam2 = eagles;
    }
    
    private void CreateBuffaloBills()
    {
        var bills = ScriptableObject.CreateInstance<NFLTeam>();
        bills.teamName = "Bills";
        bills.cityName = "Buffalo";
        bills.abbreviation = "BUF";
        bills.teamId = 3;
        bills.primaryColor = new Color(0f, 0.2f, 0.65f); // Bills Blue
        bills.secondaryColor = Color.red;
        bills.overallRating = 4;
        bills.offenseRating = 5;
        bills.defenseRating = 4;
        
        bills.quarterback = new TecmoPlayer
        {
            playerName = "Josh Allen",
            position = "QB",
            jerseyNumber = 17,
            isStarPlayer = true,
            maxSpeed = 5,
            ballControl = 5,
            passControl = 6,
            hitPower = 6, // Strong arm, can truck defenders
            awareness = 85,
            clutch = 85
        };
        
        allNFLTeams.Add(bills);
    }
    
    private void CreateDallasCowboys()
    {
        var cowboys = ScriptableObject.CreateInstance<NFLTeam>();
        cowboys.teamName = "Cowboys";
        cowboys.cityName = "Dallas";
        cowboys.abbreviation = "DAL";
        cowboys.teamId = 4;
        cowboys.primaryColor = new Color(0f, 0.2f, 0.4f); // Navy
        cowboys.secondaryColor = new Color(0.7f, 0.7f, 0.7f); // Silver
        cowboys.accentColor = Color.white;
        cowboys.overallRating = 3;
        cowboys.offenseRating = 4;
        cowboys.defenseRating = 3;
        
        cowboys.quarterback = new TecmoPlayer
        {
            playerName = "Dak Prescott",
            position = "QB",
            jerseyNumber = 4,
            maxSpeed = 4,
            ballControl = 5,
            passControl = 5,
            hitPower = 4,
            awareness = 80
        };
        
        allNFLTeams.Add(cowboys);
    }
    #endregion
    
    #region Playbook Creation
    private List<TecmoPlay> CreateChiefsPlaybook()
    {
        return new List<TecmoPlay>
        {
            new TecmoPlay
            {
                playName = "Mahomes Rollout Right",
                playType = PlayType.Pass,
                playNumber = 1,
                direction = PlayDirection.Right,
                averageYards = 8,
                maxYards = 25,
                successRate = 0.75f
            },
            new TecmoPlay
            {
                playName = "Pacheco Up the Middle",
                playType = PlayType.Run,
                playNumber = 2,
                direction = PlayDirection.Center,
                averageYards = 4,
                maxYards = 12,
                successRate = 0.70f
            },
            new TecmoPlay
            {
                playName = "Kelce Deep Route",
                playType = PlayType.Pass,
                playNumber = 3,
                direction = PlayDirection.Deep,
                averageYards = 12,
                maxYards = 35,
                successRate = 0.65f
            },
            new TecmoPlay
            {
                playName = "Quick Slant Left",
                playType = PlayType.Pass,
                playNumber = 4,
                direction = PlayDirection.Left,
                averageYards = 6,
                maxYards = 15,
                successRate = 0.80f
            }
        };
    }
    #endregion
}

/// <summary>
/// Static data for easy access to team information
/// </summary>
public static class NFLData
{
    public static readonly string[] AllTeamAbbreviations = {
        "ARI", "ATL", "BAL", "BUF", "CAR", "CHI", "CIN", "CLE",
        "DAL", "DEN", "DET", "GB", "HOU", "IND", "JAX", "KC",
        "LV", "LAC", "LAR", "MIA", "MIN", "NE", "NO", "NYG",
        "NYJ", "PHI", "PIT", "SF", "SEA", "TB", "TEN", "WAS"
    };
    
    public static readonly Dictionary<string, string> TeamCities = new Dictionary<string, string>
    {
        {"ARI", "Arizona"}, {"ATL", "Atlanta"}, {"BAL", "Baltimore"}, {"BUF", "Buffalo"},
        {"CAR", "Carolina"}, {"CHI", "Chicago"}, {"CIN", "Cincinnati"}, {"CLE", "Cleveland"},
        {"DAL", "Dallas"}, {"DEN", "Denver"}, {"DET", "Detroit"}, {"GB", "Green Bay"},
        {"HOU", "Houston"}, {"IND", "Indianapolis"}, {"JAX", "Jacksonville"}, {"KC", "Kansas City"},
        {"LV", "Las Vegas"}, {"LAC", "Los Angeles"}, {"LAR", "Los Angeles"}, {"MIA", "Miami"},
        {"MIN", "Minnesota"}, {"NE", "New England"}, {"NO", "New Orleans"}, {"NYG", "New York"},
        {"NYJ", "New York"}, {"PHI", "Philadelphia"}, {"PIT", "Pittsburgh"}, {"SF", "San Francisco"},
        {"SEA", "Seattle"}, {"TB", "Tampa Bay"}, {"TEN", "Tennessee"}, {"WAS", "Washington"}
    };
}
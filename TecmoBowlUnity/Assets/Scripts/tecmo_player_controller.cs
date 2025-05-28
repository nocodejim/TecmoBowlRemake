using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Main player controller for Tecmo Bowl gameplay
/// Handles the authentic arcade-style movement and one-player-at-a-time control
/// This recreates the classic Tecmo Bowl feel with modern Unity features
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class TecmoBowlPlayerController : MonoBehaviour
{
    #region Core Components
    [Header("Core Components")]
    public TecmoPlayer playerData;
    public PlayerTeam team;
    public PlayerRole role;
    
    private Rigidbody2D rb;
    private Collider2D col;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    
    // Player control state
    private bool isPlayerControlled = false;
    private bool isActive = true;
    private bool hasBall = false;
    #endregion
    
    #region Movement & Physics
    [Header("Movement Settings")]
    [SerializeField] private float baseSpeed = 5f;
    [SerializeField] private float sprintMultiplier = 1.5f;
    [SerializeField] private float tackleRange = 1.2f;
    [SerializeField] private float passRange = 8f;
    
    // Movement variables
    private Vector2 moveDirection;
    private float currentSpeed;
    private bool isSprinting = false;
    private bool isTackled = false;
    
    // Authentic Tecmo Bowl physics
    private const float FIELD_DRAG = 2f;
    private const float TACKLE_FORCE = 8f;
    #endregion
    
    #region Ball Handling
    [Header("Ball Handling")]
    public Transform ballPosition;
    private GameObject ballObject;
    private bool isPassingMode = false;
    private bool canCatchBall = true;
    private Vector2 passTarget;
    #endregion
    
    #region AI Behavior
    [Header("AI Settings")]
    public AIBehaviorType aiBehavior = AIBehaviorType.FollowBall;
    private Vector2 aiTarget;
    private float aiUpdateTime = 0.2f;
    private float aiTimer = 0f;
    private TecmoBowlPlayerController ballCarrier;
    #endregion
    
    #region Events
    public static System.Action<TecmoBowlPlayerController> OnPlayerTackled;
    public static System.Action<TecmoBowlPlayerController> OnPlayerScored;
    public static System.Action<TecmoBowlPlayerController, TecmoBowlPlayerController> OnPlayerCaughtPass;
    public static System.Action<Vector2> OnBallThrown;
    #endregion
    
    #region Unity Lifecycle
    private void Awake()
    {
        // Get required components
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        
        // Configure physics for Tecmo Bowl feel
        rb.drag = FIELD_DRAG;
        rb.gravityScale = 0f; // Top-down game
        
        // Set up collision detection
        col.isTrigger = false; // We want physical collisions
    }
    
    private void Start()
    {
        InitializePlayer();
    }
    
    private void Update()
    {
        if (!isActive || isTackled) return;
        
        if (isPlayerControlled)
        {
            HandlePlayerInput();
        }
        else
        {
            HandleAI();
        }
        
        UpdateAnimation();
    }
    
    private void FixedUpdate()
    {
        if (!isActive || isTackled) return;
        
        // Apply movement
        ApplyMovement();
        
        // Check for tackles and interactions
        CheckInteractions();
    }
    #endregion
    
    #region Player Initialization
    private void InitializePlayer()
    {
        if (playerData == null)
        {
            Debug.LogWarning($"No player data assigned to {gameObject.name}");
            return;
        }
        
        // Set player name
        gameObject.name = $"{playerData.playerName} ({playerData.position})";
        
        // Calculate speed based on player rating
        currentSpeed = baseSpeed * (1f + (playerData.maxSpeed / 7f));
        
        // Set team colors
        if (spriteRenderer != null)
        {
            // Team colors would be set by TeamManager
            UpdatePlayerAppearance();
        }
        
        // Initialize ball position if needed
        if (ballPosition == null)
        {
            GameObject ballPos = new GameObject("BallPosition");
            ballPos.transform.SetParent(transform);
            ballPos.transform.localPosition = new Vector3(0.5f, 0f, 0f);
            ballPosition = ballPos.transform;
        }
        
        Debug.Log($"Initialized player: {playerData.playerName} - Speed: {currentSpeed}");
    }
    
    private void UpdatePlayerAppearance()
    {
        if (playerData.playerSprite != null)
        {
            spriteRenderer.sprite = playerData.playerSprite;
        }
        
        // Add player number text (would be handled by UI system)
        UpdatePlayerNumber();
    }
    
    private void UpdatePlayerNumber()
    {
        // Create or update number display
        Transform numberDisplay = transform.Find("NumberDisplay");
        if (numberDisplay == null)
        {
            GameObject numberObj = new GameObject("NumberDisplay");
            numberObj.transform.SetParent(transform);
            numberObj.transform.localPosition = Vector3.zero;
            
            // Add TextMesh component for number display
            var textMesh = numberObj.AddComponent<TextMesh>();
            textMesh.text = playerData.jerseyNumber.ToString();
            textMesh.fontSize = 20;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.color = Color.white;
        }
    }
    #endregion
    
    #region Player Input
    private void HandlePlayerInput()
    {
        // Get input (Tecmo Bowl style - simple directional movement)
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        moveDirection = new Vector2(horizontal, vertical).normalized;
        
        // Sprint button (classic Tecmo Bowl had one action button)
        isSprinting = Input.GetButton("Fire1") || Input.GetButton("Jump");
        
        // Passing (if quarterback with ball)
        if (hasBall && playerData.position.Contains("QB") && Input.GetButtonDown("Fire2"))
        {
            EnterPassingMode();
        }
        
        // Throw pass
        if (isPassingMode && Input.GetButtonDown("Fire1"))
        {
            ThrowPass();
        }
        
        // Lateral/Pitch (rare but authentic Tecmo Bowl feature)
        if (hasBall && Input.GetButtonDown("Fire3"))
        {
            AttemptLateral();
        }
    }
    #endregion
    
    #region Movement System
    private void ApplyMovement()
    {
        if (moveDirection == Vector2.zero) return;
        
        // Calculate final speed with sprint modifier
        float finalSpeed = currentSpeed;
        if (isSprinting && playerData.maxSpeed > 4) // Only fast players can effectively sprint
        {
            finalSpeed *= sprintMultiplier;
            
            // Star players get extra sprint boost (Bo Jackson effect)
            if (playerData.isStarPlayer)
            {
                finalSpeed *= 1.2f;
            }
        }
        
        // Apply speed penalty if carrying ball
        if (hasBall)
        {
            finalSpeed *= 0.9f; // 10% slower with ball
        }
        
        // Move the player
        Vector2 targetVelocity = moveDirection * finalSpeed;
        rb.velocity = Vector2.Lerp(rb.velocity, targetVelocity, Time.fixedDeltaTime * 10f);
        
        // Update sprite direction
        if (moveDirection.x != 0)
        {
            spriteRenderer.flipX = moveDirection.x < 0;
        }
    }
    #endregion
    
    #region Ball Handling
    public void GiveBall()
    {
        hasBall = true;
        
        // Create ball visual if needed
        if (ballObject == null)
        {
            ballObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ballObject.name = "Football";
            ballObject.transform.SetParent(ballPosition);
            ballObject.transform.localPosition = Vector3.zero;
            ballObject.transform.localScale = Vector3.one * 0.3f;
            
            // Make it brown like a football
            ballObject.GetComponent<Renderer>().material.color = new Color(0.6f, 0.3f, 0.1f);
            
            // Remove collider - ball is just visual when held
            Destroy(ballObject.GetComponent<Collider>());
        }
        
        Debug.Log($"{playerData.playerName} has the ball!");
    }
    
    public void LoseBall()
    {
        hasBall = false;
        isPassingMode = false;
        
        if (ballObject != null)
        {
            Destroy(ballObject);
        }
        
        Debug.Log($"{playerData.playerName} lost the ball!");
    }
    
    private void EnterPassingMode()
    {
        isPassingMode = true;
        Debug.Log($"{playerData.playerName} is looking to pass...");
        
        // Slow down when passing (authentic Tecmo Bowl behavior)
        currentSpeed *= 0.5f;
        
        // Show passing targets (would be handled by UI system)
        ShowPassTargets();
    }
    
    private void ShowPassTargets()
    {
        // Find all eligible receivers
        var receivers = FindObjectsOfType<TecmoBowlPlayerController>();
        
        foreach (var receiver in receivers)
        {
            if (receiver.team == this.team && 
                receiver.role == PlayerRole.Receiver && 
                receiver != this)
            {
                // Highlight receiver (visual effect)
                receiver.HighlightAsTarget(true);
            }
        }
    }
    
    private void ThrowPass()
    {
        if (!isPassingMode) return;
        
        // Calculate pass target (simplified - in real game this would be more complex)
        Vector2 passDirection = Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position;
        passTarget = (Vector2)transform.position + passDirection.normalized * passRange;
        
        // Pass accuracy based on player rating and pressure
        float accuracy = playerData.passControl / 7f;
        if (isSprinting) accuracy *= 0.7f; // Penalty for throwing on the run
        
        // Create the football projectile
        CreatePassingBall(passTarget, accuracy);
        
        // Player no longer has ball
        LoseBall();
        isPassingMode = false;
        
        OnBallThrown?.Invoke(passTarget);
        
        Debug.Log($"{playerData.playerName} throws to {passTarget}!");
    }
    
    private void CreatePassingBall(Vector2 target, float accuracy)
    {
        // Create football object
        GameObject football = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        football.name = "PassingFootball";
        football.transform.position = ballPosition.position;
        football.transform.localScale = Vector3.one * 0.3f;
        
        // Add ball physics
        var ballRb = football.AddComponent<Rigidbody2D>();
        ballRb.gravityScale = 0f;
        
        // Add pass behavior
        var passBehavior = football.AddComponent<FootballPassBehavior>();
        passBehavior.Initialize(target, accuracy, team);
        
        // Make it brown
        football.GetComponent<Renderer>().material.color = new Color(0.6f, 0.3f, 0.1f);
    }
    
    private void AttemptLateral()
    {
        // Find nearest teammate for lateral pass
        var teammates = FindTeammates();
        TecmoBowlPlayerController nearestTeammate = null;
        float nearestDistance = float.MaxValue;
        
        foreach (var teammate in teammates)
        {
            float distance = Vector2.Distance(transform.position, teammate.transform.position);
            if (distance < nearestDistance && distance < 3f) // Lateral range
            {
                nearestDistance = distance;
                nearestTeammate = teammate;
            }
        }
        
        if (nearestTeammate != null)
        {
            // Transfer ball
            LoseBall();
            nearestTeammate.GiveBall();
            Debug.Log($"Lateral from {playerData.playerName} to {nearestTeammate.playerData.playerName}!");
        }
    }
    #endregion
    
    #region Tackling System
    private void CheckInteractions()
    {
        if (!hasBall) return; // Only ball carrier can be tackled
        
        // Check for nearby defenders
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, tackleRange);
        
        foreach (var collider in nearbyColliders)
        {
            var otherPlayer = collider.GetComponent<TecmoBowlPlayerController>();
            if (otherPlayer != null && otherPlayer.team != this.team && otherPlayer.isActive)
            {
                AttemptTackle(otherPlayer);
                break; // One tackle attempt per frame
            }
        }
    }
    
    private void AttemptTackle(TecmoBowlPlayerController tackler)
    {
        // Calculate tackle success based on player ratings
        bool tackleSuccessful = !playerData.AttemptTackleBreak(tackler.playerData);
        
        if (tackleSuccessful)
        {
            GetTackled(tackler);
        }
        else
        {
            // Tackle broken! 
            Debug.Log($"{playerData.playerName} breaks the tackle from {tackler.playerData.playerName}!");
            
            // Push tackler away
            Vector2 pushDirection = (tackler.transform.position - transform.position).normalized;
            tackler.rb.AddForce(pushDirection * TACKLE_FORCE, ForceMode2D.Impulse);
            
            // Brief invincibility to prevent multiple tackle attempts
            StartCoroutine(TackleInvincibility());
        }
    }
    
    private void GetTackled(TecmoBowlPlayerController tackler)
    {
        isTackled = true;
        rb.velocity = Vector2.zero;
        
        Debug.Log($"{playerData.playerName} tackled by {tackler.playerData.playerName}!");
        
        // Play tackle animation
        if (animator != null)
        {
            animator.SetTrigger("Tackled");
        }
        
        // Check for fumble (rare but impactful like original)
        if (Random.value < 0.02f) // 2% fumble chance
        {
            AttemptFumble();
        }
        
        OnPlayerTackled?.Invoke(this);
        
        // End the play
        StartCoroutine(EndPlaySequence());
    }
    
    private void AttemptFumble()
    {
        // Fumble chance based on ball control rating
        float fumbleChance = 0.05f - (playerData.ballControl / 7f * 0.04f);
        
        if (Random.value < fumbleChance)
        {
            Debug.Log($"FUMBLE by {playerData.playerName}!");
            
            // Create fumbled ball
            CreateFumbledBall();
            LoseBall();
            
            // Notify game manager of turnover
            TecmoBowlGameManager.Instance?.SetGameState(GameState.PlayResult);
        }
    }
    
    private void CreateFumbledBall()
    {
        GameObject fumbledBall = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        fumbledBall.name = "FumbledFootball";
        fumbledBall.transform.position = transform.position;
        fumbledBall.transform.localScale = Vector3.one * 0.3f;
        
        // Add physics
        var ballRb = fumbledBall.AddComponent<Rigidbody2D>();
        ballRb.AddForce(Vector2.right * Random.Range(-3f, 3f), ForceMode2D.Impulse);
        
        // Add recovery component
        fumbledBall.AddComponent<FumbleRecoveryBehavior>();
    }
    
    private IEnumerator TackleInvincibility()
    {
        // Brief period where player can't be tackled again
        col.enabled = false;
        yield return new WaitForSeconds(0.5f);
        col.enabled = true;
    }
    
    private IEnumerator EndPlaySequence()
    {
        yield return new WaitForSeconds(2f);
        
        // Reset for next play
        isTackled = false;
        
        // Notify game manager that play is complete
        if (TecmoBowlGameManager.Instance != null)
        {
            TecmoBowlGameManager.Instance.SetGameState(GameState.PlayResult);
        }
    }
    #endregion
    
    #region AI Behavior
    private void HandleAI()
    {
        aiTimer += Time.deltaTime;
        if (aiTimer >= aiUpdateTime)
        {
            aiTimer = 0f;
            UpdateAITarget();
        }
        
        // Move towards AI target
        if (aiTarget != Vector2.zero)
        {
            moveDirection = (aiTarget - (Vector2)transform.position).normalized;
            
            // AI players sprint when chasing or running
            isSprinting = Vector2.Distance(transform.position, aiTarget) > 3f;
        }
    }
    
    private void UpdateAITarget()
    {
        switch (aiBehavior)
        {
            case AIBehaviorType.FollowBall:
                ballCarrier = FindBallCarrier();
                if (ballCarrier != null)
                {
                    aiTarget = ballCarrier.transform.position;
                }
                break;
                
            case AIBehaviorType.CoverReceiver:
                // Find opposing team receiver to cover
                var receiver = FindNearestOpposingReceiver();
                if (receiver != null)
                {
                    aiTarget = receiver.transform.position;
                }
                break;
                
            case AIBehaviorType.RunRoute:
                // Run predetermined route (would be more complex in full implementation)
                RunReceivingRoute();
                break;
                
            case AIBehaviorType.BlockPlayer:
                // Find player to block
                var blockTarget = FindNearestOpposingPlayer();
                if (blockTarget != null)
                {
                    aiTarget = blockTarget.transform.position;
                }
                break;
        }
    }
    
    private TecmoBowlPlayerController FindBallCarrier()
    {
        var allPlayers = FindObjectsOfType<TecmoBowlPlayerController>();
        foreach (var player in allPlayers)
        {
            if (player.hasBall)
            {
                return player;
            }
        }
        return null;
    }
    
    private TecmoBowlPlayerController FindNearestOpposingReceiver()
    {
        var allPlayers = FindObjectsOfType<TecmoBowlPlayerController>();
        TecmoBowlPlayerController nearest = null;
        float nearestDistance = float.MaxValue;
        
        foreach (var player in allPlayers)
        {
            if (player.team != this.team && player.role == PlayerRole.Receiver)
            {
                float distance = Vector2.Distance(transform.position, player.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = player;
                }
            }
        }
        
        return nearest;
    }
    
    private TecmoBowlPlayerController FindNearestOpposingPlayer()
    {
        var allPlayers = FindObjectsOfType<TecmoBowlPlayerController>();
        TecmoBowlPlayerController nearest = null;
        float nearestDistance = float.MaxValue;
        
        foreach (var player in allPlayers)
        {
            if (player.team != this.team)
            {
                float distance = Vector2.Distance(transform.position, player.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = player;
                }
            }
        }
        
        return nearest;
    }
    
    private void RunReceivingRoute()
    {
        // Simplified route running - in full game this would be much more complex
        // For now, just run forward
        aiTarget = transform.position + Vector3.up * 5f;
    }
    
    private List<TecmoBowlPlayerController> FindTeammates()
    {
        var teammates = new List<TecmoBowlPlayerController>();
        var allPlayers = FindObjectsOfType<TecmoBowlPlayerController>();
        
        foreach (var player in allPlayers)
        {
            if (player.team == this.team && player != this)
            {
                teammates.Add(player);
            }
        }
        
        return teammates;
    }
    #endregion
    
    #region Animation & Visual
    private void UpdateAnimation()
    {
        if (animator == null) return;
        
        // Set animation parameters
        animator.SetFloat("Speed", rb.velocity.magnitude);
        animator.SetBool("HasBall", hasBall);
        animator.SetBool("IsSprinting", isSprinting);
        animator.SetBool("IsTackled", isTackled);
        animator.SetBool("IsPassingMode", isPassingMode);
    }
    
    public void HighlightAsTarget(bool highlight)
    {
        // Visual highlight for passing targets
        if (highlight)
        {
            spriteRenderer.color = Color.yellow;
        }
        else
        {
            spriteRenderer.color = Color.white;
        }
    }
    #endregion
    
    #region Public Interface
    public void SetPlayerControl(bool controlled)
    {
        isPlayerControlled = controlled;
        
        // Visual indicator for controlled player
        if (controlled)
        {
            // Add highlight or indicator
            gameObject.transform.localScale = Vector3.one * 1.1f;
        }
        else
        {
            gameObject.transform.localScale = Vector3.one;
        }
    }
    
    public void SetAIBehavior(AIBehaviorType behavior)
    {
        aiBehavior = behavior;
    }
    
    public bool HasBall()
    {
        return hasBall;
    }
    
    public bool IsPlayerControlled()
    {
        return isPlayerControlled;
    }
    
    public Vector2 GetPosition()
    {
        return transform.position;
    }
    
    public float GetSpeed()
    {
        return currentSpeed;
    }
    #endregion
}

#region Supporting Classes and Enums
public enum PlayerTeam
{
    Home,
    Away
}

public enum PlayerRole
{
    Quarterback,
    RunningBack, 
    Receiver,
    Linebacker,
    Defensive
}

public enum AIBehaviorType
{
    FollowBall,
    CoverReceiver,
    RunRoute,
    BlockPlayer,
    PassRush
}

/// <summary>
/// Component for handling football pass behavior
/// </summary>
public class FootballPassBehavior : MonoBehaviour
{
    private Vector2 target;
    private float accuracy;
    private PlayerTeam throwingTeam;
    private Rigidbody2D rb;
    private float speed = 10f;
    
    public void Initialize(Vector2 targetPos, float passAccuracy, PlayerTeam team)
    {
        target = targetPos;
        accuracy = passAccuracy;
        throwingTeam = team;
        rb = GetComponent<Rigidbody2D>();
        
        // Add some inaccuracy based on player rating
        Vector2 inaccuracy = Random.insideUnitCircle * (1f - accuracy) * 2f;
        target += inaccuracy;
        
        // Launch towards target
        Vector2 direction = (target - (Vector2)transform.position).normalized;
        rb.velocity = direction * speed;
        
        // Destroy after reasonable time
        Destroy(gameObject, 3f);
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponent<TecmoBowlPlayerController>();
        if (player != null && player.canCatchBall)
        {
            // Attempt catch
            bool caught = player.playerData.AttemptCatch();
            
            if (caught && player.team == throwingTeam)
            {
                // Successful catch
                player.GiveBall();
                TecmoBowlPlayerController.OnPlayerCaughtPass?.Invoke(player, null);
                Debug.Log($"Caught by {player.playerData.playerName}!");
            }
            else if (player.team != throwingTeam)
            {
                // Interception attempt
                if (player.playerData.AttemptCatch(5)) // Harder for defense
                {
                    player.GiveBall();
                    Debug.Log($"INTERCEPTION by {player.playerData.playerName}!");
                }
            }
            
            Destroy(gameObject);
        }
    }
}

/// <summary>
/// Component for handling fumble recovery
/// </summary>
public class FumbleRecoveryBehavior : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponent<TecmoBowlPlayerController>();
        if (player != null)
        {
            // Player recovers fumble
            player.GiveBall();
            Debug.Log($"Fumble recovered by {player.playerData.playerName}!");
            Destroy(gameObject);
        }
    }
}
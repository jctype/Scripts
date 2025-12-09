using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    public ProgressionData progressionData;
    public Camera mainCamera;

    [Header("Prefabs - Assign in Inspector")]
    public GameObject forgeheartPrefab;
    public Transform windliftSpawnPoint;
    public Transform anvilGoalTransform;

    [Header("Current Game State")]
    public int currentTier = 0;
    public int currentLevel = 0;
    public GameState currentState = GameState.Build;

    public enum GameState { Build, Launching, Watching, Results }

    [Header("Test Shots")]
    public int maxTestBallsPerLevel = 3;
    public int testBallsUsedThisLevel = 0;

    private int ballsLaunched = 0;
    private int ballsGoal = 0;
    private int ballsVoid = 0;
    private float levelTimer = 0f;
    private int trampCount = 0;
    private List<RebounderType> usedTypesThisLevel = new List<RebounderType>();
    private ObjectPool forgeheartPool;

    public int BallsLaunched => ballsLaunched;
    public int BallsGoal => ballsGoal;
    public int BallsVoid => ballsVoid;
    public int TrampCount => trampCount;
    public float LevelTimer => levelTimer;
    public ObjectPool ForgeheartPool => forgeheartPool;

    public bool CanLaunchTestBall() =>
        currentTier >= 1 &&
        currentState == GameState.Build &&
        testBallsUsedThisLevel < maxTestBallsPerLevel;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Initialize forgeheart pool
        if (forgeheartPrefab != null)
        {
            forgeheartPool = new ObjectPool(forgeheartPrefab, 30);
        }
        else
        {
            Debug.LogError("Forgeheart prefab not assigned in GameManager! Creating fallback.");
            // Create fallback prefab
            GameObject fallbackPrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fallbackPrefab.name = "FallbackForgeheart";
            fallbackPrefab.AddComponent<Rigidbody>();
            fallbackPrefab.AddComponent<ForgeheartController>();
            fallbackPrefab.tag = "Forgeheart";

            // Store reference and create pool
            forgeheartPrefab = fallbackPrefab;
            forgeheartPool = new ObjectPool(forgeheartPrefab, 30);

            // Hide the template
            fallbackPrefab.SetActive(false);
        }
    }

    private void Start()
    {
        StartLevel(currentTier, currentLevel);
    }

    public void StartLevel(int tier, int level)
    {
        currentTier = tier;
        currentLevel = level;
        testBallsUsedThisLevel = 0;
        StartCoroutine(LevelSequence());
    }

    private IEnumerator LevelSequence()
    {
        if (ProcGenManager.Instance != null)
            ProcGenManager.Instance.Generate(currentTier, currentLevel);

        if (progressionData.GetTier(currentTier).introVO != null && AudioManager.Instance != null)
            AudioManager.Instance.PlayVO(progressionData.GetTier(currentTier).introVO);

        ballsLaunched = ballsGoal = ballsVoid = trampCount = 0;
        usedTypesThisLevel.Clear();
        levelTimer = 0f;

        currentState = GameState.Build;
        UIManager.Instance?.ShowBuildUI();
        yield return null;
    }

    public void LaunchBalls()
    {
        if (currentState != GameState.Build) return;

        int ballsToLaunch = Random.Range(
            progressionData.GetTier(currentTier).ballsToLaunchMin,
            progressionData.GetTier(currentTier).ballsToLaunchMax + 1);

        WindliftController.Instance?.StartLaunchSequence(ballsToLaunch);
        currentState = GameState.Launching;
        UIManager.Instance?.HideBuildUI();
    }

    public void LaunchTestBall()
    {
        if (!CanLaunchTestBall()) return;

        testBallsUsedThisLevel++;

        GameObject ball = ForgeheartPool.Get();
        if (ball == null) return;

        Renderer rend = ball.GetComponentInChildren<Renderer>();
        if (rend != null)
            rend.material.color = new Color(0.3f, 0.95f, 1f, 0.75f);

        ball.tag = "TestForgeheart";
        ball.transform.position = windliftSpawnPoint.position;

        Rigidbody rb = ball.GetComponent<Rigidbody>();
        if (rb != null)
            rb.velocity = Vector3.up * 8f + Random.insideUnitSphere * 1.5f;

        Destroy(ball, 15f);

        if (AudioManager.Instance?.launchWhoosh != null)
            AudioManager.Instance.PlaySound(AudioManager.Instance.launchWhoosh);

        UIManager.Instance?.UpdateTestShotCounter();
    }

    private void Update()
    {
        if (currentState == GameState.Watching || currentState == GameState.Launching)
        {
            levelTimer += Time.deltaTime;
            UIManager.Instance?.UpdateTimer(levelTimer);
        }
    }

    public void RegisterGoal() { ballsGoal++; UIManager.Instance?.UpdateHUD(); CheckIfLevelComplete(); }
    public void RegisterVoid() { ballsVoid++; CheckIfLevelComplete(); }
    public void RegisterBallLaunched() { ballsLaunched++; UIManager.Instance?.UpdateHUD(); }

    public void RegisterTrampPlaced(RebounderType type)
    {
        trampCount++;
        if (!usedTypesThisLevel.Contains(type))
            usedTypesThisLevel.Add(type);
        UIManager.Instance?.UpdateHUD();
    }

    private void CheckIfLevelComplete()
    {
        if (WindliftController.Instance == null) return;
        if (ballsLaunched < WindliftController.Instance.TotalBallsLaunched) return;

        currentState = GameState.Results;
        UIManager.Instance?.ShowResults(CalculateStars());
    }

    private int CalculateStars()
    {
        if (ballsLaunched == 0) return 0;

        var data = progressionData.GetTier(currentTier);
        float goalPct = (float)ballsGoal / ballsLaunched * 100f;
        bool timeOk = levelTimer < data.timeLimitSeconds;
        bool efficiencyOk = trampCount <= data.maxTrampSockets * 0.4f;
        bool noLoss = ballsVoid == 0;

        int stars = 0;
        if (goalPct >= data.requiredGoalPercentage) stars++;
        if (timeOk) stars++;
        if (efficiencyOk) stars++;
        if (noLoss) stars++;

        return stars;
    }

    public void NextLevel()
    {
        currentLevel++;
        if (currentLevel > 2)
        {
            currentTier++;
            currentLevel = 0;
        }
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void RetryLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
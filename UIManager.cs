using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Canvas Groups")]
    public CanvasGroup buildUI;
    public CanvasGroup resultsUI;
    public CanvasGroup hudUI;

    [Header("Text Fields")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI goalsText;
    public TextMeshProUGUI trampsUsedText;
    public TextMeshProUGUI starsText;
    public TextMeshProUGUI tierLevelText;

    [Header("Buttons")]
    public Button launchButton;
    public Button retryButton;
    public Button nextButton;

    // NEW: Test Shot UI
    [Header("Test Shot UI")]
    public Button testShotButton;                    // Assign in Inspector (on buildUI canvas)
    public TextMeshProUGUI testShotCounterText;      // Assign in Inspector – shows "Tests: 0/3"

    [Header("Challenge Icons")]
    public Image[] challengeIcons; // assign 5 images in inspector
    public Sprite challengeCompleteSprite;
    public Sprite challengeIncompleteSprite;

    [Header("Rebounder Selection")]
    public GameObject rebounderButtonPrefab;
    public Transform radialPanel;

    private List<Button> rebounderButtons = new List<Button>();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Initialize radial menu if needed
        if (rebounderButtonPrefab != null && radialPanel != null)
        {
            InitializeRadialMenu();
        }

        // Setup button listeners
        launchButton.onClick.AddListener(() =>
        {
            if (GameManager.Instance != null)
                GameManager.Instance.LaunchBalls();
        });

        retryButton.onClick.AddListener(() =>
        {
            if (GameManager.Instance != null)
                GameManager.Instance.RetryLevel();
        });

        nextButton.onClick.AddListener(() =>
        {
            if (GameManager.Instance != null)
                GameManager.Instance.NextLevel();
        });

        // NEW: Test Shot button – safe, immediate feedback
        if (testShotButton != null)
        {
            testShotButton.onClick.RemoveAllListeners();
            testShotButton.onClick.AddListener(() =>
            {
                GameManager.Instance?.LaunchTestBall();
                UpdateTestShotCounter();  // Instant update so player sees count drop
            });
        }

        HideAll();
    }

    private void InitializeRadialMenu()
    {
        // Clear existing buttons
        foreach (Transform child in radialPanel)
            Destroy(child.gameObject);
        rebounderButtons.Clear();

        // Create buttons for each rebounder type
        foreach (RebounderType type in System.Enum.GetValues(typeof(RebounderType)))
        {
            if (type == RebounderType.None) continue;

            GameObject btnObj = Instantiate(rebounderButtonPrefab, radialPanel);
            Button btn = btnObj.GetComponent<Button>();
            TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();

            if (btnText != null)
                btnText.text = type.ToString().Replace("_", " ");

            btn.onClick.AddListener(() =>
            {
                PlacementManager.Instance.SelectRebounder(type);
            });

            rebounderButtons.Add(btn);
        }
    }

    public void ShowBuildUI()
    {
        HideAll();
        buildUI.alpha = 1f;
        buildUI.interactable = true;
        buildUI.blocksRaycasts = true;
        hudUI.alpha = 1f;
        hudUI.interactable = true;

        UpdateHUD();
        UpdateTestShotCounter();  // NEW: Make sure test counter is correct on entry

        // Update tier/level display
        if (GameManager.Instance != null)
        {
            tierLevelText.text = $"Tier {GameManager.Instance.currentTier + 1} - Level {GameManager.Instance.currentLevel + 1}";
        }
    }

    public void HideBuildUI()
    {
        buildUI.alpha = 0f;
        buildUI.interactable = false;
        buildUI.blocksRaycasts = false;
    }

    public void UpdateTimer(float time)
    {
        if (timerText != null)
            timerText.text = $"Time: {time:F1}s";
    }

    public void UpdateHUD()
    {
        if (GameManager.Instance == null) return;

        if (goalsText != null)
            goalsText.text = $"Goals: {GameManager.Instance.BallsGoal} / {GameManager.Instance.BallsLaunched}";

        if (trampsUsedText != null)
            trampsUsedText.text = $"Tramps: {GameManager.Instance.TrampCount}";
    }

    // NEW: Dedicated test shot counter update – called on launch and on ShowBuildUI
    public void UpdateTestShotCounter()
    {
        if (testShotCounterText == null || GameManager.Instance == null) return;

        int remaining = GameManager.Instance.maxTestBallsPerLevel - GameManager.Instance.testBallsUsedThisLevel;

        testShotCounterText.text = $"Tests: {remaining}";

        // Grey out button when no tests left – clear visual feedback
        if (testShotButton != null)
        {
            testShotButton.interactable = remaining > 0;
        }
    }

    public void ShowResults(int stars)
    {
        HideAll();
        resultsUI.alpha = 1f;
        resultsUI.interactable = true;
        resultsUI.blocksRaycasts = true;

        if (starsText != null)
            starsText.text = $"Stars: {stars}/5";

        // Update challenge icons
        UpdateChallengeIcons(stars);

        // Play success/fail VO
        if (AudioManager.Instance != null)
        {
            if (stars >= 4)
                AudioManager.Instance.PlayVO(Resources.Load<AudioClip>("VO/Success"));
            else
                AudioManager.Instance.PlayVO(Resources.Load<AudioClip>("VO/Fail"));
        }
    }

    private void UpdateChallengeIcons(int stars)
    {
        if (challengeIcons == null || challengeCompleteSprite == null || challengeIncompleteSprite == null)
            return;

        for (int i = 0; i < challengeIcons.Length; i++)
        {
            if (challengeIcons[i] != null)
            {
                challengeIcons[i].sprite = (i < stars) ? challengeCompleteSprite : challengeIncompleteSprite;
            }
        }
    }

    private void HideAll()
    {
        buildUI.alpha = hudUI.alpha = resultsUI.alpha = 0f;
        buildUI.interactable = hudUI.interactable = resultsUI.interactable = false;
        buildUI.blocksRaycasts = hudUI.blocksRaycasts = resultsUI.blocksRaycasts = false;
    }
}
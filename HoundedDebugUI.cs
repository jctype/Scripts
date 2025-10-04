using UnityEngine;
using UnityEngine.UI;
using TMPro; // Use TextMeshPro for better text quality

public class HoundedDebugUI : MonoBehaviour
{
    [Header("UI References")]
    public Canvas debugCanvas;
    public GameObject panel;

    [Header("Player Profile Debug")]
    public Slider aqueousWeightSlider;
    public TextMeshProUGUI aqueousWeightText;
    public Slider mobilityWeightSlider;
    public TextMeshProUGUI mobilityWeightText;
    public Slider deceptionWeightSlider;
    public TextMeshProUGUI deceptionWeightText;
    public Slider riskWeightSlider;
    public TextMeshProUGUI riskWeightText;

    [Header("Hunter State Debug")]
    public Slider physicalFatigueSlider;
    public TextMeshProUGUI physicalFatigueText;
    public Slider mentalFocusSlider;
    public TextMeshProUGUI mentalFocusText;
    public Slider frustrationSlider;
    public TextMeshProUGUI frustrationText;

    [Header("AI Behavior Toggles")]
    public Toggle enableScentTracking;
    public Toggle enableSoundTracking;
    public Toggle enablePlayerDetection;
    public Toggle enableProfileLearning;
    public Toggle enableHunterFatigue;

    [Header("AI Weight Adjustments")]
    public Slider sightPriorityWeight;
    public TextMeshProUGUI sightPriorityText;
    public Slider scentPriorityWeight;
    public TextMeshProUGUI scentPriorityText;
    public Slider soundPriorityWeight;
    public TextMeshProUGUI soundPriorityText;
    public Slider profilePredictionWeight;
    public TextMeshProUGUI profilePredictionText;

    [Header("Hunter Controller Overrides")]
    public Slider decisionIntervalSlider;
    public TextMeshProUGUI decisionIntervalText;
    public Slider patrolSpeedSlider;
    public TextMeshProUGUI patrolSpeedText;
    public Slider chaseSpeedSlider;
    public TextMeshProUGUI chaseSpeedText;

    [Header("Action Scores Display")]
    public TextMeshProUGUI chaseScoreText;
    public TextMeshProUGUI investigateScoreText;
    public TextMeshProUGUI patrolScoreText;
    public TextMeshProUGUI ambushScoreText;
    public TextMeshProUGUI currentActionText;

    // References to your systems
    private HunterAIController hunterController;
    private PlayerManager playerManager;
    private HunterUtilityAI utilityAI;

    void Start()
    {
        // Find system references
        hunterController = FindObjectOfType<HunterAIController>();
        playerManager = PlayerManager.Instance;
        utilityAI = FindObjectOfType<HunterUtilityAI>();

        // Initialize UI elements
        InitializeSliders();
        InitializeToggles();

        // Start with UI hidden, press F1 to toggle
        debugCanvas.enabled = false;
    }

    void Update()
    {
        // Toggle debug UI with F1
        if (Input.GetKeyDown(KeyCode.F1))
        {
            debugCanvas.enabled = !debugCanvas.enabled;
        }

        if (debugCanvas.enabled)
        {
            UpdateDisplayValues();
            ApplyDebugOverrides();
        }
    }

    private void InitializeSliders()
    {
        // Player Profile Sliders
        if (aqueousWeightSlider != null)
        {
            aqueousWeightSlider.minValue = 0f;
            aqueousWeightSlider.maxValue = 100f;
            aqueousWeightSlider.onValueChanged.AddListener(OnAqueousWeightChanged);
        }

        // Hunter State Sliders
        if (physicalFatigueSlider != null)
        {
            physicalFatigueSlider.minValue = 0f;
            physicalFatigueSlider.maxValue = 100f;
            physicalFatigueSlider.onValueChanged.AddListener(OnFatigueChanged);
        }

        // AI Weight Sliders
        if (sightPriorityWeight != null)
        {
            sightPriorityWeight.minValue = 0f;
            sightPriorityWeight.maxValue = 2f;
            sightPriorityWeight.onValueChanged.AddListener(OnSightWeightChanged);
        }

        // Hunter Controller Sliders
        if (decisionIntervalSlider != null)
        {
            decisionIntervalSlider.minValue = 0.1f;
            decisionIntervalSlider.maxValue = 3f;
            decisionIntervalSlider.onValueChanged.AddListener(OnDecisionIntervalChanged);
        }
    }

    private void InitializeToggles()
    {
        if (enableScentTracking != null)
            enableScentTracking.onValueChanged.AddListener(OnScentTrackingToggled);

        if (enableProfileLearning != null)
            enableProfileLearning.onValueChanged.AddListener(OnProfileLearningToggled);
    }

    private void UpdateDisplayValues()
    {
        // Update Player Profile Values
        if (playerManager != null)
        {
            UpdateSliderAndText(aqueousWeightSlider, aqueousWeightText, playerManager.Profile.AqueousWeight, "Aqueous: {0:F1}");
            UpdateSliderAndText(mobilityWeightSlider, mobilityWeightText, playerManager.Profile.MobilityWeight, "Mobility: {0:F1}");
            UpdateSliderAndText(deceptionWeightSlider, deceptionWeightText, playerManager.Profile.DeceptionWeight, "Deception: {0:F1}");
            UpdateSliderAndText(riskWeightSlider, riskWeightText, playerManager.Profile.RiskWeight, "Risk: {0:F1}");
        }

        // Update Hunter State
        if (HunterManager.Instance != null)
        {
            UpdateSliderAndText(physicalFatigueSlider, physicalFatigueText, HunterManager.Instance.PhysicalFatigue, "Fatigue: {0:F1}");
            UpdateSliderAndText(mentalFocusSlider, mentalFocusText, HunterManager.Instance.MentalFocus, "Focus: {0:F1}");
            // Add Frustration to HunterManager if needed
            UpdateSliderAndText(frustrationSlider, frustrationText, HunterManager.Instance.Frustration, "Frustration: {0:F1}");
        }

        // Update Action Scores
        if (utilityAI != null)
        {
            chaseScoreText.text = $"Chase: {utilityAI.lastChaseScore:F2}";
            investigateScoreText.text = $"Investigate: {utilityAI.lastInvestigateScore:F2}";
            patrolScoreText.text = $"Patrol: {utilityAI.lastPatrolScore:F2}";
            ambushScoreText.text = $"Ambush: {utilityAI.lastAmbushScore:F2}";

            currentActionText.text = $"Current: {hunterController?.GetCurrentIntent().ToString() ?? "Unknown"}";
        }

        // Update Hunter Controller Values
        if (hunterController != null)
        {
            UpdateSliderAndText(decisionIntervalSlider, decisionIntervalText, hunterController.decisionInterval, "Decision Int: {0:F2}s");
            UpdateSliderAndText(patrolSpeedSlider, patrolSpeedText, hunterController.patrolSpeed, "Patrol Speed: {0:F1}");
            UpdateSliderAndText(chaseSpeedSlider, chaseSpeedText, hunterController.chaseSpeed, "Chase Speed: {0:F1}");
        }
    }

    private void ApplyDebugOverrides()
    {
        // Apply toggle overrides to systems
        if (hunterController != null)
        {
            // You'd add bools to your HunterAIController like:
            // hunterController.scentTrackingEnabled = enableScentTracking.isOn;
            // hunterController.profileLearningEnabled = enableProfileLearning.isOn;
        }

        if (utilityAI != null)
        {
            // Apply weight overrides
            utilityAI.sightPriorityWeight = sightPriorityWeight.value;
            utilityAI.scentPriorityWeight = scentPriorityWeight.value;
            utilityAI.soundPriorityWeight = soundPriorityWeight.value;
            utilityAI.profilePredictionWeight = profilePredictionWeight.value;

            // Update slider text displays
            UpdateSliderText(sightPriorityText, "Sight Weight: {0:F2}", sightPriorityWeight.value);
            UpdateSliderText(scentPriorityText, "Scent Weight: {0:F2}", scentPriorityWeight.value);
            UpdateSliderText(soundPriorityText, "Sound Weight: {0:F2}", soundPriorityWeight.value);
            UpdateSliderText(profilePredictionText, "Profile Weight: {0:F2}", profilePredictionWeight.value);
        }
    }

    // ===== EVENT HANDLERS =====

    private void OnAqueousWeightChanged(float value)
    {
        if (playerManager != null)
            playerManager.Profile.AqueousWeight = value;
    }

    private void OnFatigueChanged(float value)
    {
        if (HunterManager.Instance != null)
            HunterManager.Instance.PhysicalFatigue = value;
    }

    private void OnSightWeightChanged(float value)
    {
        if (utilityAI != null)
            utilityAI.sightPriorityWeight = value;
    }

    private void OnDecisionIntervalChanged(float value)
    {
        if (hunterController != null)
            hunterController.decisionInterval = value;
    }

    private void OnScentTrackingToggled(bool enabled)
    {
        Debug.Log($"Scent tracking: {enabled}");
        // hunterController.EnableScentTracking(enabled);
    }

    private void OnProfileLearningToggled(bool enabled)
    {
        Debug.Log($"Profile learning: {enabled}");
        // utilityAI.EnableProfileLearning(enabled);
    }

    // ===== HELPER METHODS =====

    private void UpdateSliderAndText(Slider slider, TextMeshProUGUI text, float value, string format)
    {
        if (slider != null) slider.value = value;
        UpdateSliderText(text, format, value);
    }

    private void UpdateSliderText(TextMeshProUGUI text, string format, float value)
    {
        if (text != null) text.text = string.Format(format, value);
    }

    // ===== PUBLIC METHODS FOR EXTERNAL CONTROL =====

    public void ForceHunterAction(string action)
    {
        // Force the hunter to perform a specific action (for testing)
        switch (action.ToLower())
        {
            case "patrol":
                // utilityAI.ForceAction(HunterUtilityAI.ActionType.Patrol);
                break;
            case "chase":
                // utilityAI.ForceAction(HunterUtilityAI.ActionType.Chase);
                break;
            case "investigate":
                // utilityAI.ForceAction(HunterUtilityAI.ActionType.Investigate);
                break;
        }
    }

    public void ResetHunterState()
    {
        // Reset all hunter state to default
        if (HunterManager.Instance != null)
        {
            HunterManager.Instance.PhysicalFatigue = 0f;
            HunterManager.Instance.MentalFocus = 100f;
            HunterManager.Instance.Frustration = 0f;
        }
    }

    public void ResetPlayerProfile()
    {
        // Reset player profile to neutral
        if (playerManager != null)
        {
            playerManager.Profile.AqueousWeight = 0f;
            playerManager.Profile.MobilityWeight = 0f;
            playerManager.Profile.DeceptionWeight = 0f;
            playerManager.Profile.RiskWeight = 0f;
        }
    }
}
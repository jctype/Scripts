using UnityEngine;

public class HunterManager : MonoBehaviour
{
    public static HunterManager Instance;

    public HunterAIController HunterActor; // The physical hunter in the world
    public HunterUtilityAI HunterBrain;
    public float PhysicalFatigue;
    public float MentalFocus;
    public float Frustration;

    // Clue detection parameters
    public float MaxSenseRadius = 10f;
    public LayerMask ClueLayerMask;

    private float tickTimer = 0f;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (HunterBrain != null && HunterActor != null)
        {
            HunterBrain.hunterController = HunterActor;
        }
    }

    void Update()
    {
        Debug.Log("HunterManager.Update() started");

        // Update hunter state dynamically
        MentalFocus = Mathf.Max(0, MentalFocus - Time.deltaTime * 0.1f);
        if (HunterActor != null && HunterActor.GetCurrentIntent() == HunterActionIntent.Chase)
        {
            PhysicalFatigue += Time.deltaTime * 0.5f;
        }
        if (HunterBrain != null && HunterBrain.detectedClues.Count == 0)
        {
            Frustration += Time.deltaTime * 0.05f;
        }
        else
        {
            Frustration = Mathf.Max(0, Frustration - Time.deltaTime * 0.1f);
        }

        tickTimer += Time.deltaTime;
        if (tickTimer >= 5f)
        {
            HunterBrain.Tick(); // Runs the Utility AI decision loop every 5 seconds
            tickTimer = 0f;
        }
        CheckForClues(); // Added call to detect clues each frame
        Debug.Log("HunterManager.Update() ended");
    }

    void CheckForClues()
    {
        Debug.Log("HunterManager.CheckForClues() started");
        // 1. Cheap Physics OverlapSphere check to get nearby clues
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, MaxSenseRadius, ClueLayerMask);

        foreach (Collider col in nearbyColliders)
        {
            Clue clue = col.GetComponent<Clue>();
            if (clue != null)
            {
                Debug.Log("Clue Detected");
                // 2. Calculate detection chance (as we discussed)
                float detectChance = CalculateDetectionChance(clue);

                // 3. Roll the dice
                if (Random.Range(0f, 1f) <= detectChance)
                {
                    // 4. THE PAYOFF: The Hunter's AI Brain now knows about this clue!
                    // This clue becomes a "Consideration" in the Utility AI scorers.
                    HunterBrain.RegisterDetectedClue(clue);
                }
            }
        }
        Debug.Log("HunterManager.CheckForClues() ended");
    }

    private float CalculateDetectionChance(Clue clue)
    {
        Debug.Log("HunterManager.CalculateDetectionChance() started");
        // Improved chance calculation based on clue intensity, distance, fatigue, and environment

        float distance = Vector3.Distance(transform.position, clue.transform.position);
        float baseChance = clue.Intensity / (distance + 1f); // Base formula

        // Factor in hunter's physical fatigue (more fatigue reduces chance)
        float fatigueFactor = Mathf.Clamp01(1f - PhysicalFatigue / 100f);

        // Factor in mental focus (higher focus increases chance)
        float focusFactor = Mathf.Clamp01(MentalFocus / 100f);

        float finalChance = baseChance * fatigueFactor * focusFactor;

        Debug.Log($"Detection chance calculated: base={baseChance}, fatigueFactor={fatigueFactor}, focusFactor={focusFactor}, final={finalChance}");

        Debug.Log("HunterManager.CalculateDetectionChance() ended");
        return Mathf.Clamp(finalChance, 0f, 1f);
    }
}

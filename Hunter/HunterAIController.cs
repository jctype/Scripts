using UnityEngine;
using UnityEngine.AI;
using ProjectHounded.AI.Core;

public class HunterAIController : MonoBehaviour
{
  [Header("AI Configuration")]
  public float decisionInterval = 10f;
  public float patrolSpeed = 3.5f;
  public float chaseSpeed = 6f;
  public float patrolRadius = 10f;
  public float investigationTime = 5f;
  public float patrolScanTime = 3f;

  [Header("System Dependencies")]
  public GameObject scentManager;
  public GameObject soundManager;
  public GameObject environmentDetector; // Reference to EnvironmentDetector GameObject

  private NavMeshAgent agent;
  private float decisionTimer = 0f;
  private Vector3 patrolPoint;
  private bool isInvestigating;
  private float investigationTimer;
  private ScentSystem scentSystemScript;
  private SoundSystem soundSystemScript;
  private EnvironmentDetector environmentDetectorScript; // Changed to EnvironmentDetector

  private HunterAnimationController animationController;
  private HunterActionIntent currentIntent = HunterActionIntent.Patrol;

  private enum AIState { Moving, Scanning }
  private AIState currentState = AIState.Scanning;
  private float scanTimer = 0f;
  private float scanDuration = 3f;

  void Start()
  {
    agent = GetComponent<NavMeshAgent>();
    if (agent == null)
    {
      Debug.LogError("NavMeshAgent component missing on HunterAIController!");
    }

    // Cache system scripts
    scentSystemScript = scentManager?.GetComponent<ScentSystem>();
    soundSystemScript = soundManager?.GetComponent<SoundSystem>();
    environmentDetectorScript = environmentDetector?.GetComponent<EnvironmentDetector>(); // Fixed this line

    animationController = GetComponent<HunterAnimationController>();

    // Log warnings if systems are not assigned or scripts are missing
    if (scentSystemScript == null && scentManager != null) Debug.LogWarning("ScentSystem script not found on assigned scentManager GameObject!");
    if (soundSystemScript == null && soundManager != null) Debug.LogWarning("SoundSystem script not found on assigned soundManager GameObject!");
    if (environmentDetectorScript == null && environmentDetector != null) Debug.LogWarning("EnvironmentDetector script not found on assigned environmentDetector GameObject!");

    // Set initial patrol point
    patrolPoint = transform.position;
  }

  void Update()
  {
    decisionTimer += Time.deltaTime;

    // Check if reached destination and switch to scanning
    if (currentState == AIState.Moving && agent.hasPath && agent.remainingDistance <= agent.stoppingDistance)
    {
      currentState = AIState.Scanning;
      agent.isStopped = true;
      scanTimer = 0f;
      scanDuration = (currentIntent == HunterActionIntent.Investigate) ? investigationTime : patrolScanTime;
      Debug.Log("Hunter reached destination, starting scan for " + scanDuration + " seconds");
    }

    // Handle scanning
    if (currentState == AIState.Scanning)
    {
      scanTimer += Time.deltaTime;
      // During scanning, check for new clues every frame
      CheckForNewClues();
      if (scanTimer >= scanDuration)
      {
        currentState = AIState.Moving;
        agent.isStopped = false;
        if (currentIntent == HunterActionIntent.Investigate)
        {
          currentIntent = HunterActionIntent.Patrol; // Resume patrol after investigation
        }
        Debug.Log("Scan complete, resuming movement");
      }
    }
    /*
    // For chase, update destination every frame
    if (currentIntent == HunterActionIntent.Chase && environmentDetectorScript != null &&
        environmentDetectorScript.playerObject != null && IsPlayerVisible())
    {
      agent.SetDestination(environmentDetectorScript.playerObject.transform.position);
    }
    */
    if (decisionTimer >= decisionInterval)
    {
      decisionTimer = 0f;
      UpdateWorldState();
      EvaluateAndExecuteBestAction();
    }

    // Handle investigation timeout
    if (isInvestigating)
    {
      investigationTimer += Time.deltaTime;
      if (investigationTimer >= investigationTime)
      {
        isInvestigating = false;
        agent.speed = patrolSpeed;
      }
    }
  }

  // NEW METHOD: Check if player is visible using EnvironmentDetector
  private bool IsPlayerVisible()
  {
    if (environmentDetectorScript == null || environmentDetectorScript.playerObject == null)
    {
      Debug.Log("IsPlayerVisible: EnvironmentDetector or playerObject is null");
      return false;
    }

    // Get player's environment type
    EnvironmentType playerEnv = environmentDetectorScript.GetEnvironmentAtPosition(environmentDetectorScript.playerObject.transform.position);

    bool result = environmentDetectorScript.CanDetectPlayerAtDistance(
        environmentDetectorScript.playerObject.transform.position,
        playerEnv);
    Debug.Log($"IsPlayerVisible called, result: {result}");
    return result;
  }

  private void UpdateWorldState()
  {
    HunterActionIntent newIntent = DetermineActionIntent();

    if (newIntent != currentIntent)
    {
      currentIntent = newIntent;

      if (newIntent == HunterActionIntent.Chase && environmentDetectorScript != null &&
          environmentDetectorScript.playerObject != null && IsPlayerVisible())
      {
        agent.SetDestination(environmentDetectorScript.playerObject.transform.position);
        agent.speed = chaseSpeed;
        currentState = AIState.Moving;
        agent.isStopped = false;
        isInvestigating = false;
        Debug.Log("Intent changed to Chase, moving to player");
      }
      else if (newIntent == HunterActionIntent.Investigate)
      {
        Vector3 target = Vector3.zero;
        bool found = false;
        if (scentSystemScript != null)
        {
          ScentData scentData = scentSystemScript.GetMostRelevantScent();
          if (scentData.IsValid)
          {
            target = scentData.WorldPosition;
            found = true;
          }
        }
        if (!found && soundSystemScript != null)
        {
          SoundEvent soundEvent = soundSystemScript.GetMostRelevantSound();
          if (soundEvent.IsValid)
          {
            target = soundEvent.WorldPosition;
            found = true;
          }
        }
        if (found)
        {
          agent.SetDestination(target);
          agent.speed = patrolSpeed;
          currentState = AIState.Moving;
          agent.isStopped = false;
          isInvestigating = true;
          investigationTimer = 0f;
          Debug.Log("Intent changed to Investigate, moving to clue");
        }
      }
      else if (newIntent == HunterActionIntent.Patrol)
      {
        isInvestigating = false;
      }
    }
  }

  private HunterActionIntent DetermineActionIntent()
  {
    if (environmentDetectorScript != null && IsPlayerVisible())
    {
      Debug.Log("Hunter chose to Chase");
      return HunterActionIntent.Chase;
    }

    if (scentSystemScript != null && scentSystemScript.GetMostRelevantScent().IsValid)
    {
      Debug.Log("Hunter chose to Investigate");
      return HunterActionIntent.Investigate;
    }

    if (soundSystemScript != null && soundSystemScript.GetMostRelevantSound().IsValid)
    {
      Debug.Log("Hunter chose to Search");
      return HunterActionIntent.Search;
    }

    Debug.Log("Hunter chose to Patrol");
    return HunterActionIntent.Patrol;
  }

  public HunterActionIntent GetCurrentIntent() => currentIntent;
  public bool IsMoving() => agent.velocity.magnitude > 0.1f;
  public bool IsInDanger() => currentIntent == HunterActionIntent.Chase;
  public bool HasDetectedSomething() => currentIntent != HunterActionIntent.Patrol;

  public void TriggerEnvironmentalReaction(string reactionType)
  {
    if (animationController.IsAvailableForNewAnimation())
    {
      animationController.TriggerEmergencyAnimation(reactionType, 2.0f);
    }
  }

  private void EvaluateAndExecuteBestAction()
  {
    if (currentState == AIState.Moving && agent.hasPath && agent.remainingDistance > agent.stoppingDistance)
    {
      return;
    }

    if (currentState == AIState.Scanning)
    {
      return;
    }

    if (currentIntent == HunterActionIntent.Patrol && currentState == AIState.Moving &&
        (!agent.hasPath || agent.remainingDistance <= agent.stoppingDistance))
    {
      patrolPoint = transform.position;
      Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
      randomDirection += patrolPoint;
      NavMeshHit hit;
      if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, NavMesh.AllAreas))
      {
        agent.SetDestination(hit.position);
        agent.speed = patrolSpeed;
        isInvestigating = false;
        Debug.Log("Setting new patrol point");
      }
    }
  }

  private void CheckForNewClues()
  {
    Debug.Log("CheckForNewClues called");
    // Check for player
    if (environmentDetectorScript != null && environmentDetectorScript.playerObject != null && IsPlayerVisible())
    {
      currentIntent = HunterActionIntent.Chase;
      agent.isStopped = false;
      agent.SetDestination(environmentDetectorScript.playerObject.transform.position);
      agent.speed = chaseSpeed;
      currentState = AIState.Moving;
      Debug.Log("New clue: Player detected during scan, switching to chase");
      return;
    }

    // Check scent
    if (scentSystemScript != null)
    {
      ScentData scentData = scentSystemScript.GetMostRelevantScent();
      if (scentData.IsValid)
      {
        currentIntent = HunterActionIntent.Investigate;
        agent.isStopped = false;
        agent.SetDestination(scentData.WorldPosition);
        agent.speed = patrolSpeed;
        currentState = AIState.Moving;
        Debug.Log("New clue: Scent detected during scan, investigating");
        return;
      }
    }

    // Check sound
    if (soundSystemScript != null)
    {
      SoundEvent soundEvent = soundSystemScript.GetMostRelevantSound();
      if (soundEvent.IsValid)
      {
        currentIntent = HunterActionIntent.Investigate;
        agent.isStopped = false;
        agent.SetDestination(soundEvent.WorldPosition);
        agent.speed = patrolSpeed;
        currentState = AIState.Moving;
        Debug.Log("New clue: Sound detected during scan, investigating");
        return;
      }
    }
  }

  public void SetActionFromUtilityAI(HunterUtilityAI.ActionType intent, Vector3 target)
  {
    switch (intent)
    {
      case HunterUtilityAI.ActionType.Chase:
        currentIntent = HunterActionIntent.Chase;
        agent.speed = chaseSpeed;
        break;
      case HunterUtilityAI.ActionType.Investigate:
        currentIntent = HunterActionIntent.Investigate;
        agent.speed = patrolSpeed;
        break;
      case HunterUtilityAI.ActionType.Patrol:
        currentIntent = HunterActionIntent.Patrol;
        agent.speed = patrolSpeed;
        break;
    }

    agent.SetDestination(target);
    isInvestigating = (intent == HunterUtilityAI.ActionType.Investigate);
    Debug.Log("Hunter action set to " + intent.ToString() + " at " + target.ToString());
  }
}
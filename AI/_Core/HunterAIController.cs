using UnityEngine;
using UnityEngine.AI;
using ProjectHounded.AI.Core; // Added to access ScentSystem, SoundSystem, PlayerDetectionSystem

public class HunterAIController : MonoBehaviour
{
  [Header("AI Configuration")]
  public float decisionInterval = 0.5f;
  public float patrolSpeed = 3.5f;
  public float chaseSpeed = 6f;
  public float patrolRadius = 10f;
  public float investigationTime = 5f;

  [Header("System Dependencies")]
  public GameObject scentManager; // GameObject with ScentSystem script
  public GameObject soundManager; // GameObject with SoundSystem script
  public GameObject playerDetection; // GameObject with PlayerDetectionSystem script

  private NavMeshAgent agent;
  private float decisionTimer = 0f;
  private Vector3 patrolPoint;
  private bool isInvestigating;
  private float investigationTimer;
  private ScentSystem scentSystemScript;
  private SoundSystem soundSystemScript;
  private PlayerDetectionSystem playerDetectionSystemScript;

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
    playerDetectionSystemScript = playerDetection?.GetComponent<PlayerDetectionSystem>();

    // Log warnings if systems are not assigned or scripts are missing
    if (scentSystemScript == null && scentManager != null) Debug.LogWarning("ScentSystem script not found on assigned scentManager GameObject!");
    if (soundSystemScript == null && soundManager != null) Debug.LogWarning("SoundSystem script not found on assigned soundManager GameObject!");
    if (playerDetectionSystemScript == null && playerDetection != null) Debug.LogWarning("PlayerDetectionSystem script not found on assigned playerDetection GameObject!");

    // Set initial patrol point
    patrolPoint = transform.position;
  }

  void Update()
  {
    decisionTimer += Time.deltaTime;

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

  private void UpdateWorldState()
  {
    // Check player detection
    if (playerDetectionSystemScript != null && playerDetectionSystemScript.IsPlayerVisible())
    {
      // Use playerObject from PlayerDetectionSystem as target
      if (playerDetectionSystemScript.playerObject != null)
      {
        agent.SetDestination(playerDetectionSystemScript.playerObject.transform.position);
        agent.speed = chaseSpeed;
        isInvestigating = false;
        return;
      }
    }

    // Check scent system
    if (scentSystemScript != null)
    {
      ScentSystem.ScentData scentData = scentSystemScript.GetMostRelevantScent();
      if (scentData.IsValid)
      {
        agent.SetDestination(scentData.WorldPosition);
        isInvestigating = true;
        investigationTimer = 0f;
        agent.speed = patrolSpeed;
        return;
      }
    }

    // Check sound system
    if (soundSystemScript != null)
    {
      SoundSystem.SoundEvent soundEvent = soundSystemScript.GetMostRelevantSound();
      if (soundEvent.IsValid)
      {
        agent.SetDestination(soundEvent.WorldPosition);
        isInvestigating = true;
        investigationTimer = 0f;
        agent.speed = patrolSpeed;
        return;
      }
    }
  }

  private void EvaluateAndExecuteBestAction()
  {
    // If already moving to a target (chasing or investigating), continue
    if (agent.hasPath && (agent.remainingDistance > agent.stoppingDistance || isInvestigating))
    {
      return;
    }

    // Patrol logic: move to a random point within patrolRadius
    Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
    randomDirection += patrolPoint;
    NavMeshHit hit;
    if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, NavMesh.AllAreas))
    {
      agent.SetDestination(hit.position);
      agent.speed = patrolSpeed;
      isInvestigating = false;
    }
  }
}
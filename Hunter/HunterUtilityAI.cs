using UnityEngine;
using System.Collections.Generic;

public class HunterUtilityAI : MonoBehaviour
{
    public HunterAIController hunterController;

    // Debug UI adjustable weights
    public float sightPriorityWeight = 1f;
    public float scentPriorityWeight = 1f;
    public float soundPriorityWeight = 1f;
    public float profilePredictionWeight = 1f;

    // Exposed scores for UI display
    public float lastChaseScore;
    public float lastInvestigateScore;
    public float lastPatrolScore;
    public float lastAmbushScore;

    // List to store detected clues
    public List<Clue> detectedClues = new List<Clue>();

    // Commenting out Update to prevent conflicting rapid destination sets with HunterAIController
    /*
    void Update()
    {
        // Get the best action based on current game state
        AIAction bestAction = CalculateBestAction();

        // Tell the hunter controller what to do
        ExecuteAction(bestAction);
    }
    */

    public void Tick()
    {
        // Same as Update for compatibility with HunterManager
        AIAction bestAction = CalculateBestAction();
        ExecuteAction(bestAction);
    }

    public void RegisterDetectedClue(Clue clue)
    {
        if (!detectedClues.Contains(clue))
        {
            detectedClues.Add(clue);
        }
    }

    private Vector3 GetPlayerPosition()
    {
        // Instead of direct player position, follow clue trail to estimate player location
        if (detectedClues.Count > 0)
        {
            // Use the most recent clue's position as best guess
            Clue latestClue = detectedClues[detectedClues.Count - 1];
            return latestClue.transform.position;
        }
        else
        {
            // No clues, fallback to searching patrol points
            return Vector3.zero; // Or some default/fallback position
        }
    }

    private Vector3 GetInvestigationPoint()
    {
        // Point to investigate is the position of the most relevant clue
        if (detectedClues.Count > 0)
        {
            // Use the most recent clue's position
            Clue latestClue = detectedClues[detectedClues.Count - 1];
            return latestClue.transform.position;
        }
        else
        {
            // No clues, fallback to "best guess" based on collected data (for now, random near hunter)
            return transform.position + Random.insideUnitSphere * 10f;
        }
    }

    private Vector3 GetPatrolPoint()
    {
        // Placeholder: Return a patrol point
        return transform.position + Random.insideUnitSphere * 20f;
    }

    private AIAction CalculateBestAction()
    {
        // Get references to the systems
        PlayerProfileManager profileManager = PlayerProfileManager.Instance;
        // You'd also get references to scent, sound systems from hunterController

        // Calculate scores for each possible action
        float chaseScore = 0f;
        float investigateScore = 0f;
        float patrolScore = 0.1f; // Base patrol score
        float ambushScore = 0.05f; // Base ambush score

        // SCORING LOGIC:
        // If player loves water (AqueousWeight is very negative), check rivers more
        if (profileManager != null)
        {
            // Player who uses water a lot? Prioritize river searches
            if (profileManager.profile.AqueousWeight < -50f)
            {
                investigateScore += 0.3f;
            }

            // Player who moves fast? Be more aggressive in chasing
            if (profileManager.profile.MobilityWeight > 70f)
            {
                chaseScore += 0.2f;
            }
        }

        // Add scores from your existing detection systems
        if (hunterController.HasDetectedSomething())
        {
            investigateScore += 0.4f;
        }

        // Add scores based on detected clues
        if (detectedClues.Count > 0)
        {
            investigateScore += detectedClues.Count * 0.2f; // More clues, higher investigate score
            chaseScore += 0.3f; // If has clues, more likely to chase
        }

        // Update exposed scores for UI
        lastChaseScore = chaseScore;
        lastInvestigateScore = investigateScore;
        lastPatrolScore = patrolScore;
        lastAmbushScore = ambushScore;

        // If it's night, hunter won't track (reduce chase and investigate scores)
        if (TimeManager.Instance != null && TimeManager.Instance.IsNight())
        {
            chaseScore *= 0.1f; // Significantly reduce chase at night
            investigateScore *= 0.5f; // Reduce investigate at night
        }

        // Return the highest scoring action
        AIAction action;
        if (chaseScore > investigateScore && chaseScore > patrolScore)
            action = new AIAction { type = ActionType.Chase, target = GetPlayerPosition() };
        else if (investigateScore > patrolScore)
            action = new AIAction { type = ActionType.Investigate, target = GetInvestigationPoint() };
        else
            action = new AIAction { type = ActionType.Patrol, target = GetPatrolPoint() };
        Debug.Log("Hunter chose action: " + action.type.ToString() + " at " + action.target.ToString());
        return action;
    }

    private void ExecuteAction(AIAction action)
    {
        Debug.Log("Executing Hunter action: " + action.type.ToString());
        // Use the method we added to your HunterAIController
        hunterController.SetActionFromUtilityAI(action.type, action.target);
    }

    // Simple data structure to pass actions around
    public struct AIAction
    {
        public ActionType type;
        public Vector3 target;
    }

    public enum ActionType
    {
        Patrol,
        Investigate,
        Chase,
        Ambush
    }
}
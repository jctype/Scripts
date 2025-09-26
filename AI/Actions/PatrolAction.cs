using UnityEngine;

[CreateAssetMenu(fileName = "PatrolAction", menuName = "AI/Patrol Action")]
public class PatrolAction : UtilityAction
{
    public override bool IsValid(HunterState state, WorldState worldState)
    {
        return !worldState.IsPlayerInSight && worldState.TimeSinceLastClue > 5f;
    }

    public override float CalculateScore(HunterState state, HunterHypothesis hypothesis, WorldState worldState)
    {
        return 0.7f; // Medium priority when no clues
    }

    public override void Execute(HunterState state, WorldState worldState)
    {
        // This would trigger movement to patrol points
        Debug.Log("Hunter: Patrolling...");
    }
}
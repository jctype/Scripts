// ChasePlayerAction.cs
using UnityEngine;

[CreateAssetMenu(menuName = "HunterAI/Actions/Chase Player")]
public class ChasePlayerAction : UtilityAction
{
    public override bool IsValid(HunterState hunterState, WorldState worldState)
    {
        bool isValid = worldState.IsPlayerInSight;
        Debug.Log($"[ChasePlayer] Valid: {isValid} (Player in sight: {worldState.IsPlayerInSight})");
        return isValid;
    }

    public override void Execute(HunterState hunterState, WorldState worldState)
    {
        Debug.Log($"[ChasePlayer] EXECUTING - Player spotted! Initiating chase!");
    }
}
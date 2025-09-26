// FollowScentAction.cs
using UnityEngine;
using ProjectHounded.AI.Core;

[CreateAssetMenu(menuName = "HunterAI/Actions/Follow Scent")]
public class FollowScentAction : UtilityAction
{
    public override bool IsValid(HunterState hunterState, WorldState worldState)
    {
        bool isValid = worldState.PrimaryScentClue.Freshness > 0.1f;
        Debug.Log($"[FollowScent] Valid: {isValid} (Scent Freshness: {worldState.PrimaryScentClue.Freshness:F2})");
        return isValid;
    }

    public override void Execute(HunterState hunterState, WorldState worldState)
    {
        Debug.Log($"[FollowScent] EXECUTING - Moving to scent at {worldState.PrimaryScentClue.WorldPosition} " +
                  $"(Freshness: {worldState.PrimaryScentClue.Freshness:F2})");
        // Add movement logic here
    }
}

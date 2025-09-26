// InvestigateSoundAction.cs
using UnityEngine;
using ProjectHounded.AI.Core;

[CreateAssetMenu(menuName = "HunterAI/Actions/Investigate Sound")]
public class InvestigateSoundAction : UtilityAction
{
    public override bool IsValid(HunterState hunterState, WorldState worldState)
    {
        bool isValid = worldState.PrimarySoundClue.Loudness > 0.2f &&
                      !worldState.IsPlayerInSight;
        Debug.Log($"[InvestigateSound] Valid: {isValid} (Loudness: {worldState.PrimarySoundClue.Loudness:F2}, " +
                 $"Player Visible: {worldState.IsPlayerInSight})");
        return isValid;
    }

    public override void Execute(HunterState hunterState, WorldState worldState)
    {
        Debug.Log($"[InvestigateSound] EXECUTING - Investigating {worldState.PrimarySoundClue} sound at " +
                 $"{worldState.PrimarySoundClue.WorldPosition} (Loudness: {worldState.PrimarySoundClue.Loudness:F2})");
    }
}

// SearchLastKnownPositionAction.cs
using UnityEngine;
using ProjectHounded.AI.Core;

[CreateAssetMenu(menuName = "HunterAI/Actions/Search Last Known Position")]
public class SearchLastKnownPositionAction : UtilityAction
{
    public override bool IsValid(HunterState hunterState, WorldState worldState)
    {
        bool isValid = worldState.TimeSinceLastClue > 5f && 
                      worldState.TimeSinceLastClue < 60f;
        Debug.Log($"[SearchLastKnown] Valid: {isValid} (Time since clue: {worldState.TimeSinceLastClue:F1}s)");
        return isValid;
    }

    public override void Execute(HunterState hunterState, WorldState worldState)
    {
        Vector3 searchPos = worldState.PrimaryScentClue.Freshness > worldState.PrimarySoundClue.Loudness ? 
                           worldState.PrimaryScentClue.WorldPosition : worldState.PrimarySoundClue.WorldPosition;
        
        Debug.Log($"[SearchLastKnown] EXECUTING - Searching area around {searchPos} " +
                 $"(Clue age: {worldState.TimeSinceLastClue:F1}s)");
    }
}
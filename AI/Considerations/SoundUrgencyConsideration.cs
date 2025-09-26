// SoundUrgencyConsideration.cs
using UnityEngine;
using ProjectHounded.AI.Core;

[CreateAssetMenu(menuName = "HunterAI/Considerations/Sound Urgency")]
public class SoundUrgencyConsideration : Consideration
{
    public override float Score(ProjectHounded.AI.Core.HunterState hunterState, ProjectHounded.AI.Core.HunterHypothesis currentHypothesis, ProjectHounded.AI.Core.WorldState worldState)
    {
        float score = worldState.PrimarySoundClue.Loudness;
        Debug.Log($"[SoundUrgency] Score: {score:F2} (Loudness: {worldState.PrimarySoundClue.Loudness:F2})");
        return score;
    }
}
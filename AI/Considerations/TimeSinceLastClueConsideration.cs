// TimeSinceLastClueConsideration.cs  
using UnityEngine;
using ProjectHounded.AI.Core;

[CreateAssetMenu(menuName = "HunterAI/Considerations/Time Since Last Clue")]
public class TimeSinceLastClueConsideration : Consideration
{
    public AnimationCurve timeCurve = AnimationCurve.EaseInOut(0, 1, 30, 0); // Score drops over 30 seconds

    public override float Score(ProjectHounded.AI.Core.HunterState hunterState, ProjectHounded.AI.Core.HunterHypothesis currentHypothesis, ProjectHounded.AI.Core.WorldState worldState)
    {
        float score = timeCurve.Evaluate(worldState.TimeSinceLastClue);
        Debug.Log($"[TimeSinceClue] Score: {score:F2} (Time: {worldState.TimeSinceLastClue:F1}s)");
        return score;
    }
}
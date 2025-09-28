using UnityEngine;

namespace ProjectHounded.AI.Core
{
    [CreateAssetMenu(menuName = "HunterAI/Considerations/Scent Freshness")]
    public class ScentFreshnessConsideration : Consideration
    {
        public override float Score(HunterState hunterState, HunterHypothesis currentHypothesis, WorldState worldState)
        {
            float score = worldState.PrimaryScentClue.Freshness;
            Debug.Log($"[ScentFreshness] Score: {score:F2} (Freshness: {worldState.PrimaryScentClue.Freshness:F2})");
            return score;
        }
    }
}
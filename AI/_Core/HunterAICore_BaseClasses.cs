using UnityEngine;

namespace ProjectHounded.AI.Core
{
    public abstract class Consideration : ScriptableObject
    {
        [TextArea] public string description;
        public abstract float Score(HunterState hunterState, HunterHypothesis currentHypothesis, WorldState worldState);
    }

    public abstract class UtilityAction : ScriptableObject
    {
        [Header("Utility Scoring")]
        public Consideration[] considerations;

        [Header("Action Metadata")]
        public string actionName;
        [TextArea] public string actionDescription;

        public abstract bool IsValid(HunterState hunterState, WorldState worldState);
        public abstract void Execute(HunterState hunterState, WorldState worldState);

        public float CalculateScore(HunterState hunterState, HunterHypothesis hypothesis, WorldState worldState)
        {
            if (!IsValid(hunterState, worldState) || considerations == null || considerations.Length == 0)
                return 0f;

            float finalScore = 1f;
            foreach (var consideration in considerations)
            {
                if (consideration != null)
                {
                    finalScore *= consideration.Score(hunterState, hypothesis, worldState);
                }
            }
            return finalScore;
        }
    }
}
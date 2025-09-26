using UnityEngine;

public abstract class UtilityAction : ScriptableObject
{
    [Header("Action Settings")]
    public string actionName;

    public virtual bool IsValid(HunterState state, WorldState worldState)
    {
        return true;
    }

    public virtual float CalculateScore(HunterState state, HunterHypothesis hypothesis, WorldState worldState)
    {
        return 0f;
    }

    public virtual void Execute(HunterState state, WorldState worldState)
    {
        Debug.Log($"Executing: {actionName}");
    }
}
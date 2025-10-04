using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class AnimationContext
{
    public HunterMentalState mentalState;
    public HunterMovementState movementState;
    public HunterActionIntent actionIntent;
    public EnvironmentType environment;
    public float fatigueLevel;
    public float alertnessLevel;
    public Vector3 currentVelocity;
    public bool isInDanger;
    public float timeOfDay;
}

public enum HunterMentalState
{
    Calm,
    Alert,
    Agitated,
    Fearful,
    Exhausted,
    Determined
}

public enum HunterMovementState
{
    Stationary,
    Walking,
    Running,
    Sneaking,
    Crawling,
    Climbing,
    Falling
}

public enum HunterActionIntent
{
    None,
    Patrol,
    Investigate,
    Chase,
    Search,
    Rest,
    React,
    Combat
}

public enum EnvironmentType
{
    Forest,
    Clearing,
    Water,
    Rocky,
    ThickBrush,
    Village
}

[System.Serializable]
public class AnimationTrigger
{
    public string animationName;
    public AnimationPriority priority;
    public float minDuration;
    public float maxDuration;
    public List<HunterMentalState> validMentalStates;
    public List<HunterMovementState> validMovementStates;
    public List<EnvironmentType> validEnvironments;
    public float minFatigue;
    public float maxFatigue;
    public float probability = 1.0f;
}

public enum AnimationPriority
{
    Low,        // Idle variations, subtle movements
    Medium,     // Movement transitions, environmental reactions
    High,       // Investigation, important actions
    Critical,   // Combat, falls, major reactions
    Emergency   // Interrupt everything
}

public class HunterAnimationController : MonoBehaviour
{
    [Header("Animation Configuration")]
    public Animator animator;
    public float stateEvaluationInterval = 0.3f;

    [Header("Animation Libraries")]
    public List<AnimationTrigger> idleAnimations;
    public List<AnimationTrigger> movementAnimations;
    public List<AnimationTrigger> reactionAnimations;
    public List<AnimationTrigger> investigationAnimations;
    public List<AnimationTrigger> environmentalAnimations;

    private AnimationContext currentContext;
    private string currentAnimation;
    private AnimationPriority currentPriority;
    private Coroutine currentAnimationRoutine;
    private bool isAnimationLocked = false;

    // External state providers
    private HunterAIController aiController;
    private HunterMentalStateController mentalController;
    private EnvironmentDetector environmentDetector;

    void Start()
    {
        aiController = GetComponent<HunterAIController>();
        mentalController = GetComponent<HunterMentalStateController>();
        environmentDetector = GetComponent<EnvironmentDetector>();

        currentContext = new AnimationContext();
        StartCoroutine(ContinuousStateEvaluation());
    }

    private IEnumerator ContinuousStateEvaluation()
    {
        while (true)
        {
            UpdateAnimationContext();
            EvaluateAndTriggerAnimation();
            yield return new WaitForSeconds(stateEvaluationInterval);
        }
    }

    private void UpdateAnimationContext()
    {
        // Gather all contextual information
        currentContext.mentalState = mentalController.GetCurrentMentalState();
        currentContext.movementState = GetMovementState();
        currentContext.actionIntent = aiController.GetCurrentIntent();
        currentContext.environment = environmentDetector.GetCurrentEnvironment();
        currentContext.fatigueLevel = mentalController.GetFatigueLevel();
        currentContext.alertnessLevel = mentalController.GetAlertnessLevel();
        currentContext.currentVelocity = GetComponent<Rigidbody>().velocity;
        currentContext.isInDanger = aiController.IsInDanger();
        currentContext.timeOfDay = TimeManager.Instance.GetNormalizedTime();
    }

    private HunterMovementState GetMovementState()
    {
        float speed = currentContext.currentVelocity.magnitude;

        if (speed < 0.1f) return HunterMovementState.Stationary;
        if (speed < 2.0f) return HunterMovementState.Walking;
        if (speed < 5.0f) return HunterMovementState.Running;
        return HunterMovementState.Running;
    }

    private void EvaluateAndTriggerAnimation()
    {
        AnimationTrigger nextAnimation = SelectAppropriateAnimation();

        if (nextAnimation != null && ShouldInterruptCurrentAnimation(nextAnimation))
        {
            TriggerAnimation(nextAnimation);
        }
    }

    private AnimationTrigger SelectAppropriateAnimation()
    {
        List<AnimationTrigger> candidateAnimations = new List<AnimationTrigger>();
        List<AnimationLibrary> librariesToCheck = GetRelevantLibraries();

        foreach (var library in librariesToCheck)
        {
            foreach (var anim in GetAnimationsFromLibrary(library))
            {
                if (IsAnimationValid(anim, currentContext) && Random.value <= anim.probability)
                {
                    candidateAnimations.Add(anim);
                }
            }
        }

        if (candidateAnimations.Count == 0) return null;

        // Select based on priority and context relevance
        return SelectBestAnimation(candidateAnimations);
    }

    private List<AnimationLibrary> GetRelevantLibraries()
    {
        List<AnimationLibrary> libraries = new List<AnimationLibrary>();

        // Always check environmental reactions
        libraries.Add(AnimationLibrary.Environmental);

        // Check based on current state
        switch (currentContext.movementState)
        {
            case HunterMovementState.Stationary:
                libraries.Add(AnimationLibrary.Idle);
                break;
            case HunterMovementState.Walking:
            case HunterMovementState.Running:
                libraries.Add(AnimationLibrary.Movement);
                break;
        }

        // Check based on action intent
        switch (currentContext.actionIntent)
        {
            case HunterActionIntent.Investigate:
                libraries.Add(AnimationLibrary.Investigation);
                break;
            case HunterActionIntent.React:
                libraries.Add(AnimationLibrary.Reaction);
                break;
        }

        return libraries;
    }

    private List<AnimationTrigger> GetAnimationsFromLibrary(AnimationLibrary library)
    {
        switch (library)
        {
            case AnimationLibrary.Idle: return idleAnimations;
            case AnimationLibrary.Movement: return movementAnimations;
            case AnimationLibrary.Reaction: return reactionAnimations;
            case AnimationLibrary.Investigation: return investigationAnimations;
            case AnimationLibrary.Environmental: return environmentalAnimations;
            default: return new List<AnimationTrigger>();
        }
    }

    private bool IsAnimationValid(AnimationTrigger anim, AnimationContext context)
    {
        // Check mental state
        if (anim.validMentalStates.Count > 0 && !anim.validMentalStates.Contains(context.mentalState))
            return false;

        // Check movement state
        if (anim.validMovementStates.Count > 0 && !anim.validMovementStates.Contains(context.movementState))
            return false;

        // Check environment
        if (anim.validEnvironments.Count > 0 && !anim.validEnvironments.Contains(context.environment))
            return false;

        // Check fatigue
        if (context.fatigueLevel < anim.minFatigue || context.fatigueLevel > anim.maxFatigue)
            return false;

        return true;
    }

    private AnimationTrigger SelectBestAnimation(List<AnimationTrigger> candidates)
    {
        // Group by priority and select from highest priority group
        var groupedByPriority = candidates.GroupBy(a => a.priority)
                                         .OrderByDescending(g => g.Key)
                                         .First();

        // If multiple in same priority, use weighted random selection
        return WeightedRandomSelection(groupedByPriority.ToList());
    }

    private AnimationTrigger WeightedRandomSelection(List<AnimationTrigger> animations)
    {
        float totalWeight = animations.Sum(a => a.probability);
        float randomValue = Random.Range(0f, totalWeight);

        foreach (var anim in animations)
        {
            if (randomValue < anim.probability)
                return anim;
            randomValue -= anim.probability;
        }

        return animations[0];
    }

    private bool ShouldInterruptCurrentAnimation(AnimationTrigger newAnimation)
    {
        if (string.IsNullOrEmpty(currentAnimation)) return true;
        if (newAnimation.priority > currentPriority) return true;
        if (newAnimation.priority == AnimationPriority.Emergency) return true;

        return false;
    }

    private void TriggerAnimation(AnimationTrigger animationTrigger)
    {
        if (currentAnimationRoutine != null)
            StopCoroutine(currentAnimationRoutine);

        currentAnimationRoutine = StartCoroutine(PlayAnimationSequence(animationTrigger));
    }

    private IEnumerator PlayAnimationSequence(AnimationTrigger animationTrigger)
    {
        isAnimationLocked = true;
        currentAnimation = animationTrigger.animationName;
        currentPriority = animationTrigger.priority;

        // Trigger the animation
        animator.SetTrigger(currentAnimation);

        // Calculate duration
        float duration = Random.Range(animationTrigger.minDuration, animationTrigger.maxDuration);

        // Wait for animation to complete or be interrupted
        yield return new WaitForSeconds(duration);

        // Animation complete
        isAnimationLocked = false;
        currentPriority = AnimationPriority.Low;
    }

    // Public method for external systems to trigger animations
    public void TriggerEmergencyAnimation(string animationName, float duration)
    {
        var emergencyTrigger = new AnimationTrigger
        {
            animationName = animationName,
            priority = AnimationPriority.Emergency,
            minDuration = duration,
            maxDuration = duration
        };

        TriggerAnimation(emergencyTrigger);
    }

    public bool IsAvailableForNewAnimation()
    {
        return !isAnimationLocked || currentPriority <= AnimationPriority.Medium;
    }
}

public enum AnimationLibrary
{
    Idle,
    Movement,
    Reaction,
    Investigation,
    Environmental
}
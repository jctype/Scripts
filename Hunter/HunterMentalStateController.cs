using UnityEngine;
using System.Collections;

public class HunterMentalStateController : MonoBehaviour
{
    [Header("Mental State Configuration")]
    public float baseFatigueRate = 0.1f;
    public float baseAlertnessDecay = 0.05f;
    public float maxFatigue = 100f;
    public float maxAlertness = 100f;

    private float currentFatigue = 0f;
    private float currentAlertness = 50f;
    private HunterMentalState currentMentalState = HunterMentalState.Calm;
    private HunterAIController aiController;

    void Start()
    {
        aiController = GetComponent<HunterAIController>();
        StartCoroutine(MentalStateUpdate());
    }

    private IEnumerator MentalStateUpdate()
    {
        while (true)
        {
            UpdateFatigue();
            UpdateAlertness();
            UpdateMentalState();
            yield return new WaitForSeconds(1f);
        }
    }

    private void UpdateFatigue()
    {
        // Increase fatigue based on activity
        float activityMultiplier = aiController.IsMoving() ? 1.5f : 1.0f;
        currentFatigue += baseFatigueRate * activityMultiplier * Time.deltaTime;
        currentFatigue = Mathf.Clamp(currentFatigue, 0f, maxFatigue);
    }

    private void UpdateAlertness()
    {
        // Decay alertness naturally, but increase with stimuli
        currentAlertness -= baseAlertnessDecay * Time.deltaTime;

        // Add stimuli from AI controller
        if (aiController.HasDetectedSomething())
            currentAlertness += 10f;

        currentAlertness = Mathf.Clamp(currentAlertness, 0f, maxAlertness);
    }

    private void UpdateMentalState()
    {
        // Determine mental state based on fatigue and alertness
        if (currentFatigue > 80f)
        {
            currentMentalState = HunterMentalState.Exhausted;
        }
        else if (currentAlertness > 70f)
        {
            currentMentalState = aiController.IsInDanger() ?
                HunterMentalState.Fearful : HunterMentalState.Alert;
        }
        else if (currentAlertness > 40f)
        {
            currentMentalState = HunterMentalState.Agitated;
        }
        else
        {
            currentMentalState = HunterMentalState.Calm;
        }
    }

    public HunterMentalState GetCurrentMentalState() => currentMentalState;
    public float GetFatigueLevel() => currentFatigue / maxFatigue;
    public float GetAlertnessLevel() => currentAlertness / maxAlertness;

    public void AddStimulus(float intensity, StimulusType type)
    {
        currentAlertness += intensity;
        // Additional logic based on stimulus type
    }
}

public enum StimulusType
{
    Visual,
    Auditory,
    Scent,
    Physical
}
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum WeatherState
{
    Clear,
    PartlyCloudy,
    Overcast,
    Stormy,
    Clearing
}

[System.Serializable]
public class WeatherPreset
{
    public WeatherState state;
    public float coverage;
    public float density;
    public float noiseScale;
    public float detailIntensity;
    public float transitionDuration = 60f;
    public float minDuration = 120f;
}

public class WeatherSystem : MonoBehaviour
{
    [Header("Weather Presets")]
    public WeatherPreset clearPreset;
    public WeatherPreset partlyCloudyPreset;
    public WeatherPreset overcastPreset;
    public WeatherPreset stormyPreset;

    [Header("Cloud Layers")]
    public CloudLayer[] cloudLayers;

    [Header("Current Weather")]
    public WeatherState currentState = WeatherState.Clear;
    public WeatherState targetState = WeatherState.Clear;

    private Hashtable presetMap;
    private Coroutine weatherSequenceCoroutine;
    private float stateStartTime;

    // Events for other systems
    public System.Action<WeatherState> OnWeatherChanged;
    public System.Action<WeatherState> OnWeatherTransitionComplete;

    void Start()
    {
        InitializePresetMap();
        StartCoroutine(WeatherLifecycleRoutine());
    }

    private void InitializePresetMap()
    {
        presetMap = new Hashtable
        {
            { WeatherState.Clear, clearPreset },
            { WeatherState.PartlyCloudy, partlyCloudyPreset },
            { WeatherState.Overcast, overcastPreset },
            { WeatherState.Stormy, stormyPreset }
        };
    }

    public void SetWeather(WeatherState newState)
    {
        if (newState == currentState) return;

        targetState = newState;
        Debug.Log($"Weather changing from {currentState} to {newState}");

        if (weatherSequenceCoroutine != null)
            StopCoroutine(weatherSequenceCoroutine);

        weatherSequenceCoroutine = StartCoroutine(WeatherTransitionSequence(newState));
    }

    private IEnumerator WeatherLifecycleRoutine()
    {
        while (true)
        {
            WeatherPreset currentPreset = (WeatherPreset)presetMap[currentState];
            float timeInState = Time.time - stateStartTime;
            float remainingTime = Mathf.Max(0, currentPreset.minDuration - timeInState);

            yield return new WaitForSeconds(remainingTime + Random.Range(30f, 180f));

            WeatherState[] possibleStates = GetPossibleWeatherTransitions();
            WeatherState nextState = possibleStates[Random.Range(0, possibleStates.Length)];
            SetWeather(nextState);
        }
    }

    private IEnumerator WeatherTransitionSequence(WeatherState newState)
    {
        WeatherPreset targetPreset = (WeatherPreset)presetMap[newState];
        WeatherPreset currentPreset = (WeatherPreset)presetMap[currentState];

        OnWeatherChanged?.Invoke(newState);

        float transitionStartTime = Time.time;
        float transitionEndTime = transitionStartTime + targetPreset.transitionDuration;

        while (Time.time < transitionEndTime)
        {
            float progress = (Time.time - transitionStartTime) / targetPreset.transitionDuration;

            foreach (CloudLayer layer in cloudLayers)
            {
                layer.SetTargetParameters(
                    Mathf.Lerp(currentPreset.coverage, targetPreset.coverage, progress),
                    Mathf.Lerp(currentPreset.density, targetPreset.density, progress),
                    Mathf.Lerp(currentPreset.noiseScale, targetPreset.noiseScale, progress),
                    Mathf.Lerp(currentPreset.detailIntensity, targetPreset.detailIntensity, progress)
                );
            }

            yield return null;
        }

        foreach (CloudLayer layer in cloudLayers)
        {
            layer.SetTargetParameters(targetPreset.coverage, targetPreset.density,
                                    targetPreset.noiseScale, targetPreset.detailIntensity);
        }

        currentState = newState;
        stateStartTime = Time.time;
        OnWeatherTransitionComplete?.Invoke(newState);

        TriggerWeatherEffects(newState);
    }

    private void TriggerWeatherEffects(WeatherState state)
    {
        switch (state)
        {
            case WeatherState.Stormy:
                Debug.Log("Starting rain effects");
                break;
            case WeatherState.Overcast:
                Debug.Log("Starting overcast effects");
                break;
            case WeatherState.Clear:
                Debug.Log("Stopping weather effects");
                break;
        }
    }

    private WeatherState[] GetPossibleWeatherTransitions()
    {
        switch (currentState)
        {
            case WeatherState.Clear:
                return new WeatherState[] { WeatherState.PartlyCloudy };
            case WeatherState.PartlyCloudy:
                return new WeatherState[] { WeatherState.Clear, WeatherState.Overcast };
            case WeatherState.Overcast:
                return new WeatherState[] { WeatherState.PartlyCloudy, WeatherState.Stormy, WeatherState.Clearing };
            case WeatherState.Stormy:
                return new WeatherState[] { WeatherState.Overcast, WeatherState.Clearing };
            case WeatherState.Clearing:
                return new WeatherState[] { WeatherState.PartlyCloudy, WeatherState.Clear };
            default:
                return new WeatherState[] { WeatherState.Clear };
        }
    }

    public void SetClear() => SetWeather(WeatherState.Clear);
    public void SetPartlyCloudy() => SetWeather(WeatherState.PartlyCloudy);
    public void SetOvercast() => SetWeather(WeatherState.Overcast);
    public void SetStormy() => SetWeather(WeatherState.Stormy);

    void OnDestroy()
    {
        if (weatherSequenceCoroutine != null)
            StopCoroutine(weatherSequenceCoroutine);
    }
}
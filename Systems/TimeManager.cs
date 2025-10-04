using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance;

    public float gameTime = 0f; // Game time in seconds

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        gameTime += Time.deltaTime;
    }

    public float GetNormalizedTime()
    {
        // Return normalized time of day (0-1) based on game time
        return (gameTime % 24f) / 24f; // Cycle every 24 seconds
    }

    public bool IsNight()
    {
        float normalizedTime = GetNormalizedTime();
        // Assume night is from 0.75 to 1.0 (18:00 to 24:00) and 0.0 to 0.25 (00:00 to 06:00)
        return normalizedTime >= 0.75f || normalizedTime <= 0.25f;
    }
}

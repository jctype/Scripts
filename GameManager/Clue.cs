using UnityEngine;

public enum ClueType { Scent, Sound, Visual }

public class Clue : MonoBehaviour
{
    public ClueType Type;
    public float Intensity;
    public float DecayRate;
    // ... other properties ...

    void Update()
    {
        // Decay over time
        Intensity -= DecayRate * Time.deltaTime;
        if (Intensity <= 0) Destroy(gameObject);
    }
}

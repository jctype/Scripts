using UnityEngine;

public class PlayerProfileManager : MonoBehaviour
{
    public static PlayerProfileManager Instance;

    [System.Serializable]
    public class PlayerProfile
    {
        public float AqueousWeight = 0f;      // -100 to +100
        public float MobilityWeight = 0f;     // 0 to 100  
        public float DeceptionWeight = 0f;    // 0 to 100
        public float RiskWeight = 0f;         // 0 to 100
                                              // Add other weights as needed

        // THE UNIFIED METHOD WE DISCUSSED
        public void ModifyProfileWeight(ref float weight, float delta, float decayRate)
        {
            weight += delta;
            weight = Mathf.Clamp(weight, -100f, 100f);
            // Decay would be handled in an Update method
        }
    }

    public PlayerProfile profile = new PlayerProfile();

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        // Apply decay to all weights over time
        profile.AqueousWeight *= 0.995f; // Slowly forgets water habits
        profile.MobilityWeight *= 0.998f;
        // etc...
    }

    // Called by other systems when player does things
    public void OnPlayerEnteredWater()
    {
        profile.ModifyProfileWeight(ref profile.AqueousWeight, -15f, 0f);
    }

    public void OnPlayerUsedTool()
    {
        profile.ModifyProfileWeight(ref profile.DeceptionWeight, 12f, 0f);
    }
}
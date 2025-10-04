using UnityEngine;

public enum TerrainType { Grass, Mud, Water, Rock }

public class PlayerManager : MonoBehaviour
{
    // --- Singleton Pattern (Easy access from other scripts) ---
    public static PlayerManager Instance;

    // --- The Master Player Profile ---
    public PlayerProfileManager.PlayerProfile Profile; // This is the class with AqueousWeight, MobilityWeight, etc.

    // --- Player State Variables ---
    public float CurrentStamina;
    public float MaxStamina;
    public bool IsSprinting;
    public bool IsCrouching;
    public TerrainType CurrentTerrain; // Enum: Grass, Mud, Water, Rock
    public float NoiseLevel; // 0-1 value, calculated from movement

    void Awake() {
        if (Instance == null) Instance = this;
        Profile = new PlayerProfileManager.PlayerProfile(); // Initialize the profile
    }

    void Update() {
        // This manager updates the profile based on the player character's actions
        UpdateProfileFromMovement();
    }

    void UpdateProfileFromMovement() {
        // Get the PlayerController (the script that handles input/physics)
        PlayerController pc = GetComponent<PlayerController>(); // Or find it

        IsSprinting = pc.IsSprinting;
        IsCrouching = pc.IsCrouching;

        // Calculate noise. This is a simple example.
        NoiseLevel = IsSprinting ? 0.8f : (IsCrouching ? 0.1f : 0.4f);

        // Tell the profile to update MobilityWeight
        // We use our unified method! +2 per second while sprinting, decay of -0.1/sec
        if (IsSprinting) {
            Profile.ModifyProfileWeight(ref Profile.MobilityWeight, 2.0f * Time.deltaTime, -0.1f);
        }

        // Tell the profile to update AqueousWeight if in water
        if (CurrentTerrain == TerrainType.Water) {
            Profile.ModifyProfileWeight(ref Profile.AqueousWeight, -15.0f, -0.05f);
        }
    }

    // Public method for other scripts to call, e.g., when a tool is used.
    public void OnPlayerUsedTool() {
        Profile.ModifyProfileWeight(ref Profile.DeceptionWeight, 12.0f, -0.03f);
    }
}

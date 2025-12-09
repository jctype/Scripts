using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ProgressionData", menuName = "Clinkforge/Progression Data")]
public class ProgressionData : ScriptableObject
{
    // One entry per tier (6 tiers + finale handled separately)
    public List<TierData> tiers = new List<TierData>();

    [System.Serializable]
    public class TierData
    {
        public string tierName = "Surface Delves";           // VO + UI name
        public int maxTrampSockets = 15;                     // How many rebounders player can place
        public int ballsToLaunchMin = 5;
        public int ballsToLaunchMax = 10;
        public float timeLimitSeconds = 60f;
        public int requiredGoalPercentage = 70;              // Entry level requirement
        public RebounderType newUnlockAfterBoss = RebounderType.None; // Set in inspector
        public AudioClip introVO;                            // Optional gruff dwarf line
        // Challenge tightening per level type done in GameManager
    }

    public TierData GetTier(int tierIndexZeroBased) => tiers[tierIndexZeroBased];
}
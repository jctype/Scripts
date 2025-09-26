// HunterAICore_DataStructures.cs
using UnityEngine;

namespace ProjectHounded.AI.Core
{
    [System.Serializable]
    public class HunterState
    {
        [Range(0f, 1f)] public float Certainty = 0.5f;
        [Range(0f, 1f)] public float Suspicion = 0.1f;
        [Range(0f, 1f)] public float Focus = 1.0f;
        [Range(0f, 1f)] public float Fatigue = 0.0f;
        [Range(0f, 1f)] public float Frustration = 0.0f;
    }

    [System.Serializable]
    public class HunterHypothesis
    {
        public Vector3 TargetLocation = Vector3.zero;
        public enum PlayerIntent { Fleeing, Hiding, SettingAmbush, CreatingDiversion }
        public PlayerIntent EstimatedIntent = PlayerIntent.Fleeing;
        [Range(0f, 1f)] public float Confidence = 0.0f;
    }

    public struct ScentData
    {
        public Vector3 WorldPosition;
        [Range(0f, 1f)] public float Freshness;
        [Range(0f, 1f)] public float Strength;
        public static ScentData Invalid => new ScentData { Freshness = 0f, Strength = 0f };
    }

    public struct SoundEvent
    {
        public Vector3 WorldPosition;
        [Range(0f, 1f)] public float Loudness;
        public string Type;
        public float CreationTime;
        public float CurrentLoudness;
        public float PropagationRadius;

        public static SoundEvent Invalid => new SoundEvent
        {
            Loudness = 0f,
            CurrentLoudness = 0f,
            CreationTime = -1f
        };
    }

    [System.Serializable]
    public class WorldState
    {
        public ScentData PrimaryScentClue = ScentData.Invalid;
        public SoundEvent PrimarySoundClue = SoundEvent.Invalid;
        public float TimeSinceLastClue = Mathf.Infinity;
        public bool IsPlayerInSight = false;
    }
}
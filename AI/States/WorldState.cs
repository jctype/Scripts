using UnityEngine;

[System.Serializable]
public class WorldState
{
    public bool IsPlayerInSight = false;
    public float TimeSinceLastClue = 0f;

    [System.Serializable]
    public class ScentClue
    {
        public Vector3 WorldPosition;
        public float Freshness = 0f;
    }

    [System.Serializable]
    public class SoundClue
    {
        public Vector3 WorldPosition;
        public float Loudness = 0f;
        public string Type = "";
    }

    public ScentClue PrimaryScentClue = new ScentClue();
    public SoundClue PrimarySoundClue = new SoundClue();
}
using UnityEngine;
using System.Collections.Generic;

namespace ProjectHounded.AI.Core
{
    public class SoundSystem : MonoBehaviour
    {
        [Header("Sound Configuration")]
        public float maxSoundLifetime = 10f;

        private List<SoundEvent> activeSoundEvents = new List<SoundEvent>();

        void Update() => CleanupExpiredSounds();

        public SoundEvent GetMostRelevantSound()
        {
            if (activeSoundEvents.Count == 0)
                return SoundEvent.Invalid;

            SoundEvent mostRelevant = activeSoundEvents[0];
            for (int i = 1; i < activeSoundEvents.Count; i++)
            {
                if (activeSoundEvents[i].Loudness > mostRelevant.Loudness)
                    mostRelevant = activeSoundEvents[i];
            }
            return mostRelevant;
        }

        public void RegisterSoundEvent(Vector3 position, float loudness, string type)
        {
            SoundEvent newEvent = new SoundEvent
            {
                WorldPosition = position,
                Loudness = loudness,
                Type = type,
                CreationTime = Time.time,
                CurrentLoudness = loudness,
                PropagationRadius = 10f // Default propagation radius
            };
            activeSoundEvents.Add(newEvent);
        }

        private void CleanupExpiredSounds()
        {
            for (int i = activeSoundEvents.Count - 1; i >= 0; i--)
            {
                if (Time.time - activeSoundEvents[i].CreationTime > maxSoundLifetime)
                    activeSoundEvents.RemoveAt(i);
            }
        }


    }
}
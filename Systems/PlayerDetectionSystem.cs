// AI/Core/PlayerDetectionSystem.cs
using UnityEngine;

namespace ProjectHounded.AI.Core
{
    public class PlayerDetectionSystem : MonoBehaviour
    {
        public float sightRange = 20f;
        public Transform hunterEyePosition;
        public GameObject playerObject;

        public bool IsPlayerVisible()
        {
            if (playerObject == null) return false;

            Vector3 direction = (playerObject.transform.position - hunterEyePosition.position).normalized;
            float distance = Vector3.Distance(hunterEyePosition.position, playerObject.transform.position);

            return distance <= sightRange &&
                   !Physics.Raycast(hunterEyePosition.position, direction, distance);
        }
    }
}
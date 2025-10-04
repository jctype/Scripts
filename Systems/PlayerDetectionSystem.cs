using UnityEngine;

namespace ProjectHounded.AI.Core
{
    public class PlayerDetectionSystem : MonoBehaviour
    {
        public float sightRange = 800f;
        public Transform hunterEyePosition;
        public GameObject playerObject;

        public bool IsPlayerVisible()
        {
            if (playerObject == null)
            {
                playerObject = GameObject.FindWithTag("Player");
                if (playerObject == null) return false;
            }

            if (hunterEyePosition == null)
            {
                hunterEyePosition = transform; // Default to hunter's transform
            }

            Vector3 direction = (playerObject.transform.position - hunterEyePosition.position).normalized;
            float distance = Vector3.Distance(hunterEyePosition.position, playerObject.transform.position);

            if (distance > sightRange) return false;

            RaycastHit hit;
            if (Physics.Raycast(hunterEyePosition.position, direction, out hit, distance))
            {
                // If the hit is the player, it's visible
                return hit.collider.gameObject == playerObject;
            }
            else
            {
                // No hit, visible
                return true;
            }
        }
    }
}
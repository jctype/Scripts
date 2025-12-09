using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ForgeheartController : MonoBehaviour
{
    private Rigidbody rb;
    private TrailRenderer trail;
    private int bounceCount = 0;

    [Header("Drift Correction")]
    public float maxBouncesBeforeNudge = 20;
    public float nudgeStrength = 0.008f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        trail = GetComponent<TrailRenderer>();
        if (trail == null)
        {
            trail = gameObject.AddComponent<TrailRenderer>();
            trail.time = 0.5f;
            trail.startWidth = 0.05f;
            trail.endWidth = 0.01f;
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.startColor = Color.yellow;
            trail.endColor = new Color(1, 1, 0, 0);
        }
    }

    private void OnEnable()
    {
        // Reset state when enabled from pool
        bounceCount = 0;
        if (trail != null)
            trail.Clear();
    }

    private void OnCollisionEnter(Collision collision)
    {
        bounceCount++;

        // Play clink sound
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayClink(rb.velocity.magnitude);

        // Tier 4+ drift correction
        if (GameManager.Instance != null && bounceCount > maxBouncesBeforeNudge && GameManager.Instance.currentTier >= 3)
        {
            Vector3 vel = rb.velocity;
            if (vel.magnitude > 0.1f)
            {
                vel += vel.normalized * nudgeStrength;
                rb.velocity = vel;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Goal"))
        {
            if (GameManager.Instance != null)
                GameManager.Instance.RegisterGoal();
            ReturnToPool();
        }
        else if (other.CompareTag("Void"))
        {
            if (GameManager.Instance != null)
                GameManager.Instance.RegisterVoid();
            ReturnToPool();
        }
    }

    private void ReturnToPool()
    {
        // Reset velocity
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Return to pool via GameManager
        if (GameManager.Instance != null && GameManager.Instance.ForgeheartPool != null)
        {
            GameManager.Instance.ForgeheartPool.Return(gameObject);
        }
        else
        {
            // Fallback
            gameObject.SetActive(false);
        }
    }
}
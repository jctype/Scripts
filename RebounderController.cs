using UnityEngine;

public class RebounderController : MonoBehaviour
{
    public RebounderType type;
    public ParticleSystem hitEffect;
    public AudioClip[] hitSounds;

    private bool hasBeenUsed = false; // To prevent multiple registrations

    protected virtual void OnBallHit(ForgeheartController ball)
    {
        // Play effect
        if (hitEffect) hitEffect.Play();
        if (hitSounds.Length > 0)
            AudioManager.Instance.PlaySound(hitSounds[Random.Range(0, hitSounds.Length)]);

        // Register placement for challenges (only once per rebounder)
        if (!hasBeenUsed)
        {
            GameManager.Instance.RegisterTrampPlaced(type);
            hasBeenUsed = true;
        }

        // Override in child classes for special behavior
        switch (type)
        {
            case RebounderType.Rune_of_Twinning:
                Instantiate(ball.gameObject, transform.position + Vector3.up * 0.5f, Random.rotation);
                Instantiate(ball.gameObject, transform.position + Vector3.up * 0.5f + Vector3.right, Random.rotation);
                break;
            case RebounderType.Gate_of_Return:
                ball.transform.position = GameManager.Instance.anvilGoalTransform.position + Vector3.up * 2f;
                break;
                // etc. – you get the idea
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Forgeheart"))
        {
            ForgeheartController ball = collision.gameObject.GetComponent<ForgeheartController>();
            if (ball) OnBallHit(ball);
        }
    }
}
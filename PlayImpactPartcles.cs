using UnityEngine;

public class PlayImpactParticles : MonoBehaviour
{
    // Assign in inspector OR auto-find if left empty
    public ParticleSystem bounceEmbers;
    public ParticleSystem impactSmoke;
    public ParticleSystem sparkleCloud;

    void Awake()
    {
        // Auto-find if the user didn't drag-references
        if (bounceEmbers == null || impactSmoke == null || sparkleCloud == null)
        {
            ParticleSystem[] ps = GetComponentsInChildren<ParticleSystem>(true);

            foreach (var p in ps)
            {
                if (p.name == "BounceEmbers") bounceEmbers = p;
                else if (p.name == "ImpactSmoke") impactSmoke = p;
                else if (p.name == "SpakleCloud" || p.name == "SparkleCloud") sparkleCloud = p;
                // you spelled it SpakleCloud once so I included both ;)
            }
        }
    }

    public float autoDisableTime = 2f;

    void OnEnable()
    {
        PlayParticle(bounceEmbers);
        PlayParticle(impactSmoke);
        PlayParticle(sparkleCloud);

        if (autoDisableTime > 0)
            Invoke(nameof(DisableSelf), autoDisableTime);
    }

    void DisableSelf()
    {
        gameObject.SetActive(false);
    }

    private void PlayParticle(ParticleSystem ps)
    {
        if (ps == null) return;

        // Reset and play
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.Play(true);
    }
}

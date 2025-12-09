using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WindliftController : MonoBehaviour
{
    public static WindliftController Instance;

    public Transform[] stackPositions; // assign 5-slot transforms in inspector
    public float feedDelay = 0.8f; // Time between ball launches

    private int ballsToLaunch = 0;
    private int ballsLaunched = 0;

    public int TotalBallsLaunched => ballsLaunched;
    public bool IsLaunching => ballsLaunched < ballsToLaunch;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartLaunchSequence(int count)
    {
        ballsToLaunch = count;
        ballsLaunched = 0;
        StartCoroutine(LaunchSequence());
    }

    private IEnumerator LaunchSequence()
    {
        for (int i = 0; i < ballsToLaunch; i++)
        {
            // Get ball from pool
            if (GameManager.Instance == null || GameManager.Instance.ForgeheartPool == null)
            {
                Debug.LogError("GameManager or ForgeheartPool not initialized!");
                yield break;
            }

            GameObject ball = GameManager.Instance.ForgeheartPool.Get();
            if (ball == null)
            {
                Debug.LogError("No ball available from pool!");
                continue;
            }

            ball.transform.position = stackPositions[0].position;

            // Animate up the stack (simple lerp)
            for (int s = 0; s < stackPositions.Length - 1; s++)
            {
                float t = 0;
                Vector3 startPos = stackPositions[s].position;
                while (t < 1f)
                {
                    t += Time.deltaTime * 2f;
                    ball.transform.position = Vector3.Lerp(startPos, stackPositions[s + 1].position, t);
                    yield return null;
                }
            }

            // Launch!
            Rigidbody rb = ball.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.up * 8f + Random.insideUnitSphere * 1.5f;
            }

            ballsLaunched++;

            // Register launch with GameManager
            if (GameManager.Instance != null)
                GameManager.Instance.RegisterBallLaunched();

            yield return new WaitForSeconds(feedDelay);
        }

        Debug.Log($"All {ballsToLaunch} balls launched");
    }
}
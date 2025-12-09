using UnityEngine;

public enum PlayerMode
{
    Move,
    Dig,
    Placement
}

public class ModeController : MonoBehaviour
{
    public static ModeController Instance;

    public PlayerMode currentMode = PlayerMode.Move;

    private void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        Instance = this;
    }

    private void Update()
    {
        // Toggle Dig mode with 'R' key (as requested)
        if (Input.GetKeyDown(KeyCode.R))
        {
            currentMode = (currentMode == PlayerMode.Dig) ? PlayerMode.Move : PlayerMode.Dig;
        }

        // Toggle Placement mode with 'F' key (as requested)
        if (Input.GetKeyDown(KeyCode.F))
        {
            currentMode = (currentMode == PlayerMode.Placement) ? PlayerMode.Move : PlayerMode.Placement;
        }
    }
}
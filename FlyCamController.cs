// COMPLETE, SACRED FlyCamController.cs
// Fixed the input vector construction (no more ambiguous ternary mess that could confuse the parser)
// Standard, rock-solid flycam movement: full Vertical for forward/back, full Horizontal for strafe, Q/E for pure up/down
// Everything else unchanged and preserved exactly.

using UnityEngine;

public class FlyCamController : MonoBehaviour
{
    [Header("Movement")]
    [Range(1f, 50f)] public float moveSpeed = 15f;
    [Range(1f, 10f)] public float boostMultiplier = 3f;
    [Range(0.05f, 1f)] public float moveSmoothTime = 0.12f;  // Velocity smoothing (feels cinematic)

    [Header("Mouse Look")]
    [Range(50f, 500f)] public float lookSensitivity = 200f;
    [Range(0f, 90f)] public float maxPitch = 89f;
    public bool requireRightMouseHold = false;

    [Header("Extras")]
    public bool lockCursorOnStart = true;

    private Camera cam;
    private Vector3 currentVelocity;
    private Vector3 smoothDampVelocity;

    private float pitch = 0f;
    private float yaw = 0f;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;

        if (lockCursorOnStart)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.currentState != GameManager.GameState.Build)
            return;

        HandleMouseLook();
        HandleMovementInput();
    }

    private void LateUpdate()
    {
        // Smooth velocity application – feels perfect
        transform.position += currentVelocity * Time.deltaTime;
    }

    private void HandleMouseLook()
    {
        bool allowLook = !requireRightMouseHold || Input.GetMouseButton(1);

        if (allowLook)
        {
            yaw += Input.GetAxis("Mouse X") * lookSensitivity * Time.deltaTime;
            pitch -= Input.GetAxis("Mouse Y") * lookSensitivity * Time.deltaTime;
            pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);

            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        // Escape or Tab toggles cursor
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab))
        {
            Cursor.lockState = Cursor.lockState == CursorLockMode.Locked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = !Cursor.visible;
        }
    }

    private void HandleMovementInput()
    {
        Vector3 input = Vector3.zero;

        input += transform.forward * Input.GetAxisRaw("Vertical");
        input += transform.right * Input.GetAxisRaw("Horizontal");

        if (Input.GetKey(KeyCode.E)) input += Vector3.up;
        if (Input.GetKey(KeyCode.Q)) input += Vector3.down;

        float speed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            speed *= boostMultiplier;

        Vector3 targetVelocity = input.normalized * speed;

        // Smooth damping – buttery and responsive
        currentVelocity = Vector3.SmoothDamp(currentVelocity, targetVelocity, ref smoothDampVelocity, moveSmoothTime);
    }
}
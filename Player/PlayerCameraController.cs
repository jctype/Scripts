using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    [Header("References")]
    public Transform player;          // Your PlayerArmature
    public Camera firstPersonCam;     // 1st camera
    public Camera thirdPersonCam;     // 3rd camera

    [Header("Third-Person Settings")]
    public Vector3 thirdPersonOffset = new Vector3(0, 2, -4);
    public float thirdPersonSmoothSpeed = 0.1f;

    [Header("Mouse Look Settings")]
    public float mouseSensitivity = 100f;
    public float pitchMin = -30f;
    public float pitchMax = 60f;

    private float yaw = 0f;   // Horizontal rotation
    private float pitch = 0f; // Vertical rotation

    void Start()
    {
        // Start with third person enabled
        firstPersonCam.enabled = false;
        thirdPersonCam.enabled = true;

        // Set tags
        firstPersonCam.tag = "Untagged";
        thirdPersonCam.tag = "MainCamera";

        // Make first person camera child of player for easy positioning
        firstPersonCam.transform.SetParent(player);
        firstPersonCam.transform.localPosition = new Vector3(0, 1.6f, 0);
        firstPersonCam.transform.localRotation = Quaternion.identity;

        // Lock cursor for mouse look
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleMouseLook();
        HandleCameraSwitch();
    }

    void LateUpdate()
    {
        UpdateThirdPersonCamera();
        UpdateFirstPersonCamera();
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

        // Rotate player horizontally
        player.rotation = Quaternion.Euler(0, yaw, 0);
    }

    void UpdateThirdPersonCamera()
    {
        if (!thirdPersonCam.enabled) return;

        if (Application.isPlaying)
        {
            Vector3 desiredPos = player.position + player.TransformDirection(thirdPersonOffset);
            thirdPersonCam.transform.position = Vector3.Lerp(thirdPersonCam.transform.position, desiredPos, thirdPersonSmoothSpeed);

            // Look at player's head
            thirdPersonCam.transform.LookAt(player.position + Vector3.up * 1.5f);
        }
    }

    void UpdateFirstPersonCamera()
    {
        if (!firstPersonCam.enabled) return;

        if (Application.isPlaying)
        {
            // Rotate pitch
            firstPersonCam.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
        }
    }

    void HandleCameraSwitch()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            bool isFirst = firstPersonCam.enabled;
            firstPersonCam.enabled = !isFirst;
            thirdPersonCam.enabled = isFirst;

            // Update tags
            if (firstPersonCam.enabled)
            {
                firstPersonCam.tag = "MainCamera";
                thirdPersonCam.tag = "Untagged";
            }
            else
            {
                thirdPersonCam.tag = "MainCamera";
                firstPersonCam.tag = "Untagged";
            }
        }
    }
}

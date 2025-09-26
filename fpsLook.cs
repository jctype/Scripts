using UnityEngine;

public class HeadMouseLook : MonoBehaviour
{
    public float mouseSensitivity = 100f;
    private float xRotation = 0f;
    private Transform playerArmature;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        // Find PlayerArmature directly by name
        playerArmature = GameObject.Find("PlayerArmature").transform;

        if (playerArmature == null)
        {
            Debug.LogError("Could not find PlayerArmature!");
        }
    }

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Look up/down - rotate the Camera
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Look left/right - rotate the entire PlayerArmature
        if (playerArmature != null)
        {
            playerArmature.Rotate(Vector3.up * mouseX);
        }
    }
}
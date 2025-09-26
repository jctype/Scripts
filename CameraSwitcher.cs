using UnityEngine;
using Cinemachine;

public class CameraSwitcher : MonoBehaviour
{
    public CinemachineVirtualCamera thirdPersonCam; // Assign your third-person cam
    public CinemachineVirtualCamera firstPersonCam; // Assign your first-person cam
    private bool isThirdPerson = true;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C)) // Press C to switch
        {
            isThirdPerson = !isThirdPerson;
            thirdPersonCam.Priority = isThirdPerson ? 10 : 0;
            firstPersonCam.Priority = isThirdPerson ? 0 : 10;
        }
    }
}
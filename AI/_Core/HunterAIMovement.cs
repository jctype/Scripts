using UnityEngine;

public class HunterAIMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float rotationSpeed = 120f;

    [Header("References")]
    public Animator animator;

    private CharacterController characterController;
    private Vector3 movementDirection;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        HandleMovement();
        UpdateAnimations();
    }

    public void MoveTo(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0;
        movementDirection = direction;
    }

    public void StopMoving()
    {
        movementDirection = Vector3.zero;
    }

    private void HandleMovement()
    {
        if (movementDirection != Vector3.zero)
        {
            // Rotate toward movement direction
            Quaternion targetRotation = Quaternion.LookRotation(movementDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            // Move forward
            characterController.Move(transform.forward * moveSpeed * Time.deltaTime);
        }
    }

    private void UpdateAnimations()
    {
        if (animator != null)
        {
            float speed = movementDirection != Vector3.zero ? moveSpeed : 0f;
            animator.SetFloat("Speed", speed);
        }
    }
}
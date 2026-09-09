using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Sprint Settings")]
    [SerializeField] private float sprintMultiplier = 2f;

    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Footstep Audio")]
    [SerializeField]
    private AudioSource audioSource;

    [SerializeField]
    private AudioClip walkStepClip;

    [SerializeField]
    private AudioClip sprintStepClip;

    [SerializeField]
    private float walkStepInterval = 0.5f;

    [SerializeField]
    private float sprintStepInterval = 0.3f;

    private float stepTimer = 0f;

    // Status sprint saat ini
    public bool IsSprinting { get; private set; }

    private void Update()
    {
        MovePlayer();
        HandleFootsteps();
    }

    // mainkan suara langkah berkala
    private void HandleFootsteps()
    {
        bool isMoving = IsSprinting || IsCurrentlyMoving();

        if (!isMoving)
        {
            stepTimer = 0f;
            return;
        }

        stepTimer -= Time.deltaTime;

        if (stepTimer > 0f)
            return;

        float interval = IsSprinting ? sprintStepInterval : walkStepInterval;
        stepTimer = interval;

        AudioClip clip = IsSprinting ? sprintStepClip : walkStepClip;

        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    private bool IsCurrentlyMoving()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        return new Vector3(horizontal, 0f, vertical).sqrMagnitude > 0.0001f;
    }

    private void MovePlayer()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 movement = new Vector3(
            horizontal,
            0f,
            vertical
        ).normalized;

        IsSprinting =
            movement != Vector3.zero &&
            Input.GetKey(KeyCode.LeftShift);

        float currentSpeed = moveSpeed;

        if (IsSprinting)
        {
            currentSpeed *= sprintMultiplier;
        }

        // Gerakkan Player
        transform.position +=
            movement *
            currentSpeed *
            Time.deltaTime;

        // Putar Player mengikuti arah gerakan
        if (movement != Vector3.zero)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(movement);

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
        }
    }
}
using UnityEngine;

public class PlayerNoise : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private PlayerController playerController;

    [Header("Whistle Audio")]
    [SerializeField]
    private AudioSource audioSource;

    [SerializeField]
    private AudioClip whistleClip;

    [Header("Noise Settings (Space)")]
    [SerializeField]
    private float noiseRadius = 6f;

    [SerializeField]
    private float noiseDuration = 0.5f;

    [Header("Noise Settings (Movement)")]
    [SerializeField]
    private float walkNoiseRadius = 3f;

    [SerializeField]
    private float sprintNoiseRadius = 9f;

    [SerializeField]
    private float movementThreshold = 0.01f;

    private float noiseTimer = 0f;
    private Vector3 lastPosition;
    private bool isMoving;

    public bool IsMakingNoise =>
        noiseTimer > 0f || isMoving;

    public bool IsSprinting =>
        playerController != null && playerController.IsSprinting;

    public float NoiseRadius
    {
        get
        {
            float radius = 0f;

            if (noiseTimer > 0f)
            {
                radius = Mathf.Max(radius, noiseRadius);
            }

            if (isMoving)
            {
                float movementRadius =
                    IsSprinting ? sprintNoiseRadius : walkNoiseRadius;

                radius = Mathf.Max(radius, movementRadius);
            }

            return radius;
        }
    }

    // Simpan posisi awal
    private void Start()
    {
        lastPosition = transform.position;
    }

    private void Update()
    {
        // Tekan SPACE untuk menghasilkan suara
        if (Input.GetKeyDown(KeyCode.Space))
        {
            noiseTimer = noiseDuration;

            if (audioSource != null && whistleClip != null)
                audioSource.PlayOneShot(whistleClip);
        }

        if (noiseTimer > 0f)
        {
            noiseTimer -= Time.deltaTime;
        }

        DetectMovementNoise();
    }

    // Deteksi suara dari gerakan
    private void DetectMovementNoise()
    {
        float distanceMoved = Vector3.Distance(transform.position, lastPosition);

        isMoving = distanceMoved > movementThreshold;

        lastPosition = transform.position;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        Gizmos.DrawWireSphere(
            transform.position,
            noiseRadius
        );

        Gizmos.color = Color.blue;

        Gizmos.DrawWireSphere(
            transform.position,
            sprintNoiseRadius
        );
    }
}

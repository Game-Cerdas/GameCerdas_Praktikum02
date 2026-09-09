using UnityEngine;

public class NPCSensor : MonoBehaviour
{
    // ======================================
    // TARGET
    // ======================================

    [Header("Target")]
    [SerializeField]
    private Transform player;

    [SerializeField]
    private PlayerNoise playerNoise;

    // ======================================
    // VISION SETTINGS
    // ======================================

    [Header("Vision Settings")]
    [SerializeField]
    private float viewRadius = 8f;

    [Range(0f, 360f)]
    [SerializeField]
    private float viewAngle = 90f;

    [SerializeField]
    private LayerMask obstacleMask;

    // ======================================
    // HEARING SETTINGS
    // ======================================

    [Header("Hearing Settings")]
    [SerializeField]
    private float hearingRadius = 6f;

    // ======================================
    // EYE SETTINGS
    // ======================================

    [Header("Eye Settings")]
    [SerializeField]
    private float eyeHeight = 1.2f;

    // ======================================
    // OUTPUT SENSOR
    // ======================================

    public bool CanSeePlayer
    {
        get;
        private set;
    }

    public bool CanHearPlayer
    {
        get;
        private set;
    }

    public Transform Player =>
        player;

    public Vector3 LastHeardPosition
    {
        get;
        private set;
    }

    // ======================================
    // UPDATE
    // ======================================

    private void Update()
    {
        DetectPlayer();
        DetectSound();
    }

    // ======================================
    // VISUAL SENSOR
    // ======================================

    private void DetectPlayer()
    {
        CanSeePlayer = false;

        if (player == null)
            return;

        Vector3 directionToPlayer =
            player.position -
            transform.position;

        float distanceToPlayer =
            directionToPlayer.magnitude;

        // STEP 1 : DISTANCE
        if (distanceToPlayer > viewRadius)
            return;

        Vector3 normalizedDirection =
            directionToPlayer.normalized;

        // STEP 2 : FIELD OF VIEW
        float angleToPlayer =
            Vector3.Angle(
                transform.forward,
                normalizedDirection
            );

        if (angleToPlayer >
            viewAngle / 2f)
        {
            return;
        }

        // STEP 3 : LINE OF SIGHT
        Vector3 eyePosition =
            transform.position +
            Vector3.up * eyeHeight;

        Vector3 targetPosition =
            player.position +
            Vector3.up * 0.5f;

        Vector3 rayDirection =
            targetPosition -
            eyePosition;

        float rayDistance =
            rayDirection.magnitude;

        if (Physics.Raycast(
            eyePosition,
            rayDirection.normalized,
            rayDistance,
            obstacleMask))
        {
            return;
        }

        CanSeePlayer = true;
    }

    // ======================================
    // HEARING SENSOR - CHALLENGE 4
    // ======================================

    private void DetectSound()
    {
        CanHearPlayer = false;

        if (player == null ||
            playerNoise == null)
        {
            return;
        }

        if (!playerNoise.IsMakingNoise)
        {
            return;
        }

        float distanceToPlayer =
            Vector3.Distance(
                transform.position,
                player.position
            );

        if (distanceToPlayer <= hearingRadius)
        {
            CanHearPlayer = true;

            LastHeardPosition =
                player.position;
        }
    }

    // ======================================
    // GIZMOS
    // ======================================

    private void OnDrawGizmosSelected()
    {
        // VIEW RADIUS
        Gizmos.color =
            Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            viewRadius
        );

        // FIELD OF VIEW
        Vector3 leftBoundary =
            DirectionFromAngle(
                -viewAngle / 2f
            );

        Vector3 rightBoundary =
            DirectionFromAngle(
                viewAngle / 2f
            );

        Gizmos.DrawLine(
            transform.position,
            transform.position +
            leftBoundary * viewRadius
        );

        Gizmos.DrawLine(
            transform.position,
            transform.position +
            rightBoundary * viewRadius
        );

        // HEARING RADIUS
        Gizmos.color =
            Color.cyan;

        Gizmos.DrawWireSphere(
            transform.position,
            hearingRadius
        );

        // PLAYER VISIBLE
        if (player != null &&
            CanSeePlayer)
        {
            Gizmos.color =
                Color.red;

            Gizmos.DrawLine(
                transform.position +
                Vector3.up * eyeHeight,
                player.position +
                Vector3.up * 0.5f
            );
        }

        // PLAYER HEARD
        if (CanHearPlayer)
        {
            Gizmos.color =
                Color.cyan;

            Gizmos.DrawLine(
                transform.position,
                LastHeardPosition
            );
        }
    }

    private Vector3 DirectionFromAngle(
        float angle
    )
    {
        float finalAngle =
            transform.eulerAngles.y +
            angle;

        return new Vector3(
            Mathf.Sin(
                finalAngle *
                Mathf.Deg2Rad
            ),
            0f,
            Mathf.Cos(
                finalAngle *
                Mathf.Deg2Rad
            )
        );
    }
}
using UnityEngine;
using UnityEngine.AI;
using TMPro;

public class NPCBrain : MonoBehaviour
{
    public enum NPCState
    {
        Patrol,
        Suspicious,
        Chase,
        Search
    }

    // ======================================
    // REFERENCES
    // ======================================

    [Header("References")]
    [SerializeField]
    private NPCSensor sensor;

    [SerializeField]
    private NavMeshAgent agent;

    [SerializeField]
    private TMP_Text alertIndicator;

    [Header("Voice Audio")]
    [SerializeField]
    private AudioSource audioSource;

    [SerializeField]
    private AudioClip suspiciousVoiceClip;

    [SerializeField]
    private AudioClip chaseVoiceClip;

    // ======================================
    // PATROL SETTINGS
    // ======================================

    [Header("Patrol Settings")]
    [SerializeField]
    private Transform[] patrolPoints;

    [SerializeField]
    private float waypointTolerance = 0.7f;

    [SerializeField]
    private float patrolSpeed = 2f;

    // Challenge 1
    [SerializeField]
    private float waypointWaitDuration = 2f;

    private float waypointWaitTimer = 0f;
    private bool isWaitingAtWaypoint = false;

    [Header("Suspicion Settings")]
    [SerializeField]
    private float suspicionIncreaseRate = 20f;

    [SerializeField]
    private float suspicionIncreaseRateSprint = 45f;

    [SerializeField]
    private float suspicionDecreaseRate = 15f;

    [SerializeField]
    private float suspicionThreshold = 100f;

    [SerializeField]
    private float suspicionTurnSpeed = 5f;

    [SerializeField]
    private float suspicionMaxScale = 2f;

    private float suspicionMeter;

    private Vector3 alertIndicatorBaseScale = Vector3.one;

    // ======================================
    // CHASE SETTINGS
    // ======================================

    [Header("Chase Settings")]
    [SerializeField]
    private float chaseSpeed = 4f;

    // ======================================
    // SEARCH SETTINGS
    // ======================================

    [Header("Search Settings")]
    [SerializeField]
    private float searchDuration = 4f;

    [SerializeField]
    private float searchTolerance = 0.8f;

    [SerializeField]
    private float searchRotationSpeed = 120f;

    [SerializeField]
    private float searchTurnAngle = 60f;

    private enum SearchPhase
    {
        Moving,
        LookLeft,
        LookRight
    }

    private SearchPhase searchPhase = SearchPhase.Moving;

    private Quaternion searchCenterRotation;
    private Quaternion searchLeftRotation;
    private Quaternion searchRightRotation;

    [Header("Alert Broadcast Settings")]
    [SerializeField]
    private float alertBroadcastRadius = 15f;

    // ======================================
    // DEBUG
    // ======================================

    [Header("Debug")]
    [SerializeField]
    private NPCState currentState;

    private NPCState previousState;

    // ======================================
    // PATROL DATA
    // ======================================

    private int patrolIndex = 0;

    // ======================================
    // MEMORY
    // ======================================

    private Vector3 lastKnownPosition;

    private bool hasLastKnownPosition;

    private float searchTimer;

    // ======================================
    // UNITY METHODS
    // ======================================

    private void Start()
    {
        currentState = NPCState.Patrol;
        previousState = currentState;

        if (alertIndicator != null)
        {
            alertIndicatorBaseScale =
                alertIndicator.transform.localScale;
        }

        GoToCurrentPatrolPoint();
        UpdateAlertIndicator();
    }

    private void Update()
    {
        UpdateMemory();

        MakeDecision();

        ExecuteCurrentState();
    }

    // ======================================
    // MEMORY
    // ======================================

    private void UpdateMemory()
    {
        // Memory dari penglihatan
        if (sensor.CanSeePlayer)
        {
            lastKnownPosition =
                sensor.Player.position;

            hasLastKnownPosition = true;
        }

        // Challenge 4:
        // Memory dari suara
        else if (sensor.CanHearPlayer)
        {
            lastKnownPosition =
                sensor.LastHeardPosition;

            hasLastKnownPosition = true;
        }
    }

    // ======================================
    // DECISION
    // ======================================

    private void MakeDecision()
    {
        // ==================================
        // PRIORITAS 1
        // PLAYER TERLIHAT
        // ==================================

        if (sensor.CanSeePlayer)
        {
            suspicionMeter = 0f;

            ChangeState(
                NPCState.Chase
            );

            return;
        }

        // ==================================
        // PRIORITAS 2
        // PLAYER BARU HILANG
        // ==================================

        if (currentState == NPCState.Chase &&
            hasLastKnownPosition)
        {
            suspicionMeter = 0f;

            searchTimer = searchDuration;

            searchPhase = SearchPhase.Moving;

            ChangeState(
                NPCState.Search
            );

            return;
        }

        // ==================================
        // PRIORITAS 3
        // ==================================

        if (sensor.CanHearPlayer)
        {
            float increaseRate =
                sensor.IsPlayerSprinting
                    ? suspicionIncreaseRateSprint
                    : suspicionIncreaseRate;

            suspicionMeter +=
                increaseRate * Time.deltaTime;

            ChangeState(
                NPCState.Suspicious
            );

            if (suspicionMeter >= suspicionThreshold)
            {
                suspicionMeter = 0f;

                searchTimer = searchDuration;

                searchPhase = SearchPhase.Moving;

                ChangeState(
                    NPCState.Search
                );
            }

            return;
        }

        // ==================================
        // PRIORITAS 4
        // ==================================

        if (currentState == NPCState.Suspicious)
        {
            suspicionMeter -=
                suspicionDecreaseRate * Time.deltaTime;

            if (suspicionMeter <= 0f)
            {
                suspicionMeter = 0f;

                ChangeState(
                    NPCState.Patrol
                );
            }

            return;
        }

        // ==================================
        // PRIORITAS 5
        // SEARCH SELESAI
        // ==================================

        if (currentState == NPCState.Search &&
            searchTimer <= 0f)
        {
            hasLastKnownPosition = false;

            searchPhase = SearchPhase.Moving;

            ChangeState(
                NPCState.Patrol
            );
        }
    }

    // ======================================
    // ACTION
    // ======================================

    private void ExecuteCurrentState()
    {
        switch (currentState)
        {
            case NPCState.Patrol:

                Patrol();
                break;

            case NPCState.Suspicious:

                Suspicious();
                break;

            case NPCState.Chase:

                Chase();
                break;

            case NPCState.Search:

                Search();
                break;
        }
    }

    // ======================================
    // PATROL
    // ======================================

    private void Patrol()
    {
        agent.speed = patrolSpeed;

        if (patrolPoints == null ||
            patrolPoints.Length == 0)
        {
            return;
        }

        // ==================================
        // CHALLENGE 1
        // NPC berhenti sementara di waypoint
        // ==================================

        if (isWaitingAtWaypoint)
        {
            waypointWaitTimer -= Time.deltaTime;

            if (waypointWaitTimer <= 0f)
            {
                isWaitingAtWaypoint = false;

                patrolIndex++;

                if (patrolIndex >= patrolPoints.Length)
                {
                    patrolIndex = 0;
                }

                GoToCurrentPatrolPoint();
            }

            return;
        }

        // NPC sudah mencapai waypoint
        if (!agent.pathPending &&
            agent.remainingDistance <= waypointTolerance)
        {
            isWaitingAtWaypoint = true;

            waypointWaitTimer =
                waypointWaitDuration;

            // Hentikan NPC selama menunggu
            agent.ResetPath();
        }
    }

    private void GoToCurrentPatrolPoint()
    {
        if (patrolPoints == null ||
            patrolPoints.Length == 0)
        {
            return;
        }

        agent.SetDestination(
            patrolPoints[
                patrolIndex
            ].position
        );
    }

    // ======================================
    // SUSPICIOUS
    // ======================================

    // Menoleh ke arah suara
    private void Suspicious()
    {
        Vector3 direction =
            sensor.LastHeardPosition -
            transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(direction);

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    suspicionTurnSpeed * Time.deltaTime
                );
        }

        UpdateAlertIndicatorScale();
    }

    // Perbesar indikator sesuai progress
    private void UpdateAlertIndicatorScale()
    {
        if (alertIndicator == null)
        {
            return;
        }

        float progress =
            Mathf.Clamp01(
                suspicionMeter / suspicionThreshold
            );

        alertIndicator.transform.localScale =
            alertIndicatorBaseScale *
            Mathf.Lerp(1f, suspicionMaxScale, progress);
    }

    // ======================================
    // CHASE
    // ======================================

    private void Chase()
    {
        agent.speed = chaseSpeed;

        if (sensor.Player == null)
        {
            return;
        }

        agent.SetDestination(
            sensor.Player.position
        );
    }

    // ======================================
    // SEARCH
    // ======================================

    private void Search()
    {
        agent.speed = patrolSpeed;

        if (searchPhase == SearchPhase.Moving)
        {
            agent.SetDestination(lastKnownPosition);

            if (!agent.pathPending &&
                agent.remainingDistance <= searchTolerance)
            {
                agent.ResetPath();

                // Simpan arah hadap saat sampai
                searchCenterRotation =
                    transform.rotation;

                // Hitung arah kiri
                searchLeftRotation =
                    searchCenterRotation *
                    Quaternion.Euler(0f, -searchTurnAngle, 0f);

                // Hitung arah kanan
                searchRightRotation =
                    searchCenterRotation *
                    Quaternion.Euler(0f, searchTurnAngle, 0f);

                searchPhase = SearchPhase.LookLeft;
            }

            return;
        }

        if (searchPhase == SearchPhase.LookLeft)
        {
            transform.rotation =
                Quaternion.RotateTowards(
                    transform.rotation,
                    searchLeftRotation,
                    searchRotationSpeed * Time.deltaTime
                );

            if (Quaternion.Angle(transform.rotation, searchLeftRotation) < 1f)
                searchPhase = SearchPhase.LookRight;

            return;
        }

        if (searchPhase == SearchPhase.LookRight)
        {
            transform.rotation =
                Quaternion.RotateTowards(
                    transform.rotation,
                    searchRightRotation,
                    searchRotationSpeed * Time.deltaTime
                );

            if (Quaternion.Angle(transform.rotation, searchRightRotation) < 1f)
                searchTimer -= Time.deltaTime;

            return;
        }
    }

    // ======================================
    // STATE TRANSITION
    // ======================================

    private void ChangeState(
        NPCState newState
    )
    {
        if (currentState == newState)
        {
            return;
        }

        NPCState previous = currentState;

        previousState = previous;

        currentState =
            newState;

        UpdateAlertIndicator();
        PlayStateVoice(currentState);

        // Jika meninggalkan PATROL,
        // batalkan timer menunggu waypoint
        if (currentState != NPCState.Patrol)
        {
            isWaitingAtWaypoint = false;

            waypointWaitTimer = 0f;
        }

        // Masuk SUSPICIOUS, hentikan gerak
        if (currentState == NPCState.Suspicious)
        {
            agent.ResetPath();
        }

        // Keluar SUSPICIOUS, kembalikan skala
        if (previousState == NPCState.Suspicious &&
            alertIndicator != null)
        {
            alertIndicator.transform.localScale =
                alertIndicatorBaseScale;
        }

        // Masuk SEARCH, mulai dari fase jalan
        if (currentState == NPCState.Search)
        {
            searchPhase = SearchPhase.Moving;
        }

        // Masuk CHASE pertama kali, sebar alert
        if (currentState == NPCState.Chase &&
            previous != NPCState.Chase)
        {
            BroadcastAlert(sensor.Player.position);
        }

        Debug.Log(
            gameObject.name +
            ": " +
            previousState +
            " -> " +
            currentState
        );

        // Jika kembali PATROL,
        // lanjutkan ke waypoint aktif
        if (currentState == NPCState.Patrol)
        {
            GoToCurrentPatrolPoint();
        }
    }

    // sebar info lokasi ke NPC lain di sekitar
    private void BroadcastAlert(Vector3 alertPosition)
    {
        NPCBrain[] allGuards = FindObjectsOfType<NPCBrain>();

        foreach (NPCBrain guard in allGuards)
        {
            if (guard == this)
                continue;

            float distance = Vector3.Distance(
                transform.position,
                guard.transform.position
            );

            if (distance <= alertBroadcastRadius)
                guard.ReceiveAlert(alertPosition);
        }
    }

    // terima info dari NPC lain yang lihat player duluan
    public void ReceiveAlert(Vector3 alertPosition)
    {
        if (currentState == NPCState.Chase)
            return;

        lastKnownPosition = alertPosition;
        hasLastKnownPosition = true;
        searchTimer = searchDuration;

        ChangeState(NPCState.Search);
    }

    // mainkan suara sekali saat masuk state tertentu
    private void PlayStateVoice(NPCState state)
    {
        if (audioSource == null)
            return;

        AudioClip clip = null;

        if (state == NPCState.Suspicious)
            clip = suspiciousVoiceClip;
        else if (state == NPCState.Chase)
            clip = chaseVoiceClip;

        if (clip != null)
            audioSource.PlayOneShot(clip);
    }

    private void UpdateAlertIndicator()
    {
        if (alertIndicator == null)
            return;

        switch (currentState)
        {
            case NPCState.Patrol:
                alertIndicator.gameObject.SetActive(false);
                break;

            case NPCState.Suspicious:
                alertIndicator.gameObject.SetActive(true);
                alertIndicator.text = "?";
                break;

            case NPCState.Chase:
                alertIndicator.gameObject.SetActive(true);
                alertIndicator.text = "!";
                break;

            case NPCState.Search:
                alertIndicator.gameObject.SetActive(true);
                alertIndicator.text = "?";
                break;
        }
    }

    // ======================================
    // DEBUG GIZMOS
    // ======================================

    private void OnDrawGizmosSelected()
    {
        switch (currentState)
        {
            case NPCState.Patrol:

                Gizmos.color =
                    Color.green;
                break;

            case NPCState.Suspicious:

                Gizmos.color =
                    Color.yellow;
                break;

            case NPCState.Chase:

                Gizmos.color =
                    Color.red;
                break;

            case NPCState.Search:

                Gizmos.color =
                    Color.blue;
                break;
        }

        Gizmos.DrawWireSphere(
            transform.position,
            0.8f
        );

        // Last Known Position
        if (hasLastKnownPosition)
        {
            Gizmos.color =
                Color.magenta;

            Gizmos.DrawSphere(
                lastKnownPosition,
                0.3f
            );

            Gizmos.DrawLine(
                transform.position,
                lastKnownPosition
            );
        }
    }
}
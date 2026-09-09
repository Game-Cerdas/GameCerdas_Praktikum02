using UnityEngine;
using UnityEngine.AI;
using TMPro;

public class NPCBrain : MonoBehaviour
{
    public enum NPCState
    {
        Patrol,
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

    // Challenge 2
    [SerializeField]
    private float searchTurnAngle = 60f;

    [SerializeField]
    private float searchRotationSpeed = 120f;

    private enum SearchPhase
    {
        Moving,
        LookLeft,
        LookRight,
        Waiting
    }

    private SearchPhase searchPhase = SearchPhase.Moving;

    private Quaternion searchCenterRotation;
    private Quaternion searchLeftRotation;
    private Quaternion searchRightRotation;

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
            ChangeState(
                NPCState.Chase
            );

            return;
        }

        // ==================================
        // PRIORITAS 2
        // PLAYER TERDENGAR
        // ==================================

        if (sensor.CanHearPlayer)
        {
            searchTimer = searchDuration;

            searchPhase =
                SearchPhase.Moving;

            ChangeState(
                NPCState.Search
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
            searchTimer = searchDuration;

            searchPhase = SearchPhase.Moving;

            ChangeState(
                NPCState.Search
            );

            return;
        }

        // ==================================
        // PRIORITAS 3
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

        // ==================================
        // FASE 1
        // Menuju posisi terakhir Player
        // ==================================

        if (searchPhase == SearchPhase.Moving)
        {
            agent.SetDestination(
                lastKnownPosition
            );

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
                    Quaternion.Euler(
                        0f,
                        -searchTurnAngle,
                        0f
                    );

                // Hitung arah kanan
                searchRightRotation =
                    searchCenterRotation *
                    Quaternion.Euler(
                        0f,
                        searchTurnAngle,
                        0f
                    );

                searchPhase =
                    SearchPhase.LookLeft;
            }

            return;
        }

        // ==================================
        // FASE 2
        // Lihat ke kiri
        // ==================================

        if (searchPhase == SearchPhase.LookLeft)
        {
            transform.rotation =
                Quaternion.RotateTowards(
                    transform.rotation,
                    searchLeftRotation,
                    searchRotationSpeed *
                    Time.deltaTime
                );

            if (Quaternion.Angle(
                    transform.rotation,
                    searchLeftRotation) < 1f)
            {
                searchPhase =
                    SearchPhase.LookRight;
            }

            return;
        }

        // ==================================
        // FASE 3
        // Lihat ke kanan
        // ==================================

        if (searchPhase == SearchPhase.LookRight)
        {
            transform.rotation =
                Quaternion.RotateTowards(
                    transform.rotation,
                    searchRightRotation,
                    searchRotationSpeed *
                    Time.deltaTime
                );

            if (Quaternion.Angle(
                    transform.rotation,
                    searchRightRotation) < 1f)
            {
                searchPhase =
                    SearchPhase.Waiting;
            }

            return;
        }

        // ==================================
        // FASE 4
        // Menunggu sebelum kembali PATROL
        // ==================================

        if (searchPhase == SearchPhase.Waiting)
        {
            searchTimer -= Time.deltaTime;
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

        previousState =
            currentState;

        currentState =
            newState;

        UpdateAlertIndicator();

        // Jika meninggalkan PATROL,
        // batalkan timer menunggu waypoint
        if (currentState != NPCState.Patrol)
        {
            isWaitingAtWaypoint = false;

            waypointWaitTimer = 0f;
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

    private void UpdateAlertIndicator()
    {
        if (alertIndicator == null)
            return;

        switch (currentState)
        {
            case NPCState.Patrol:
                alertIndicator.gameObject.SetActive(false);
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
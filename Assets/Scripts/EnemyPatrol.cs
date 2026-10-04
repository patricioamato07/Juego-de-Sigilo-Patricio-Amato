using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Patrullaje y movimiento de enemigos. Usa la lógica de waypoints de PlatformMovement
/// (PingPong / Loop / waitTime) pero mueve con NavMeshAgent para rodear obstáculos.
/// Reacciona a EnemyDetection: te mira si te ve, investiga tu última posición si te pierde.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent))]
[DefaultExecutionOrder(10)] // Después de EnemyDetection (orden 0): lee datos del frame actual, no del anterior.
public class EnemyPatrol : MonoBehaviour
{
    public enum LoopMode { PingPong, Loop }
    private enum State { Patrol, WaitAtWaypoint, Watching, Investigate, LookAround }

    [Header("Ruta (waypoints fuera de la jerarquía del enemigo)")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private LoopMode loopMode = LoopMode.PingPong;
    [SerializeField] private float waitTime = 1.5f;

    [Header("Velocidades")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float investigateSpeed = 4f;
    [SerializeField] private float turnSpeed = 240f;

    [Header("Investigación")]
    [Tooltip("Segundos que mira a su alrededor al llegar a tu última posición conocida.")]
    [SerializeField] private float lookAroundTime = 3f;
    [SerializeField] private float lookAroundAngle = 70f;
    [SerializeField] private float lookAroundSpeed = 2f;

    [Header("Navegación")]
    [Tooltip("Margen extra sobre el stopping distance para considerar que llegó.")]
    [SerializeField] private float arriveThreshold = 0.3f;
    [Tooltip("Radio para ajustar destinos al NavMesh más cercano.")]
    [SerializeField] private float sampleDistance = 2f;

    [Header("Referencias")]
    [Tooltip("Si está vacío, lo busca en este mismo objeto.")]
    [SerializeField] private EnemyDetection detection;

    private NavMeshAgent agent;
    private State state;
    private int currentIndex;
    private int direction = 1;
    private float timer;
    private float lookStartYaw;
    private float lookElapsed;
    private Vector3 lastKnownPosition;
    private bool hasLastKnown;

    private void Reset()
    {
        detection = GetComponent<EnemyDetection>();
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (detection == null) detection = GetComponent<EnemyDetection>();
    }

    private void Start()
    {
        if (!agent.isOnNavMesh)
        {
            // Solo se desactiva la patrulla; EnemyDetection sigue funcionando.
            Debug.LogWarning($"{name}: el enemigo no está sobre un NavMesh. ¿Hiciste el Bake?", this);
            enabled = false;
            return;
        }

        EnterState(State.Patrol);
    }

    private void Update()
    {
        bool seesTarget = detection != null && detection.CanSeeTarget && detection.CurrentTarget != null;
        bool caught = detection != null && detection.State == DetectionState.Danger;

        if (seesTarget)
        {
            lastKnownPosition = detection.CurrentTarget.position;
            hasLastKnown = true;
        }

        // Prioridad: si te ve (o ya te atrapó), te mira.
        if (seesTarget || caught)
        {
            if (state != State.Watching) EnterState(State.Watching);
        }
        else if (state == State.Watching)
        {
            // Acaba de perderte de vista.
            EnterState(hasLastKnown ? State.Investigate : State.Patrol);
        }

        switch (state)
        {
            case State.Patrol:
                if (Arrived())
                {
                    AdvanceWaypoint();
                    if (waitTime > 0f && waypoints != null && waypoints.Length > 0)
                    {
                        timer = waitTime;
                        EnterState(State.WaitAtWaypoint);
                    }
                    else
                    {
                        GoToCurrentWaypoint();
                    }
                }
                break;

            case State.WaitAtWaypoint:
                timer -= Time.deltaTime;
                if (timer <= 0f) EnterState(State.Patrol);
                break;

            case State.Watching:
                FacePoint(lastKnownPosition);
                break;

            case State.Investigate:
                if (Arrived())
                {
                    timer = lookAroundTime;
                    EnterState(State.LookAround);
                }
                break;

            case State.LookAround:
                lookElapsed += Time.deltaTime;
                float yaw = lookStartYaw + Mathf.Sin(lookElapsed * lookAroundSpeed) * lookAroundAngle;
                transform.rotation = Quaternion.Euler(0f, yaw, 0f);

                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    hasLastKnown = false;
                    EnterState(State.Patrol);
                }
                break;
        }
    }

    private void EnterState(State next)
    {
        state = next;

        switch (next)
        {
            case State.Patrol:
                agent.speed = patrolSpeed;
                GoToCurrentWaypoint();
                break;

            case State.WaitAtWaypoint:
            case State.Watching:
                Stop();
                break;

            case State.Investigate:
                agent.speed = investigateSpeed;
                MoveTo(lastKnownPosition);
                break;

            case State.LookAround:
                Stop();
                lookStartYaw = transform.eulerAngles.y;
                lookElapsed = 0f;
                break;
        }
    }

    // --- Waypoints (misma lógica que PlatformMovement) ---

    private void GoToCurrentWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0) { Stop(); return; }

        Transform target = waypoints[currentIndex];
        if (target != null) MoveTo(target.position);
    }

    private void AdvanceWaypoint()
    {
        if (waypoints == null || waypoints.Length <= 1) return;

        if (loopMode == LoopMode.Loop)
        {
            currentIndex = (currentIndex + 1) % waypoints.Length;
            return;
        }

        if (currentIndex + direction >= waypoints.Length || currentIndex + direction < 0)
        {
            direction *= -1;
        }
        currentIndex += direction;
    }

    // --- Utilidades de navegación ---

    private void MoveTo(Vector3 point)
    {
        if (NavMesh.SamplePosition(point, out NavMeshHit hit, sampleDistance, NavMesh.AllAreas))
            agent.SetDestination(hit.position);
        else
            agent.SetDestination(point);
    }

    private void Stop()
    {
        agent.ResetPath();
        agent.velocity = Vector3.zero;
    }

    private bool Arrived()
    {
        if (agent.pathPending) return false;
        if (agent.pathStatus == NavMeshPathStatus.PathInvalid) return true; // destino inalcanzable: se salta
        return agent.remainingDistance <= agent.stoppingDistance + arriveThreshold;
    }

    private void FacePoint(Vector3 point)
    {
        Vector3 dir = point - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, Quaternion.LookRotation(dir), turnSpeed * Time.deltaTime);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (waypoints != null && waypoints.Length >= 2)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] == null) continue;
                Gizmos.DrawSphere(waypoints[i].position, 0.2f);
                if (i < waypoints.Length - 1 && waypoints[i + 1] != null)
                    Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
            }
            // En modo Loop se cierra el circuito.
            if (loopMode == LoopMode.Loop && waypoints[0] != null && waypoints[waypoints.Length - 1] != null)
                Gizmos.DrawLine(waypoints[waypoints.Length - 1].position, waypoints[0].position);
        }

        if (Application.isPlaying && hasLastKnown)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(lastKnownPosition, 0.4f);
        }
    }
#endif
}

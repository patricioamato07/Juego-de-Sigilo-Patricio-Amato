using UnityEngine;

/// <summary>
/// Plataforma que avanza entre waypoints SOLO mientras el jugador
/// está parado encima. Al bajarse, se frena en el lugar (no vuelve
/// atrás ni sigue sola).
///
/// Necesita DOS colliders en el mismo GameObject (o hijos):
///   1. Un collider sólido (no trigger) para que el jugador pise.
///   2. Un BoxCollider en Is Trigger = true, levemente arriba de la
///      superficie, para detectar cuándo el jugador está parado ahí.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PressureMovingPlatform : MonoBehaviour
{
    public enum LoopMode { PingPong, Loop }

    [Header("Recorrido")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float speed = 2f;
    [SerializeField] private LoopMode loopMode = LoopMode.PingPong;
    [Tooltip("Segundos de pausa al llegar a cada waypoint antes de seguir")]
    [SerializeField] private float waitTime = 0f;

    [Header("Pasajero")]
    [SerializeField] private string playerTag = "Player";

    private Rigidbody _rb;
    private int _currentIndex;
    private int _direction = 1;
    private float _waitTimer;
    private bool _playerOnPlatform;
    private Rigidbody _playerRb;
    private Vector3 _appliedVelocity; // cuánta velocidad le metimos al jugador el frame anterior

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.isKinematic = true;
    }

    private void FixedUpdate()
    {
        if (!_playerOnPlatform) return; // quieta si no hay nadie encima
        if (waypoints == null || waypoints.Length < 2) return;

        if (_waitTimer > 0f)
        {
            _waitTimer -= Time.fixedDeltaTime;
            return;
        }

        Vector3 previousPos = _rb.position;

        Transform target = waypoints[_currentIndex];
        Vector3 newPos = Vector3.MoveTowards(previousPos, target.position, speed * Time.fixedDeltaTime);
        _rb.MovePosition(newPos);

        // Lo que importa para arrastrar al jugador es CUÁNTO se movió
        // la plataforma este frame, no hacia dónde se dirige. Por eso
        // usamos el delta, no la posición del waypoint.
        Vector3 platformDelta = newPos - previousPos;
        if (_playerRb != null)
        {
            Vector3 requiredVelocity = platformDelta / Time.fixedDeltaTime;

            // Solo aplicamos la DIFERENCIA contra lo que ya le habíamos
            // metido el frame anterior. Esto efectivamente "setea" el
            // aporte de la plataforma a la velocidad del jugador sin
            // acumularlo, y usa AddForce (no MovePosition), así no
            // compite con el AddForce propio de ThirdPersonController.
            Vector3 velocityChange = requiredVelocity - _appliedVelocity;
            _playerRb.AddForce(velocityChange, ForceMode.VelocityChange);
            _appliedVelocity = requiredVelocity;
        }

        if (Vector3.Distance(newPos, target.position) < 0.01f)
        {
            _waitTimer = waitTime;
            AdvanceWaypoint();
        }
    }

    private void AdvanceWaypoint()
    {
        if (loopMode == LoopMode.Loop)
        {
            _currentIndex = (_currentIndex + 1) % waypoints.Length;
            return;
        }

        if (_currentIndex + _direction >= waypoints.Length || _currentIndex + _direction < 0)
        {
            _direction *= -1;
        }
        _currentIndex += _direction;
    }

    // --- Detección + arrastre del jugador ---

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        var playerRb = other.attachedRigidbody;
        if (playerRb == null)
        {
            Debug.LogWarning("PressureMovingPlatform: el jugador no tiene Rigidbody, no se le puede aplicar fuerza.");
            return;
        }

        _playerOnPlatform = true;
        _playerRb = playerRb;
        _appliedVelocity = Vector3.zero;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        // Sacamos el aporte de velocidad que le habíamos metido, para
        // que no se quede "pegado" moviéndose solo en esa dirección.
        if (_playerRb != null)
        {
            _playerRb.AddForce(-_appliedVelocity, ForceMode.VelocityChange);
        }

        _playerOnPlatform = false;
        _playerRb = null;
        _appliedVelocity = Vector3.zero;
    }

    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length < 2) return;
        Gizmos.color = Color.yellow;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;
            Gizmos.DrawSphere(waypoints[i].position, 0.2f);
            if (i < waypoints.Length - 1 && waypoints[i + 1] != null)
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
        }
    }
}

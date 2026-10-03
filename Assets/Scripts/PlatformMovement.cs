using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlatformMovement : MonoBehaviour
{
    public enum LoopMode { PingPong, Loop }

    [Header("Path")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float speed = 2f;
    [SerializeField] private LoopMode loopMode = LoopMode.PingPong;
    [SerializeField] private float waitTime = 0f;

    [Header("Object")]

    private Rigidbody _rb;
    private int _currentIndex;
    private int _direction = 1;
    private float _waitTimer;
    private Vector3 newPos;

    // Cuánto se movió la plataforma en el último FixedUpdate.
    // Esto es lo único que necesita el jugador para "venir con ella".
    private Vector3 _frameDelta;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.isKinematic = true;
    }

    private void FixedUpdate()
    {
        _frameDelta = Vector3.zero; // por defecto, no se movió nada este frame

        if (waypoints == null || waypoints.Length < 2) return;

        if (_waitTimer > 0f)
        {
            _waitTimer -= Time.fixedDeltaTime;
            return;
        }

        Vector3 previousPos = _rb.position;
        Transform target = waypoints[_currentIndex];
        newPos = Vector3.MoveTowards(previousPos, target.position, speed * Time.fixedDeltaTime);
        _rb.MovePosition(newPos);

        _frameDelta = newPos - previousPos;

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

    private void OnCollisionStay(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;
        if (_frameDelta.sqrMagnitude <= 0f) return; // la plataforma no se movió este frame

        Rigidbody playerRb = collision.rigidbody; // más genérico que buscar ThirdPersonController
        if (playerRb == null) return;

        // Fuerza equivalente al desplazamiento de la plataforma este
        // frame. AddForce se acumula con lo que ya haya aplicado el
        // script de movimiento del jugador en el mismo FixedUpdate,
        // así que no lo pisa.
        Vector3 requiredVelocity = _frameDelta / Time.fixedDeltaTime;
        playerRb.AddForce(requiredVelocity, ForceMode.VelocityChange);
    }

    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length < 2) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;
            Gizmos.DrawSphere(waypoints[i].position, 0.2f);
            if (i < waypoints.Length - 1 && waypoints[i + 1] != null)
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
        }
    }
}
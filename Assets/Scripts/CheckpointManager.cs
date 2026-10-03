using UnityEngine;

/// <summary>
/// Guarda la última posición de respawn activa.
/// Ni la placa ni la zona de caída hablan directo con el jugador:
/// ambas pasan por acá. Esto evita duplicar lógica cuando tengas
/// varias placas en el nivel.
/// </summary>
public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    [Tooltip("Punto de spawn inicial, por si el jugador cae antes de pisar una placa")]
    [SerializeField] private Transform defaultSpawnPoint;

    private Vector3 _currentSpawnPosition;
    private Quaternion _currentSpawnRotation;

    private void Awake()
    {
        // Singleton simple. Si ya existe uno, este se destruye.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (defaultSpawnPoint != null)
        {
            _currentSpawnPosition = defaultSpawnPoint.position;
            _currentSpawnRotation = defaultSpawnPoint.rotation;
        }
    }

    /// <summary>Llamado por RespawnPlate cuando el jugador la pisa.</summary>
    public void SetCheckpoint(Vector3 position, Quaternion rotation)
    {
        _currentSpawnPosition = position;
        _currentSpawnRotation = rotation;
    }

    /// <summary>Llamado por FallResetZone (o quien necesite reposicionar al jugador).</summary>
    public void RespawnPlayer(GameObject player)
    {
        // Si usás CharacterController, hay que apagarlo antes de mover
        // el transform, si no Unity ignora el cambio de posición.
        var cc = player.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            player.transform.SetPositionAndRotation(_currentSpawnPosition, _currentSpawnRotation);
            cc.enabled = true;
        }
        else
        {
            player.transform.SetPositionAndRotation(_currentSpawnPosition, _currentSpawnRotation);
        }

        // Si el jugador tiene Rigidbody (con o sin CharacterController),
        // hay que resetear la velocidad para que no arrastre el impulso
        // de la caída.
        var rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}

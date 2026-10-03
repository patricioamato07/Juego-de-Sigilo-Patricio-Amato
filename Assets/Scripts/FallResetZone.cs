using UnityEngine;

public class FallResetZone : MonoBehaviour
{
    [Tooltip("Coordenada Y del mundo. Si el jugador cae por debajo, se lo respawnea.")]
    [SerializeField] private float yThreshold = -20f;

    [SerializeField] private string playerTag = "Player";

    private Transform _player;

    private bool _hasRespawned;

    private void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
        {
            _player = playerObj.transform;
        }
        else
        {
            Debug.LogWarning($"FallResetZone: Tagged object '{playerTag}' not found.");
        }
    }

    private void Update()
    {
        if (_player == null) return;

        if (_player.position.y < yThreshold)
        {
            if (_hasRespawned) return;
            _hasRespawned = true;

            if (CheckpointManager.Instance == null)
            {
                Debug.LogWarning("Missing CheckpointManager.");
                return;
            }

            CheckpointManager.Instance.RespawnPlayer(_player.gameObject);
        }
        else
        {
            _hasRespawned = false;
        }
    }
}

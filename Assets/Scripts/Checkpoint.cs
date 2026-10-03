using UnityEngine;

/// <summary>
/// Colocar en la "placa de respawn". El Collider debe tener
/// Is Trigger = true. No guarda nada localmente: le avisa al
/// CheckpointManager, que es la única fuente de verdad.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Checkpoint : MonoBehaviour
{
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private string playerTag = "Player";

    // Opcional: para no reactivar la misma placa infinitamente
    // (ej. si querés dar feedback visual solo la primera vez)
    private bool _activated;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
        Quaternion rot = spawnPoint != null ? spawnPoint.rotation : transform.rotation;

        CheckpointManager.Instance.SetCheckpoint(pos, rot);

        if (!_activated)
        {
            _activated = true;
            // Sound Effect
        }
    }
}
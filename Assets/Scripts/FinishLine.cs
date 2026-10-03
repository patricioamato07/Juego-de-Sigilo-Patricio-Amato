using UnityEngine;

/// <summary>
/// Colocar en la línea de meta. Collider en Is Trigger = true.
/// Al tocarla: muestra el mensaje de meta y detiene el movimiento
/// del jugador (desactiva ThirdPersonController).
/// </summary>
[RequireComponent(typeof(Collider))]
public class FinishLine : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";

    private bool _finished;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_finished) return; // evita retrigger si el collider es grande
        if (!other.CompareTag(playerTag)) return;

        _finished = true;

        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.ShowFinishMessage();
        }

        var controller = other.GetComponent<ThirdPersonController>();
        if (controller != null)
        {
            controller.enabled = false;
        }

        var rb = other.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}

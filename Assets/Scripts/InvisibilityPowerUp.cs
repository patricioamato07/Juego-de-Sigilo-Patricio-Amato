using UnityEngine;

[RequireComponent(typeof(Collider))]
public class InvisibilityPowerUp : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float duration = 8f;
    [SerializeField] private AudioClip pickupSound;
    [SerializeField] private GameObject pickupParticles;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        var invisibility = other.GetComponentInParent<PlayerInvisibility>();
        if (invisibility == null)
        {
            Debug.LogWarning("InvisibilityPowerUp: el jugador no tiene el componente PlayerInvisibility.");
            return;
        }

        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        }

        if (pickupParticles != null)
        {
            Instantiate(pickupParticles, transform.position, Quaternion.identity);
        }

        invisibility.Activate(duration);

        // Si quieres mensaje en pantalla, agrega este método a GameUIManager y descomenta:
        // if (GameUIManager.Instance != null)
        //     GameUIManager.Instance.ShowInvisibilityMessage(duration);

        Destroy(gameObject);
    }
}

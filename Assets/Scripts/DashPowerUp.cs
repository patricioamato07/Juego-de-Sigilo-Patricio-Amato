using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DashPowerUp : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private AudioClip pickupSound;
    [SerializeField] private GameObject pickupParticles;
    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        var controller = other.GetComponent<ThirdPersonController>();
        if (controller == null)

        {
            Debug.LogWarning("DashPowerUp: el objeto con tag Player no tiene ThirdPersonController.");
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

        controller.UnlockDash();

        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.ShowDashUnlockedMessage();
        }

        Destroy(gameObject);
    }
}

using UnityEngine;

/// <summary>
/// Colocar en cada moneda del escenario. Collider en Is Trigger = true.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CoinPickup : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private int value = 1;
    [SerializeField] private GameObject pickupEffectPrefab;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (GameUIManager.Instance == null)
        {
            Debug.LogWarning("CoinPickup: no hay GameUIManager en la escena.");
            return;
        }

        GameUIManager.Instance.AddCoin(value);

        if (pickupEffectPrefab != null)
        {
            Instantiate(pickupEffectPrefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}

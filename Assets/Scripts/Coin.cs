using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Coin : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float rotationSpeed = 90f;

    [Tooltip("Hovering Height")]
    [SerializeField] private float floatAmplitude = 0.25f;

    [Tooltip("Hovering Speed")]
    [SerializeField] private float floatSpeed = 2f;

    [Header("Interaction")]
    [SerializeField] private string playerTag = "Player";

    [Header("Additional Effects")]
    [SerializeField] private AudioClip pickupSound;
    [SerializeField] private GameObject pickupParticles;

    private Vector3 startPosition;

    private void Start()
    {
        startPosition = transform.position;

        Collider col = GetComponent<Collider>();
    }

    private void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

        float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        }

        if (pickupParticles != null)
        {
            Instantiate(pickupParticles, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}

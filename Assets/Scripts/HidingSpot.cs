using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Escondite (casillero, arbusto, caja...). No usa triggers: PlayerHiding consulta la distancia
/// al presionar la tecla de interacción, lo que evita problemas de OnTriggerEnter/Exit al teletransportar.
/// </summary>
public class HidingSpot : MonoBehaviour
{
    public static readonly List<HidingSpot> All = new List<HidingSpot>();

    [Header("Posiciones")]
    [Tooltip("Dónde se coloca el jugador al esconderse. Si está vacío usa este mismo transform.")]
    [SerializeField] private Transform hidePoint;
    [Tooltip("Dónde aparece el jugador al salir. Ponlo FUERA del escondite, delante de la puerta.")]
    [SerializeField] private Transform exitPoint;

    [Header("Interacción")]
    [SerializeField] private float interactRange = 2f;
    [Tooltip("Opcional: objeto con el texto 'Presiona E'. Se muestra cuando el jugador puede esconderse aquí.")]
    [SerializeField] private GameObject promptUI;

    [Header("Eventos (animación de puerta, sonido, etc.)")]
    public UnityEvent onPlayerHidden;
    public UnityEvent onPlayerRevealed;

    public bool Occupied { get; private set; }
    public Transform HidePoint => hidePoint != null ? hidePoint : transform;
    public Transform ExitPoint => exitPoint != null ? exitPoint : transform;
    public float InteractRange => interactRange;

    private void OnEnable() => All.Add(this);

    private void OnDisable()
    {
        All.Remove(this);
        if (promptUI != null) promptUI.SetActive(false);
    }

    private void Update()
    {
        if (promptUI == null) return;

        PlayerHiding player = PlayerHiding.Instance;
        bool show = player != null
                    && !player.IsHidden
                    && !Occupied
                    && (player.transform.position - transform.position).sqrMagnitude <= interactRange * interactRange;

        if (promptUI.activeSelf != show) promptUI.SetActive(show);
    }

    public void SetOccupied(bool value) => Occupied = value;
    public void NotifyHidden() => onPlayerHidden?.Invoke();
    public void NotifyRevealed() => onPlayerRevealed?.Invoke();

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, interactRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(HidePoint.position, 0.15f);

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(ExitPoint.position, 0.15f);
    }
#endif
}

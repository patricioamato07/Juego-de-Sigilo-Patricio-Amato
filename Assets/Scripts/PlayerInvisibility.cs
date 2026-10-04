using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Va en el jugador. Maneja el estado de invisibilidad, su duración y el aspecto visual.
/// Los enemigos consultan IsInvisible (ver EnemyDetection).
/// </summary>
public class PlayerInvisibility : MonoBehaviour
{
    [Header("Visual")]
    [Tooltip("Material translúcido (URP Lit en Surface Type: Transparent) para el efecto fantasma.")]
    [SerializeField] private Material invisibleMaterial;

    [Header("Gameplay")]
    [Tooltip("Aun invisible, un enemigo te detecta si estás a esta distancia o menos.")]
    [SerializeField] private float proximityRadius = 1f;

    [Header("Eventos")]
    public UnityEvent onInvisibilityStarted;
    public UnityEvent onInvisibilityEnded;

    public bool IsInvisible { get; private set; }
    public float TimeRemaining { get; private set; }
    public float ProximityRadius => proximityRadius;

    private readonly List<Renderer> renderers = new List<Renderer>();
    private readonly List<Material[]> originalMaterials = new List<Material[]>();

    private void Awake()
    {
        // Solo mallas: se excluyen partículas, trails, etc. para no romperlos al cambiar materiales.
        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
        {
            if (r is MeshRenderer || r is SkinnedMeshRenderer)
            {
                renderers.Add(r);
                originalMaterials.Add(r.sharedMaterials);
            }
        }
    }

    private void Update()
    {
        if (!IsInvisible) return;

        TimeRemaining -= Time.deltaTime;
        if (TimeRemaining <= 0f) Deactivate();
    }

    /// <summary>Activa la invisibilidad. Si ya está activa, reinicia el tiempo (no se acumula).</summary>
    public void Activate(float duration)
    {
        TimeRemaining = duration;
        if (IsInvisible) return;

        IsInvisible = true;
        ApplyVisual(true);
        onInvisibilityStarted?.Invoke();
    }

    public void Deactivate()
    {
        if (!IsInvisible) return;

        IsInvisible = false;
        TimeRemaining = 0f;
        ApplyVisual(false);
        onInvisibilityEnded?.Invoke();
    }

    private void ApplyVisual(bool invisible)
    {
        for (int i = 0; i < renderers.Count; i++)
        {
            if (renderers[i] == null) continue;

            if (!invisible)
            {
                renderers[i].sharedMaterials = originalMaterials[i];
            }
            else if (invisibleMaterial != null)
            {
                var ghost = new Material[originalMaterials[i].Length];
                for (int m = 0; m < ghost.Length; m++) ghost[m] = invisibleMaterial;
                renderers[i].sharedMaterials = ghost;
            }
        }
    }

    private void OnDisable()
    {
        // Evita que el jugador quede con el material fantasma si se desactiva con el efecto activo.
        if (IsInvisible) Deactivate();
    }
}

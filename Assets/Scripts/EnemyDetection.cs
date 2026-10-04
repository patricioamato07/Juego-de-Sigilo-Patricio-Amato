using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public enum DetectionState { Hidden, Caution, Danger }

/// <summary>
/// Detección de enemigos para un juego de sigilo.
/// 1) OverlapSphere      -> radio de detección
/// 2) Ángulo horizontal  -> cono de visión (con radio corto de proximidad sin ángulo)
/// 3) Raycast/SphereCast -> línea de visión (paredes y obstáculos)
/// Estados: Hidden (alerta 0) / Caution (alerta subiendo o bajando) / Danger (detectado = derrota).
/// </summary>
[DisallowMultipleComponent]
public class EnemyDetection : MonoBehaviour
{
    public enum SightCheck { Raycast, SphereCast }

    [Header("Referencias")]
    [Tooltip("Punto desde donde 've' el enemigo (cabeza). Debe ser hijo del enemigo y estar FUERA de colliders.")]
    [SerializeField] private Transform eyes;

    [Header("Radio y ángulo")]
    [SerializeField, Min(0.1f)] private float detectionRadius = 12f;
    [SerializeField, Range(1f, 360f)] private float viewAngle = 110f;
    [Tooltip("Dentro de este radio detecta sin importar el ángulo (ignora el cono, no las paredes).")]
    [SerializeField] private float closeRadius = 2f;

    [Header("Línea de visión")]
    [SerializeField] private SightCheck sightCheck = SightCheck.Raycast;
    [Tooltip("Solo se usa con SphereCast. Mantenlo pequeño (0.1 - 0.3).")]
    [SerializeField] private float sphereCastRadius = 0.2f;
    [SerializeField] private LayerMask targetMask;    // Capa del jugador
    [Tooltip("Paredes, cajas, puertas... NO incluyas la capa del propio enemigo.")]
    [SerializeField] private LayerMask obstacleMask;

    [Header("Alerta")]
    [Tooltip("Segundos para detectar al jugador si está a distancia máxima.")]
    [SerializeField, Min(0.01f)] private float timeToSpot = 1.5f;
    [Tooltip("Qué tan rápido baja la alerta cuando pierde de vista al jugador (por segundo).")]
    [SerializeField] private float cooldownRate = 0.4f;
    [Tooltip("Multiplicador de velocidad de detección cuando el jugador está pegado al enemigo.")]
    [SerializeField] private float closeSpeedMultiplier = 3f;

    [Header("Visor")]
    [SerializeField] private Renderer visorRenderer;
    [Tooltip("Opcional: luz que acompaña el color del visor.")]
    [SerializeField] private Light visorLight;
    [SerializeField] private Color hiddenColor = new Color(0.2f, 1f, 0.3f);
    [SerializeField] private Color cautionColor = new Color(1f, 0.8f, 0f);
    [SerializeField] private Color dangerColor = new Color(1f, 0.1f, 0.1f);
    [SerializeField] private float colorLerpSpeed = 10f;
    [Tooltip("Requiere Emission activado en el material del visor.")]
    [SerializeField] private float emissionIntensity = 2f;

    [Header("Derrota")]
    [SerializeField] private bool reloadSceneOnCaught = true;
    [SerializeField] private float reloadDelay = 1.5f;

    [Header("Eventos")]
    [Tooltip("Se dispara al llegar a Danger. Úsalo para pantalla de derrota, bloquear controles, etc.")]
    public UnityEvent onPlayerCaught;
    [Tooltip("El enemigo perdió al jugador (la alerta volvió a 0).")]
    public UnityEvent onTargetLost;

    /// <summary>Todos los enemigos activos. La UI y PlayerHiding leen de aquí.</summary>
    public static readonly List<EnemyDetection> Active = new List<EnemyDetection>();

    public float Awareness01 { get; private set; }   // 0 = tranquilo, 1 = detectado
    public bool CanSeeTarget { get; private set; }
    public bool HasSpotted { get; private set; }
    public Transform CurrentTarget { get; private set; }
    public DetectionState State { get; private set; } = DetectionState.Hidden;
    public event Action<DetectionState> StateChanged;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private readonly Collider[] buffer = new Collider[8];
    private MaterialPropertyBlock visorBlock;
    private Color currentVisorColor;
    private bool reloadPending;

    private void Reset()
    {
        eyes = transform;
    }

    private void OnEnable() => Active.Add(this);
    private void OnDisable() => Active.Remove(this);

    private void Awake()
    {
        if (eyes == null) eyes = transform;

        visorBlock = new MaterialPropertyBlock();
        currentVisorColor = hiddenColor;
        ApplyVisorColor(currentVisorColor);

        if (targetMask.value == 0)
            Debug.LogWarning($"{name}: 'Target Mask' está vacío; este enemigo no detectará a nadie.", this);
    }

    private void Update()
    {
        // Tras ser atrapado el estado es final: la alerta ya no sube ni baja.
        if (reloadPending)
        {
            UpdateVisor();
            return;
        }

        CanSeeTarget = Scan(out Transform target, out float distance);

        if (CanSeeTarget)
        {
            CurrentTarget = target;

            float t = Mathf.Clamp01(distance / detectionRadius);
            float speed = Mathf.Lerp(closeSpeedMultiplier, 1f, t) / timeToSpot;
            Awareness01 = Mathf.Min(1f, Awareness01 + speed * Time.deltaTime);
        }
        else
        {
            Awareness01 = Mathf.Max(0f, Awareness01 - cooldownRate * Time.deltaTime);
        }

        if (!HasSpotted && Awareness01 >= 1f)
        {
            HasSpotted = true;
            OnCaught();
        }
        else if (HasSpotted && Awareness01 <= 0f)
        {
            HasSpotted = false;
            CurrentTarget = null;
            onTargetLost?.Invoke();
        }

        UpdateState();
        UpdateVisor();
    }

    /// <summary>
    /// El enemigo olvida al jugador (alerta a 0). Dispara onTargetLost si estaba en estado Danger,
    /// para que tu lógica de persecución vuelva a patrullar. No tiene efecto si ya fue atrapado.
    /// </summary>
    public void ForgetTarget()
    {
        if (reloadPending) return;

        Awareness01 = 0f;
        CurrentTarget = null;

        if (HasSpotted)
        {
            HasSpotted = false;
            onTargetLost?.Invoke();
        }
    }

    private void UpdateState()
    {
        DetectionState next =
            HasSpotted ? DetectionState.Danger :
            Awareness01 > 0f ? DetectionState.Caution :
            DetectionState.Hidden;

        if (next == State) return;

        State = next;
        StateChanged?.Invoke(State);
    }

    private void UpdateVisor()
    {
        Color target = State == DetectionState.Danger ? dangerColor
                     : State == DetectionState.Caution ? cautionColor
                     : hiddenColor;

        float k = 1f - Mathf.Exp(-colorLerpSpeed * Time.deltaTime);
        currentVisorColor = Color.Lerp(currentVisorColor, target, k);
        ApplyVisorColor(currentVisorColor);
    }

    private void ApplyVisorColor(Color color)
    {
        if (visorRenderer != null)
        {
            visorRenderer.GetPropertyBlock(visorBlock);
            visorBlock.SetColor(BaseColorId, color);
            visorBlock.SetColor(EmissionColorId, color * emissionIntensity);
            visorRenderer.SetPropertyBlock(visorBlock);
        }

        if (visorLight != null) visorLight.color = color;
    }

    private void OnCaught()
    {
        onPlayerCaught?.Invoke();

        if (reloadSceneOnCaught)
        {
            reloadPending = true;
            Invoke(nameof(ReloadScene), reloadDelay);
        }
    }

    private void ReloadScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.buildIndex < 0)
        {
            Debug.LogError($"La escena '{scene.name}' no está en la lista de escenas del Build; no se puede recargar.", this);
            return;
        }

        SceneManager.LoadScene(scene.buildIndex);
    }

    private bool Scan(out Transform target, out float distance)
    {
        target = null;
        distance = 0f;

        Vector3 origin = eyes.position;
        int count = Physics.OverlapSphereNonAlloc(
            origin, detectionRadius, buffer, targetMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            Collider c = buffer[i];
            Vector3 toTarget = c.bounds.center - origin;
            float dist = toTarget.magnitude;
            if (dist < 0.001f) continue;

            // Jugador invisible: solo se detecta dentro de su radio de proximidad.
            PlayerInvisibility invisibility = c.GetComponentInParent<PlayerInvisibility>();
            if (invisibility != null && invisibility.IsInvisible && dist > invisibility.ProximityRadius)
                continue;

            // Jugador escondido: no se detecta a ninguna distancia.
            PlayerHiding hiding = c.GetComponentInParent<PlayerHiding>();
            if (hiding != null && hiding.IsHidden)
                continue;

            Vector3 dir = toTarget / dist;

            bool inCloseRange = dist <= closeRadius;
            if (!inCloseRange && !InViewCone(dir))
                continue;

            if (HasLineOfSight(origin, dir, dist))
            {
                target = GetTargetTransform(c);
                distance = dist;
                return true;
            }
        }

        return false;
    }

    /// <summary>Cono horizontal: coincide con lo que dibuja el gizmo y no penaliza diferencias de altura.</summary>
    private bool InViewCone(Vector3 dir)
    {
        Vector3 forward = eyes.forward;
        forward.y = 0f;
        dir.y = 0f;

        // Mirando recto arriba/abajo, o el objetivo justo encima/debajo: no hay ángulo horizontal que medir.
        if (forward.sqrMagnitude < 0.0001f || dir.sqrMagnitude < 0.0001f) return true;

        return Vector3.Angle(forward, dir) <= viewAngle * 0.5f;
    }

    private bool HasLineOfSight(Vector3 origin, Vector3 dir, float dist)
    {
        int mask = obstacleMask.value | targetMask.value;
        float maxDist = dist + 0.1f;
        RaycastHit hit;
        bool hasHit;

        if (sightCheck == SightCheck.Raycast)
        {
            hasHit = Physics.Raycast(origin, dir, out hit, maxDist, mask,
                QueryTriggerInteraction.Ignore);
        }
        else
        {
            hasHit = Physics.SphereCast(origin, sphereCastRadius, dir, out hit, maxDist, mask,
                QueryTriggerInteraction.Ignore);
        }

        if (!hasHit) return false;

        // Lo primero que golpea el rayo debe estar en la capa del jugador, no ser una pared.
        // (Se compara por capa y no por transform.root: con jerarquías compartidas, root da falsos positivos.)
        return (targetMask.value & (1 << hit.collider.gameObject.layer)) != 0;
    }

    /// <summary>Transform "lógico" del objetivo: el del Rigidbody si lo tiene, si no el del collider.</summary>
    private static Transform GetTargetTransform(Collider c)
    {
        return c.attachedRigidbody != null ? c.attachedRigidbody.transform : c.transform;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Transform e = eyes != null ? eyes : transform;
        Vector3 pos = e.position;

        Gizmos.color = new Color(1f, 0.9f, 0f, 0.8f);
        Gizmos.DrawWireSphere(pos, detectionRadius);

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
        Gizmos.DrawWireSphere(pos, closeRadius);

        Vector3 fwd = Vector3.ProjectOnPlane(e.forward, Vector3.up);
        if (fwd.sqrMagnitude < 0.0001f) fwd = e.forward;
        fwd.Normalize();

        float half = viewAngle * 0.5f;
        Vector3 left = Quaternion.AngleAxis(-half, Vector3.up) * fwd;
        Vector3 right = Quaternion.AngleAxis(half, Vector3.up) * fwd;
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(pos, left * detectionRadius);
        Gizmos.DrawRay(pos, right * detectionRadius);

        if (Application.isPlaying && CurrentTarget != null)
        {
            Gizmos.color = CanSeeTarget ? Color.green : Color.red;
            Gizmos.DrawLine(pos, CurrentTarget.position + Vector3.up);
        }
    }
#endif
}

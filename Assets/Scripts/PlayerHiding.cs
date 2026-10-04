using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Va en el jugador. Entra/sale de escondites con la tecla de interacción.
/// Mientras está escondido, EnemyDetection lo ignora; tras giveUpDelay los enemigos lo olvidan.
/// </summary>
public class PlayerHiding : MonoBehaviour
{
    public static PlayerHiding Instance { get; private set; }

#if ENABLE_LEGACY_INPUT_MANAGER
    [Header("Input")]
    [Tooltip("Solo para el Input Manager clásico. Con el nuevo Input System se usa la tecla E.")]
    [SerializeField] private KeyCode legacyInteractKey = KeyCode.E;
#endif

    [Header("Reglas")]
    [Tooltip("Si un enemigo te está viendo ahora mismo, no puedes esconderte.")]
    [SerializeField] private bool blockHidingIfSeen = true;
    [Tooltip("Segundos escondido antes de que los enemigos olviden al jugador.")]
    [SerializeField] private float giveUpDelay = 1.5f;

    [Header("Mientras está escondido")]
    [Tooltip("Scripts a desactivar (ej. ThirdPersonController) para que el jugador no se mueva.")]
    [SerializeField] private Behaviour[] disableWhileHidden;
    [SerializeField] private bool hideMeshes = true;

    [Header("Eventos")]
    public UnityEvent onHidden;
    public UnityEvent onRevealed;
    [Tooltip("Intentó esconderse pero un enemigo lo estaba viendo (sonido de error, mensaje, etc.).")]
    public UnityEvent onHideDenied;

    public bool IsHidden { get; private set; }
    public HidingSpot CurrentSpot { get; private set; }

    private CharacterController characterController;
    private Rigidbody body;
    private readonly List<Renderer> renderers = new List<Renderer>();
    private readonly List<bool> rendererWasEnabled = new List<bool>();
    private Coroutine giveUpRoutine;

    private void Reset()
    {
        var controller = GetComponent<ThirdPersonController>();
        if (controller != null) disableWhileHidden = new Behaviour[] { controller };
    }

    private void Awake()
    {
        Instance = this;
        characterController = GetComponent<CharacterController>();
        body = GetComponent<Rigidbody>();

        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
        {
            if (r is MeshRenderer || r is SkinnedMeshRenderer)
            {
                renderers.Add(r);
                rendererWasEnabled.Add(r.enabled);
            }
        }
    }

    private void Update()
    {
        if (!InteractPressed()) return;

        if (IsHidden)
        {
            Reveal();
        }
        else
        {
            HidingSpot spot = FindNearestSpot();
            if (spot != null) TryHide(spot);
        }
    }

    public bool TryHide(HidingSpot spot)
    {
        if (IsHidden || spot == null || spot.Occupied) return false;

        if (blockHidingIfSeen && IsSeenByAnyEnemy())
        {
            onHideDenied?.Invoke();
            return false;
        }

        IsHidden = true;
        CurrentSpot = spot;
        spot.SetOccupied(true);

        SetControlsEnabled(false);
        SetBodyFrozen(true);
        Teleport(spot.HidePoint.position, spot.HidePoint.rotation);
        if (hideMeshes) SetMeshesVisible(false);

        giveUpRoutine = StartCoroutine(GiveUpAfterDelay());

        spot.NotifyHidden();
        onHidden?.Invoke();
        return true;
    }

    public void Reveal()
    {
        if (!IsHidden) return;

        IsHidden = false;
        if (giveUpRoutine != null) StopCoroutine(giveUpRoutine);

        HidingSpot spot = CurrentSpot;
        CurrentSpot = null;

        if (spot != null)
        {
            Teleport(spot.ExitPoint.position, spot.ExitPoint.rotation);
            spot.SetOccupied(false);
            spot.NotifyRevealed();
        }

        if (hideMeshes) SetMeshesVisible(true);
        SetBodyFrozen(false);
        SetControlsEnabled(true);
        onRevealed?.Invoke();
    }

    private IEnumerator GiveUpAfterDelay()
    {
        yield return new WaitForSeconds(giveUpDelay);

        var enemies = EnemyDetection.Active;
        for (int i = 0; i < enemies.Count; i++)
            enemies[i].ForgetTarget();
    }

    private bool IsSeenByAnyEnemy()
    {
        var enemies = EnemyDetection.Active;
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i].CanSeeTarget) return true;
        }
        return false;
    }

    private HidingSpot FindNearestSpot()
    {
        HidingSpot best = null;
        float bestSqr = float.MaxValue;

        for (int i = 0; i < HidingSpot.All.Count; i++)
        {
            HidingSpot s = HidingSpot.All[i];
            if (s.Occupied) continue;

            float sqr = (s.transform.position - transform.position).sqrMagnitude;
            if (sqr <= s.InteractRange * s.InteractRange && sqr < bestSqr)
            {
                best = s;
                bestSqr = sqr;
            }
        }
        return best;
    }

    private void Teleport(Vector3 position, Quaternion rotation)
    {
        // Un CharacterController activo pisa el cambio de posición del transform.
        bool wasEnabled = characterController != null && characterController.enabled;
        if (characterController != null) characterController.enabled = false;

        transform.SetPositionAndRotation(position, rotation);

        if (characterController != null) characterController.enabled = wasEnabled;
    }

    private void SetBodyFrozen(bool frozen)
    {
        if (body == null) return;

        if (frozen)
        {
            // Se anula la velocidad ANTES de volverlo kinemático (en kinemático da advertencia).
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
        body.isKinematic = frozen;
    }

    private void SetControlsEnabled(bool value)
    {
        foreach (Behaviour b in disableWhileHidden)
        {
            if (b != null) b.enabled = value;
        }
    }

    private void SetMeshesVisible(bool visible)
    {
        for (int i = 0; i < renderers.Count; i++)
        {
            if (renderers[i] == null) continue;
            renderers[i].enabled = visible ? rendererWasEnabled[i] : false;
        }
    }

    private bool InteractPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(legacyInteractKey)) return true;
#endif
        return false;
    }

    private void OnDisable()
    {
        // Evita dejar al jugador invisible o sin controles si se desactiva escondido.
        if (IsHidden) Reveal();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}

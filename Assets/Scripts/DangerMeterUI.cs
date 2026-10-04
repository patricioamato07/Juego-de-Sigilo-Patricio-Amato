using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Barra de peligro. Muestra el MÁXIMO de alerta entre todos los enemigos activos:
/// llega a 100% exactamente cuando algún enemigo te detecta (derrota).
/// </summary>
public class DangerMeterUI : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Image con Image Type = Filled, Fill Method = Horizontal. Requiere un sprite asignado.")]
    [SerializeField] private Image fillImage;
    [Tooltip("Opcional: en el contenedor de la barra, para desvanecerla cuando estás a salvo.")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Aspecto")]
    [SerializeField] private Gradient colorByDanger;
    [Tooltip("Qué tan rápido sigue la barra al valor real (más alto = más reactiva).")]
    [SerializeField] private float fillSpeed = 8f;

    [Header("Visibilidad")]
    [SerializeField] private bool hideWhenSafe = true;
    [SerializeField] private float fadeSpeed = 4f;

    private float displayed;

    private void Reset()
    {
        // Verde -> amarillo -> rojo, coherente con los colores del visor.
        colorByDanger = new Gradient();
        colorByDanger.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.2f, 1f, 0.3f), 0f),
                new GradientColorKey(new Color(1f, 0.8f, 0f), 0.5f),
                new GradientColorKey(new Color(1f, 0.1f, 0.1f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            });
    }

    private void Update()
    {
        float target = GetMaxAwareness();

        float k = 1f - Mathf.Exp(-fillSpeed * Time.deltaTime);
        displayed = Mathf.Lerp(displayed, target, k);
        if (Mathf.Abs(displayed - target) < 0.001f) displayed = target;

        if (fillImage != null)
        {
            fillImage.fillAmount = displayed;
            fillImage.color = colorByDanger.Evaluate(displayed);
        }

        if (hideWhenSafe && canvasGroup != null)
        {
            float targetAlpha = displayed > 0.01f ? 1f : 0f;
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);
        }
    }

    private static float GetMaxAwareness()
    {
        float max = 0f;
        var enemies = EnemyDetection.Active;
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i].Awareness01 > max) max = enemies[i].Awareness01;
        }
        return max;
    }
}

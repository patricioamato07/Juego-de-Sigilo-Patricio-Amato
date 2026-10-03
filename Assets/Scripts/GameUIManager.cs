using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Punto único para actualizar UI de juego: contador de monedas y
/// mensajes temporales (dash desbloqueado, meta alcanzada, etc).
/// Otros scripts (CoinPickup, DashPowerUp, FinishLine) le avisan a
/// este manager en vez de tocar los Text directamente.
/// </summary>
public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance { get; private set; }

    [Header("Monedas")]
    [SerializeField] private TMP_Text coinCounterText;
    [SerializeField] private string coinFormat = "Monedas: {0}";

    [Header("Mensaje temporal")]
    [SerializeField] private GameObject messagePanel; // contenedor que se activa/desactiva
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private float defaultMessageDuration = 2.5f;

    [Header("Textos predefinidos")]
    [SerializeField] private string dashUnlockedMessage = "¡Dash desbloqueado!";
    [SerializeField] private string finishMessage = "¡Meta alcanzada!";

    private int _coinCount;
    private Coroutine _messageRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        UpdateCoinText();
        if (messagePanel != null) messagePanel.SetActive(false);
    }

    public void AddCoin(int amount = 1)
    {
        _coinCount += amount;
        UpdateCoinText();
    }

    public int GetCoinCount() => _coinCount;

    private void UpdateCoinText()
    {
        if (coinCounterText != null)
        {
            coinCounterText.text = string.Format(coinFormat, _coinCount);
        }
    }

    public void ShowDashUnlockedMessage()
    {
        ShowMessage(dashUnlockedMessage, defaultMessageDuration);
    }

    public void ShowFinishMessage()
    {
        // La meta normalmente querés que el mensaje quede más tiempo
        // (o para siempre). Acá lo dejamos fijo sin ocultar.
        ShowMessage(finishMessage, -1f);
    }

    /// <summary>
    /// Muestra un mensaje. Si duration es negativo, el mensaje queda
    /// visible indefinidamente (útil para pantalla de fin de nivel).
    /// </summary>
    public void ShowMessage(string text, float duration)
    {
        if (messageText != null) messageText.text = text;
        if (messagePanel != null) messagePanel.SetActive(true);

        if (_messageRoutine != null)
        {
            StopCoroutine(_messageRoutine);
            _messageRoutine = null;
        }

        if (duration >= 0f)
        {
            _messageRoutine = StartCoroutine(HideMessageAfter(duration));
        }
    }

    private IEnumerator HideMessageAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (messagePanel != null) messagePanel.SetActive(false);
    }
}

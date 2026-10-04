using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Punto único para mostrar mensajes temporales en pantalla.
/// Otros scripts le avisan a este manager en vez de tocar los Text directamente.
/// </summary>
public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance { get; private set; }

    [Header("Mensaje temporal")]
    [SerializeField] private GameObject messagePanel; // contenedor que se activa/desactiva
    [SerializeField] private TMP_Text messageText;

    [Header("Textos predefinidos")]
    [SerializeField] private string finishMessage = "¡Meta alcanzada!";

    private Coroutine _messageRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (messagePanel != null) messagePanel.SetActive(false);
    }

    /// <summary>
    /// Mensaje de meta alcanzada. Queda visible indefinidamente (fin de nivel).
    /// </summary>
    public void ShowFinishMessage()
    {
        ShowMessage(finishMessage, -1f);
    }

    /// <summary>
    /// Muestra un mensaje. Si duration es negativo, el mensaje queda
    /// visible indefinidamente (útil para pantallas de fin de partida).
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

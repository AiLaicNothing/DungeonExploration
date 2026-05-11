using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sistema de notificaciones tipo "toast" (mensaje temporal en pantalla).
/// Singleton para que cualquier sistema pueda mostrar mensajes desde donde sea.
///
/// Setup:
///   - GameObject en HUD Canvas con este script
///   - panelRoot: el GameObject que se activa/desactiva
///   - messageText: el TMP_Text donde se muestra el mensaje
///   - canvasGroup: opcional, para fade in/out
/// </summary>
public class ToastNotificationUI : MonoBehaviour
{
    public static ToastNotificationUI Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Tiempos")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float displayDuration = 3f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    private Coroutine _currentRoutine;

    void Awake()
    {
        Instance = this;
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Muestra un toast con título y mensaje. Si hay uno activo, lo reemplaza.</summary>
    public void Show(string title, string message)
    {
        if (_currentRoutine != null) StopCoroutine(_currentRoutine);
        _currentRoutine = StartCoroutine(ShowRoutine(title, message));
    }

    private IEnumerator ShowRoutine(string title, string message)
    {
        if (titleText != null) titleText.text = title;
        if (messageText != null) messageText.text = message;
        if (panelRoot != null) panelRoot.SetActive(true);

        // Fade in
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
            float t = 0;
            while (t < fadeInDuration)
            {
                t += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Clamp01(t / fadeInDuration);
                yield return null;
            }
            canvasGroup.alpha = 1;
        }

        // Display
        yield return new WaitForSecondsRealtime(displayDuration);

        // Fade out
        if (canvasGroup != null)
        {
            float t = 0;
            while (t < fadeOutDuration)
            {
                t += Time.unscaledDeltaTime;
                canvasGroup.alpha = 1f - Mathf.Clamp01(t / fadeOutDuration);
                yield return null;
            }
            canvasGroup.alpha = 0;
        }

        if (panelRoot != null) panelRoot.SetActive(false);
        _currentRoutine = null;
    }
}
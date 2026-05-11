using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Singleton que mantiene un registro de qué paneles UI están abiertos.
/// Los scripts de gameplay consultan IsAnyUIOpen para bloquear input.
///
/// Tiene auto-cleanup: si un panel registrado se desactiva o destruye sin
/// llamar a Unregister, lo detecta en Update y lo limpia automáticamente.
/// </summary>
public class UIBlockingManager : MonoBehaviour
{
    public static UIBlockingManager Instance { get; private set; }

    private readonly HashSet<MonoBehaviour> _openPanels = new();

    [Tooltip("Si está marcado, muestra logs en consola cada vez que cambia el estado.")]
    [SerializeField] private bool verboseLogging = true;

    /// <summary>True si hay al menos un panel UI activo registrado.</summary>
    public static bool IsAnyUIOpen
    {
        get
        {
            if (Instance == null) return false;
            Instance.CleanupDeadPanels();
            return Instance._openPanels.Count > 0;
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        CleanupDeadPanels();
    }

    /// <summary>
    /// Elimina del set cualquier panel que se haya destruido o desactivado
    /// sin pasar por Unregister.
    /// </summary>
    private void CleanupDeadPanels()
    {
        int before = _openPanels.Count;
        _openPanels.RemoveWhere(p => p == null || !p.isActiveAndEnabled);

        if (verboseLogging && before != _openPanels.Count)
        {
            Debug.Log($"[UIBlockingManager] Auto-limpieza: {before - _openPanels.Count} paneles 'fantasma' quitados. " +
                      $"Restantes: {_openPanels.Count}. {ListPanels()}");
        }
    }

    public void Register(MonoBehaviour panel)
    {
        if (panel == null) return;

        bool added = _openPanels.Add(panel);

        if (verboseLogging && added)
            Debug.Log($"[UIBlockingManager] REGISTRADO: {panel.GetType().Name} ({panel.gameObject.name}). " +
                      $"Total: {_openPanels.Count}. {ListPanels()}");

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Unregister(MonoBehaviour panel)
    {
        if (panel == null) return;

        bool removed = _openPanels.Remove(panel);

        if (verboseLogging && removed)
            Debug.Log($"[UIBlockingManager] DESREGISTRADO: {panel.GetType().Name} ({panel.gameObject.name}). " +
                      $"Restantes: {_openPanels.Count}. {ListPanels()}");

        if (_openPanels.Count == 0)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    /// <summary>Limpia todos los registros. Útil al cambiar de escena.</summary>
    public void ClearAll()
    {
        if (verboseLogging && _openPanels.Count > 0)
            Debug.Log($"[UIBlockingManager] ClearAll: forzando vaciado de {_openPanels.Count} paneles.");
        _openPanels.Clear();
    }

    private string ListPanels()
    {
        if (_openPanels.Count == 0) return "[]";
        return "[" + string.Join(", ", _openPanels.Select(p => p == null ? "<null>" : p.GetType().Name)) + "]";
    }
}
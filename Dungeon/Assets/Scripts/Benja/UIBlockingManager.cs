using System.Collections;
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

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
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
            Debug.Log(
                $"[UIBlockingManager] Auto-limpieza: {before - _openPanels.Count} paneles 'fantasma' quitados. " +
                $"Restantes: {_openPanels.Count}. {ListPanels()}"
            );
        }
    }

    public void Register(MonoBehaviour panel)
    {
        if (panel == null) return;

        _openPanels.Add(panel);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (InteractionUI.Instance != null)
            InteractionUI.Instance.HideUI();
    }

    public void Unregister(MonoBehaviour panel)
    {
        if (panel == null) return;

        _openPanels.Remove(panel);

        if (_openPanels.Count == 0)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (InteractionUI.Instance != null)
                InteractionUI.Instance.ShowUI();
        }
    }

    private IEnumerator LockCursorNextFrame()
    {
        yield return null;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    //private void LateUpdate()
    //{
    //    if (_openPanels.Count == 0)
    //    {
    //        Cursor.lockState = CursorLockMode.Locked;
    //        Cursor.visible = false;
    //    }
    //}

    /// <summary>
    /// Limpia todos los registros. Útil al cambiar de escena.
    /// </summary>
    //public void ClearAll()
    //{
    //    if (verboseLogging && _openPanels.Count > 0)
    //    {
    //        Debug.Log(
    //            $"[UIBlockingManager] ClearAll: forzando vaciado de {_openPanels.Count} paneles."
    //        );
    //    }

    //    _openPanels.Clear();

    //    Cursor.visible = false;
    //    Cursor.lockState = CursorLockMode.Locked;
    //}

    private string ListPanels()
    {
        if (_openPanels.Count == 0)
            return "[]";

        return "[" + string.Join(", ",
            _openPanels.Select(p => p == null ? "<null>" : p.GetType().Name)) + "]";
    }
}
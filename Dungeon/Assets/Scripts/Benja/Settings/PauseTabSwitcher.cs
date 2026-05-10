using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gestiona el cambio entre pestañas del menú de pausa.
///
/// Setup:
///   - Crea N pares Botón + Panel.
///   - Asigna cada par como un TabEntry en la lista.
///   - El primero se mostrará por defecto al abrir el menú.
/// </summary>
public class PauseTabSwitcher : MonoBehaviour
{
    [System.Serializable]
    public class TabEntry
    {
        public string name;            // solo para identificar en el inspector
        public Button button;          // el botón de la pestaña
        public GameObject panel;       // el panel asociado
        public Color activeColor = new Color(0.4f, 0.7f, 1f);
        public Color inactiveColor = new Color(0.6f, 0.6f, 0.6f);
    }

    [Header("Pestañas")]
    [SerializeField] private List<TabEntry> tabs = new();

    [Header("Estado inicial")]
    [SerializeField] private int defaultTabIndex = 0;

    private int _currentIndex = -1;

    void OnEnable()
    {
        // Reconectar listeners cada vez que el menú se abre (por si los botones se recrean)
        for (int i = 0; i < tabs.Count; i++)
        {
            var entry = tabs[i];
            int captured = i;

            if (entry.button != null)
            {
                entry.button.onClick.RemoveAllListeners();
                entry.button.onClick.AddListener(() => ShowTab(captured));
            }
        }

        ShowTab(defaultTabIndex);
    }

    public void ShowTab(int index)
    {
        if (index < 0 || index >= tabs.Count) return;

        for (int i = 0; i < tabs.Count; i++)
        {
            var entry = tabs[i];
            bool isActive = (i == index);

            if (entry.panel != null) entry.panel.SetActive(isActive);

            if (entry.button != null)
            {
                var img = entry.button.GetComponent<Image>();
                if (img != null) img.color = isActive ? entry.activeColor : entry.inactiveColor;

                var label = entry.button.GetComponentInChildren<TMP_Text>();
                if (label != null) label.fontStyle = isActive ? FontStyles.Bold : FontStyles.Normal;
            }
        }

        _currentIndex = index;
    }
}
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class PauseTabSwitcher : MonoBehaviour
{
    [System.Serializable]
    public class Tab
    {
        public string name;
        public Button button;
        public GameObject panel;
        public Image indicator; // opcional: imagen para subrayar la pestaña activa
    }

    [SerializeField] private List<Tab> tabs = new();
    [SerializeField] private int defaultTabIndex = 0;
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color inactiveColor = new Color(1, 1, 1, 0.4f);

    void Awake()
    {
        for (int i = 0; i < tabs.Count; i++)
        {
            int idx = i;
            if (tabs[i].button != null)
                tabs[i].button.onClick.AddListener(() => SelectTab(idx));
        }
    }

    void OnEnable()
    {
        SelectTab(defaultTabIndex);
    }

    public void SelectTab(int index)
    {
        for (int i = 0; i < tabs.Count; i++)
        {
            bool isActive = i == index;
            if (tabs[i].panel != null) tabs[i].panel.SetActive(isActive);
            if (tabs[i].indicator != null) tabs[i].indicator.color = isActive ? activeColor : inactiveColor;
        }
    }
}


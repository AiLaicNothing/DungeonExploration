using TMPro;
using UnityEngine;

public class InteractionUI : MonoBehaviour
{
    public static InteractionUI Instance;

    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI function_Text;

    private void Awake()
    {
        Instance = this;

        if (panel != null) panel.SetActive(false);
    }
    private void Update()
    {
        if (panel.activeSelf && UIBlockingManager.IsAnyUIOpen)
        {
            panel.SetActive(false);
        }
    }

    public void SetUp(string function)
    {
        function_Text.text = function;  
    }

    public void ShowUI()
    {
        if (UIBlockingManager.IsAnyUIOpen)
            return;

        panel.SetActive(true);
    }

    public void HideUI()
    {
        panel.SetActive(false);
    }
}

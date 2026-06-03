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

    public void SetUp(string function)
    {
        function_Text.text = function;  
    }

    public void ShowUI()
    {
        panel.SetActive(true);
    }

    public void HideUI()
    {
        panel.SetActive(false);
    }
}

using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class UIPopUp : MonoBehaviour
{
    public static UIPopUp Instance;

    [SerializeField] private GameObject popUpPanel;
    [SerializeField] private RawImage videoScreen;
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private TextMeshProUGUI tittleText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    [SerializeField] private Button closeButton;
    private void Awake()
    {
        Instance = this;

        if (popUpPanel != null) popUpPanel.SetActive(false);

        if (closeButton != null) closeButton.onClick.AddListener(ClosePopUp);
    }

    public void SetUp(Texture texture, VideoClip video, string tittle, string description)
    {
        videoScreen.texture = texture;
        videoPlayer.clip = video;
        tittleText.text = tittle;
        descriptionText.text = description;
    }

    public void ShowPopUp()
    {
        popUpPanel.SetActive(true);

        if (UIBlockingManager.Instance != null)
            UIBlockingManager.Instance.Register(this);
    }

    public void ClosePopUp()
    {
        videoScreen.texture = null;
        tittleText.text = null;
        descriptionText.text = null;

        popUpPanel.SetActive(false);

        if (UIBlockingManager.Instance != null)
            UIBlockingManager.Instance.Unregister(this);
    }
}

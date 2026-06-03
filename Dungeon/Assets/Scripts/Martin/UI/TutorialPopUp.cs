using Unity.Netcode;
using UnityEngine;
using UnityEngine.Video;

public class TutorialPopUp : NetworkBehaviour
{
    [SerializeField] private string tittle;

    [TextArea(5, 20)]
    [SerializeField] private string description;
    [SerializeField] private Texture texture;
    [SerializeField] private VideoClip video;

    private bool hasActivated = false;
    public UIPopUp UI;

    private void Awake()
    {
        UI = FindAnyObjectByType<UIPopUp>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasActivated) return;

        PlayerController player = other.GetComponent<PlayerController>();

        if (player == null) return;

        if (!player.IsOwner) return;

        hasActivated = true;

        if (UI != null)
        {
            UI.SetUp(texture, video, tittle, description);
            UI.ShowPopUp();
        }

    }
}

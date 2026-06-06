using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Video;

public class TutorialPopUp : NetworkBehaviour
{
    [SerializeField] private List<PopUpPage> pages = new List<PopUpPage>();

    private bool hasActivated = false;
    private UIPopUp ui;

    private void Awake()
    {
        ui = FindAnyObjectByType<UIPopUp>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasActivated) return;

        PlayerController player = other.GetComponent<PlayerController>();

        if (player == null) return;

        if (!player.IsOwner) return;

        hasActivated = true;

        if (ui != null) ui.ShowPopUp(pages);
    }
}

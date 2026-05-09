using System;
using Unity.Netcode;
using UnityEngine;


public static class LocalPlayer
{
    public static PlayerController Controller { get; private set; }
    public static PlayerStats Stats => Controller != null ? Controller.Stats : null;
    public static bool IsReady => Controller != null;


    public static event Action<PlayerController> OnLocalPlayerReady;
    public static event Action OnLocalPlayerDespawned;

    public static void RegisterLocalPlayer(PlayerController controller)
    {
        Controller = controller;
        OnLocalPlayerReady?.Invoke(controller);
        Debug.Log($"[LocalPlayer] Registrado: {controller.name} (Owner: {controller.OwnerClientId})");
    }

    public static void UnregisterLocalPlayer()
    {
        if (Controller == null) return;
        Controller = null;
        OnLocalPlayerDespawned?.Invoke();
    }


    public static void SubscribeOrInvokeIfReady(Action<PlayerController> callback)
    {
        OnLocalPlayerReady += callback;
        if (Controller != null) callback(Controller);
    }

    public static void Unsubscribe(Action<PlayerController> callback)
    {
        OnLocalPlayerReady -= callback;
    }
}
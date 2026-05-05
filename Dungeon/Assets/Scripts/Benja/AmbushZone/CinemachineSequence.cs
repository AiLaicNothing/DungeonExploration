using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Cinemachine;

[Serializable]
public class CinemachineShot
{
    [Tooltip("Cámara de Cinemachine que se activará durante este shot.")]
    public CinemachineVirtualCameraBase camera;

    [Tooltip("Duración en segundos.")]
    public float duration = 2f;

    [Tooltip("Prioridad cuando este shot está activo (debe ser > prioridad de la cámara del jugador).")]
    public int activePriority = 20;

    [Tooltip("Evento opcional al INICIAR este shot. Útil para disparar animaciones o spawns " +
             "exactamente cuando la cámara cambia (ej: 'cerrar puerta cuando la cámara la muestra').")]
    public UnityEvent onShotStart;

    [Tooltip("Evento opcional al TERMINAR este shot.")]
    public UnityEvent onShotEnd;
}

public class CinemachineSequence : MonoBehaviour
{
    [Header("Shots")]
    [SerializeField] private List<CinemachineShot> shots = new();

    [Header("Config")]
    [Tooltip("Prioridad que tendrán las cámaras de la secuencia cuando NO sean el shot activo.")]
    [SerializeField] private int inactivePriority = 0;

    [Tooltip("Si está activo, el input del jugador se deshabilita durante la secuencia.")]
    [SerializeField] private bool disablePlayerInputDuringSequence = false;

    [Tooltip("Opcional: el GameObject que tiene PlayerInput. Solo necesario si disablePlayerInput está activo.")]
    [SerializeField] private GameObject playerInputObject;

    public bool IsPlaying { get; private set; }

    public event Action OnSequenceCompleted;

    private Coroutine _routine;

    void Awake()
    {
        foreach (var shot in shots)
        {
            if (shot.camera != null) shot.camera.Priority = inactivePriority;
        }
    }

    public void Play()
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(PlayRoutine());
    }

    public void Stop()
    {
        if (_routine != null) { StopCoroutine(_routine); _routine = null; }
        ResetAllCameras();
        SetPlayerInputEnabled(true);
        IsPlaying = false;
    }

    private IEnumerator PlayRoutine()
    {
        IsPlaying = true;

        if (disablePlayerInputDuringSequence) SetPlayerInputEnabled(false);

        foreach (var shot in shots)
        {
            if (shot.camera == null) continue;

            shot.camera.Priority = shot.activePriority;
            shot.onShotStart?.Invoke();
            yield return new WaitForSeconds(shot.duration);
            shot.camera.Priority = inactivePriority;

            shot.onShotEnd?.Invoke();
        }

        if (disablePlayerInputDuringSequence) SetPlayerInputEnabled(true);

        IsPlaying = false;
        _routine = null;
        OnSequenceCompleted?.Invoke();
    }

    private void ResetAllCameras()
    {
        foreach (var shot in shots)
            if (shot.camera != null) shot.camera.Priority = inactivePriority;
    }

    private void SetPlayerInputEnabled(bool enabled)
    {
        if (playerInputObject != null)
        {
            // Si tienes un PlayerInput component, lo deshabilitamos.
            // (Comportamiento simple: desactiva todo el GameObject del input.)
            playerInputObject.SetActive(enabled);
        }
    }
}
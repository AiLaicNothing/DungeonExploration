using UnityEngine;

/// <summary>
/// Componente visual del checkpoint. Maneja los efectos visuales (luz, partículas, materiales)
/// que cambian cuando el checkpoint pasa de "no descubierto" a "descubierto a nivel mundo".
///
/// Setup: añadirlo como componente al GameObject del Checkpoint (o a un hijo).
/// Asignar las refs en el inspector según los efectos que quieras usar.
/// </summary>
public class CheckpointVisual : MonoBehaviour
{
    [Header("Particles")]
    [SerializeField] private ParticleSystem activeParticles;
    [SerializeField] private ParticleSystem inactiveParticles;

    [Header("Lights")]
    [SerializeField] private Light activeLight;
    [SerializeField] private Light inactiveLight;

    [Header("Renderers / Materiales")]
    [Tooltip("Renderers cuyo material cambiará al activarse (opcional).")]
    [SerializeField] private Renderer[] renderersToSwap;
    [SerializeField] private Material activeMaterial;
    [SerializeField] private Material inactiveMaterial;

    [Header("GameObjects")]
    [Tooltip("GameObjects que se activan SOLO cuando el checkpoint está descubierto.")]
    [SerializeField] private GameObject[] activeOnlyObjects;
    [Tooltip("GameObjects que se activan SOLO cuando el checkpoint NO está descubierto.")]
    [SerializeField] private GameObject[] inactiveOnlyObjects;

    [Header("Audio")]
    [SerializeField] private AudioSource activationAudio;

    private bool _currentlyActive;

    void Awake()
    {
        // Estado inicial: desactivado (el Checkpoint llamará a ActivateVisual cuando corresponda)
        DeactivateVisual();
    }

    /// <summary>Pone el checkpoint en estado "activado" (descubierto en el mundo).</summary>
    public void ActivateVisual()
    {
        if (_currentlyActive) return;
        _currentlyActive = true;

        if (activeParticles != null) activeParticles.Play();
        if (inactiveParticles != null) inactiveParticles.Stop();

        if (activeLight != null) activeLight.enabled = true;
        if (inactiveLight != null) inactiveLight.enabled = false;

        if (renderersToSwap != null && activeMaterial != null)
        {
            foreach (var r in renderersToSwap)
                if (r != null) r.material = activeMaterial;
        }

        foreach (var go in activeOnlyObjects)
            if (go != null) go.SetActive(true);
        foreach (var go in inactiveOnlyObjects)
            if (go != null) go.SetActive(false);

        if (activationAudio != null) activationAudio.Play();
    }

    /// <summary>Pone el checkpoint en estado "no descubierto".</summary>
    public void DeactivateVisual()
    {
        if (!_currentlyActive && Application.isPlaying) return;
        _currentlyActive = false;

        if (activeParticles != null) activeParticles.Stop();
        if (inactiveParticles != null) inactiveParticles.Play();

        if (activeLight != null) activeLight.enabled = false;
        if (inactiveLight != null) inactiveLight.enabled = true;

        if (renderersToSwap != null && inactiveMaterial != null)
        {
            foreach (var r in renderersToSwap)
                if (r != null) r.material = inactiveMaterial;
        }

        foreach (var go in activeOnlyObjects)
            if (go != null) go.SetActive(false);
        foreach (var go in inactiveOnlyObjects)
            if (go != null) go.SetActive(true);
    }
}
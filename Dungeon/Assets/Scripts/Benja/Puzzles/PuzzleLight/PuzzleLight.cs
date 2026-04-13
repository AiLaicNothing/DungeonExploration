using UnityEngine;

public class PuzzleLight : MonoBehaviour, IActivatable
{
    public Light targetLight;
    public Color activeColor = Color.green;
    public Color inactiveColor = Color.red;
    public ParticleSystem particles;

    void Start() => targetLight.color = inactiveColor;

    public void Activate()
    {
        targetLight.enabled = true;
        targetLight.color = activeColor;
        particles?.Play();
    }

    public void Deactivate()
    {
        targetLight.color = inactiveColor;
        particles?.Stop();
    }
}
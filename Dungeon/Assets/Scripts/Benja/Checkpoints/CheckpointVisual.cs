using UnityEngine;

public class CheckpointVisual : MonoBehaviour
{
    public Light checkpointLight;
    public GameObject particles;

    public void ActivateVisual()
    {
        if (checkpointLight != null)
            checkpointLight.enabled = true;

        if (particles != null)
            particles.SetActive(true);
    }
}
using Cinemachine;
using UnityEngine;

public class PlayerDitherFade : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Camera cam;
    [SerializeField] private Renderer[] renderers;

    [SerializeField] private float fadeStart = 2f;
    [SerializeField] private float fadeEnd = 0.8f;
    public float speed = 5f;

    private float currentAlpha = 1f;

    private void Start()
    {
        cam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
    }

    void Update()
    {
        float dist = Vector3.Distance(cam.transform.position, player.position);

        float targetAlpha = 1f;

        if (dist < fadeStart)
        {
            targetAlpha = Mathf.InverseLerp(fadeEnd, fadeStart, dist);
        }

        currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, Time.deltaTime * speed);

        SetAlpha(currentAlpha);
    }

    void SetAlpha(float alpha)
    {
        foreach (var r in renderers)
        {
            foreach (var mat in r.materials)
            {
                if (mat.HasProperty("_BaseColor"))
                {
                    Color color = mat.GetColor("_BaseColor");
                    color.a = alpha;
                    mat.SetColor("_BaseColor", color);
                }
            }
        }
    }
}

using Unity.Netcode;
using UnityEngine;


public class PlayerDitherFade : NetworkBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Camera cam;
    [SerializeField] private Renderer[] renderers;

    [SerializeField] private float fadeStart = 2f;
    [SerializeField] private float fadeEnd = 0.8f;
    public float speed = 5f;

    private float currentAlpha = 1f;
    private bool _isLocalOwner;

    public override void OnNetworkSpawn()
    {
        _isLocalOwner = IsOwner;

        if (!_isLocalOwner)
        {
            enabled = false;
            return;
        }

        if (player == null) player = transform;
        TryFindCamera();
    }

    void Update()
    {
        if (!_isLocalOwner) return;
        if (player == null) return;

        if (cam == null)
        {
            TryFindCamera();
            if (cam == null) return; 
        }

        float dist = Vector3.Distance(cam.transform.position, player.position);
        float targetAlpha = 1f;

        if (dist < fadeStart)
            targetAlpha = Mathf.InverseLerp(fadeEnd, fadeStart, dist);

        currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, Time.deltaTime * speed);
        SetAlpha(currentAlpha);
    }

    private void TryFindCamera()
    {

        cam = Camera.main;

        if (cam == null)
        {
            var go = GameObject.FindGameObjectWithTag("MainCamera");
            if (go != null) cam = go.GetComponent<Camera>();
        }
    }

    void SetAlpha(float alpha)
    {
        if (renderers == null) return;

        foreach (var r in renderers)
        {
            if (r == null) continue;

            foreach (var mat in r.materials)
            {
                if (mat == null) continue;
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
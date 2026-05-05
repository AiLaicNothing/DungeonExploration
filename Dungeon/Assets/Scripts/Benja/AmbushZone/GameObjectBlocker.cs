using UnityEngine;

public class GameObjectBlocker : MonoBehaviour, IAmbushBlocker
{
    [Tooltip("GameObject a activar al bloquear. Si no se asigna, usa el propio.")]
    [SerializeField] private GameObject blockerObject;

    void Awake()
    {
        if (blockerObject == null) blockerObject = gameObject;
        // Por defecto empieza desbloqueado
        blockerObject.SetActive(false);
    }

    public void Block() => blockerObject.SetActive(true);
    public void Unblock() => blockerObject.SetActive(false);
}

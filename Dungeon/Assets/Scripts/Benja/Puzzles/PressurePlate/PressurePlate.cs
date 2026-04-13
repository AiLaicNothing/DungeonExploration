using UnityEngine;

public class PressurePlate : MonoBehaviour, IActivator
{
    [Header("Configuración")]
    public PuzzleReceiver receiver;
    public LayerMask validLayers;          
    public bool canDeactivate = true;  

    //[Header("Visual")]
   // public Animator animator;

    private int _objectsOnPlate = 0;
    public bool IsActive => _objectsOnPlate > 0;

    void Start()
    {
        receiver?.RegisterActivator(this);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsInValidLayer(other.gameObject)) return;

        _objectsOnPlate++;
        //animator?.SetBool("IsPressed", true);
        receiver?.Evaluate();
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsInValidLayer(other.gameObject)) return;
        if (!canDeactivate) return;

        _objectsOnPlate = Mathf.Max(0, _objectsOnPlate - 1);

        if (_objectsOnPlate == 0)
        {
           // animator?.SetBool("IsPressed", false);
            receiver?.Evaluate();
        }
    }

    bool IsInValidLayer(GameObject obj) =>
        (validLayers.value & (1 << obj.layer)) != 0;

    public void RegisterReceiver(PuzzleReceiver r) => receiver = r;
}
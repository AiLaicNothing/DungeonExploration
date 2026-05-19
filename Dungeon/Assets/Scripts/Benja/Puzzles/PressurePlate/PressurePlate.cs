// PressurePlate.cs

using Unity.Netcode;
using UnityEngine;

public class PressurePlate : NetworkBehaviour, IActivator
{
    [Header("Configuración")]
    public PuzzleReceiver receiver;

    public LayerMask validLayers;

    public bool canDeactivate = true;

    private NetworkVariable<int> _objectsOnPlate =
        new NetworkVariable<int>(0);

    public bool IsActive => _objectsOnPlate.Value > 0;

    private void Start()
    {
        Debug.Log($"[PressurePlate] Start -> {name}");

        receiver?.RegisterActivator(this);
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log($"[PressurePlate] OnNetworkSpawn -> {name}");

        _objectsOnPlate.OnValueChanged += OnPlateStateChanged;

        UpdateVisual(_objectsOnPlate.Value > 0);
    }

    public override void OnNetworkDespawn()
    {
        _objectsOnPlate.OnValueChanged -= OnPlateStateChanged;
    }

    private void OnPlateStateChanged(int previous, int current)
    {
        Debug.Log($"[PressurePlate] Estado cambió: {previous} -> {current}");

        UpdateVisual(current > 0);
    }

    private void UpdateVisual(bool pressed)
    {
        Debug.Log($"[PressurePlate] Visual actualizado. Pressed = {pressed}");
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[PressurePlate] TriggerEnter detectó: {other.name}");

        if (!IsServer)
        {
            Debug.Log("[PressurePlate] Ignorado porque no soy servidor");
            return;
        }

        if (!IsInValidLayer(other.gameObject))
        {
            Debug.Log($"[PressurePlate] Layer inválido: {other.gameObject.layer}");
            return;
        }

        _objectsOnPlate.Value++;

        Debug.Log($"[PressurePlate] Objetos encima: {_objectsOnPlate.Value}");

        receiver?.Evaluate();
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log($"[PressurePlate] TriggerExit detectó: {other.name}");

        if (!IsServer)
        {
            Debug.Log("[PressurePlate] Exit ignorado porque no soy servidor");
            return;
        }

        if (!IsInValidLayer(other.gameObject))
        {
            Debug.Log($"[PressurePlate] Layer inválido al salir: {other.gameObject.layer}");
            return;
        }

        if (!canDeactivate)
        {
            Debug.Log("[PressurePlate] canDeactivate = false");
            return;
        }

        _objectsOnPlate.Value =
            Mathf.Max(0, _objectsOnPlate.Value - 1);

        Debug.Log($"[PressurePlate] Objetos restantes: {_objectsOnPlate.Value}");

        receiver?.Evaluate();
    }

    private bool IsInValidLayer(GameObject obj)
    {
        bool valid =
            (validLayers.value & (1 << obj.layer)) != 0;

        Debug.Log($"[PressurePlate] ValidLayer check: {obj.name} -> {valid}");

        return valid;
    }

    public void RegisterReceiver(PuzzleReceiver r)
    {
        receiver = r;
    }
}
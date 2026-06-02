using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Puente entre el PlayerInput del Player Prefab y los singletons de UI
/// que viven en escena (PauseMenuUI, etc.).
///
/// El PlayerInput está en el Player Prefab → no puede referenciar GameObjects de escena
/// en el inspector. Este script hace de intermediario: el evento del PlayerInput
/// llama a un método LOCAL de este script, y este reenvía al singleton apropiado.
///
/// Setup:
///   - Añadir al Player Prefab (mismo GameObject que tiene PlayerController/PlayerInput)
///   - En el PlayerInput component, sección Events:
///     · Acción "Pause" → conectar al método OnPauseInput de este script
///     · Otras acciones se mantienen como las tengas
/// </summary>
public class PlayerInputBridge : NetworkBehaviour
{
    /// <summary>Conectar este método al evento "Pause" del PlayerInput.</summary>
    public void OnPauseInput(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (!IsOwner) return;

        if (CheckpointSkillUI.Instance != null &&
            CheckpointSkillUI.Instance.IsOpen)
        {
            CheckpointSkillUI.Instance.Close();
            return;
        }

        if (CheckpointUpgradeUI.Instance != null &&
            CheckpointUpgradeUI.Instance.IsOpen)
        {
            CheckpointUpgradeUI.Instance.Close();
            return;
        }

        if (TeleporterPanelUI.Instance != null &&
            TeleporterPanelUI.Instance.IsOpen)
        {
            TeleporterPanelUI.Instance.Close();
            return;
        }

        if (CheckpointMenuUI.Instance != null &&
            CheckpointMenuUI.Instance.IsOpen)
        {
            CheckpointMenuUI.Instance.Close();
            return;
        }

        if (PauseMenuUI.Instance != null)
        {
            PauseMenuUI.Instance.OnPause(context);
        }
    }
}   
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    public Vector2 moveInput { get; private set; }
    public bool hasJumped { get; private set; }
    public bool isAiming {  get; private set; }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if(context.started)
        {
            Debug.Log("Has jumped");
            hasJumped = true;
        }
    }

    public void OnAiming(InputAction.CallbackContext context)
    {
        isAiming = context.ReadValueAsButton();
    }

    public void LateUpdate()
    {
        hasJumped = false;
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    public float attackBufferTime = 0.2f;
    private float attackBufferCounter;

    public Vector2 moveInput { get; private set; }
    public bool hasJumped { get; private set; }
    public bool hasDashed {  get; private set; }
    public bool isAiming {  get; private set; }
    public bool attackPressed { get; private set; }
    public bool AttackBuffered => attackBufferCounter > 0;
    public bool skill1Pressed { get; private set; }
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if(context.started)
        {
            hasJumped = true;
        }
    }
    public void OnDash(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            hasDashed = true;
            Debug.Log("Has dashed");
        }
    }

    public void OnAiming(InputAction.CallbackContext context)
    {
        isAiming = context.ReadValueAsButton();
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if(context.started)
        {
            attackBufferCounter = attackBufferTime;
            attackPressed = true;
        }
    }

    public void OnSkill1Pressed(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            skill1Pressed = true;
        }
    }

    public void UseAttackBufer()
    {
        attackBufferCounter = 0f;
    }

    private void Update()
    {
        if (attackBufferCounter  > 0)
        {
            attackBufferCounter -= Time.deltaTime;
        }
    }

    public void LateUpdate()
    {
        hasJumped = false;
        attackPressed = false;
        hasDashed = false;
        skill1Pressed = false;
    }
}

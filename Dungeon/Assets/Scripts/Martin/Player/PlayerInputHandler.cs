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
    public Vector2 lookInput { get; private set; }
    public bool onLockTarget { get; private set; }
    public bool attackPressed { get; private set; }
    public bool AttackBuffered => attackBufferCounter > 0;
    public bool skill1Pressed { get; private set; }
    public int skillPressedIndex { get; private set; } = -1;
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

    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    public void OnLockTarget(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            onLockTarget = true;
        }
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

    public void OnSkill1(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            skillPressedIndex = 0;

        }
    }

    public void OnSkill2(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            skillPressedIndex = 1;

        }
    }

    public void OnSkill3(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            skillPressedIndex = 2;

        }
    }

    public void OnSkill4(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            skillPressedIndex = 3;

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
        onLockTarget = false;

        skillPressedIndex = -1;
    }
}

using UnityEngine;

public class PlayerController : MonoBehaviour
{
    //--> Variables
    [Header("Variables")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float rotSpeed = 10f;
    [SerializeField] private float moveMultiplier = 1f;

    //-->Combat variables
    [Header("Combat")]
    [SerializeField] private BasicComboData basicComboData;
    public bool isGrounded { get; private set; }
    public bool isPerformingAction = false;
    public bool hasUsedDash = false;
    public bool hasUsedAirAttack = false;

    //--> References
    [Header("Component References")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private PlayerInputHandler input;
    [SerializeField] private Transform playerModel;

    public Rigidbody Rb => rb;
    public PlayerInputHandler Input => input;
    public BasicComboData ComboData => basicComboData;

    //-->States for movements
    PlayerStateMachine movementSM;
    public PlayerIddle_State iddle_State;
    public PlayerMove_State move_State;
    public PlayerJump_State jump_State;
    public PlayerFall_State fall_State;

    //-->States for actions
    PlayerStateMachine actionSM;
    public PlayerIddleAction iddeAction_State;
    public PlayerBasicAttack basicAttack_State;
    public PlayerAirAttack AirAttack_State;

    private void Awake()
    {
        movementSM = new PlayerStateMachine();
        actionSM = new PlayerStateMachine();

        iddle_State = new PlayerIddle_State(this);
        move_State = new PlayerMove_State(this);
        jump_State = new PlayerJump_State(this);
        fall_State = new PlayerFall_State(this);

        iddeAction_State = new PlayerIddleAction(this);
        basicAttack_State = new PlayerBasicAttack(this);
        AirAttack_State = new PlayerAirAttack(this);
    }

    private void Start()
    {
        movementSM.Initialize(iddle_State);
        actionSM.Initialize(iddeAction_State);
    }
    private void Update()
    {
        CheckGround();
        movementSM.Update();
    }

    private void FixedUpdate()
    {
        Movement();

        movementSM.FixedUpdate();
    }

    public void ChangeState(PlayerStates nextState)
    {
        movementSM.ChangeState(nextState);
    }
    public void ChangeActionState(PlayerStates nextState)
    {
        actionSM.ChangeState(nextState);
    }

    private void Movement()
    {
        Vector2 inputDir = input.moveInput.normalized;

        Vector3 camForward = Camera.main.transform.forward ;
        camForward.y = 0;
        camForward.Normalize();

        Vector3 camRight = Camera.main.transform.right;
        camRight.y = 0;
        camRight.Normalize();

        Vector3 moveDir = camForward * inputDir.y + camRight * inputDir.x;

        Vector3 velocity = moveDir * moveSpeed * moveMultiplier;
        velocity.y = rb.linearVelocity.y;  
        rb.linearVelocity = velocity;

        HandleRotation(moveDir);
    }

    public void Jump()
    {
        if (!isGrounded)
        {
            Debug.Log("Is not grounded cant jump");
            return;
        }

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.y);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    void HandleRotation(Vector3 moveDir)
    {
        Vector3 lookDir;

        if (!input.isAiming)
        {
            if (moveDir.magnitude < 0.1f)
            {
                return;
            }

            lookDir = moveDir;
        }
        else
        {
            //look foward
            lookDir = Camera.main.transform.forward;
            lookDir.y = 0;
            lookDir.Normalize();
        }

        Quaternion targetRotation = Quaternion.LookRotation(lookDir);
        playerModel.rotation = Quaternion.Slerp(playerModel.rotation, targetRotation, rotSpeed * Time.deltaTime);

    }

    private void CheckGround()
    {
        isGrounded = Physics.Raycast(groundCheck.position, Vector3.down, 1.1f);
    }
}

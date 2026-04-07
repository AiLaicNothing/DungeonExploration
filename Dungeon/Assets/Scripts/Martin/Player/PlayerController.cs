using UnityEngine;

public class PlayerController : MonoBehaviour
{
    //--> Variables
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float rotSpeed = 10f;

    public bool isGrounded { get; private set; }

    //--> References
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private PlayerInputHandler input;

    public Rigidbody Rb => rb;
    public PlayerInputHandler Input => input;

    //-->States
    PlayerStateMachine movementSM;
    public PlayerIddle_State iddle_State;
    public PlayerMove_State move_State;
    public PlayerJump_State jump_State;
    public PlayerFall_State fall_State;

    private void Awake()
    {
        movementSM = new PlayerStateMachine();

        iddle_State = new PlayerIddle_State(this);
        move_State = new PlayerMove_State(this);
        jump_State = new PlayerJump_State(this);
        fall_State = new PlayerFall_State(this);
    }

    private void Start()
    {
        movementSM.Initialize(iddle_State);
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

    private void Movement()
    {
        Vector2 inputDir = input.moveInput;

        Vector3 moveDir = new Vector3(inputDir.x, 0, inputDir.y);
        moveDir.y = 0;

        Vector3 velocity = moveDir.normalized * moveSpeed;
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

        Debug.Log("Try Jump");
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.y);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    private void HandleRotation(Vector3 move)
    {
        if (move.magnitude < 0.1f)
        {
            return;
        }

        Vector3 dir;

        //--> Change camera depending
        if (input.isAiming)
        {

        }
        else
        {

        }

        //Quaternion targetRot = Quaternion.LookRotation(dir);
        //transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotSpeed * Time.deltaTime);
    }

    private void CheckGround()
    {
        isGrounded = Physics.Raycast(groundCheck.position, Vector3.down, 1.1f);
    }
}

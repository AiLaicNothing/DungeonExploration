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

    //-->States
    PlayerStateMachine movementSM;

    private void Awake()
    {
        movementSM = new PlayerStateMachine();
    }

    private void Start()
    {
        //movementSM.Initialize();
    }
    private void Update()
    {
        movementSM.Update();
    }

    private void FixedUpdate()
    {
        CheckGround();
        Movement();

        movementSM.FixedUpdate();
    }

    private void Movement()
    {

    }

    public void Jump()
    {

    }

    private void HandleRotation()
    {

    }

    private void CheckGround()
    {
        isGrounded = Physics.Raycast(groundCheck.position, Vector3.down, 1.1f);
    }
}

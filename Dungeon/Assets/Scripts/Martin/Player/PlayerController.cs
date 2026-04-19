using UnityEngine;

public class PlayerController : MonoBehaviour
{
    //public float currentHealth;
    //public float currentStamina;
    //public float currentMana;

    //--> Variables
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float rotSpeed = 10f;
    [SerializeField] private float moveMultiplier = 1f;

    [Header("Dash")]
    [SerializeField] private float dashDistance = 5f;
    [SerializeField] private float dashDuration = 0.3f;
    [SerializeField] private float dashCost = 20f;

    //[SerializeField] private float maxStamina;

    //-->Combat variables
    [Header("Combat")]
    [SerializeField] private BasicComboData basicComboData;
    [SerializeField] private BasicComboData airComboData;
    public bool isGrounded { get; private set; }
    public bool isPerformingAction = false;
    public bool hasUsedDash = false;
    public bool hasUsedAirAttack = false;

    [Header("Skills")]
    [SerializeField] private int maxSkillSlots = 4;
    [SerializeField] private Skill[] skills;
    private float[] skillsCooldown;

    //--> References
    [Header("Component References")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private PlayerInputHandler input;
    [SerializeField] private Transform playerModel;
    public GameObject hitboxPrefab;

    public Rigidbody Rb => rb;
    public Transform PlayerModel => playerModel;
    public PlayerInputHandler Input => input;
    public BasicComboData ComboData => basicComboData;
    public BasicComboData AirComboData => airComboData;

    //--> make data accesible to other scripts that has acces to this one.
    public float CurrentStamina => PlayerStats.Instance.Stamina.CurrentValue;
    public float MaxStamina => PlayerStats.Instance.Stamina.Max;
    public float DashDistance => dashDistance;
    public float DashDuration => dashDuration;
    public float DashCost => dashCost;

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
    public PlayerAirAttack airAttack_State;
    public PlayerDash dash_State;
    public PlayerSkill skill_State;

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
        airAttack_State = new PlayerAirAttack(this);
        dash_State = new PlayerDash(this);
        skill_State = new PlayerSkill(this);

        skillsCooldown = new float[skills.Length];
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
        actionSM.Update();

        for (int i = 0; i < skillsCooldown.Length; i++)
        {
            if (skillsCooldown[i] > 0)
            {
                skillsCooldown[i] -= Time.deltaTime;
            }
        }
    }

    private void FixedUpdate()
    {
        if (!isPerformingAction)
        {
            Movement();
        }

        movementSM.FixedUpdate();
        actionSM.FixedUpdate();
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

    public bool HasStamina(float cost) =>
        PlayerStats.Instance.Stamina.CurrentValue >= cost;

    public void ConsumeStamina(float cost) =>
        PlayerStats.Instance.Stamina.Modify(-cost);

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
        bool previous = isGrounded;

        isGrounded = Physics.Raycast(groundCheck.position, Vector3.down, 1.1f);

        if(!previous && isGrounded)
        {
            hasUsedAirAttack = false;
        }
    }

    //---SECTION RELATED TO SKILLS---
    public Skill GetSkill(int index)
    {
        if (index < 0 || index >= skills.Length)
        {
            return null;
        }
        return skills[index];
    }

    public bool HasResource(ResourceType type, float cost)
    {
        return type switch
        {
            ResourceType.Stamina => PlayerStats.Instance.Stamina.CurrentValue >= cost,
            ResourceType.Mana => PlayerStats.Instance.Mana.CurrentValue >= cost,
            ResourceType.Health => PlayerStats.Instance.Health.CurrentValue >= cost,
            _ => true
        };
    }

    public void ConsumeResource(ResourceType type, float cost)  // corregí el typo "ConsumreResource"
    {
        switch (type)
        {
            case ResourceType.Stamina: PlayerStats.Instance.Stamina.Modify(-cost); break;
            case ResourceType.Mana: PlayerStats.Instance.Mana.Modify(-cost); break;
            case ResourceType.Health: PlayerStats.Instance.Health.Modify(-cost); break;
        }
    }
    public bool IsSkillReady(int index)
    {
        return skillsCooldown[index] <= 0;
    }

    public void TriggerCooldown(int index)
    {
        skillsCooldown[index] = skills[index].cooldown;
    }

    //---TEMPORAL---
    public void ShowHitbox(Vector3 center, Vector3 size, Quaternion rot)
    {
        GameObject box = GameObject.Instantiate(hitboxPrefab, center, rot);

        box.transform.localScale = size;

        Destroy(box, 0.2f);
    }
}

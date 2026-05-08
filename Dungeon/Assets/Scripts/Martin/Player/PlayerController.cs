using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour, IDamageable
{

    //--> Variables
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float rotSpeed = 10f;
    [SerializeField] private float moveMultiplier = 1f;
    [SerializeField] private LayerMask whatIsGround;

    [Header("Dash")]
    [SerializeField] private float dashDistance = 5f;
    [SerializeField] private float dashDuration = 0.3f;
    [SerializeField] private float dashCost = 20f;

    [SerializeField] private float baseGravity = -9.81f;
    [SerializeField] private float fallGravityMultiplier = 2.5f;
    private float currentGravityMultiplier;
    //[SerializeField] private float maxStamina;

    //-->Combat variables
    [Header("Combat")]
    [SerializeField] private bool isRange;
    [SerializeField] private BasicComboData basicComboData;
    [SerializeField] private BasicComboData airComboData;
    [Header("Variables for range")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private ShootData shootData;

    public bool isGrounded { get; private set; }
    public bool isPerformingAction = false;
    public bool blockVelocity = false;
    public bool hasUsedDash = false;
    public bool hasUsedAirAttack = false;
    public bool isDead { get; private set; } = false;

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
    [SerializeField] private LockOnTarget lockOnTarget;
    [SerializeField] private GameObject thirdCam;

    [SerializeField] private PlatformRider platformRider;
    public GameObject hitboxPrefab;

    public Rigidbody Rb => rb;
    public Transform PlayerModel => playerModel;

    public LockOnTarget LockTarget => lockOnTarget;
    public bool IsRange => isRange;
    public Transform FirePoint => firePoint;
    public PlayerInputHandler Input => input;
    public BasicComboData ComboData => basicComboData;
    public BasicComboData AirComboData => airComboData;
    public ShootData ShootData => shootData;

    public float FallGravityMultiplier => fallGravityMultiplier;

    //--> make data accesible to other scripts that has acces to this one.

    //--> ESTADISTICAS (todas leen de PlayerStats.Instance)
    public float MaxHealth => PlayerStats.Instance.Health.Max;
    public float CurrentHealth => PlayerStats.Instance.Health.CurrentValue;

    public float CurrentStamina => PlayerStats.Instance.Stamina.CurrentValue;
    public float MaxStamina => PlayerStats.Instance.Stamina.Max;

    public float MaxMana => PlayerStats.Instance.Mana.Max;
    public float CurrentMana => PlayerStats.Instance.Mana.CurrentValue;

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
    public PlayerShoot shoot_State;
    public PlayerDash dash_State;
    public PlayerSkill skill_State;

    public override void OnNetworkSpawn()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();

        if (input == null) input = GetComponent<PlayerInputHandler>();

        if (lockOnTarget == null) lockOnTarget = GetComponent<LockOnTarget>();

        if (platformRider == null) platformRider = GetComponent<PlatformRider>();

        if (!IsOwner)
        {
            thirdCam.SetActive(false);
        }
        else
        {
            thirdCam.SetActive(true);
        }
    }

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
        shoot_State = new PlayerShoot(this);
        dash_State = new PlayerDash(this);
        skill_State = new PlayerSkill(this);

        skillsCooldown = new float[skills.Length];

        // NOTA: No inicializamos vida/mana aquí.
        // PlayerStats.Instance ya lo hace con los baseValue del ScriptableObject.
    }

    private void Start()
    {
        if (!IsOwner) return;

        movementSM.Initialize(iddle_State);
        actionSM.Initialize(iddeAction_State);
    }

    private void Update()
    {
        if (!IsOwner) return;

        // Chequeo de muerte: usa la propiedad correcta y solo dispara una vez
        if (!isDead && CurrentHealth <= 0)
        {
            Destroy(gameObject);
            Die();
        }

        CheckGround();
        GetViewPoint();
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
        if (!IsOwner) return;

        if (!isPerformingAction)
        {
            rb.AddForce(Vector3.up * baseGravity * currentGravityMultiplier, ForceMode.Acceleration);
            Movement();
        }
        if (blockVelocity)
        {
            rb.linearVelocity = new Vector3(0, 0, 0);
        }

        movementSM.FixedUpdate();
        actionSM.FixedUpdate();
    }
    // ── State Machine ──────────────────────────────────────────────────────

    public void ChangeState(PlayerStates nextState) => movementSM.ChangeState(nextState);
    public void ChangeActionState(PlayerStates nextState) => actionSM.ChangeState(nextState);

    // ── Movement ──────────────────────────────────────────────────────

    private void Movement()
    {
        Vector2 inputDir = input.moveInput.normalized;

        Vector3 camForward = Camera.main.transform.forward;
        camForward.y = 0;
        camForward.Normalize();

        Vector3 camRight = Camera.main.transform.right;
        camRight.y = 0;
        camRight.Normalize();

        Vector3 moveDir = camForward * inputDir.y + camRight * inputDir.x;

        Vector3 velocity = moveDir * moveSpeed * moveMultiplier;
        velocity.y = rb.linearVelocity.y;

        if (platformRider != null && platformRider.IsOnPlatform)
        {
            velocity.x += platformRider.CurrentPlatformVelocity.x;
            velocity.z += platformRider.CurrentPlatformVelocity.z;
            // ascensor
            // velocity.y += platformRider.CurrentPlatformVelocity.y;
            // velocity.y += platformRider.CurrentPlatformVelocity.y; // ascensor
        }

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


    // ── Stamina ──────────────────────────────────────────────────────
    public bool HasStamina(float cost) => PlayerStats.Instance.Stamina.CurrentValue >= cost;
    public void ConsumeStamina(float cost) => PlayerStats.Instance.Stamina.Modify(-cost);


    // ── Mana ─────────────────────────────────────────────────────────
    public bool HasMana(float cost) => PlayerStats.Instance.Mana.CurrentValue >= cost;
    public void ConsumeMana(float cost) => PlayerStats.Instance.Mana.Modify(-cost);


    // ── Health ───────────────────────────────────────────────────────
    public bool IsAlive => CurrentHealth > 0;

    /// <summary>Cura al jugador (clamp al max automático).</summary>
    public void Heal(float amount)
    {
        if (isDead) return;
        PlayerStats.Instance.Health.Modify(amount);
    }

    void HandleRotation(Vector3 moveDir)
    {
        Vector3 lookDir;

        if (input.isAiming || lockOnTarget.isTargeting)
        {
            lookDir = Camera.main.transform.forward; // Usa la referencia 'mainCamera' que ya tienes en tu script
            lookDir = Camera.main.transform.forward;
            lookDir.y = 0;
            lookDir.Normalize();
        }
        else
        {
            if (moveDir.magnitude < 0.1f)
            {
                return;
            }

            if (moveDir.magnitude < 0.1f) return;
            lookDir = moveDir;
        }

        Quaternion targetRotation = Quaternion.LookRotation(lookDir);
        playerModel.rotation = Quaternion.Slerp(playerModel.rotation, targetRotation, rotSpeed * Time.deltaTime);

    }

    private void CheckGround()
    {
        bool previous = isGrounded;
        isGrounded = Physics.Raycast(groundCheck.position, Vector3.down, 1.1f, whatIsGround);

        if(!previous && isGrounded)
        if (!previous && isGrounded)
        {
            hasUsedAirAttack = false;
        }
    }

    public Vector3 GetViewPoint()
    {

        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width/2, Screen.height/2, 0));

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.green);
            Debug.DrawLine(hit.point, hit.point + Vector3.up * 0.5f, Color.blue);

            return hit.point;

        }

        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red);

        return ray.origin + ray.direction * 100; 
    }

    public Vector3 GetAimPoint(float maxRange, LayerMask groundLayer)
    {
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f));

        Vector3 origin = ray.origin;
        Vector3 dir = ray.direction;

        if (Physics.Raycast(ray, out RaycastHit hit, maxRange, groundLayer))
        {
            return hit.point;
        }

        return origin + dir * maxRange;
    }

    // ── Melee Request ──────────────────────────────────────────────────────

    public void RequestMeleeAttack(int comboIndex, bool isGrounded)
    {
        MeleeAttackServerRpc(comboIndex, isGrounded);
    }

    [ServerRpc]
    private void MeleeAttackServerRpc(int comboIndex, bool isGrounded)
    {
        AttackSteps attack = isGrounded? ComboData.attackSteps[comboIndex] : airComboData.attackSteps[comboIndex];

        Vector3 center = PlayerModel.transform.position + PlayerModel.transform.forward * attack.hitBoxOffSet.z + Vector3.up * attack.hitBoxOffSet.y;

        Collider[] hits = Physics.OverlapBox(center, attack.hitBoxSize * 0.5f, PlayerModel.transform.rotation);

        //ShowHitbox(center, attack.hitBoxSize, PlayerModel.transform.rotation);

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                Debug.Log($"Hit Enemy: {hit.name}");

                //Add damage logic

                IDamageable damageable = hit.GetComponent<IDamageable>();

                if (damageable != null)
                {
                    Vector3 hitDir = PlayerModel.transform.forward;

                    damageable.TakeDamage(10f * attack.hitData.damageMultiplier, attack.hitData.throwType, hitDir, attack.hitData.stunDuration, attack.hitData.keepInAir, attack.hitData.airLiftForce, attack.hitData.staggerCharge);
                }

                Debug.Log($"HIT {hit.name}");
            }
        }

        ShowHitboxClientRpc(center, attack.hitBoxSize, PlayerModel.rotation);
    }

    // ── Skill Request ──────────────────────────────────────────────────────

    public void RequestSkill(int skillIndex, Vector3 targetPoint)
    {
        Vector3 lockTargetPos = Vector3.zero;

        if (lockOnTarget != null && lockOnTarget.isTargeting && lockOnTarget.CurrentTarget != null)
        {
            lockTargetPos = lockOnTarget.CurrentTarget.position;
        }

        UseSkillServerRpc(skillIndex, targetPoint, lockTargetPos);
    }

    [ServerRpc]
    private void UseSkillServerRpc(int skillIndex, Vector3 targetPoint, Vector3 lockTargetPos)
    {
        if (skillIndex < 0 || skillIndex >= skills.Length) return;

        Skill skill = skills[skillIndex];

        if ( skill == null) return;

        skill.ServerExecute(this, targetPoint, lockTargetPos);
    }


    //---SECTION RELATED TO SKILLS---
    public Skill GetSkill(int index)
    {
        if (index < 0 || index >= skills.Length)
        {
            return null;
        }
        if (index < 0 || index >= skills.Length) return null;
        return skills[index];
    }

    public bool HasResource(ResourceType type, float cost) => type switch
    {
        ResourceType.Stamina => PlayerStats.Instance.Stamina.CurrentValue >= cost,
        ResourceType.Mana => PlayerStats.Instance.Mana.CurrentValue >= cost,
        ResourceType.Health => PlayerStats.Instance.Health.CurrentValue >= cost,
        _ => true
    };

    public void ConsumeResource(ResourceType type, float cost)
    {
        switch (type)
        {
            case ResourceType.Stamina: PlayerStats.Instance.Stamina.Modify(-cost); break;
            case ResourceType.Mana: PlayerStats.Instance.Mana.Modify(-cost); break;
            case ResourceType.Health: PlayerStats.Instance.Health.Modify(-cost); break;
        }
    }

    public bool IsSkillReady(int index) => skillsCooldown[index] <= 0;
    public void TriggerCooldown(int index) => skillsCooldown[index] = skills[index].cooldown;

    //---TEMPORAL---
    [ClientRpc]
    public void ShowHitboxClientRpc(Vector3 center, Vector3 size, Quaternion rot)
    {
        GameObject box = GameObject.Instantiate(hitboxPrefab, center, rot);

        box.transform.localScale = size;

        Destroy(box, 0.2f);
    }

    public GameObject ShowHitboxPersistent(Vector3 center, Vector3 size, Quaternion rot, GameObject debugBox)
    {
        if (debugBox == null)
        {
            debugBox = Instantiate(hitboxPrefab);
        }

        debugBox.transform.SetPositionAndRotation(center, rot);
        debugBox.transform.localScale = size;

        return debugBox;
    }

    public void SetGravityMultiplier(float value)
    {
        currentGravityMultiplier = value;
    }

    // ── Daño y muerte ───────────────────────────────────────────────
    public void TakeDamage(float damage, ThrowType throwType, Vector3 hitDir, float stunDuration, bool keepOnAir, float airLift, float staggerBuild)
    {
        if (isDead) return;

        // Modifica la stat de vida. Se clampea a [0, Max] automáticamente.
        PlayerStats.Instance.Health.Modify(-damage);

        Debug.Log($"Player recibió {damage} de daño. Vida actual: {CurrentHealth}/{MaxHealth}");

        // El chequeo de muerte ocurre en Update() para no disparar aquí lógica pesada.
    }

    /// <summary>Se llama UNA VEZ cuando la vida llega a 0.</summary>
    private void Die()
    {
        isDead = true;
        Debug.Log("Player ha muerto");

        // Opciones de qué hacer aquí (elige según tu juego):
        // 1. Respawn en el último checkpoint
        // 2. Pantalla de game over
        // 3. Cargar la última partida guardada
        // De momento solo lo marcamos como muerto para no destruirlo.

        // Ejemplo: respawn en último checkpoint si existe
        if (CheckpointManager.Instance != null && CheckpointManager.Instance.activeCheckpoint != null)
        {
            Respawn();
        }
    }

    /// <summary>Resetea al jugador al último checkpoint activo.</summary>
    public void Respawn()
    {
        var cp = CheckpointManager.Instance.activeCheckpoint;
        if (cp == null || cp.spawnPoint == null)
        {
            Debug.LogWarning("No hay checkpoint activo para respawn.");
            return;
        }

        // Teletransportar al spawn point
        transform.position = cp.spawnPoint.position;
        rb.linearVelocity = Vector3.zero;

        // Restaurar vida al máximo
        PlayerStats.Instance.Health.SetCurrentValue(MaxHealth);

        isDead = false;
    }
}

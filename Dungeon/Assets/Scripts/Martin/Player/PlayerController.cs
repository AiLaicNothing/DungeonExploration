using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class PlayerController : NetworkBehaviour, IDamageable
{
    // ─────────────────────────────────────────
    // MOVEMENT
    // ─────────────────────────────────────────

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float rotSpeed = 10f;
    [SerializeField] private float moveMultiplier = 1f;
    [SerializeField] private LayerMask whatIsGround;

    [Header("Gravity")]
    [SerializeField] private float baseGravity = -9.81f;
    [SerializeField] private float fallGravityMultiplier = 2.5f;

    [Header("Dash")]
    [SerializeField] private float dashDistance = 5f;
    [SerializeField] private float dashDuration = 0.3f;
    [SerializeField] private float dashCost = 20f;

    private float currentGravityMultiplier = 1f;

    // ─────────────────────────────────────────
    // COMBAT
    // ─────────────────────────────────────────

    [Header("Combat")]
    [SerializeField] private bool isRange;
    [SerializeField] private BasicComboData basicComboData;
    [SerializeField] private BasicComboData airComboData;

    [Header("Range")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private ShootData shootData;

    // ─────────────────────────────────────────
    // SKILLS
    // ─────────────────────────────────────────

    [Header("Skills")]
    [SerializeField] private int maxSkillSlots = 4;

    [SerializeField]
    private Skill[] skills = new Skill[4];

    private float[] skillsCooldown;

    // ─────────────────────────────────────────
    // FLAGS
    // ─────────────────────────────────────────

    public bool isGrounded { get; private set; }
    public bool isPerformingAction = false;
    public bool blockVelocity = false;
    public bool hasUsedDash = false;
    public bool hasUsedAirAttack = false;
    public bool isDead { get; private set; }

    private bool startingHealthInitialized = false;

    // ─────────────────────────────────────────
    // REFERENCES
    // ─────────────────────────────────────────

    [Header("References")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private PlayerInputHandler input;
    [SerializeField] private Transform playerModel;
    [SerializeField] private LockOnTarget lockOnTarget;
    [SerializeField] private GameObject thirdCam;
    [SerializeField] private PlatformRider platformRider;
    [SerializeField] private PlayerStats stats;

    [Header("Attack VFX")]
    [SerializeField] private GameObject[] meleeAttackVfx;
    [SerializeField] private GameObject[] airAttackVfx;

    [Header("Debug")]
    public bool showHitBox;
    public GameObject hitboxPrefab;

    // ─────────────────────────────────────────
    // PROPERTIES
    // ─────────────────────────────────────────

    public Rigidbody Rb => rb;
    public Transform PlayerModel => playerModel;
    public LockOnTarget LockTarget => lockOnTarget;
    public bool IsRange => isRange;
    public Transform FirePoint => firePoint;
    public PlayerInputHandler Input => input;
    public BasicComboData ComboData => basicComboData;
    public BasicComboData AirComboData => airComboData;
    public ShootData ShootData => shootData;
    public PlayerStats Stats => stats;
    public float FallGravityMultiplier => fallGravityMultiplier;
    public float DashDistance => dashDistance;
    public float DashDuration => dashDuration;
    public float DashCost => dashCost;

    public float MaxHealth => stats.Health.Max;
    public float CurrentHealth => stats.Health.CurrentValue;

    public float MaxMana => stats.Mana.Max;
    public float CurrentMana => stats.Mana.CurrentValue;

    public float MaxStamina => stats.Stamina.Max;
    public float CurrentStamina => stats.Stamina.CurrentValue;

    // ─────────────────────────────────────────
    // STATE MACHINE
    // ─────────────────────────────────────────

    private PlayerStateMachine movementSM;
    private PlayerStateMachine actionSM;

    public PlayerIddle_State iddle_State;
    public PlayerMove_State move_State;
    public PlayerJump_State jump_State;
    public PlayerFall_State fall_State;

    public PlayerIddleAction iddeAction_State;
    public PlayerBasicAttack basicAttack_State;
    public PlayerAirAttack airAttack_State;
    public PlayerShoot shoot_State;
    public PlayerDash dash_State;
    public PlayerSkill skill_State;

    // ─────────────────────────────────────────
    // CAMERA
    // ─────────────────────────────────────────

    private Camera _cachedCam;

    private Camera MainCam
    {
        get
        {
            if (_cachedCam == null)
                _cachedCam = Camera.main;

            return _cachedCam;
        }
    }

    // ─────────────────────────────────────────
    // UNITY
    // ─────────────────────────────────────────

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

        skillsCooldown = new float[maxSkillSlots];
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log(
            $"[PLAYER CONTROLLER] Spawn " +
            $"Name={gameObject.name} " +
            $"NetId={NetworkObjectId} " +
            $"Owner={OwnerClientId} " +
            $"IsOwner={IsOwner} " +
            $"IsServer={IsServer}"
        );

        var inventory = GetComponent<PlayerSkillInventory>();

        if (inventory != null)
        {
            inventory.OnSkillsChanged += RefreshSkillsFromInventory;
        }

        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (input == null)
            input = GetComponent<PlayerInputHandler>();

        if (lockOnTarget == null)
            lockOnTarget = GetComponent<LockOnTarget>();

        if (platformRider == null)
            platformRider = GetComponent<PlatformRider>();

        if (stats == null)
            stats = GetComponent<PlayerStats>();

        LoadEquippedSkillsFromInventory();

        if (IsOwner)
        {
            Debug.Log(
                $"[PLAYER CONTROLLER] Registering LocalPlayer " +
                $"Owner={OwnerClientId} " +
                $"NetId={NetworkObjectId}"
            );

            if (thirdCam != null)
                thirdCam.SetActive(true);

            LocalPlayer.RegisterLocalPlayer(this);
        }
        else
        {
            if (thirdCam != null)
                thirdCam.SetActive(false);
        }

        if (stats != null)
        {
            StartCoroutine(InitializeStartingHealthWhenReady());
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
            LocalPlayer.UnregisterLocalPlayer();
    }

    private void Start()
    {
        Debug.Log(
            $"[PLAYER START] " +
            $"Name={name} " +
            $"Owner={OwnerClientId} " +
            $"IsOwner={IsOwner}"
        );

        if (!IsOwner)
        {
            Debug.Log(
                $"[PLAYER START] ABORTED (not owner)"
            );

            return;
        }

        Debug.Log(
            $"[PLAYER START] Initializing state machines"
        );

        movementSM.Initialize(iddle_State);
        actionSM.Initialize(iddeAction_State);
    }

    private void Update()
    {
        if (!IsOwner)
            return;

        Debug.Log(
            $"[PLAYER UPDATE] " +
            $"Owner={OwnerClientId} " +
            $"Ready={startingHealthInitialized}"
        );

        if (!startingHealthInitialized)
            return;

        if (!isDead && CurrentHealth <= 0)
        {
            Die();
        }

        if (!isDead)
        {
            CheckGround();

            if (!UIBlockingManager.IsAnyUIOpen)
            {
                movementSM.Update();
                actionSM.Update();
            }
        }

        UpdateCooldowns();
    }

    private void FixedUpdate()
    {
        if (!IsOwner)
            return;

        if (!startingHealthInitialized)
            return;

        if (isDead)
            return;

        if (UIBlockingManager.IsAnyUIOpen)
        {
            rb.linearVelocity = new Vector3(
                0,
                rb.linearVelocity.y,
                0
            );

            return;
        }

        ApplyGravity();

        if (!isPerformingAction)
        {
            Movement();
        }

        if (blockVelocity)
        {
            rb.linearVelocity = Vector3.zero;
        }

        movementSM.FixedUpdate();
        actionSM.FixedUpdate();
    }

    // ─────────────────────────────────────────
    // COOLDOWNS
    // ─────────────────────────────────────────

    private void UpdateCooldowns()
    {
        for (int i = 0; i < skillsCooldown.Length; i++)
        {
            if (skillsCooldown[i] > 0f)
            {
                skillsCooldown[i] -= Time.deltaTime;
            }
        }
    }

    // ─────────────────────────────────────────
    // GRAVITY
    // ─────────────────────────────────────────

    private void ApplyGravity()
    {
        if (rb == null) return;

        rb.AddForce(Vector3.up * baseGravity * currentGravityMultiplier, ForceMode.Acceleration);
    }

    public void SetGravityMultiplier(float value)
    {
        currentGravityMultiplier = value;
    }

    // ─────────────────────────────────────────
    // MOVEMENT
    // ─────────────────────────────────────────

    private void Movement()
    {
        Vector2 inputDir = input.moveInput.normalized;

        var cam = MainCam;

        if (cam == null) return;

        Vector3 camForward = cam.transform.forward;
        camForward.y = 0f;
        camForward.Normalize();

        Vector3 camRight = cam.transform.right;
        camRight.y = 0f;
        camRight.Normalize();

        Vector3 moveDir = camForward * inputDir.y + camRight * inputDir.x;

        Vector3 velocity = moveDir * moveSpeed * moveMultiplier;

        velocity.y = rb.linearVelocity.y;

        if (platformRider != null && platformRider.IsOnPlatform)
        {
            velocity += platformRider.CurrentPlatformVelocity;
        }

        rb.linearVelocity = velocity;

        HandleRotation(moveDir);
    }

    public void Jump()
    {
        if (!isGrounded) return;

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        rb.AddForce( Vector3.up * jumpForce, ForceMode.Impulse);
    }

    private void HandleRotation(Vector3 moveDir)
    {
        Vector3 lookDir;

        if ( input.isAiming ||(lockOnTarget != null && lockOnTarget.isTargeting)
        )
        {
            var cam = MainCam;

            if (cam == null) return;

            lookDir = cam.transform.forward;
            lookDir.y = 0f;
            lookDir.Normalize();
        }
        else
        {
            if (moveDir.magnitude < 0.1f) return;

            lookDir = moveDir;
        }

        Quaternion targetRotation = Quaternion.LookRotation(lookDir);

        playerModel.rotation = Quaternion.Slerp( playerModel.rotation, targetRotation, rotSpeed * Time.deltaTime);
    }

    private void CheckGround()
    {
        bool previousGrounded = isGrounded;

        isGrounded = Physics.Raycast(groundCheck.position, Vector3.down,1.1f, whatIsGround);

        if (!previousGrounded && isGrounded)
        {
            hasUsedAirAttack = false;
            hasUsedDash = false;
        }
    }

    // ─────────────────────────────────────────
    // CAMERA
    // ─────────────────────────────────────────

    public Vector3 GetViewPoint()
    {
        var cam = MainCam;

        if (cam == null)  return transform.position;

        Ray ray = cam.ScreenPointToRay( new Vector3(  Screen.width / 2f, Screen.height / 2f));

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            return hit.point;
        }

        return ray.origin + ray.direction * 100f;
    }

    public Vector3 GetAimPoint(float maxRange, LayerMask groundLayer)
    {
        var cam = MainCam;

        if (cam == null) return transform.position;

        Ray ray =  cam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f));

        if (Physics.Raycast( ray, out RaycastHit hit, maxRange, groundLayer))
        {
            return hit.point;
        }

        return ray.origin + ray.direction * maxRange;
    }

    // ─────────────────────────────────────────
    // SKILL INVENTORY
    // ─────────────────────────────────────────

    private void LoadEquippedSkillsFromInventory()
    {
        PlayerSkillInventory inventory =
            GetComponent<PlayerSkillInventory>();

        if (inventory == null)
        {
            Debug.LogWarning(
                "[PlayerController] No PlayerSkillInventory found."
            );

            return;
        }

        for (int i = 0; i < maxSkillSlots; i++)
        {
            skills[i] = inventory.GetEquippedSkill(i);
        }

        Debug.Log(
            "[PlayerController] Equipped skills loaded."
        );
    }

    public void RefreshSkillsFromInventory()
    {
        LoadEquippedSkillsFromInventory();
    }

    // ─────────────────────────────────────────
    // STATE MACHINE
    // ─────────────────────────────────────────

    public void ChangeState(PlayerStates nextState)
    {
        movementSM.ChangeState(nextState);
    }

    public void ChangeActionState(PlayerStates nextState)
    {
        actionSM.ChangeState(nextState);
    }

    // ─────────────────────────────────────────
    // MELEE ATTACK
    // ─────────────────────────────────────────

    public void RequestMeleeAttack(int comboIndex, bool isAirAttack )
    {
        MeleeAttackServerRpc(comboIndex, isAirAttack);
    }

    [ServerRpc]
    private void MeleeAttackServerRpc(int comboIndex, bool isGrounded)
    {
        AttackSteps attack = isGrounded ? ComboData.attackSteps[comboIndex] : airComboData.attackSteps[comboIndex];

        Vector3 center = PlayerModel.transform.position + PlayerModel.transform.forward * attack.hitBoxOffSet.z + Vector3.up * attack.hitBoxOffSet.y;

        Collider[] hits = Physics.OverlapBox(center, attack.hitBoxSize * 0.5f, PlayerModel.transform.rotation);

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                IDamageable damageable = hit.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    Vector3 hitDir = PlayerModel.transform.forward;
                    damageable.TakeDamage((Stats.PhysicalDamage.CurrentValue * attack.hitData.physicalScale) + (Stats.MagicalDamage.CurrentValue * attack.hitData.magicalScale), attack.hitData.throwType, hitDir, attack.hitData.stunDuration, attack.hitData.keepInAir, attack.hitData.airLiftForce, attack.hitData.staggerCharge);
                }
                Debug.Log($"[Server] Player {OwnerClientId} hit {hit.name}");
            }
        }
        if (showHitBox)
        {
            ShowHitboxClientRpc(center, attack.hitBoxSize, PlayerModel.rotation);
        }
    }
    // ─────────────────────────────────────────
    // SHOOT
    // ─────────────────────────────────────────

    public void RequestShoot(Vector3 spawnPosition, Vector3 direction)
    {
        if (!IsOwner) return;

        RequestShootServerRpc( spawnPosition, direction);
    }

    [ServerRpc]
    private void RequestShootServerRpc(Vector3 spawnPosition, Vector3 direction)
    {
        SpawnProjectile( spawnPosition, direction);

        ShootClientRpc( spawnPosition, direction);
    }

    [ClientRpc]
    private void ShootClientRpc( Vector3 spawnPosition, Vector3 direction)
    {
        if (IsOwner) return;

        SpawnProjectile(spawnPosition, direction);
    }

    private void SpawnProjectile( Vector3 spawnPosition, Vector3 direction)
    {
        if (shootData == null) return;

        if (shootData.proyectilePrefab == null) return;

        GameObject projectile =Instantiate(shootData.proyectilePrefab, spawnPosition, Quaternion.LookRotation(direction));

        Rigidbody projectileRb =  projectile.GetComponent<Rigidbody>();

        if (projectileRb != null)
        {
            projectileRb.linearVelocity = direction.normalized * shootData.proyectileSpeed;
        }
    }

    // ─────────────────────────────────────────
    // ATTACK VFX
    // ─────────────────────────────────────────

    public void RequestAttackVfx(int comboIndex, bool isGrounded)
    {
        RequestAttackVfxServerRpc(comboIndex, isGrounded);
    }

    [ServerRpc]
    private void RequestAttackVfxServerRpc(int comboIndex, bool isGrounded)
    {
        // Server tells all clients to play the VFX
        PlayAttackVfxClientRpc(comboIndex, isGrounded);
    }

    [ClientRpc]
    public void PlayAttackVfxClientRpc(int comboIndex, bool isGrounded)
    {
        if (IsOwner) return;

        SpawnAttackVfx(comboIndex, isGrounded);
    }

    // local-only VFX spawn, used by the owner instantly
    public void PlayAttackVfxLocal(int comboIndex, bool isGrounded)
    {
        SpawnAttackVfx(comboIndex, isGrounded);
    }

    // shared VFX spawn logic
    private void SpawnAttackVfx(int comboIndex, bool isGrounded)
    {
        AttackSteps attack = isGrounded ? ComboData.attackSteps[comboIndex]: AirComboData.attackSteps[comboIndex];

        if (attack == null || attack.attackVfx == null) return;

        StartCoroutine(SpawnAttackVfxRoutine(attack));
    }

    // handles delay + lifetime per attack step
    private IEnumerator SpawnAttackVfxRoutine(AttackSteps attack)
    {
        if (attack.vfxSpawnTime > 0f) yield return new WaitForSeconds(attack.vfxSpawnTime);

        Transform root = PlayerModel != null ? PlayerModel : transform;

        Vector3 spawnPos = root.TransformPoint(attack.vfxOffset);

        Quaternion spawnRot = root.rotation * Quaternion.Euler(attack.vfxRotOffset);

        GameObject vfx = Instantiate(attack.attackVfx, spawnPos, spawnRot);

        if (attack.vfxDuration > 0f) Destroy(vfx, attack.vfxDuration);
    }

    // ─────────────────────────────────────────
    // SKILLS
    // ─────────────────────────────────────────

    public void RequestSkill( int skillIndex, Vector3 targetPoint)
    {
        if (!IsOwner) return;

        Vector3 lockTargetPos = Vector3.zero;

        if (lockOnTarget != null && lockOnTarget.isTargeting && lockOnTarget.CurrentTarget != null)
        {
            lockTargetPos = lockOnTarget.CurrentTarget.position;
        }

        UseSkillServerRpc( skillIndex, targetPoint, lockTargetPos);
    }

    [ServerRpc]
    private void UseSkillServerRpc(int skillIndex, Vector3 targetPoint, Vector3 lockTargetPos)
    {
        if (skillIndex < 0 || skillIndex >= skills.Length)
        {
            return;
        }

        Skill skill = skills[skillIndex];

        if (skill == null)
        {
            Debug.LogWarning($"[PlayerController] Skill NULL in slot {skillIndex}");

            return;
        }

        if (!IsSkillReady(skillIndex))
        {
            Debug.Log($"[PlayerController] Skill cooldown active.");

            return;
        }

        if (!HasResource(skill.resourceType, skill.cost))
        {
            Debug.Log($"[PlayerController] Not enough resource.");

            return;
        }

        ConsumeResource(skill.resourceType,skill.cost);

        TriggerCooldown(skillIndex);

        skill.ServerExecute(this, targetPoint, lockTargetPos);
    }

    public Skill GetSkill(int index)
    {
        if (index < 0 || index >= skills.Length
        )
        {
            return null;
        }

        return skills[index];
    }

    public bool IsSkillReady(int index)
    {
        if (index < 0 || index >= skillsCooldown.Length)
        {
            return false;
        }

        return skillsCooldown[index] <= 0f;
    }

    public void TriggerCooldown(int index)
    {
        if (index < 0 ||  index >= skills.Length)
        {
            return;
        }

        if (skills[index] == null)
            return;

        skillsCooldown[index] = skills[index].cooldown;
    }

    // ─────────────────────────────────────────
    // RESOURCES
    // ─────────────────────────────────────────

    public bool HasResource(ResourceType type,float cost)
    {
        return stats.HasResource(type, cost);
    }

    public void ConsumeResource(
        ResourceType type,
        float cost
    )
    {
        switch (type)
        {
            case ResourceType.Health:
                stats.Health.Modify(-cost);
                break;

            case ResourceType.Mana:
                stats.Mana.Modify(-cost);
                break;

            case ResourceType.Stamina:
                stats.Stamina.Modify(-cost);
                break;
        }
    }

    public bool HasStamina(float cost)
    {
        return stats.Stamina.CurrentValue >= cost;
    }

    public void ConsumeStamina(float cost)
    {
        stats.Stamina.Modify(-cost);
    }

    public bool HasMana(float cost)
    {
        return stats.Mana.CurrentValue >= cost;
    }

    public void ConsumeMana(float cost)
    {
        stats.Mana.Modify(-cost);
    }

    // ─────────────────────────────────────────
    // HEALTH
    // ─────────────────────────────────────────

    public bool IsAlive => CurrentHealth > 0f;

    public void Heal(float amount)
    {
        if (isDead)
            return;

        stats.Health.Modify(amount);
    }

    private IEnumerator InitializeStartingHealthWhenReady()
    {
        Debug.Log(
            $"[HEALTH INIT] Started Owner={OwnerClientId}"
        );

        while (
            stats == null ||
            !stats.IsStatsReady ||
            stats.Health.Max <= 0f
        )
        {
            Debug.Log(
                $"[HEALTH INIT] Waiting..." +
                $" StatsNull={stats == null}" +
                $" Ready={(stats != null ? stats.IsStatsReady : false)}" +
                $" Max={(stats != null ? stats.Health.Max : -1)}"
            );

            yield return new WaitForSeconds(1f);
        }

        Debug.Log(
            $"[HEALTH INIT] Finished"
        );

        if (stats.Health.CurrentValue <= 0f)
        {
            stats.Health.SetCurrentValue(
                stats.Health.Max
            );
        }

        startingHealthInitialized = true;
    }

    // ─────────────────────────────────────────
    // DAMAGE
    // ─────────────────────────────────────────

    public void TakeDamage( float damage, ThrowType throwType, Vector3 hitDir, float stunDuration, bool keepOnAir, float airLift, float staggerBuild)
    {
        if (isDead)  return;

        if (IsServer)
        {
            stats.Health.Modify(-damage);
        }
        else
        {
            TakeDamageServerRpc(damage);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void TakeDamageServerRpc(float damage)
    {
        if (isDead)
            return;

        stats.Health.Modify(-damage);
    }

    // ─────────────────────────────────────────
    // DEATH
    // ─────────────────────────────────────────

    private void Die()
    {
        isDead = true;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
        }

        Respawn();
    }

    public void Respawn()
    {
        var data =
            GetComponent<PlayerCheckpointData>();

        if (data == null)
            return;

        string lastCheckpoint =
            data.LastUsedCheckpoint.Value.ToString();

        if (string.IsNullOrEmpty(lastCheckpoint))
            return;

        RequestTeleportToCheckpoint(lastCheckpoint);
    }

    public void RequestTeleportToCheckpoint(
        string checkpointName
    )
    {
        if (IsServer)
        {
            TeleportToCheckpoint_Server(checkpointName);
        }
        else
        {
            TeleportToCheckpointServerRpc(
                checkpointName
            );
        }
    }
    public void TeleportToPosition(Vector3 position)
    {
        if (IsServer)
        {
            TeleportToPosition_Server(position);
        }
        else
        {
            TeleportToPositionServerRpc(position);
        }
    }
    [ServerRpc(RequireOwnership = false)]
    private void TeleportToPositionServerRpc(Vector3 position)
    {
        TeleportToPosition_Server(position);
    }
    private void TeleportToPosition_Server(Vector3 position)
    {
        ApplyTeleport(position);

        ForceTeleportClientRpc(position);
    }
    [ServerRpc]
    private void TeleportToCheckpointServerRpc(
        string checkpointName
    )
    {
        TeleportToCheckpoint_Server(checkpointName);
    }

    private void TeleportToCheckpoint_Server(
        string checkpointName
    )
    {
        if (CheckpointManager.Instance == null)
            return;

        var cp =
            CheckpointManager.Instance.GetByName(
                checkpointName
            );

        if (
            cp == null ||
            cp.spawnPoint == null
        )
        {
            return;
        }

        Vector3 targetPos =
            cp.spawnPoint.position;

        ApplyTeleport(targetPos);

        ForceTeleportClientRpc(targetPos);

        if (isDead)
        {
            stats.Health.SetCurrentValue(MaxHealth);
            isDead = false;
        }

        TeleportFinishedClientRpc();
    }

    private void ApplyTeleport(Vector3 pos)
    {
        CharacterController cc =
            GetComponent<CharacterController>();

        NetworkTransform nt =
            GetComponent<NetworkTransform>();

        if (cc != null)
            cc.enabled = false;

        if (nt != null)
            nt.enabled = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = pos;
        }

        transform.position = pos;

        Physics.SyncTransforms();

        if (nt != null)
            nt.enabled = true;

        if (cc != null)
            cc.enabled = true;
    }

    [ClientRpc]
    private void ForceTeleportClientRpc(Vector3 pos)
    {
        ApplyTeleport(pos);
    }

    [ClientRpc]
    private void TeleportFinishedClientRpc()
    {
        isDead = false;
    }

    // ─────────────────────────────────────────
    // DEBUG
    // ─────────────────────────────────────────

    [ClientRpc]
    public void ShowHitboxClientRpc(
        Vector3 center,
        Vector3 size,
        Quaternion rot
    )
    {
        if (hitboxPrefab == null)
            return;

        GameObject box =
            Instantiate(
                hitboxPrefab,
                center,
                rot
            );

        box.transform.localScale = size;

        Destroy(box, 0.2f);
    }

    public GameObject ShowHitboxPersistent(
        Vector3 center,
        Vector3 size,
        Quaternion rot,
        GameObject debugBox
    )
    {
        if (hitboxPrefab == null)
            return null;

        if (debugBox == null)
        {
            debugBox = Instantiate(hitboxPrefab);
        }

        debugBox.transform.SetPositionAndRotation(
            center,
            rot
        );

        debugBox.transform.localScale = size;

        return debugBox;
    }
}


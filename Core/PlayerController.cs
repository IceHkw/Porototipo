using UnityEngine;
using System;

public class PlayerController : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField] private KeyCode movementAbilityKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode ultimateAbilityKey = KeyCode.Q;
    [SerializeField] private KeyCode attackKey = KeyCode.Mouse0;

    [Header("Core References")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private ClickAttackSpawner attackSpawner;

    public IMovementAbility MovementAbility { get; private set; }
    public IUltimateAbility UltimateAbility { get; private set; }
    public bool HasMovementAbility => MovementAbility != null;
    public bool HasUltimateAbility => UltimateAbility != null;

    private bool isAbilityActive => (HasMovementAbility && MovementAbility.IsActive) || (HasUltimateAbility && UltimateAbility.IsActive);
    private bool inputEnabled = true;

    // Eventos públicos
    public event Action<IMovementAbility> OnMovementAbilityChanged;
    public event Action<IUltimateAbility> OnUltimateAbilityChanged;
    public event Action OnMovementAbilityUsed;
    public event Action OnUltimateAbilityUsed;

    void Awake()
    {
        InitializeComponents();
        DetectAbilities();
        SubscribeToEvents();
    }

    void Update()
    {
        if (!inputEnabled || (playerStats != null && playerStats.EstaMuerto)) return;

        // El PlayerController deshabilita el movimiento durante las habilidades,
        // y ClickAttackSpawner lo gestiona durante los ataques.
        if (playerMovement != null)
        {
            playerMovement.SetControlHabilitado(!isAbilityActive);
        }

        // --- CORRECCIÓN AQUÍ ---
        // Usamos la nueva propiedad 'IsAttacking' del ClickAttackSpawner.
        if (!isAbilityActive && (attackSpawner == null || !attackSpawner.IsAttacking))
        {
            HandleAttackInputs();
            HandleAbilityInputs();
        }
    }

    private void HandleAttackInputs()
    {
        if (attackSpawner != null && Input.GetKeyDown(attackKey))
        {
            // El spawner ahora se encarga de la lógica de si puede atacar o no.
            attackSpawner.HandleAttackInput();
        }
    }

    private void HandleAbilityInputs()
    {
        // Solo podemos usar habilidades si no estamos atacando.
        if (attackSpawner != null && attackSpawner.IsAttacking) return;

        if (HasMovementAbility && Input.GetKeyDown(movementAbilityKey))
        {
            if (MovementAbility.TryActivate())
            {
                OnMovementAbilityUsed?.Invoke();
            }
        }

        if (HasUltimateAbility && Input.GetKeyDown(ultimateAbilityKey))
        {
            if (UltimateAbility.TryActivate())
            {
                OnUltimateAbilityUsed?.Invoke();
            }
        }
    }

    private void HandlePlayerDeath()
    {
        inputEnabled = false;
        MovementAbility?.OnPlayerDeath();
        UltimateAbility?.OnPlayerDeath();
    }

    public void OnPlayerRespawn()
    {
        inputEnabled = true;
        MovementAbility?.OnPlayerRespawn();
        UltimateAbility?.OnPlayerRespawn();
    }

    #region Initialization
    void InitializeComponents()
    {
        playerStats = GetComponent<PlayerStats>();
        playerMovement = GetComponent<PlayerMovement>();
        attackSpawner = GetComponent<ClickAttackSpawner>();

        if (playerStats == null || playerMovement == null || attackSpawner == null)
        {
            Debug.LogError("[PlayerController] Faltan componentes esenciales (Stats, Movement, AttackSpawner)!", this);
            enabled = false;
        }
    }

    void DetectAbilities()
    {
        MovementAbility = GetComponent<IMovementAbility>();
        if (HasMovementAbility) OnMovementAbilityChanged?.Invoke(MovementAbility);

        UltimateAbility = GetComponent<IUltimateAbility>();
        if (HasUltimateAbility) OnUltimateAbilityChanged?.Invoke(UltimateAbility);
    }

    void SubscribeToEvents()
    {
        if (playerStats != null)
        {
            playerStats.OnPlayerDeath += HandlePlayerDeath;
        }
    }

    void OnDestroy()
    {
        if (playerStats != null)
        {
            playerStats.OnPlayerDeath -= HandlePlayerDeath;
        }
    }
    #endregion

    #region Public Methods
    public void SetInputEnabled(bool enabled) => inputEnabled = enabled;
    public void AddUltimateCharge(float amount) => UltimateAbility?.AddCharge(amount);
    public void ResetAllCooldowns()
    {
        MovementAbility?.ResetCooldown();
        UltimateAbility?.ResetCharge();
    }
    #endregion
}
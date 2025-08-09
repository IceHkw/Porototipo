// Player/Core/PlayerController.cs
using UnityEngine;
using Debug = UnityEngine.Debug; // Resolución explícita para evitar ambigüedad
using UnityEngine.InputSystem;
using System;
using System.Diagnostics;

public class PlayerController : MonoBehaviour
{
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
    private Vector2 moveInput; // Guardaremos el input de movimiento aquí

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

    // El método Update ahora está mucho más limpio
    void Update()
    {
        if (!inputEnabled || (playerStats != null && playerStats.EstaMuerto))
        {
            playerMovement?.SetControlHabilitado(false);
            return;
        }

        // --- LÍNEA MODIFICADA ---
        // Se ha eliminado la condición que comprobaba si el jugador estaba atacando.
        playerMovement?.SetControlHabilitado(!isAbilityActive);
    }

    #region Input Event Handlers
    // --- ESTOS SON LOS MÉTODOS QUE APARECERÁN EN EL EDITOR ---

    public void OnMove(InputAction.CallbackContext context)
    {
        // Leemos el valor Vector2 de la acción de movimiento y lo guardamos
        moveInput = context.ReadValue<Vector2>();
        playerMovement?.SetMoveInput(moveInput);
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        // Pasamos el contexto directamente al método que ya tenías en PlayerMovement
        playerMovement?.HandleJumpInput(context);
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        // Solo atacamos cuando el botón se presiona (performed)
        if (context.performed && attackSpawner != null)
        {
            // Le pasamos el input de movimiento que ya tenemos guardado
            attackSpawner.HandleAttackInput(moveInput);
        }
    }

    public void OnMovementAbility(InputAction.CallbackContext context)
    {
        if (context.performed && HasMovementAbility)
        {
            if (MovementAbility.TryActivate())
            {
                OnMovementAbilityUsed?.Invoke();
            }
        }
    }

    public void OnUltimateAbility(InputAction.CallbackContext context)
    {
        if (context.performed && HasUltimateAbility)
        {
            if (UltimateAbility.TryActivate())
            {
                OnUltimateAbilityUsed?.Invoke();
            }
        }
    }

    #endregion

    private void HandlePlayerDeath()
    {
        inputEnabled = false;
        playerMovement?.SetMoveInput(Vector2.zero); // Detener el movimiento al morir
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
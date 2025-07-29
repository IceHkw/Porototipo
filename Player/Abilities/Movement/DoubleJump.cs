using UnityEngine;
using System;

public class DoubleJumpAbility : MonoBehaviour, IMovementAbility
{
    [Header("Configuración del Doble Salto")]
    [SerializeField] private float fuerzaDobleJalto = 15f;
    [SerializeField] private bool habilitado = true; // El valor inicial ahora se usa aquí

    [Header("Efectos Visuales")]
    [SerializeField] private GameObject efectoDobleJalto;
    [SerializeField] private bool crearEfectoAlSaltar = false;

    [Header("UI")]
    [SerializeField] private Sprite abilityIcon;

    private PlayerMovement playerMovement;
    private PlayerAnimatorController animatorController;
    private Rigidbody2D rb;
    private GroundChecker groundChecker;

    private bool segundoSaltoDisponible = false;
    private bool esperandoSoltarBoton = false;

    // --- IMPLEMENTACIÓN DE INTERFAZ CORREGIDA ---
    public string AbilityName => "Double Jump";
    public Sprite AbilityIcon => abilityIcon;
    public bool IsReady => Habilitado && segundoSaltoDisponible;
    public bool IsActive => false;
    public float CurrentCooldown => 0f;
    public float MaxCooldown => 0f;

    public event Action<float, float, float> OnCooldownChanged;

    // --- PROPIEDAD AÑADIDA ---
    public bool Habilitado { get; private set; }

    void Awake() // Cambiado a Awake para asegurar que se inicialice antes que Start
    {
        Habilitado = habilitado; // Asignar el valor inicial
        InitializeComponents();
        SubscribeToEvents();
    }

    void InitializeComponents()
    {
        playerMovement = GetComponent<PlayerMovement>();
        animatorController = GetComponentInChildren<PlayerAnimatorController>(); // Mejor buscar en hijos también
        rb = GetComponent<Rigidbody2D>();

        if (playerMovement != null)
        {
            groundChecker = playerMovement.GroundChecker;
        }
    }

    void SubscribeToEvents()
    {
        if (groundChecker != null)
        {
            groundChecker.OnGroundEntered += ResetDoubleJump;
            groundChecker.OnGroundExited += EnableDoubleJump;
        }
    }

    void Update()
    {
        if (!Habilitado) return;

        if (esperandoSoltarBoton && !Input.GetButton("Jump"))
        {
            esperandoSoltarBoton = false;
        }

        if (Input.GetButtonDown("Jump") && CanPerformDoubleJump())
        {
            PerformDoubleJump();
        }
    }

    bool CanPerformDoubleJump()
    {
        return segundoSaltoDisponible &&
               groundChecker != null && !groundChecker.EnSuelo &&
               !esperandoSoltarBoton;
    }

    void PerformDoubleJump()
    {
        segundoSaltoDisponible = false;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, fuerzaDobleJalto);

        if (animatorController != null)
        {
            animatorController.TriggerDobleSalto();
        }

        if (crearEfectoAlSaltar && efectoDobleJalto != null)
        {
            Instantiate(efectoDobleJalto, transform.position, Quaternion.identity);
        }
    }

    void ResetDoubleJump()
    {
        segundoSaltoDisponible = false;
        esperandoSoltarBoton = false;
    }

    void EnableDoubleJump()
    {
        if (Input.GetButton("Jump"))
        {
            esperandoSoltarBoton = true;
        }
        segundoSaltoDisponible = true;
    }

    public bool TryActivate()
    {
        if (CanPerformDoubleJump())
        {
            PerformDoubleJump();
            return true;
        }
        return false;
    }

    public void ForceStop() { }
    public void ResetCooldown() { }

    public void OnPlayerDeath()
    {
        ResetDoubleJump();
        Habilitado = false;
    }

    public void OnPlayerRespawn()
    {
        Habilitado = true;
        ResetDoubleJump();
    }

    void OnDestroy()
    {
        if (groundChecker != null)
        {
            groundChecker.OnGroundEntered -= ResetDoubleJump;
            groundChecker.OnGroundExited -= EnableDoubleJump;
        }
    }
}
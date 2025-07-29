using UnityEngine;

public class PlayerAnimatorController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private Rigidbody2D rb;

    [Header("Configuración")]
    [SerializeField] private bool buscarAnimatorEnHijos = true;
    [SerializeField] private float umbralVelocidadMinima = 0.1f;

    // Hashes de animación
    private int hashVelocidadX, hashVelocidadY, hashEstaEnSuelo, hashEstaMuerto, hashEstaCorriendo;
    private int hashEstaSaltando, hashEstaCayendo, hashUsandoHabilidad, hashTipoHabilidad, hashUsandoUltimate;
    private int hashAtaqueHorizontal, hashAtaqueArriba, hashAtaqueAbajo, hashComboStep;
    private int hashDobleSalto, hashIsDashing, hashChargingUltimate, hashReleasingUltimate;
    private int hashIsAttacking; // Hash para el nuevo booleano

    private bool inicializado = false;

    void Awake()
    {
        InicializarComponentes();
        InicializarHashesAnimacion();
        ValidarComponentes();
    }

    void Update()
    {
        if (!inicializado) return;
        ActualizarParametrosMovimiento();
        ActualizarParametrosVida();
    }

    #region Initialization
    void InicializarComponentes()
    {
        if (animator == null)
        {
            if (buscarAnimatorEnHijos)
                animator = GetComponentInChildren<Animator>();
            else
                animator = GetComponent<Animator>();
        }

        if (playerMovement == null) playerMovement = GetComponentInParent<PlayerMovement>();
        if (playerStats == null) playerStats = GetComponentInParent<PlayerStats>();
        if (rb == null) rb = GetComponentInParent<Rigidbody2D>();
    }

    void InicializarHashesAnimacion()
    {
        if (animator == null) return;

        hashVelocidadX = Animator.StringToHash("VelocidadX");
        hashVelocidadY = Animator.StringToHash("VelocidadY");
        hashEstaEnSuelo = Animator.StringToHash("EstaEnSuelo");
        hashEstaMuerto = Animator.StringToHash("EstaMuerto");
        hashEstaCorriendo = Animator.StringToHash("EstaCorriendo");
        hashEstaSaltando = Animator.StringToHash("EstaSaltando");
        hashEstaCayendo = Animator.StringToHash("EstaCayendo");
        hashAtaqueHorizontal = Animator.StringToHash("AtaqueHorizontal");
        hashAtaqueArriba = Animator.StringToHash("AtaqueArriba");
        hashAtaqueAbajo = Animator.StringToHash("AtaqueAbajo");
        hashComboStep = Animator.StringToHash("ComboStep");
        hashUsandoHabilidad = Animator.StringToHash("UsandoHabilidad");
        hashTipoHabilidad = Animator.StringToHash("TipoHabilidad");
        hashUsandoUltimate = Animator.StringToHash("UsandoUltimate");
        hashDobleSalto = Animator.StringToHash("DobleSalto");
        hashIsDashing = Animator.StringToHash("IsDashing");
        hashChargingUltimate = Animator.StringToHash("ChargingUltimate");
        hashReleasingUltimate = Animator.StringToHash("ReleasingUltimate");
        hashIsAttacking = Animator.StringToHash("IsAttacking"); // Inicializa el nuevo hash
    }

    void ValidarComponentes()
    {
        inicializado = animator != null && playerMovement != null && rb != null;
        if (!inicializado)
        {
            Debug.LogWarning($"[PlayerAnimatorController] Faltan componentes necesarios.");
        }
    }
    #endregion

    #region Update Parameters
    void ActualizarParametrosMovimiento()
    {
        float velocidadX = Mathf.Abs(rb.linearVelocity.x);
        float velocidadY = rb.linearVelocity.y;
        animator.SetFloat(hashVelocidadX, velocidadX);
        animator.SetFloat(hashVelocidadY, velocidadY);
        animator.SetBool(hashEstaEnSuelo, playerMovement.EstaEnSuelo);
        animator.SetBool(hashEstaCorriendo, velocidadX > umbralVelocidadMinima && playerMovement.EstaEnSuelo);
        animator.SetBool(hashEstaSaltando, velocidadY > umbralVelocidadMinima && !playerMovement.EstaEnSuelo);
        animator.SetBool(hashEstaCayendo, velocidadY < -umbralVelocidadMinima && !playerMovement.EstaEnSuelo);
    }

    void ActualizarParametrosVida()
    {
        if (playerStats != null)
        {
            animator.SetBool(hashEstaMuerto, playerStats.EstaMuerto);
        }
    }
    #endregion

    #region Public Methods for Attacks and Abilities
    public void IniciarAnimacionAtaqueHorizontal(int comboStep)
    {
        if (!inicializado) return;
        animator.SetInteger(hashComboStep, comboStep);
        animator.SetTrigger(hashAtaqueHorizontal);
    }

    public void IniciarAnimacionAtaqueArriba()
    {
        if (!inicializado) return;
        animator.SetTrigger(hashAtaqueArriba);
    }

    public void IniciarAnimacionAtaqueAbajo()
    {
        if (!inicializado) return;
        animator.SetTrigger(hashAtaqueAbajo);
    }

    // Método para controlar el estado general de ataque
    public void SetAttacking(bool attacking)
    {
        if (!inicializado) return;
        animator.SetBool(hashIsAttacking, attacking);
    }

    public void TriggerDobleSalto() { if (!inicializado) return; animator.SetTrigger(hashDobleSalto); }
    public void SetDashing(bool isDashing) { if (!inicializado) return; animator.SetBool(hashIsDashing, isDashing); }
    public void SetChargingUltimate(bool isCharging) { if (!inicializado) return; animator.SetBool(hashChargingUltimate, isCharging); }
    public void SetReleasingUltimate(bool isReleasing) { if (!inicializado) return; animator.SetBool(hashReleasingUltimate, isReleasing); }
    public void IniciarAnimacionUltimate() { if (!inicializado) return; animator.SetTrigger(hashUsandoUltimate); }
    public void IniciarAnimacionHabilidad(int tipoHabilidad) { /* ... */ }
    #endregion
}
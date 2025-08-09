// Player/Core/PlayerMovement.cs
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movimiento Horizontal")]
    public float velocidadMaxima = 5f;
    public float aceleracion = 15f;
    public float desaceleracion = 20f;
    public float desaceleracionAire = 5f;

    [Tooltip("Qué tan rápido frena el jugador al cambiar de dirección en el suelo.")]
    public float fuerzaFrenado = 30f;

    [Header("Configuración de Salto")]
    public float fuerzaSaltoMaxima = 15f;
    public float tiempoMinimoSalto = 0.2f;
    public float multiplicadorGravedad = 2.5f;
    public float multiplicadorCaidaRapida = 4f;

    [Header("Mejoras de 'Game Feel'")]
    [Tooltip("Tiempo en segundos que el jugador puede saltar tras dejar una plataforma.")]
    public float coyoteTime = 0.15f;
    [Tooltip("Tiempo en segundos que el input de salto se 'recuerda' antes de tocar el suelo.")]
    public float jumpBufferTime = 0.15f;

    [Header("Doble Salto")]
    [Tooltip("Habilita o deshabilita la capacidad de realizar un segundo salto en el aire.")]
    public bool dobleSaltoHabilitado = true;
    [Tooltip("La fuerza aplicada para el segundo salto.")]
    public float fuerzaDobleSalto = 12f;
    private bool dobleSaltoDisponible = false;

    [Header("Ataque Arriba")]
    public float fuerzaSaltoAtaqueArriba = 8f;

    [Header("Ground Check")]
    public GroundChecker groundChecker;
    public bool crearGroundCheckerAutomatico = true;

    [Header("Flip GameObject")]
    public bool flipearGameObject = true;
    public bool mirandoDerecha = true;

    [Header("Referencias")]
    public PlayerStats playerStats;
    public Transform visualsTransform;
    [SerializeField] private PlayerAnimatorController animatorController;


    // Variables privadas
    private Rigidbody2D rb;
    private Collider2D coll2D;
    private float inputHorizontal;
    private bool jumpInputHeld;
    private bool enSuelo;
    private bool saltando;
    private float tiempoPresionandoSalto;
    private bool botonSaltoSoltado;
    private float gravedadOriginal;
    private float velocidadObjetivo;
    private float velocidadActual;

    private bool movimientoHabilitado = true;
    private bool enRetroceso = false;

    private bool saltoAtaqueArribaUsado = false;
    private float coyoteTimeCounter;
    private float jumpBufferCounter;

    void Start()
    {
        InicializarComponentes();
        InicializarVariables();
        ConfigurarGroundChecker();
    }

    void InicializarComponentes()
    {
        rb = GetComponent<Rigidbody2D>();
        coll2D = GetComponent<Collider2D>();
        gravedadOriginal = rb.gravityScale;

        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();

        if (animatorController == null)
            animatorController = GetComponentInChildren<PlayerAnimatorController>();
    }

    void InicializarVariables()
    {
        velocidadActual = 0f;
        velocidadObjetivo = 0f;
    }

    void ConfigurarGroundChecker()
    {
        if (groundChecker == null)
            groundChecker = GetComponentInChildren<GroundChecker>();

        if (groundChecker == null && crearGroundCheckerAutomatico)
            CrearGroundCheckerAutomatico();

        if (groundChecker != null)
        {
            groundChecker.OnGroundEntered += OnTocaSuelo;
            groundChecker.OnGroundExited += OnDejaSuelo;
        }
    }

    void CrearGroundCheckerAutomatico()
    {
        GameObject groundCheckerGO = new GameObject("GroundChecker");
        groundCheckerGO.transform.SetParent(transform);
        groundCheckerGO.transform.localPosition = Vector3.zero;

        BoxCollider2D detector = groundCheckerGO.AddComponent<BoxCollider2D>();
        detector.isTrigger = true;
        detector.size = new Vector2(0.8f, 0.1f);
        detector.offset = new Vector2(0f, -0.55f);

        groundChecker = groundCheckerGO.AddComponent<GroundChecker>();
    }

    void Update()
    {
        ManejarFlipGameObject();
        enSuelo = groundChecker != null ? groundChecker.EnSuelo : false;

        if (enSuelo) coyoteTimeCounter = coyoteTime;
        else coyoteTimeCounter -= Time.deltaTime;

        jumpBufferCounter -= Time.deltaTime;

        AplicarGravedadPersonalizada();
    }

    void FixedUpdate()
    {
        if (enRetroceso || (playerStats != null && playerStats.EstaMuerto))
        {
            return;
        }
        ManejarMovimiento();
    }

    void OnTocaSuelo()
    {
        saltoAtaqueArribaUsado = false;
        dobleSaltoDisponible = false;

        if (rb.linearVelocity.y <= 0)
        {
            saltando = false;
            tiempoPresionandoSalto = 0f;
            botonSaltoSoltado = false;
        }
    }

    void OnDejaSuelo()
    {
        if (dobleSaltoHabilitado)
        {
            dobleSaltoDisponible = true;
        }
    }

    public void SetMoveInput(Vector2 moveInput)
    {
        if (movimientoHabilitado && !enRetroceso && (playerStats == null || !playerStats.EstaMuerto))
        {
            inputHorizontal = moveInput.x;
        }
    }

    public void HandleJumpInput(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (dobleSaltoHabilitado && dobleSaltoDisponible && !enSuelo)
            {
                PerformDoubleJump();
                return;
            }
            jumpBufferCounter = jumpBufferTime;
        }

        jumpInputHeld = context.ReadValueAsButton();

        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
        {
            saltando = true;
            tiempoPresionandoSalto = 0f;
            botonSaltoSoltado = false;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, fuerzaSaltoMaxima);
            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;
        }

        if (context.canceled && saltando && !botonSaltoSoltado)
        {
            botonSaltoSoltado = true;
            if (tiempoPresionandoSalto < tiempoMinimoSalto && rb.linearVelocity.y > 0)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
            }
        }
    }

    private void PerformDoubleJump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, fuerzaDobleSalto);
        dobleSaltoDisponible = false;
        saltando = true;
        tiempoPresionandoSalto = 0f;
        botonSaltoSoltado = false;

        animatorController?.TriggerDobleSalto();
    }


    void ManejarMovimiento()
    {
        if (movimientoHabilitado)
        {
            velocidadObjetivo = inputHorizontal * velocidadMaxima;
            float tasaDeCambio;

            if (Mathf.Abs(inputHorizontal) > 0.1f)
            {
                if (Mathf.Sign(inputHorizontal) != Mathf.Sign(velocidadActual) && velocidadActual != 0f && enSuelo && !enRetroceso)
                {
                    tasaDeCambio = fuerzaFrenado;
                }
                else
                {
                    tasaDeCambio = enSuelo ? aceleracion : aceleracion * 0.6f;
                }
            }
            else
            {
                tasaDeCambio = enSuelo ? desaceleracion : desaceleracionAire;
            }

            velocidadActual = Mathf.MoveTowards(velocidadActual, velocidadObjetivo, tasaDeCambio * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(velocidadActual, rb.linearVelocity.y);
        }
    }

    void AplicarGravedadPersonalizada()
    {
        if (rb.linearVelocity.y < 0)
        {
            rb.gravityScale = gravedadOriginal * multiplicadorCaidaRapida;
        }
        else if (rb.linearVelocity.y > 0 && !jumpInputHeld)
        {
            rb.gravityScale = gravedadOriginal * multiplicadorGravedad;
        }
        else
        {
            rb.gravityScale = gravedadOriginal;
        }
    }

    void ManejarFlipGameObject()
    {
        if (!flipearGameObject || visualsTransform == null) return;
        if (playerStats != null && playerStats.EstaMuerto) return;

        if (inputHorizontal > 0 && !mirandoDerecha)
        {
            Flip();
        }
        else if (inputHorizontal < 0 && mirandoDerecha)
        {
            Flip();
        }
    }

    void Flip()
    {
        mirandoDerecha = !mirandoDerecha;
        Vector3 escalaActual = visualsTransform.localScale;
        escalaActual.x *= -1f;
        visualsTransform.localScale = escalaActual;
    }

    void OnDestroy()
    {
        if (groundChecker != null)
        {
            groundChecker.OnGroundEntered -= OnTocaSuelo;
            groundChecker.OnGroundExited -= OnDejaSuelo;
        }
    }

    public bool EstaEnSuelo => enSuelo;
    public bool EstaSaltando => saltando;
    public bool MirandoDerecha => mirandoDerecha;
    public bool EstaMuerto => playerStats != null ? playerStats.EstaMuerto : false;
    public float VelocidadHorizontal => rb.linearVelocity.x;
    public float VelocidadVertical => rb.linearVelocity.y;
    public GroundChecker GroundChecker => groundChecker;

    public void SetControlHabilitado(bool habilitado)
    {
        movimientoHabilitado = habilitado;
        if (!habilitado)
        {
            ResetearVelocidad();
        }
    }

    public void IniciarRetroceso(Vector2 direccion, float fuerza)
    {
        if (!enRetroceso)
        {
            StartCoroutine(AplicarRetrocesoCoroutine(direccion, fuerza));
        }
    }

    public void AplicarPogoJump(float fuerzaPogo)
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        rb.AddForce(Vector2.up * fuerzaPogo, ForceMode2D.Impulse);

        saltando = false;
        botonSaltoSoltado = false;

        if (dobleSaltoHabilitado)
        {
            dobleSaltoDisponible = true;
        }
    }
    public void AplicarImpulsoDeAtaque(Vector2 direccion, float fuerza)
    {
        if (rb != null)
        {
            // Aplicamos una fuerza de impulso sin deshabilitar el control del jugador.
            // El jugador podrá "luchar" contra esta fuerza con su movimiento.
            rb.AddForce(direccion * fuerza, ForceMode2D.Impulse);
        }
    }
    public void RealizarSaltoAtaqueArriba()
    {
        if (!saltoAtaqueArribaUsado)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, fuerzaSaltoAtaqueArriba);
            saltoAtaqueArribaUsado = true;
        }
    }

    private IEnumerator AplicarRetrocesoCoroutine(Vector2 direccion, float fuerza)
    {
        enRetroceso = true;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(direccion * fuerza, ForceMode2D.Impulse);
        }

        yield return new WaitForSeconds(0.15f);

        enRetroceso = false;
    }

    public void ResetearVelocidad()
    {
        velocidadActual = 0f;
        velocidadObjetivo = 0f;
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
    }

    public void ForzarMirarDerecha()
    {
        if (!mirandoDerecha) Flip();
    }

    public void ForzarMirarIzquierda()
    {
        if (mirandoDerecha) Flip();
    }

    public void ForzarFlip(float inputHorizontal)
    {
        if (inputHorizontal > 0 && !mirandoDerecha)
        {
            Flip();
        }
        else if (inputHorizontal < 0 && mirandoDerecha)
        {
            Flip();
        }
    }

    public void ResetearEstado()
    {
        rb.gravityScale = gravedadOriginal;
        saltando = false;
        tiempoPresionandoSalto = 0f;
        botonSaltoSoltado = false;
        ResetearVelocidad();
    }
}
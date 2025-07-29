using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movimiento Horizontal")]
    public float velocidadMaxima = 5f;
    public float aceleracion = 15f;
    public float desaceleracion = 20f;
    public float desaceleracionAire = 5f;

    [Header("Configuración de Salto")]
    public float fuerzaSaltoMaxima = 15f;
    public float tiempoMinimoSalto = 0.2f;
    public float multiplicadorGravedad = 2.5f;
    public float multiplicadorCaidaRapida = 4f;

    [Header("Ground Check")]
    public GroundChecker groundChecker;
    public bool crearGroundCheckerAutomatico = true;

    [Header("Flip GameObject")]
    public bool flipearGameObject = true;
    public bool mirandoDerecha = true;

    [Header("Referencias")]
    public PlayerStats playerStats;
    public Transform visualsTransform;

    [Header("Integración con Habilidades")]
    [SerializeField] private DoubleJumpAbility doubleJumpAbility;

    // Variables privadas
    private Rigidbody2D rb;
    private Collider2D coll2D;
    private float inputHorizontal;
    private bool enSuelo;
    private bool saltando;
    private float tiempoPresionandoSalto;
    private bool botonSaltoSoltado;
    private float gravedadOriginal;
    private float velocidadObjetivo;
    private float velocidadActual;

    private bool movimientoHabilitado = true;
    // NUEVO: Flag para saber si el jugador está en estado de retroceso.
    private bool enRetroceso = false;

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

        if (doubleJumpAbility == null)
            doubleJumpAbility = GetComponent<DoubleJumpAbility>();
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
        if (movimientoHabilitado && !enRetroceso && (playerStats == null || !playerStats.EstaMuerto))
        {
            inputHorizontal = Input.GetAxisRaw("Horizontal");
        }
        else
        {
            inputHorizontal = 0f;
        }

        ManejarFlipGameObject();
        enSuelo = groundChecker != null ? groundChecker.EnSuelo : false;

        if (movimientoHabilitado && !enRetroceso)
        {
            ManejarSalto();
        }

        AplicarGravedadPersonalizada();
    }

    void FixedUpdate()
    {
        // MODIFICADO: Ahora, si estamos en retroceso, detenemos toda la lógica de movimiento.
        if (enRetroceso || (playerStats != null && playerStats.EstaMuerto))
        {
            return;
        }
        ManejarMovimiento();
    }

    void OnTocaSuelo()
    {
        if (rb.linearVelocity.y <= 0)
        {
            saltando = false;
            tiempoPresionandoSalto = 0f;
            botonSaltoSoltado = false;
        }
    }

    void OnDejaSuelo()
    {
        // Lógica al despegar del suelo
    }

    void ManejarMovimiento()
    {
        if (movimientoHabilitado)
        {
            velocidadObjetivo = inputHorizontal * velocidadMaxima;

            if (Mathf.Abs(inputHorizontal) > 0.1f)
            {
                float aceleracionAUsar = enSuelo ? aceleracion : aceleracion * 0.6f;
                velocidadActual = Mathf.MoveTowards(velocidadActual, velocidadObjetivo, aceleracionAUsar * Time.fixedDeltaTime);
            }
            else
            {
                float desaceleracionAUsar = enSuelo ? desaceleracion : desaceleracionAire;
                velocidadActual = Mathf.MoveTowards(velocidadActual, 0f, desaceleracionAUsar * Time.fixedDeltaTime);
            }

            rb.linearVelocity = new Vector2(velocidadActual, rb.linearVelocity.y);
        }
    }

    void ManejarSalto()
    {
        if (Input.GetButtonDown("Jump") && enSuelo)
        {
            saltando = true;
            tiempoPresionandoSalto = 0f;
            botonSaltoSoltado = false;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, fuerzaSaltoMaxima);
        }

        if (saltando && Input.GetButton("Jump"))
        {
            tiempoPresionandoSalto += Time.deltaTime;
        }

        if (Input.GetButtonUp("Jump") && saltando && !botonSaltoSoltado)
        {
            botonSaltoSoltado = true;
            if (tiempoPresionandoSalto < tiempoMinimoSalto && rb.linearVelocity.y > 0)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
            }
        }
    }

    void AplicarGravedadPersonalizada()
    {
        if (rb.linearVelocity.y < 0)
        {
            rb.gravityScale = gravedadOriginal * multiplicadorCaidaRapida;
        }
        else if (rb.linearVelocity.y > 0 && !Input.GetButton("Jump"))
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
    public bool TieneDobleSalto => doubleJumpAbility != null && doubleJumpAbility.Habilitado;

    public void SetControlHabilitado(bool habilitado)
    {
        movimientoHabilitado = habilitado;
    }

    // MODIFICADO: Este será el método público que llamaremos desde SwordAttack.
    public void IniciarRetroceso(Vector2 direccion, float fuerza)
    {
        // Solo iniciamos el retroceso si no estamos ya en uno.
        if (!enRetroceso)
        {
            StartCoroutine(AplicarRetrocesoCoroutine(direccion, fuerza));
        }
    }

    // NUEVO: Toda la lógica de retroceso ahora está en una corutina.
    private IEnumerator AplicarRetrocesoCoroutine(Vector2 direccion, float fuerza)
    {
        enRetroceso = true; // 1. Bloqueamos el control normal del jugador.

        if (rb != null)
        {
            // 2. Reseteamos la velocidad por completo para un retroceso más limpio.
            rb.linearVelocity = Vector2.zero;
            // 3. Aplicamos la fuerza de impulso.
            rb.AddForce(direccion * fuerza, ForceMode2D.Impulse);
        }

        // 4. Esperamos una fracción de segundo. Este es el tiempo que durará el retroceso.
        yield return new WaitForSeconds(0.15f);

        enRetroceso = false; // 5. Devolvemos el control normal al jugador.
    }

    public void ResetearVelocidad()
    {
        velocidadActual = 0f;
        velocidadObjetivo = 0f;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    public void ForzarMirarDerecha()
    {
        if (!mirandoDerecha) Flip();
    }

    public void ForzarMirarIzquierda()
    {
        if (mirandoDerecha) Flip();
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
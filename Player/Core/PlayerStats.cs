// Player/Core/PlayerStats.cs

using System.Collections;
using UnityEngine;

public class PlayerStats : MonoBehaviour, IDamageable
{
    [Header("Configuración de Salud")]
    public int MaxHealth = 5;
    public int CurrentHealth;

    [Header("Referencias")]
    public HealthUI healthUI;
    public PlayerDamageEffects damageEffects;
    public PlayerMovement playerMovement;
    // --- NUEVA REFERENCIA ---
    [SerializeField] private Collider2D playerCollider;

    [Header("Knockback del Jugador")]
    public bool aplicarKnockback = true;
    public float fuerzaKnockbackHorizontal = 6f;
    public float fuerzaKnockbackVertical = 2f;
    public float duracionKnockback = 0.3f;
    public bool deshabilitarControlesDuranteKnockback = true;

    [Header("Configuración de Muerte")]
    public float retardoGameOver = 0.5f;

    // --- NUEVA CONFIGURACIÓN ---
    [Header("Daño por Contacto")]
    [SerializeField] private LayerMask whatIsEnemy; // Asigna la capa de tus enemigos aquí

    private Rigidbody2D rb;
    private bool enKnockback = false;
    private bool muerteEnProceso = false;
    private bool estaMuerto = false;

    public event System.Action<int> OnHealthChanged;
    public event System.Action<int> OnDamageTaken;
    public event System.Action<int> OnHealthRestored;
    public event System.Action OnPlayerDeath;
    public event System.Action OnDeathAnimationStart;
    public event System.Action OnDeathAnimationComplete;
    public event System.Action OnKnockbackStarted;
    public event System.Action OnKnockbackEnded;

    public bool IsAlive => CurrentHealth > 0 && !muerteEnProceso;
    int IDamageable.CurrentHealth => CurrentHealth;
    int IDamageable.MaxHealth => MaxHealth;
    public Transform Transform => transform;
    public Vector3 Position => transform.position;

    void Start()
    {
        CurrentHealth = MaxHealth;
        InicializarComponentes();

        if (healthUI != null)
        {
            healthUI.SetMaxHearths(MaxHealth);
            healthUI.UpdateHearts(CurrentHealth);
        }
    }

    void Update()
    {
        // --- NUEVO MÉTODO ---
        // Comprobamos constantemente si hay daño por contacto
        CheckForContactDamage();
    }

    void InicializarComponentes()
    {
        rb = GetComponent<Rigidbody2D>();
        if (damageEffects == null) damageEffects = GetComponent<PlayerDamageEffects>();
        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
        if (playerCollider == null) playerCollider = GetComponent<Collider2D>();
    }

    // --- MÉTODO ELIMINADO ---
    // Ya no usamos OnTriggerEnter2D para el daño por contacto
    /*
    void OnTriggerEnter2D(Collider2D other)
    {
        // ...
    }
    */

    // --- NUEVO MÉTODO DE DETECCIÓN ---
    private void CheckForContactDamage()
    {
        // Si no podemos recibir daño (ya sea por invulnerabilidad o por estar muertos), no hacemos nada
        if (damageEffects != null && !damageEffects.PuedeRecibirDaño()) return;
        if (muerteEnProceso) return;

        // Usamos el collider del jugador para ver si se está solapando con algún enemigo
        Collider2D[] overlappingEnemies = Physics2D.OverlapBoxAll(playerCollider.bounds.center, playerCollider.bounds.size, 0f, whatIsEnemy);

        // Si encontramos al menos un enemigo...
        if (overlappingEnemies.Length > 0)
        {
            // Tomamos la posición del primer enemigo detectado para el knockback
            Transform damageSource = overlappingEnemies[0].transform;
            TakeDamage(1, transform.position, damageSource);
        }
    }


    public void TakeDamage(int damage, Vector3 hitPoint, Transform damageSource)
    {
        if (muerteEnProceso) return;
        if (damageEffects != null && !damageEffects.PuedeRecibirDaño()) return;

        int saludAnterior = CurrentHealth;
        CurrentHealth -= damage;
        CurrentHealth = Mathf.Max(0, CurrentHealth);

        OnHealthChanged?.Invoke(CurrentHealth);
        OnDamageTaken?.Invoke(damage);

        if (damageEffects != null)
            damageEffects.TriggerDamageEffect();

        if (aplicarKnockback && rb != null && !muerteEnProceso && damageSource != null)
        {
            Vector2 knockbackDirection = CalcularDireccionKnockback(damageSource);
            StartCoroutine(AplicarKnockback(knockbackDirection));
        }

        if (CurrentHealth <= 0 && saludAnterior > 0)
        {
            HandleDeath();
        }

        if (healthUI != null)
            healthUI.UpdateHearts(CurrentHealth);
    }

    public void TakeDamage(int damage)
    {
        TakeDamage(damage, transform.position, null);
    }

    Vector2 CalcularDireccionKnockback(Transform damageSource)
    {
        if (damageSource != null)
        {
            // Invertimos la dirección para que el knockback sea siempre alejándose del enemigo
            return (transform.position - damageSource.position).normalized;
        }
        else
        {
            float direccionX = playerMovement.MirandoDerecha ? -1f : 1f;
            return new Vector2(direccionX, 0.5f).normalized;
        }
    }

    IEnumerator AplicarKnockback(Vector2 direccion)
    {
        if (enKnockback || muerteEnProceso) yield break;

        enKnockback = true;
        OnKnockbackStarted?.Invoke();

        if (deshabilitarControlesDuranteKnockback && playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        Vector2 fuerzaKnockback = new Vector2(
            direccion.x * fuerzaKnockbackHorizontal,
            fuerzaKnockbackVertical
        );

        rb.linearVelocity = Vector2.zero; // Reseteamos la velocidad antes de aplicar la fuerza
        rb.AddForce(fuerzaKnockback, ForceMode2D.Impulse);
        yield return new WaitForSeconds(duracionKnockback);

        enKnockback = false;
        OnKnockbackEnded?.Invoke();

        if (deshabilitarControlesDuranteKnockback && playerMovement != null && !muerteEnProceso)
        {
            playerMovement.enabled = true;
        }
    }

    void HandleDeath()
    {
        if (muerteEnProceso) return;

        muerteEnProceso = true;
        estaMuerto = true;
        if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (enKnockback)
        {
            StopAllCoroutines();
            enKnockback = false;
            if (playerMovement != null)
            {
                playerMovement.enabled = true;
            }
        }

        OnPlayerDeath?.Invoke();
        StartCoroutine(ManejarAnimacionMuerte());
    }

    IEnumerator ManejarAnimacionMuerte()
    {
        OnDeathAnimationStart?.Invoke();
        yield return new WaitForSeconds(1.5f);
        OnDeathAnimationComplete?.Invoke();
        StartCoroutine(ActivarGameOverConRetraso(retardoGameOver));
    }

    IEnumerator ActivarGameOverConRetraso(float retraso)
    {
        if (retraso > 0) yield return new WaitForSeconds(retraso);
        // Aquí iría tu lógica de Game Over
    }

    public void ResetearEstadoMuerte()
    {
        // Lógica para resetear al jugador
    }

    public void Heal(int healAmount)
    {
        // Lógica de curación
    }

    public bool EstaInvulnerable => damageEffects != null && damageEffects.EsInvulnerable;
    public float PorcentajeVida => MaxHealth > 0 ? (float)CurrentHealth / MaxHealth : 0f;
    public bool SaludCompleta => CurrentHealth >= MaxHealth;
    public bool EstaMuerto => estaMuerto;
}
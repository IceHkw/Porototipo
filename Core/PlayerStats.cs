using System.Collections;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Configuración de Salud")]
    public int MaxHealth = 5;
    public int CurrentHealth;

    [Header("Referencias")]
    public HealthUI healthUI;
    public PlayerDamageEffects damageEffects;
    public PlayerMovement playerMovement;

    [Header("Knockback del Jugador")]
    public bool aplicarKnockback = true;
    public float fuerzaKnockbackHorizontal = 6f;
    public float fuerzaKnockbackVertical = 2f;
    public float duracionKnockback = 0.3f;
    public bool deshabilitarControlesDuranteKnockback = true;

    [Header("Configuración de Muerte")]
    public float retardoGameOver = 0.5f;

    // Referencias privadas
    private Rigidbody2D rb;
    private Animator animator;
    private bool enKnockback = false;
    private bool controlesDeshabilitados = false;
    private bool muerteEnProceso = false;
    private bool estaMuerto = false;

    // Hashes de animación para muerte
    private int hashEstaMuerto;

    // Eventos para notificar cambios
    public System.Action<int> OnHealthChanged;
    public System.Action<int> OnDamageTaken;
    public System.Action<int> OnHealthRestored;
    public System.Action OnPlayerDeath;
    public System.Action OnDeathAnimationStart;
    public System.Action OnDeathAnimationComplete;
    public System.Action OnKnockbackStarted;
    public System.Action OnKnockbackEnded;

    void Start()
    {
        CurrentHealth = MaxHealth;
        InicializarComponentes();
        InicializarAnimaciones();

        // Configurar UI
        if (healthUI != null)
        {
            healthUI.SetMaxHearths(MaxHealth);
            healthUI.UpdateHearts(CurrentHealth);
        }

        // Configurar referencias automáticamente
        if (damageEffects == null)
            damageEffects = GetComponent<PlayerDamageEffects>();

        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();
    }

    void InicializarComponentes()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void InicializarAnimaciones()
    {
        if (animator != null)
        {
            hashEstaMuerto = Animator.StringToHash("EstaMuerto");
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            if (damageEffects == null || damageEffects.PuedeRecibirDaño())
            {
                TakeDamageFromSource(1, other.transform);
            }
        }
    }

    public void TakeDamageFromSource(int damage, Transform damageSource)
    {
        if (muerteEnProceso) return;

        // Verificar invulnerabilidad
        if (damageEffects != null && !damageEffects.PuedeRecibirDaño())
            return;

        int saludAnterior = CurrentHealth;
        CurrentHealth -= damage;
        CurrentHealth = Mathf.Max(0, CurrentHealth);

        // Disparar eventos
        OnHealthChanged?.Invoke(CurrentHealth);
        OnDamageTaken?.Invoke(damage);

        // Activar efectos visuales
        if (damageEffects != null)
            damageEffects.TriggerDamageEffect();

        // Aplicar knockback
        if (aplicarKnockback && rb != null && !muerteEnProceso && damageSource != null)
        {
            Vector2 knockbackDirection = CalcularDireccionKnockback(damageSource);
            StartCoroutine(AplicarKnockback(knockbackDirection));
        }

        // Verificar muerte
        if (CurrentHealth <= 0 && saludAnterior > 0)
        {
            HandleDeath();
        }

        // Actualizar UI
        if (healthUI != null)
            healthUI.UpdateHearts(CurrentHealth);
    }

    public void TakeDamage(int damage)
    {
        TakeDamageFromSource(damage, null);
    }

    Vector2 CalcularDireccionKnockback(Transform damageSource)
    {
        if (damageSource != null)
        {
            Vector2 direccion = (transform.position - damageSource.position).normalized;
            return direccion;
        }
        else
        {
            float direccionX = Random.Range(0f, 1f) > 0.5f ? 1f : -1f;
            return new Vector2(direccionX, 0f);
        }
    }

    IEnumerator AplicarKnockback(Vector2 direccion)
    {
        if (enKnockback || muerteEnProceso) yield break;

        enKnockback = true;
        OnKnockbackStarted?.Invoke();

        // Deshabilitar controles durante knockback
        if (deshabilitarControlesDuranteKnockback && playerMovement != null)
        {
            controlesDeshabilitados = true;
            playerMovement.enabled = false;
        }

        // Aplicar fuerza de knockback
        Vector2 fuerzaKnockback = new Vector2(
            direccion.x * fuerzaKnockbackHorizontal,
            fuerzaKnockbackVertical
        );

        rb.linearVelocity = fuerzaKnockback;

        // Esperar duración del knockback
        yield return new WaitForSeconds(duracionKnockback);

        // Restaurar estado normal
        enKnockback = false;
        OnKnockbackEnded?.Invoke();

        // Reactivar controles
        if (deshabilitarControlesDuranteKnockback && playerMovement != null && !muerteEnProceso)
        {
            controlesDeshabilitados = false;
            playerMovement.enabled = true;
        }
    }

    public void ForzarKnockback(Vector2 direccion, float multiplicadorFuerza = 1f)
    {
        if (muerteEnProceso || rb == null) return;

        StartCoroutine(AplicarKnockbackPersonalizado(direccion, multiplicadorFuerza));
    }

    IEnumerator AplicarKnockbackPersonalizado(Vector2 direccion, float multiplicador)
    {
        enKnockback = true;

        Vector2 fuerza = new Vector2(
            direccion.x * fuerzaKnockbackHorizontal * multiplicador,
            direccion.y * fuerzaKnockbackVertical * multiplicador
        );

        rb.linearVelocity = fuerza;
        yield return new WaitForSeconds(duracionKnockback);

        enKnockback = false;
    }

    void HandleDeath()
    {
        if (muerteEnProceso) return;

        muerteEnProceso = true;
        estaMuerto = true;

        // Detener completamente la velocidad horizontal para evitar deslizamiento
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        // Cancelar knockback si estaba activo
        if (enKnockback)
        {
            StopAllCoroutines();
            enKnockback = false;

            if (controlesDeshabilitados && playerMovement != null)
            {
                controlesDeshabilitados = false;
                playerMovement.enabled = true;
            }
        }

        // Disparar evento de muerte inmediato
        OnPlayerDeath?.Invoke();

        // Activar animación de muerte directamente
        ActivarAnimacionMuerte();
    }

    // === MÉTODOS DE MUERTE MOVIDOS DESDE PlayerMovement ===

    public void ActivarAnimacionMuerte()
    {
        if (!estaMuerto) return;

        OnDeathAnimationStart?.Invoke();
        ActualizarAnimacionMuerte();
        StartCoroutine(ManejarAnimacionMuerte());
    }

    IEnumerator ManejarAnimacionMuerte()
    {
        yield return new WaitForSeconds(1.5f);
        OnAnimacionMuerteTerminada();
    }

    void OnAnimacionMuerteTerminada()
    {
        OnDeathAnimationComplete?.Invoke();
        StartCoroutine(ActivarGameOverConRetraso(retardoGameOver));
    }

    IEnumerator ActivarGameOverConRetraso(float retraso)
    {
        if (retraso > 0)
            yield return new WaitForSeconds(retraso);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerGameOver();
        }
        else
        {
            if (playerMovement != null)
                playerMovement.enabled = false;
        }
    }

    void ActualizarAnimacionMuerte()
    {
        if (animator != null)
        {
            animator.SetBool(hashEstaMuerto, estaMuerto);
        }
    }

    public void ResetearEstadoMuerte()
    {
        estaMuerto = false;
        muerteEnProceso = false;
        enKnockback = false;
        controlesDeshabilitados = false;

        CurrentHealth = MaxHealth;
        OnHealthChanged?.Invoke(CurrentHealth);

        if (healthUI != null)
            healthUI.UpdateHearts(CurrentHealth);

        if (playerMovement != null)
            playerMovement.enabled = true;

        if (animator != null)
            animator.SetBool(hashEstaMuerto, false);
    }

    // === FIN MÉTODOS DE MUERTE ===

    public void Heal(int healAmount)
    {
        if (CurrentHealth <= 0) return;

        int saludAnterior = CurrentHealth;
        CurrentHealth = Mathf.Min(CurrentHealth + healAmount, MaxHealth);

        if (CurrentHealth > saludAnterior)
        {
            OnHealthChanged?.Invoke(CurrentHealth);
            OnHealthRestored?.Invoke(CurrentHealth - saludAnterior);
        }

        if (healthUI != null)
            healthUI.UpdateHearts(CurrentHealth);
    }

    public void SetHealth(int newHealth)
    {
        if (muerteEnProceso && newHealth > 0) return;

        int saludAnterior = CurrentHealth;
        CurrentHealth = Mathf.Clamp(newHealth, 0, MaxHealth);

        if (CurrentHealth != saludAnterior)
        {
            OnHealthChanged?.Invoke(CurrentHealth);

            if (CurrentHealth > saludAnterior)
                OnHealthRestored?.Invoke(CurrentHealth - saludAnterior);
            else if (CurrentHealth < saludAnterior)
                OnDamageTaken?.Invoke(saludAnterior - CurrentHealth);
        }

        if (CurrentHealth <= 0 && saludAnterior > 0)
            HandleDeath();

        if (healthUI != null)
            healthUI.UpdateHearts(CurrentHealth);
    }

    public void ResetHealth()
    {
        ResetearEstadoMuerte();
    }

    public void IncreaseMaxHealth(int amount)
    {
        MaxHealth += amount;
        CurrentHealth += amount;

        if (healthUI != null)
        {
            healthUI.SetMaxHearths(MaxHealth);
            healthUI.UpdateHearts(CurrentHealth);
        }

        OnHealthChanged?.Invoke(CurrentHealth);
    }

    // Propiedades públicas esenciales
    public bool EstaVivo => CurrentHealth > 0 && !muerteEnProceso;
    public bool EstaInvulnerable => damageEffects != null && damageEffects.EsInvulnerable;
    public float PorcentajeVida => MaxHealth > 0 ? (float)CurrentHealth / MaxHealth : 0f;
    public bool SaludCompleta => CurrentHealth >= MaxHealth;
    public bool MuerteEnProceso => muerteEnProceso;
    public bool EnKnockback => enKnockback;
    public bool EstaMuerto => estaMuerto; // Nueva propiedad para PlayerMovement

    // Métodos de utilidad
    public bool PuedeRecibirDaño()
    {
        return EstaVivo && (damageEffects == null || damageEffects.PuedeRecibirDaño()) && !muerteEnProceso;
    }

    public bool PuedeSerCurado()
    {
        return EstaVivo && CurrentHealth < MaxHealth && !muerteEnProceso;
    }
}
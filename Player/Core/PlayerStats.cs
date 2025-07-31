// icehkw/porototipo/Porototipo-b1fff1a96575171bcd74b21b8212fb1595715ffd/Player/Core/PlayerStats.cs

using System.Collections;
using UnityEngine;

// --- LÍNEA MODIFICADA ---
// Ahora implementamos formalmente la interfaz IDamageable
public class PlayerStats : MonoBehaviour, IDamageable
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

    // Hashes de animación
    private int hashEstaMuerto;

    // Eventos
    public System.Action<int> OnHealthChanged;
    public System.Action<int> OnDamageTaken;
    public System.Action<int> OnHealthRestored;
    public System.Action OnPlayerDeath;
    public System.Action OnDeathAnimationStart;
    public System.Action OnDeathAnimationComplete;
    public System.Action OnKnockbackStarted;
    public System.Action OnKnockbackEnded;

    // =======================================================
    // ===== INICIO DE SECCIÓN CORREGIDA (IMPLEMENTACIÓN DE IDamageable) =====
    // =======================================================

    // Estas propiedades públicas cumplen con la interfaz.
    // Simplemente "apuntan" a las variables y métodos que ya tenías.
    public bool IsAlive => CurrentHealth > 0 && !muerteEnProceso;
    int IDamageable.CurrentHealth => CurrentHealth;
    int IDamageable.MaxHealth => MaxHealth;
    public Transform Transform => transform;
    public Vector3 Position => transform.position;

    // Este método AHORA tiene la firma exacta que requiere la interfaz
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

    // Este método también es requerido por la interfaz, aunque ya tenías uno.
    // Simplemente llama a la versión más detallada.
    public void TakeDamage(int damage)
    {
        TakeDamage(damage, transform.position, null);
    }

    // =======================================================
    // ===== FIN DE SECCIÓN CORREGIDA =====
    // =======================================================


    void Start()
    {
        CurrentHealth = MaxHealth;
        InicializarComponentes();
        InicializarAnimaciones();

        if (healthUI != null)
        {
            healthUI.SetMaxHearths(MaxHealth);
            healthUI.UpdateHearts(CurrentHealth);
        }

        if (damageEffects == null) damageEffects = GetComponent<PlayerDamageEffects>();
        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
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
                // Ahora llamamos al método unificado de la interfaz
                TakeDamage(1, other.transform.position, other.transform);
            }
        }
    }

    Vector2 CalcularDireccionKnockback(Transform damageSource)
    {
        if (damageSource != null)
        {
            return (transform.position - damageSource.position).normalized;
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

        if (deshabilitarControlesDuranteKnockback && playerMovement != null)
        {
            controlesDeshabilitados = true;
            playerMovement.enabled = false;
        }

        Vector2 fuerzaKnockback = new Vector2(
            direccion.x * fuerzaKnockbackHorizontal,
            fuerzaKnockbackVertical
        );

        rb.linearVelocity = fuerzaKnockback;
        yield return new WaitForSeconds(duracionKnockback);

        enKnockback = false;
        OnKnockbackEnded?.Invoke();

        if (deshabilitarControlesDuranteKnockback && playerMovement != null && !muerteEnProceso)
        {
            controlesDeshabilitados = false;
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
            if (controlesDeshabilitados && playerMovement != null)
            {
                controlesDeshabilitados = false;
                playerMovement.enabled = true;
            }
        }

        OnPlayerDeath?.Invoke();
        ActivarAnimacionMuerte();
    }

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
        if (retraso > 0) yield return new WaitForSeconds(retraso);
        if (GameManager.Instance != null) GameManager.Instance.TriggerGameOver();
        else if (playerMovement != null) playerMovement.enabled = false;
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
        if (healthUI != null) healthUI.UpdateHearts(CurrentHealth);
        if (playerMovement != null) playerMovement.enabled = true;
        if (animator != null) animator.SetBool(hashEstaMuerto, false);
    }

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
        if (healthUI != null) healthUI.UpdateHearts(CurrentHealth);
    }

    // Propiedades públicas que ya tenías.
    public bool EstaInvulnerable => damageEffects != null && damageEffects.EsInvulnerable;
    public float PorcentajeVida => MaxHealth > 0 ? (float)CurrentHealth / MaxHealth : 0f;
    public bool SaludCompleta => CurrentHealth >= MaxHealth;
    public bool MuerteEnProceso => muerteEnProceso;
    public bool EnKnockback => enKnockback;
    public bool EstaMuerto => estaMuerto;
}

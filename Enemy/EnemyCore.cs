using UnityEngine;
using System;

/// <summary>
/// Componente núcleo que todo enemigo debe tener. Maneja salud, daño y referencias básicas.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyCore : MonoBehaviour, IDamageable, IPoolable
{
    [Header("Enemy Stats")]
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private int currentHealth;

    [Header("References")]
    public Transform player;
    [HideInInspector] public Rigidbody2D rb;

    [Header("Combat")]
    public float invulnerabilityDuration = 0.5f;
    private bool isInvulnerable = false;
    private float invulnerabilityTimer = 0f;

    [Header("Knockback")]
    public float knockbackForce = 5f;
    public float knockbackDuration = 0.2f;
    private bool isKnockedBack = false;
    private float knockbackTimer = 0f;

    [Header("Death")]
    public GameObject deathEffect;
    public float deathDelay = 0f;

    [Header("Stats Tracking")]
    private float spawnTime;
    private int totalDamageDealt = 0;

    [Header("Debug")]
    public bool showDebugInfo = false;

    // Eventos
    public event Action<int, int> OnHealthChanged; // current, max
    public event Action<Vector3, Transform> OnDamageReceived;
    public event Action OnDeath;
    public event Action OnKnockbackStart;
    public event Action OnKnockbackEnd;

    // Estado
    private bool isDead = false;
    private Vector3 knockbackVelocity;

    // Pool
    private bool isActiveInPool = false;
    private string poolType = PoolObjectTypes.ENEMY_BASIC;

    // Componentes opcionales
    private SpriteRenderer spriteRenderer;
    private EnemyAnimatorController enemyAnimatorController;
    private Animator animator;
    private EnemyVFX enemyVFX;

    #region Unity Lifecycle

    void Awake()
    {
        InitializeComponents();
    }

    void Start()
    {
        if (player == null)
        {
            FindPlayer();
        }

        currentHealth = maxHealth;
    }

    void Update()
    {
        HandleTimers();
    }

    void FixedUpdate()
    {
        HandleKnockback();
    }

    #endregion

    #region Initialization

    void InitializeComponents()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        enemyAnimatorController = GetComponentInChildren<EnemyAnimatorController>();
        animator = GetComponent<Animator>();
        enemyVFX = GetComponent<EnemyVFX>();

        // Configurar Rigidbody2D
        if (rb != null)
        {
            rb.gravityScale = 1f;
            rb.freezeRotation = true;
        }
    }

    void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

    #endregion

    #region IDamageable Implementation

    public void TakeDamage(int damage)
    {
        TakeDamage(damage, transform.position, null);
    }

    public void TakeDamage(int damage, Vector3 hitPoint, Transform damageSource)
    {
        if (isDead || isInvulnerable) return;

        currentHealth = Mathf.Max(0, currentHealth - damage);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnDamageReceived?.Invoke(hitPoint, damageSource);

        // Disparar evento global de daño
        EnemyEvents.TriggerEnemyDamaged(this, damage, hitPoint, damageSource);

        // Activar invulnerabilidad temporal
        StartInvulnerability();

        // Aplicar knockback si hay fuente de daño
        if (damageSource != null && knockbackForce > 0)
        {
            ApplyKnockback(damageSource.position);
        }

        // Efectos visuales
        PlayHitEffects();

        // Verificar muerte
        if (currentHealth <= 0)
        {
            // Verificar si fue el jugador quien mató ANTES de Die()
            bool killedByPlayer = damageSource != null && damageSource.CompareTag("Player");
            Transform killer = killedByPlayer ? damageSource : null;

            Die();

            // Disparar evento específico de muerte por jugador DESPUÉS de Die()
            if (killedByPlayer)
            {
                EnemyEvents.TriggerEnemyKilledByPlayer(this, killer);
            }
        }

        if (showDebugInfo)
        {
            Debug.Log($"[EnemyCore] {name} recibió {damage} de daño. Salud: {currentHealth}/{maxHealth}");
        }
    }

    public bool IsAlive => !isDead && currentHealth > 0;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public Transform Transform => transform;
    public Vector3 Position => transform.position;

    #endregion

    #region IPoolable Implementation

    public void OnSpawnFromPool(Vector3 position, Quaternion rotation)
    {
        transform.position = position;
        transform.rotation = rotation;

        // Resetear estado
        isDead = false;
        currentHealth = maxHealth;
        isInvulnerable = false;
        isKnockedBack = false;
        invulnerabilityTimer = 0f;
        knockbackTimer = 0f;
        spawnTime = Time.time;
        totalDamageDealt = 0;

        // Reactivar componentes
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = true;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            Color c = spriteRenderer.color;
            c.a = 1f;
            spriteRenderer.color = c;
        }

        if (enemyVFX != null)
        {
            enemyVFX.ResetVisualState();
        }

        // Buscar jugador si no está asignado
        if (player == null)
        {
            FindPlayer();
        }

        isActiveInPool = true;
        gameObject.SetActive(true);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // Disparar evento de spawn
        EnemyEvents.TriggerEnemySpawned(this);
    }

    public void OnReturnToPool()
    {
        // << LÍNEA CORREGIDA >>
        // Se ha eliminado la llamada recursiva a ObjectPoolManager.Instance.Return(gameObject);
        // Ahora este método solo se encarga de resetear el estado del enemigo.

        isActiveInPool = false;

        // Disparar evento de retorno al pool
        EnemyEvents.TriggerEnemyReturnedToPool(this);

        // Limpiar eventos
        OnHealthChanged = null;
        OnDamageReceived = null;
        OnDeath = null;
        OnKnockbackStart = null;
        OnKnockbackEnd = null;

        gameObject.SetActive(false);
    }

    public bool IsActiveInPool => isActiveInPool;
    public GameObject PooledGameObject => gameObject;
    public string PoolObjectType => poolType;

    #endregion

    #region Combat Methods

    void StartInvulnerability()
    {
        isInvulnerable = true;
        invulnerabilityTimer = invulnerabilityDuration;
    }

    void ApplyKnockback(Vector3 damageSourcePosition)
    {
        if (isKnockedBack) return;

        Vector2 knockbackDirection = (transform.position - damageSourcePosition).normalized;
        knockbackVelocity = knockbackDirection * knockbackForce;

        isKnockedBack = true;
        knockbackTimer = knockbackDuration;

        OnKnockbackStart?.Invoke();
    }

    void HandleKnockback()
    {
        if (!isKnockedBack || rb == null) return;

        // Aplicar velocidad de knockback gradualmente
        float knockbackProgress = 1f - (knockbackTimer / knockbackDuration);
        Vector2 currentKnockback = Vector2.Lerp(knockbackVelocity, Vector2.zero, knockbackProgress);

        rb.linearVelocity = new Vector2(currentKnockback.x, rb.linearVelocity.y);
    }

    void PlayHitEffects()
    {
        // Si tenemos EnemyVFX, él se encarga de los efectos
        if (enemyVFX != null)
        {
            // EnemyVFX maneja el flash automáticamente a través del evento OnDamageReceived
            return;
        }

        // Fallback: Flash de daño manual si no hay EnemyVFX
        if (spriteRenderer != null)
        {
            StartCoroutine(DamageFlash());
        }

        // Animación de daño
        if (animator != null)
        {
            animator.SetTrigger("Hit");
        }
    }

    System.Collections.IEnumerator DamageFlash()
    {
        if (spriteRenderer == null) yield break;

        Color originalColor = spriteRenderer.color;
        float flashDuration = 0.1f;
        float elapsed = 0f;

        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.PingPong(elapsed * 4f, 1f);
            spriteRenderer.color = Color.Lerp(originalColor, Color.red, t);
            yield return null;
        }

        spriteRenderer.color = originalColor;
    }

    void Die()
    {
        if (isDead) return;

        isDead = true;
        OnDeath?.Invoke();

        // Disparar evento global de muerte
        EnemyEvents.TriggerEnemyDeath(this);

        // Desactivar física
        if (rb != null)
        {
            rb.simulated = false;
        }

        // Reproducir efectos de muerte
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }

        // Animación de muerte
        if (animator != null)
        {
            animator.SetTrigger("Death");
        }

        // Devolver al pool o destruir
        if (ObjectPoolManager.Instance != null && ObjectPoolManager.Instance.PoolExists(poolType))
        {
            Invoke(nameof(ReturnToPool), deathDelay);
        }
        else
        {
            Destroy(gameObject, deathDelay);
        }
    }

    void ReturnToPool()
    {
        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.Return(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    #endregion

    #region Timer Management

    void HandleTimers()
    {
        // Timer de invulnerabilidad
        if (isInvulnerable)
        {
            invulnerabilityTimer -= Time.deltaTime;
            if (invulnerabilityTimer <= 0f)
            {
                isInvulnerable = false;
            }
        }

        // Timer de knockback
        if (isKnockedBack)
        {
            knockbackTimer -= Time.deltaTime;
            if (knockbackTimer <= 0f)
            {
                isKnockedBack = false;
                OnKnockbackEnd?.Invoke();
            }
        }
    }

    #endregion

    #region Public Methods

    public void SetPoolType(string type)
    {
        poolType = type;
    }

    public void Heal(int amount)
    {
        if (isDead) return;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void SetMaxHealth(int newMaxHealth)
    {
        maxHealth = newMaxHealth;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public bool IsKnockedBack => isKnockedBack;
    public bool IsInvulnerable => isInvulnerable;
    public bool IsDead => isDead;
    public float AliveTime => isDead ? 0f : Time.time - spawnTime;
    public int TotalDamageDealt => totalDamageDealt;

    /// <summary>
    /// Registra daño hecho por este enemigo (llamado por comportamientos de ataque)
    /// </summary>
    public void RegisterDamageDealt(int damage)
    {
        totalDamageDealt += damage;
        EnemyEvents.TriggerEnemyAttackHit(this, damage);
    }

    #endregion

    #region Debug

    void OnDrawGizmosSelected()
    {
        if (!showDebugInfo) return;

        // Mostrar información de salud
        Vector3 healthPos = transform.position + Vector3.up * 2f;
        UnityEditor.Handles.Label(healthPos, $"Health: {currentHealth}/{maxHealth}");

        // Mostrar estado
        if (isInvulnerable)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }

        if (isKnockedBack)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)knockbackVelocity.normalized);
        }
    }

    #endregion
}

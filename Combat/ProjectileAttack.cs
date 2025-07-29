using UnityEngine;

public class ProjectileAttack : MonoBehaviour
{
    [Header("Projectile Settings")]
    public float speed = 10f;
    public int damage = 1;
    public float maxLifetime = 3f; // Tiempo máximo antes de auto-destruirse

    [Header("Terrain Destruction")]
    public bool destroyTerrainOnImpact = true;
    public float terrainDestructionRadius = 0.5f;
    public LayerMask groundLayer = -1; // Capa del terreno destructible

    [Header("Terrain Damage")]
    public int terrainDamage = 1;

    [Header("Detection Settings")]
    public LayerMask enemyLayer = -1; // Layer de enemigos
    public bool showDebugGizmos = true;

    [Header("Effects (Optional)")]
    public GameObject hitEffect; // Efecto al impactar (opcional)
    public GameObject destroyEffect; // Efecto al destruirse (opcional)

    // Variables internas
    private Vector3 direction;
    private LayerMask obstacleLayer;
    private ClickAttackSpawner spawner;
    private Rigidbody2D rb;
    private Collider2D projectileCollider;
    private bool hasHit = false;
    private float lifetimeTimer;

    void Start()
    {
        InitializeComponents();
        StartMovement();

        // Inicializar timer de vida
        lifetimeTimer = maxLifetime;

        // Auto-destruir después del tiempo máximo (backup)
        Destroy(gameObject, maxLifetime);
    }

    void Update()
    {
        // Countdown del lifetime
        lifetimeTimer -= Time.deltaTime;

        if (lifetimeTimer <= 0f && !hasHit)
        {
            DestroyProjectile(false);
        }
    }

    void InitializeComponents()
    {
        rb = GetComponent<Rigidbody2D>();
        projectileCollider = GetComponent<Collider2D>();

        // Configurar el Rigidbody2D si existe
        if (rb != null)
        {
            rb.gravityScale = 0f; // Sin gravedad
            rb.freezeRotation = true; // Sin rotación
        }

        // Asegurar que el collider sea trigger para detección
        if (projectileCollider != null)
        {
            projectileCollider.isTrigger = true;
        }
    }

    void StartMovement()
    {
        if (rb != null)
        {
            // Usar Rigidbody2D para movimiento
            rb.linearVelocity = direction * speed;
        }
    }

    void FixedUpdate()
    {
        // Si no hay Rigidbody2D, mover manualmente
        if (rb == null && !hasHit)
        {
            transform.position += direction * speed * Time.fixedDeltaTime;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return; // Ya impactó algo

        // Verificar si es un enemigo
        if (IsInLayerMask(other.gameObject.layer, enemyLayer))
        {
            HandleEnemyHit(other.gameObject);
            return;
        }

        // Verificar si es un obstáculo
        if (IsInLayerMask(other.gameObject.layer, obstacleLayer))
        {
            HandleObstacleHit();
            return;
        }
    }

    void HandleEnemyHit(GameObject enemy)
    {
        // Buscar IDamageable en el enemigo
        IDamageable damageable = enemy.GetComponent<IDamageable>();

        // Si no lo encontramos en el GameObject directo, buscar en el padre
        if (damageable == null && enemy.transform.parent != null)
        {
            damageable = enemy.GetComponentInParent<IDamageable>();
        }

        if (damageable != null && damageable.IsAlive)
        {
            // Calcular punto de impacto
            Vector3 hitPoint = projectileCollider.ClosestPoint(enemy.transform.position);

            // Aplicar daño usando la interfaz
            damageable.TakeDamage(damage, hitPoint, transform);

            Debug.Log($"[ProjectileAttack] Impactó a {enemy.name} con {damage} de daño. " +
                     $"Salud restante: {damageable.CurrentHealth}/{damageable.MaxHealth}");
        }

        // Destruir el proyectil tras impactar enemigo
        DestroyProjectile(true);
    }

    void HandleObstacleHit()
    {
        Debug.Log("[ProjectileAttack] Impactó con obstáculo.");

        if (destroyTerrainOnImpact)
        {
            // Acceso directo a través de la instancia caché
            if (DestructibleTerrainController.Instance != null)
            {
                DestructibleTerrainController.Instance.DestroyTerrainAt(
                    transform.position,
                    terrainDestructionRadius,
                    terrainDamage
                );
            }
        }

        DestroyProjectile(true);
    }

    void DestroyProjectile(bool wasHit)
    {
        if (hasHit) return; // Ya fue procesado

        hasHit = true;

        // Detener movimiento
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        // Reproducir efectos si están configurados
        if (wasHit && hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, Quaternion.identity);
        }
        else if (!wasHit && destroyEffect != null)
        {
            Instantiate(destroyEffect, transform.position, Quaternion.identity);
        }

        /* Notificar al spawner que este proyectil se destruyó
        if (spawner != null)
        {
            spawner.OnProjectileDestroyed(this);
        }
        */

        // Destruir el proyectil
        Destroy(gameObject);
    }

    bool IsInLayerMask(int layer, LayerMask layerMask)
    {
        return (layerMask.value & (1 << layer)) != 0;
    }

    // ===== MÉTODOS PÚBLICOS PARA CONFIGURACIÓN =====

    public void SetSpawner(ClickAttackSpawner attackSpawner)
    {
        spawner = attackSpawner;
    }

    public void SetDirection(Vector3 moveDirection)
    {
        direction = moveDirection.normalized;

        // Opcional: Rotar el proyectil para que apunte en la dirección de movimiento
        if (direction != Vector3.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }

    public void SetObstacleLayer(LayerMask obstacles)
    {
        obstacleLayer = obstacles;
    }

    public void SetDamage(int newDamage)
    {
        damage = newDamage;
    }

    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;

        // Actualizar velocidad si ya se está moviendo
        if (rb != null && direction != Vector3.zero)
        {
            rb.linearVelocity = direction * speed;
        }
    }

    void OnDestroy()
    {
        /* Backup para asegurar que el spawner se notifique
        if (spawner != null && !hasHit)
        {
            spawner.OnProjectileDestroyed(this);
        }
        */
    }

    // Visualizar la dirección y configuración en el editor
    void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;

        // Mostrar dirección de movimiento
        if (direction != Vector3.zero)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, transform.position + direction * 2f);
            Gizmos.DrawWireSphere(transform.position + direction * 2f, 0.1f);
        }

        // Mostrar collider
        if (projectileCollider != null)
        {
            Gizmos.color = Color.yellow;
            if (projectileCollider is CircleCollider2D circleCol)
            {
                Gizmos.DrawWireSphere(transform.position, circleCol.radius);
            }
            else if (projectileCollider is BoxCollider2D boxCol)
            {
                Gizmos.DrawWireCube(transform.position, boxCol.size);
            }
        }
    }

    // Propiedades públicas esenciales
    public Vector3 Direction => direction;
    public float CurrentSpeed => speed;
    public float RemainingLifetime => lifetimeTimer;
    public bool HasHit => hasHit;
}
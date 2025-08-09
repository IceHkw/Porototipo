using UnityEngine;
using System.Collections;

/// <summary>
/// Comportamiento de ataque melee que se activa mediante Animation Events.
/// </summary>
public class MeleeAttackBehavior : EnemyBehaviorBase
{
    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private float attackCooldown = 2f;

    [Header("Attack Area (Configurable)")]
    [Tooltip("El punto desde donde se origina el ataque. Debe ser un Transform hijo del enemigo.")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRadius = 0.5f;
    [SerializeField] private LayerMask playerLayer = -1;

    [Header("Timing")]
    [Tooltip("Tiempo que el enemigo se detiene después de iniciar el ataque.")]
    [SerializeField] private float postAttackDelay = 0.75f;

    // Referencia al controlador de animaciones
    private EnemyAnimatorController enemyAnimator;

    // Estado
    private float lastAttackTime = -10f;
    private bool isExecutingAttack = false;

    #region Initialization
    protected override void OnStart()
    {
        // Buscamos el controlador de animaciones en los hijos
        enemyAnimator = GetComponentInChildren<EnemyAnimatorController>();

        if (attackPoint == null)
        {
            Debug.LogError($"[MeleeAttackBehavior] en '{gameObject.name}': El 'attackPoint' no está asignado en el Inspector.", this);
            enableBehavior = false; // Deshabilitamos el script si no está bien configurado
        }
    }

    protected override void OnCleanup() { }
    #endregion

    #region Behavior Update
    protected override void UpdateBehavior()
    {
        // No hacer nada si ya estamos en medio de un ataque
        if (isExecutingAttack) return;

        // Si el jugador está en rango y no estamos en cooldown, atacamos
        if (IsPlayerInRange(attackRange) && CanAttack())
        {
            StartCoroutine(AttackCoroutine());
        }
    }
    #endregion

    private bool CanAttack()
    {
        return Time.time >= lastAttackTime + attackCooldown;
    }

    private IEnumerator AttackCoroutine()
    {
        isExecutingAttack = true;
        lastAttackTime = Time.time;

        // Detenemos el movimiento
        if (movement != null)
        {
            movement.StopMovement();
            movement.SetCanMove(false);
        }

        // 1. Disparamos la animación a través del controlador
        enemyAnimator?.TriggerAttack();

        // 2. La animación se reproduce y en el frame clave llamará a OnAttackHit()
        // (a través del EnemyAnimationEventForwarder)

        // 3. Esperamos un tiempo antes de que el enemigo pueda volver a moverse
        yield return new WaitForSeconds(postAttackDelay);

        if (movement != null)
        {
            movement.SetCanMove(true);
        }
        isExecutingAttack = false;
    }

    /// <summary>
    /// ESTE MÉTODO ES PÚBLICO PARA SER LLAMADO DESDE UN ANIMATION EVENT.
    /// Contiene la lógica de detección y aplicación de daño.
    /// </summary>
    public void OnAttackHit()
    {
        if (attackPoint == null) return;

        if (showDebugInfo) Debug.Log($"[{name}] Animation Event 'OnAttackHit' disparado.");

        // Detectar colliders en el punto y radio de ataque
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, playerLayer);

        foreach (Collider2D hit in hits)
        {
            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable != null && damageable.IsAlive)
            {
                // Aplicamos daño al jugador
                damageable.TakeDamage(attackDamage, hit.transform.position, transform);

                // Registramos el daño para estadísticas
                if (enemyCore != null)
                {
                    enemyCore.RegisterDamageDealt(attackDamage);
                }

                if (showDebugInfo) Debug.Log($"[{name}] golpeó a '{hit.name}' con {attackDamage} de daño.");

                // Rompemos el bucle para aplicar daño solo una vez por ataque
                break;
            }
        }
    }

    #region Gizmos
    public override void DrawBehaviorGizmos()
    {
        // Rango para iniciar el ataque
        Gizmos.color = new Color(1, 0.5f, 0, 0.2f); // Naranja
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Área de efecto del golpe
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
        }
    }
    #endregion
}
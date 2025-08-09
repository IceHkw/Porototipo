// Enemy/RangedAttackBehavior.cs
using UnityEngine;
using System.Collections;

/// <summary>
/// Comportamiento para enemigos que atacan a distancia, sincronizado con Animation Events.
/// </summary>
public class RangedAttackBehavior : EnemyBehaviorBase
{
    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 10f;
    [SerializeField] private float attackCooldown = 3f;
    [SerializeField] private string castAnimationTrigger = "cast";

    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private int projectileDamage = 1;

    // Referencias
    private EnemyAnimatorController enemyAnimator;
    private float lastAttackTime = -10f;

    protected override void OnStart()
    {
        enemyAnimator = GetComponentInChildren<EnemyAnimatorController>();
        if (firePoint == null)
        {
            // Si no hay un punto de disparo, usamos la posición del enemigo como fallback
            firePoint = transform;
            Debug.LogWarning($"[RangedAttackBehavior] en '{gameObject.name}': No se asignó 'firePoint', se usará la posición del objeto.", this);
        }
    }

    protected override void UpdateBehavior()
    {
        if (isExecutingBehavior || player == null) return;

        if (IsPlayerInRange(attackRange) && CanAttack())
        {
            StartAttack();
        }
    }

    private bool CanAttack()
    {
        return Time.time >= lastAttackTime + attackCooldown;
    }

    private void StartAttack()
    {
        isExecutingBehavior = true;
        lastAttackTime = Time.time;
        movement?.HaltIntentionalMovement(true);

        // 1. Mirar al jugador (usando la lógica mejorada de startsFacingLeft)
        Vector3 directionToPlayer = GetDirectionToPlayer();
        if (Mathf.Abs(directionToPlayer.x) > 0.01f && movement != null)
        {
            float facingMultiplier = movement.startsFacingLeft ? -1f : 1f;
            transform.localScale = new Vector3(Mathf.Sign(directionToPlayer.x) * facingMultiplier, 1, 1);
        }

        // 2. Disparamos la animación de casteo.
        // La animación se encargará de llamar a los eventos en los momentos clave.
        enemyAnimator?.TriggerGenericAnimation(castAnimationTrigger);
    }

    /// <summary>
    /// ESTE MÉTODO ES PÚBLICO PARA SER LLAMADO DESDE UN ANIMATION EVENT.
    /// Contiene la lógica para instanciar el proyectil.
    /// </summary>
    public void OnCastAnimationShoot()
    {
        if (showDebugInfo) Debug.Log($"[{name}] Animation Event 'OnCastAnimationShoot' disparado.");

        if (projectilePrefab == null || firePoint == null || player == null) return;

        GameObject projGO = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        EnemyProjectile projectile = projGO.GetComponent<EnemyProjectile>();

        if (projectile != null)
        {
            Vector2 direction = (player.position - firePoint.position).normalized;
            projectile.Initialize(direction, projectileSpeed, projectileDamage);

            // Registrar el daño que HARÍA el proyectil para estadísticas
            if (enemyCore != null)
            {
                // Nota: Esto no es daño real, sino una forma de registrar el intento de ataque.
                // El daño real se registra si el proyectil golpea.
            }
        }
    }

    /// <summary>
    /// ESTE MÉTODO ES PÚBLICO PARA SER LLAMADO DESDE UN ANIMATION EVENT.
    /// Finaliza la secuencia de ataque, permitiendo que el enemigo se mueva de nuevo.
    /// </summary>
    public void OnCastAnimationEnd()
    {
        if (showDebugInfo) Debug.Log($"[{name}] Animation Event 'OnCastAnimationEnd' disparado.");

        movement?.HaltIntentionalMovement(false);
        isExecutingBehavior = false;
    }

    public override void DrawBehaviorGizmos()
    {
        Gizmos.color = debugGizmoColor;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    protected override void OnCleanup() { }
}
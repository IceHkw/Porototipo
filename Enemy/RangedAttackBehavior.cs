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
            // Si no hay un punto de disparo, usamos la posici�n del enemigo como fallback
            firePoint = transform;
            Debug.LogWarning($"[RangedAttackBehavior] en '{gameObject.name}': No se asign� 'firePoint', se usar� la posici�n del objeto.", this);
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

        // 1. Mirar al jugador (usando la l�gica mejorada de startsFacingLeft)
        Vector3 directionToPlayer = GetDirectionToPlayer();
        if (Mathf.Abs(directionToPlayer.x) > 0.01f && movement != null)
        {
            float facingMultiplier = movement.startsFacingLeft ? -1f : 1f;
            transform.localScale = new Vector3(Mathf.Sign(directionToPlayer.x) * facingMultiplier, 1, 1);
        }

        // 2. Disparamos la animaci�n de casteo.
        // La animaci�n se encargar� de llamar a los eventos en los momentos clave.
        enemyAnimator?.TriggerGenericAnimation(castAnimationTrigger);
    }

    /// <summary>
    /// ESTE M�TODO ES P�BLICO PARA SER LLAMADO DESDE UN ANIMATION EVENT.
    /// Contiene la l�gica para instanciar el proyectil.
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

            // Registrar el da�o que HAR�A el proyectil para estad�sticas
            if (enemyCore != null)
            {
                // Nota: Esto no es da�o real, sino una forma de registrar el intento de ataque.
                // El da�o real se registra si el proyectil golpea.
            }
        }
    }

    /// <summary>
    /// ESTE M�TODO ES P�BLICO PARA SER LLAMADO DESDE UN ANIMATION EVENT.
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
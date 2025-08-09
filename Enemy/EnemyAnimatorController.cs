// Enemy/EnemyAnimatorController.cs
using UnityEngine;

/// <summary>
/// Controla las animaciones del enemigo basándose en su estado.
/// </summary>
// --- CAMBIO: No es necesario requerir el Animator aquí si está en un hijo ---
// [RequireComponent(typeof(Animator))] 
public class EnemyAnimatorController : MonoBehaviour
{
    [Header("Referencias (Opcional, se buscan automáticamente)")]
    [SerializeField] private Animator animator;
    [SerializeField] private EnemyCore enemyCore;
    [SerializeField] private EnemyMovementBase enemyMovement;

    // Hashes de parámetros del Animator para optimización
    private readonly int hashIsMoving = Animator.StringToHash("isMoving");
    private readonly int hashAttack = Animator.StringToHash("attack");

    void Awake()
    {
        // --- CAMBIO CLAVE AQUÍ ---
        // Obtener Animator de este objeto o de sus hijos para más flexibilidad.
        animator = GetComponentInChildren<Animator>();

        enemyCore = GetComponentInParent<EnemyCore>();
        enemyMovement = GetComponentInParent<EnemyMovementBase>();

        if (animator == null)
        {
            Debug.LogError($"[EnemyAnimatorController] en '{gameObject.name}': No se encontró un Animator en este objeto o en sus hijos.", this);
        }
    }

    void Update()
    {
        // Añadimos una comprobación por si no se encontró el animator
        if (animator == null || enemyCore == null || !enemyCore.IsAlive) return;

        // Actualizar parámetro de movimiento
        if (enemyMovement != null)
        {
            animator.SetBool(hashIsMoving, enemyMovement.IsMoving);
        }
    }

    /// <summary>
    /// Dispara la animación de ataque desde otros scripts (como MeleeAttackBehavior).
    /// </summary>
    public void TriggerAttack()
    {
        if (animator != null)
        {
            animator.SetTrigger(hashAttack);
        }
    }

    /// <summary>
    /// Dispara un trigger genérico en el Animator.
    /// </summary>
    public void TriggerGenericAnimation(string triggerName)
    {
        if (animator != null && !string.IsNullOrEmpty(triggerName))
        {
            animator.SetTrigger(triggerName);
        }
    }
}
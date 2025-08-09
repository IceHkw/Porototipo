// Enemy/EnemyAnimatorController.cs
using UnityEngine;

/// <summary>
/// Controla las animaciones del enemigo bas�ndose en su estado.
/// </summary>
// --- CAMBIO: No es necesario requerir el Animator aqu� si est� en un hijo ---
// [RequireComponent(typeof(Animator))] 
public class EnemyAnimatorController : MonoBehaviour
{
    [Header("Referencias (Opcional, se buscan autom�ticamente)")]
    [SerializeField] private Animator animator;
    [SerializeField] private EnemyCore enemyCore;
    [SerializeField] private EnemyMovementBase enemyMovement;

    // Hashes de par�metros del Animator para optimizaci�n
    private readonly int hashIsMoving = Animator.StringToHash("isMoving");
    private readonly int hashAttack = Animator.StringToHash("attack");

    void Awake()
    {
        // --- CAMBIO CLAVE AQU� ---
        // Obtener Animator de este objeto o de sus hijos para m�s flexibilidad.
        animator = GetComponentInChildren<Animator>();

        enemyCore = GetComponentInParent<EnemyCore>();
        enemyMovement = GetComponentInParent<EnemyMovementBase>();

        if (animator == null)
        {
            Debug.LogError($"[EnemyAnimatorController] en '{gameObject.name}': No se encontr� un Animator en este objeto o en sus hijos.", this);
        }
    }

    void Update()
    {
        // A�adimos una comprobaci�n por si no se encontr� el animator
        if (animator == null || enemyCore == null || !enemyCore.IsAlive) return;

        // Actualizar par�metro de movimiento
        if (enemyMovement != null)
        {
            animator.SetBool(hashIsMoving, enemyMovement.IsMoving);
        }
    }

    /// <summary>
    /// Dispara la animaci�n de ataque desde otros scripts (como MeleeAttackBehavior).
    /// </summary>
    public void TriggerAttack()
    {
        if (animator != null)
        {
            animator.SetTrigger(hashAttack);
        }
    }

    /// <summary>
    /// Dispara un trigger gen�rico en el Animator.
    /// </summary>
    public void TriggerGenericAnimation(string triggerName)
    {
        if (animator != null && !string.IsNullOrEmpty(triggerName))
        {
            animator.SetTrigger(triggerName);
        }
    }
}
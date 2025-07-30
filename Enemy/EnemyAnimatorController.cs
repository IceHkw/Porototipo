// Enemy/EnemyAnimatorController.cs
using UnityEngine;

/// <summary>
/// Controla las animaciones del enemigo basándose en su estado.
/// </summary>
[RequireComponent(typeof(Animator))]
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
        // Obtener componentes, buscando en el padre para mantener la estructura modular
        animator = GetComponent<Animator>();
        enemyCore = GetComponentInParent<EnemyCore>();
        enemyMovement = GetComponentInParent<EnemyMovementBase>();
    }

    void Update()
    {
        if (enemyCore == null || !enemyCore.IsAlive) return;

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
        animator.SetTrigger(hashAttack);
    }
}
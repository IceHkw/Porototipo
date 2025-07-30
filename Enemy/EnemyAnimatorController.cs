// Enemy/EnemyAnimatorController.cs
using UnityEngine;

/// <summary>
/// Controla las animaciones del enemigo bas�ndose en su estado.
/// </summary>
[RequireComponent(typeof(Animator))]
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
        // Obtener componentes, buscando en el padre para mantener la estructura modular
        animator = GetComponent<Animator>();
        enemyCore = GetComponentInParent<EnemyCore>();
        enemyMovement = GetComponentInParent<EnemyMovementBase>();
    }

    void Update()
    {
        if (enemyCore == null || !enemyCore.IsAlive) return;

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
        animator.SetTrigger(hashAttack);
    }
}
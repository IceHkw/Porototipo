// Enemy/EnemyAnimationEventForwarder.cs
using UnityEngine;

/// <summary>
/// Recibe eventos de animaci�n desde el Animator y los reenv�a
/// al script de comportamiento correspondiente en el objeto padre.
/// Ahora es compatible con ataques melee y a distancia.
/// </summary>
public class EnemyAnimationEventForwarder : MonoBehaviour
{
    private MeleeAttackBehavior meleeAttackBehavior;
    private RangedAttackBehavior rangedAttackBehavior;

    void Awake()
    {
        // Busca los scripts de comportamiento en el padre
        meleeAttackBehavior = GetComponentInParent<MeleeAttackBehavior>();
        rangedAttackBehavior = GetComponentInParent<RangedAttackBehavior>();
    }

    // --- Eventos para MeleeAttackBehavior ---

    /// <summary>
    /// El Animator llamar� a esta funci�n para el golpe de melee.
    /// </summary>
    public void OnAttackHit()
    {
        if (meleeAttackBehavior != null)
        {
            meleeAttackBehavior.OnAttackHit();
        }
        else
        {
            // Log de error si el evento se llama pero no hay script para recibirlo
            if (rangedAttackBehavior == null)
                Debug.LogError("AnimationEventForwarder no pudo encontrar MeleeAttackBehavior en el padre.", transform.parent.gameObject);
        }
    }

    // --- Eventos para RangedAttackBehavior ---

    /// <summary>
    /// El Animator llamar� a esta funci�n para disparar el proyectil.
    /// </summary>
    public void OnCastShoot()
    {
        if (rangedAttackBehavior != null)
        {
            rangedAttackBehavior.OnCastAnimationShoot();
        }
        else
        {
            if (meleeAttackBehavior == null)
                Debug.LogError("AnimationEventForwarder no pudo encontrar RangedAttackBehavior en el padre.", transform.parent.gameObject);
        }
    }

    /// <summary>
    /// El Animator llamar� a esta funci�n para finalizar la animaci�n de casteo.
    /// </summary>
    public void OnCastEnd()
    {
        if (rangedAttackBehavior != null)
        {
            rangedAttackBehavior.OnCastAnimationEnd();
        }
        else
        {
            if (meleeAttackBehavior == null)
                Debug.LogError("AnimationEventForwarder no pudo encontrar RangedAttackBehavior en el padre.", transform.parent.gameObject);
        }
    }
}
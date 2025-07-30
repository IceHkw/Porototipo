// Enemy/EnemyAnimationEventForwarder.cs
using UnityEngine;

/// <summary>
/// Recibe eventos de animaci�n desde el Animator y los reenv�a
/// al script de comportamiento correspondiente en el objeto padre.
/// </summary>
public class EnemyAnimationEventForwarder : MonoBehaviour
{
    private MeleeAttackBehavior meleeAttackBehavior;

    void Awake()
    {
        // Busca el script de ataque en el padre
        meleeAttackBehavior = GetComponentInParent<MeleeAttackBehavior>();
    }

    /// <summary>
    /// El Animator en este GameObject ("Visuals") llamar� a esta funci�n.
    /// </summary>
    public void OnAttackHit()
    {
        if (meleeAttackBehavior != null)
        {
            meleeAttackBehavior.OnAttackHit();
        }
        else
        {
            Debug.LogError("AnimationEventForwarder no pudo encontrar MeleeAttackBehavior en el padre.", transform.parent.gameObject);
        }
    }
}
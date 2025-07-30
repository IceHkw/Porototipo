// Enemy/EnemyAnimationEventForwarder.cs
using UnityEngine;

/// <summary>
/// Recibe eventos de animación desde el Animator y los reenvía
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
    /// El Animator en este GameObject ("Visuals") llamará a esta función.
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
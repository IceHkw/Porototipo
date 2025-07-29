using UnityEngine;

/// <summary>
/// Este script se coloca en el mismo GameObject que el Animator.
/// Su única función es recibir eventos de animación y reenviarlos
/// al componente SwordAttack en el objeto padre.
/// </summary>
public class AnimationEventForwarder : MonoBehaviour
{
    private SwordAttack swordAttack;

    void Awake()
    {
        // Busca el script SwordAttack en el objeto padre al iniciar.
        swordAttack = GetComponentInParent<SwordAttack>();
    }

    // El Animator en este objeto ("Visuals") llamará a esta función.
    public void OnAttackHit()
    {
        // Si encontramos el script principal, le reenviamos la llamada.
        if (swordAttack != null)
        {
            swordAttack.OnAttackHit();
        }
        else
        {
            // Un mensaje de error por si algo falla.
            Debug.LogError("AnimationEventForwarder no pudo encontrar el componente SwordAttack en el padre.", transform.parent.gameObject);
        }
    }
}
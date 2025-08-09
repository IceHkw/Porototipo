using UnityEngine;

/// <summary>
/// Este script act�a como un puente. Se coloca en el mismo GameObject que el Animator del jugador (ej. en "Visuals").
/// Recibe los eventos de animaci�n y los reenv�a al PlayerAnimatorController en el objeto padre.
/// </summary>
public class PlayerAnimationEventForwarder : MonoBehaviour
{
    private PlayerAnimatorController playerAnimatorController;

    void Awake()
    {
        // Al iniciar, busca el script principal en el objeto padre.
        playerAnimatorController = GetComponentInParent<PlayerAnimatorController>();

        if (playerAnimatorController == null)
        {
            Debug.LogError("PlayerAnimationEventForwarder: No se pudo encontrar 'PlayerAnimatorController' en los padres de este objeto.", transform.parent);
        }
    }

    /// <summary>
    /// El Animator en este GameObject llamar� a esta funci�n a trav�s de un Animation Event.
    /// </summary>
    public void AttackAnimationEnd()
    {
        // Si la referencia es v�lida, reenv�a la llamada al m�todo correspondiente en el script principal.
        if (playerAnimatorController != null)
        {
            playerAnimatorController.AttackAnimationEnd();
        }
    }
}
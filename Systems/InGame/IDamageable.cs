using UnityEngine;

/// <summary>
/// Interfaz para cualquier objeto que pueda recibir da�o en el juego
/// </summary>
public interface IDamageable
{
    // ===== M�TODOS PRINCIPALES =====

    /// <summary>
    /// Aplica da�o simple al objeto
    /// </summary>
    /// <param name="damage">Cantidad de da�o a aplicar</param>
    void TakeDamage(int damage);

    /// <summary>
    /// Aplica da�o con informaci�n adicional para efectos avanzados (knockback, etc.)
    /// </summary>
    /// <param name="damage">Cantidad de da�o a aplicar</param>
    /// <param name="hitPoint">Punto exacto donde impact� el ataque</param>
    /// <param name="damageSource">Transform del objeto que caus� el da�o</param>
    void TakeDamage(int damage, Vector3 hitPoint, Transform damageSource);

    // ===== PROPIEDADES =====

    /// <summary>
    /// Indica si el objeto est� vivo
    /// </summary>
    bool IsAlive { get; }

    /// <summary>
    /// Salud actual del objeto
    /// </summary>
    int CurrentHealth { get; }

    /// <summary>
    /// Salud m�xima del objeto
    /// </summary>
    int MaxHealth { get; }

    // ===== M�TODOS OPCIONALES =====

    /// <summary>
    /// Transform del objeto da�able (�til para c�lculos de direcci�n)
    /// </summary>
    Transform Transform { get; }

    /// <summary>
    /// Posici�n del objeto da�able
    /// </summary>
    Vector3 Position { get; }
}
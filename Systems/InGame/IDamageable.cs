using UnityEngine;

/// <summary>
/// Interfaz para cualquier objeto que pueda recibir daño en el juego
/// </summary>
public interface IDamageable
{
    // ===== MÉTODOS PRINCIPALES =====

    /// <summary>
    /// Aplica daño simple al objeto
    /// </summary>
    /// <param name="damage">Cantidad de daño a aplicar</param>
    void TakeDamage(int damage);

    /// <summary>
    /// Aplica daño con información adicional para efectos avanzados (knockback, etc.)
    /// </summary>
    /// <param name="damage">Cantidad de daño a aplicar</param>
    /// <param name="hitPoint">Punto exacto donde impactó el ataque</param>
    /// <param name="damageSource">Transform del objeto que causó el daño</param>
    void TakeDamage(int damage, Vector3 hitPoint, Transform damageSource);

    // ===== PROPIEDADES =====

    /// <summary>
    /// Indica si el objeto está vivo
    /// </summary>
    bool IsAlive { get; }

    /// <summary>
    /// Salud actual del objeto
    /// </summary>
    int CurrentHealth { get; }

    /// <summary>
    /// Salud máxima del objeto
    /// </summary>
    int MaxHealth { get; }

    // ===== MÉTODOS OPCIONALES =====

    /// <summary>
    /// Transform del objeto dañable (útil para cálculos de dirección)
    /// </summary>
    Transform Transform { get; }

    /// <summary>
    /// Posición del objeto dañable
    /// </summary>
    Vector3 Position { get; }
}
using UnityEngine;
using System;

/// <summary>
/// Interfaz para todas las habilidades de movimiento de los personajes.
/// Cada personaje tendrб UNA habilidad de movimiento ъnica.
/// </summary>
public interface IMovementAbility
{
    /// <summary>
    /// Nombre de la habilidad para UI y debugging
    /// </summary>
    string AbilityName { get; }

    /// <summary>
    /// Icono de la habilidad para la UI (opcional)
    /// </summary>
    Sprite AbilityIcon { get; }

    /// <summary>
    /// Si la habilidad estб disponible para usar
    /// </summary>
    bool IsReady { get; }

    /// <summary>
    /// Si la habilidad se estб ejecutando actualmente
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Cooldown actual (0 si estб lista)
    /// </summary>
    float CurrentCooldown { get; }

    /// <summary>
    /// Cooldown mбximo de la habilidad
    /// </summary>
    float MaxCooldown { get; }

    /// <summary>
    /// Evento disparado cuando cambia el cooldown
    /// Envнa: (cooldown actual, cooldown mбximo, porcentaje de cooldown restante 0-1)
    /// </summary>
    event Action<float, float, float> OnCooldownChanged;

    /// <summary>
    /// Intenta activar la habilidad
    /// </summary>
    /// <returns>True si se activу exitosamente</returns>
    bool TryActivate();

    /// <summary>
    /// Fuerza la interrupciуn de la habilidad si estб activa
    /// </summary>
    void ForceStop();

    /// <summary>
    /// Resetea el cooldown de la habilidad
    /// </summary>
    void ResetCooldown();

    /// <summary>
    /// Llamado cuando el personaje muere
    /// </summary>
    void OnPlayerDeath();

    /// <summary>
    /// Llamado cuando el personaje respawnea
    /// </summary>
    void OnPlayerRespawn();
}
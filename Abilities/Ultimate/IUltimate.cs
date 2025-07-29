using UnityEngine;
using System;

/// <summary>
/// Interfaz para todas las habilidades ultimate de los personajes.
/// Cada personaje tendrб UNA ultimate ъnica.
/// </summary>
public interface IUltimateAbility
{
    /// <summary>
    /// Nombre de la ultimate para UI y debugging
    /// </summary>
    string AbilityName { get; }

    /// <summary>
    /// Descripciуn de la ultimate para tooltips
    /// </summary>
    string AbilityDescription { get; }

    /// <summary>
    /// Icono de la ultimate para la UI
    /// </summary>
    Sprite AbilityIcon { get; }

    /// <summary>
    /// Si la ultimate estб lista para usar
    /// </summary>
    bool IsReady { get; }

    /// <summary>
    /// Si la ultimate se estб ejecutando actualmente
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Porcentaje de carga de la ultimate (0-1)
    /// </summary>
    float ChargePercent { get; }

    /// <summary>
    /// Duraciуn de la ultimate si es temporal
    /// </summary>
    float Duration { get; }

    /// <summary>
    /// Tiempo restante si estб activa
    /// </summary>
    float RemainingTime { get; }

    /// <summary>
    /// Evento disparado cuando cambia la carga
    /// Envнa: (porcentaje de carga 0-1)
    /// </summary>
    event Action<float> OnChargeChanged;

    /// <summary>
    /// Evento disparado cuando cambia el estado activo
    /// Envнa: (estб activa, tiempo restante si aplica)
    /// </summary>
    event Action<bool, float> OnActiveStateChanged;

    /// <summary>
    /// Intenta activar la ultimate
    /// </summary>
    /// <returns>True si se activу exitosamente</returns>
    bool TryActivate();

    /// <summary>
    /// Aсade carga a la ultimate (por matar enemigos, hacer daсo, etc.)
    /// </summary>
    /// <param name="amount">Cantidad de carga a aсadir (0-1)</param>
    void AddCharge(float amount);

    /// <summary>
    /// Fuerza la interrupciуn de la ultimate si estб activa
    /// </summary>
    void ForceStop();

    /// <summary>
    /// Resetea la carga de la ultimate a 0
    /// </summary>
    void ResetCharge();

    /// <summary>
    /// Llamado cuando el personaje muere
    /// </summary>
    void OnPlayerDeath();

    /// <summary>
    /// Llamado cuando el personaje respawnea
    /// </summary>
    void OnPlayerRespawn();
}
using UnityEngine;
using System;

/// <summary>
/// Interfaz base para todos los comportamientos de KillStreak
/// </summary>
public interface IKillStreakBehavior
{
    /// <summary>
    /// Nombre de la KillStreak para debug
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Si la KillStreak está activa actualmente
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Transform del jugador para referencia de posición
    /// </summary>
    Transform PlayerTransform { get; set; }

    /// <summary>
    /// Evento cuando la KillStreak se activa
    /// </summary>
    event Action<IKillStreakBehavior> OnActivated;

    /// <summary>
    /// Evento cuando la KillStreak se desactiva
    /// </summary>
    event Action<IKillStreakBehavior> OnDeactivated;

    /// <summary>
    /// Inicializa la KillStreak con la definición y el nivel inicial.
    /// </summary>
    void Initialize(KillStreakDefinition definition, Transform player, int initialLevel);

    /// <summary>
    /// Actualiza las estadísticas del comportamiento al subir de nivel.
    /// </summary>
    void UpdateStats(int newLevel);

    /// <summary>
    /// Activa la KillStreak
    /// </summary>
    void Activate();

    /// <summary>
    /// Desactiva la KillStreak
    /// </summary>
    void Deactivate();

    /// <summary>
    /// Resetea la KillStreak a su estado inicial
    /// </summary>
    void Reset();
}
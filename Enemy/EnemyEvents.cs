using UnityEngine;
using System;

/// <summary>
/// Sistema centralizado de eventos para enemigos
/// </summary>
public static class EnemyEvents
{
    // ===== EVENTOS DE CICLO DE VIDA =====

    /// <summary>
    /// Se dispara cuando un enemigo aparece/spawn
    /// </summary>
    public static event Action<EnemyCore> OnEnemySpawned;

    /// <summary>
    /// Se dispara cuando un enemigo muere
    /// </summary>
    public static event Action<EnemyCore> OnEnemyDeath;

    /// <summary>
    /// Se dispara cuando un enemigo es devuelto al pool
    /// </summary>
    public static event Action<EnemyCore> OnEnemyReturnedToPool;

    // ===== EVENTOS DE COMBATE =====

    /// <summary>
    /// Se dispara cuando un enemigo recibe daño
    /// </summary>
    public static event Action<EnemyCore, int, Vector3, Transform> OnEnemyDamaged;

    /// <summary>
    /// Se dispara cuando un enemigo es derrotado por el jugador
    /// </summary>
    public static event Action<EnemyCore, Transform> OnEnemyKilledByPlayer;

    /// <summary>
    /// Se dispara cuando un enemigo ataca exitosamente
    /// </summary>
    public static event Action<EnemyCore, int> OnEnemyAttackHit;

    // ===== EVENTOS DE COMPORTAMIENTO =====

    /// <summary>
    /// Se dispara cuando un enemigo detecta al jugador
    /// </summary>
    public static event Action<EnemyCore> OnEnemyDetectedPlayer;

    /// <summary>
    /// Se dispara cuando un enemigo pierde de vista al jugador
    /// </summary>
    public static event Action<EnemyCore> OnEnemyLostPlayer;

    /// <summary>
    /// Se dispara cuando un enemigo cambia de estado
    /// </summary>
    public static event Action<EnemyCore, string> OnEnemyStateChanged;

    // ===== MÉTODOS PARA DISPARAR EVENTOS =====

    public static void TriggerEnemySpawned(EnemyCore enemy)
    {
        OnEnemySpawned?.Invoke(enemy);
    }

    public static void TriggerEnemyDeath(EnemyCore enemy)
    {
        OnEnemyDeath?.Invoke(enemy);
    }

    public static void TriggerEnemyReturnedToPool(EnemyCore enemy)
    {
        OnEnemyReturnedToPool?.Invoke(enemy);
    }

    public static void TriggerEnemyDamaged(EnemyCore enemy, int damage, Vector3 hitPoint, Transform damageSource)
    {
        OnEnemyDamaged?.Invoke(enemy, damage, hitPoint, damageSource);
    }

    public static void TriggerEnemyKilledByPlayer(EnemyCore enemy, Transform killer)
    {
        OnEnemyKilledByPlayer?.Invoke(enemy, killer);
    }

    public static void TriggerEnemyAttackHit(EnemyCore enemy, int damage)
    {
        OnEnemyAttackHit?.Invoke(enemy, damage);
    }

    public static void TriggerEnemyDetectedPlayer(EnemyCore enemy)
    {
        OnEnemyDetectedPlayer?.Invoke(enemy);
    }

    public static void TriggerEnemyLostPlayer(EnemyCore enemy)
    {
        OnEnemyLostPlayer?.Invoke(enemy);
    }

    public static void TriggerEnemyStateChanged(EnemyCore enemy, string newState)
    {
        OnEnemyStateChanged?.Invoke(enemy, newState);
    }

    // ===== UTILIDADES =====

    /// <summary>
    /// Limpia todos los eventos (útil para cambios de escena)
    /// </summary>
    public static void ClearAllEvents()
    {
        OnEnemySpawned = null;
        OnEnemyDeath = null;
        OnEnemyReturnedToPool = null;
        OnEnemyDamaged = null;
        OnEnemyKilledByPlayer = null;
        OnEnemyAttackHit = null;
        OnEnemyDetectedPlayer = null;
        OnEnemyLostPlayer = null;
        OnEnemyStateChanged = null;
    }

    /// <summary>
    /// Información de debug sobre suscriptores
    /// </summary>
    public static void LogEventSubscribers()
    {
        Debug.Log("=== Enemy Events Subscribers ===");
        LogEvent("OnEnemySpawned", OnEnemySpawned);
        LogEvent("OnEnemyDeath", OnEnemyDeath);
        LogEvent("OnEnemyDamaged", OnEnemyDamaged);
        LogEvent("OnEnemyKilledByPlayer", OnEnemyKilledByPlayer);
    }

    private static void LogEvent(string eventName, Delegate eventDelegate)
    {
        if (eventDelegate != null)
        {
            Debug.Log($"{eventName}: {eventDelegate.GetInvocationList().Length} subscribers");
        }
        else
        {
            Debug.Log($"{eventName}: No subscribers");
        }
    }
}

/// <summary>
/// Información adicional sobre enemigos para eventos
/// </summary>
[System.Serializable]
public class EnemyDeathInfo
{
    public EnemyCore enemy;
    public Transform killer;
    public Vector3 deathPosition;
    public string enemyType;
    public float survivalTime;
    public int damageDealt;

    public EnemyDeathInfo(EnemyCore enemy, Transform killer)
    {
        this.enemy = enemy;
        this.killer = killer;
        this.deathPosition = enemy.transform.position;
        this.enemyType = enemy.PoolObjectType;
        // survivalTime y damageDealt se pueden trackear si es necesario
    }
}
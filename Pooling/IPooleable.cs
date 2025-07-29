// ====================================
// IPoolable.cs
// Interface para objetos que pueden ser pooleados
// ====================================

using UnityEngine;

/// <summary>
/// Interface que deben implementar todos los objetos que pueden ser manejados por el ObjectPoolManager
/// </summary>
public interface IPoolable
{
    /// <summary>
    /// Llamado cuando el objeto es sacado del pool y debe ser activado
    /// </summary>
    /// <param name="position">Posición donde debe aparecer</param>
    /// <param name="rotation">Rotación inicial</param>
    void OnSpawnFromPool(Vector3 position, Quaternion rotation);

    /// <summary>
    /// Llamado cuando el objeto debe regresar al pool
    /// </summary>
    void OnReturnToPool();

    /// <summary>
    /// Indica si el objeto está actualmente siendo usado (no está en el pool)
    /// </summary>
    bool IsActiveInPool { get; }

    /// <summary>
    /// El GameObject asociado con este objeto pooleable
    /// </summary>
    GameObject PooledGameObject { get; }

    /// <summary>
    /// Tipo de objeto para identificación en el pool
    /// </summary>
    string PoolObjectType { get; }
}

/// <summary>
/// Extensión opcional para objetos que necesitan configuración adicional al ser spawneados
/// </summary>
public interface IPoolableWithConfig : IPoolable
{
    /// <summary>
    /// Llamado cuando el objeto necesita configuración específica después del spawn
    /// </summary>
    /// <param name="config">Configuración específica del objeto</param>
    void ConfigureFromPool(object config);
}

/// <summary>
/// Tipos de objetos pooleables predefinidos
/// </summary>
public static class PoolObjectTypes
{
    // Enemigos
    public const string ENEMY_BASIC = "Enemy_Basic";
    public const string ENEMY_TANK = "Enemy_Tank";
    public const string ENEMY_FLYING = "Enemy_Flying";
    public const string ENEMY_RANGED = "Enemy_Ranged";
    public const string ENEMY_SPECIAL = "Enemy_Special";

    // Proyectiles (para futuro uso)
    public const string PROJECTILE_BULLET = "Projectile_Bullet";
    public const string PROJECTILE_ROCKET = "Projectile_Rocket";

    // Efectos (para futuro uso)
    public const string VFX_EXPLOSION = "VFX_Explosion";
    public const string VFX_HIT_EFFECT = "VFX_HitEffect";

    // Pickups (para futuro uso)
    public const string PICKUP_HEALTH = "Pickup_Health";
    public const string PICKUP_AMMO = "Pickup_Ammo";

    // Gibbing (para futuro uso)
    public const string GIB_CHUNK = "Gib_Chunk";
}

/// <summary>
/// Eventos del sistema de pooling
/// </summary>
public static class PoolEvents
{
    public static System.Action<string, int> OnObjectSpawnedFromPool;
    public static System.Action<string, int> OnObjectReturnedToPool;
    public static System.Action<string, int, int> OnPoolExpanded; // type, oldSize, newSize
    public static System.Action<string> OnPoolCreated;
}
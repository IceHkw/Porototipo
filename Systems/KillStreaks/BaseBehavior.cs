// Code/Systems/KillStreaks/BaseBehavior.cs
using UnityEngine;
using System;

/// <summary>
/// Clase base abstracta para todos los comportamientos de KillStreak
/// </summary>
public abstract class BaseKillStreakBehavior : MonoBehaviour, IKillStreakBehavior
{
    [Header("═══════════════════════════════════════")]
    [Header("DEBUG")]
    [Header("═══════════════════════════════════════")]

    [SerializeField] protected bool enableDebugLogs = false;
    [SerializeField] protected bool showDebugGizmos = true;

    // ===== ESTADO =====
    protected KillStreakDefinition definition;
    protected KillStreakLevelStats currentStats; // Almacena las stats del nivel actual
    protected Transform playerTransform;
    protected bool isActive = false;
    protected bool isInitialized = false;
    protected int currentLevel;

    // ===== COMPONENTES COMUNES =====
    protected AudioSource audioSource;
    protected ParticleSystem[] particleSystems;

    // ===== EVENTOS =====
    public event Action<IKillStreakBehavior> OnActivated;
    public event Action<IKillStreakBehavior> OnDeactivated;

    // ===== IMPLEMENTACIÓN DE INTERFAZ =====
    public string Name => definition != null ? definition.KillStreakName : "Unknown KillStreak";
    public bool IsActive => isActive;
    public Transform PlayerTransform
    {
        get => playerTransform;
        set => playerTransform = value;
    }

    // ===== INICIALIZACIÓN =====
    protected virtual void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        particleSystems = GetComponentsInChildren<ParticleSystem>();
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
        }
    }

    public virtual void Initialize(KillStreakDefinition killStreakDef, Transform player, int initialLevel)
    {
        if (isInitialized) return;

        definition = killStreakDef;
        playerTransform = player;

        UpdateStats(initialLevel); // Carga las stats iniciales

        OnInitialize();
        isInitialized = true;
        DebugLog($"{Name} inicializado en Nivel {currentLevel}");
    }

    public virtual void UpdateStats(int newLevel)
    {
        if (definition == null) return;

        currentLevel = newLevel;
        currentStats = definition.GetStatsForLevel(currentLevel);

        if (currentStats == null)
        {
            Debug.LogError($"No se encontraron stats para el nivel {currentLevel} en {Name}");
            return;
        }

        DebugLog($"Stats actualizadas a Nivel {currentLevel}");
    }

    protected abstract void OnInitialize();

    // ===== ACTIVACIÓN/DESACTIVACIÓN =====
    public virtual void Activate()
    {
        if (!isInitialized)
        {
            Debug.LogError($"{Name}: Intentando activar sin inicializar!");
            return;
        }
        if (isActive) return;

        isActive = true;
        OnActivate();
        OnActivated?.Invoke(this);
        DebugLog($"{Name} activado!");
    }

    public virtual void Deactivate()
    {
        if (!isActive) return;

        isActive = false;
        OnDeactivate();
        OnDeactivated?.Invoke(this);
        DebugLog($"{Name} desactivado!");
    }

    public virtual void Reset()
    {
        Deactivate();
        OnReset();
        DebugLog($"{Name} reseteado!");
    }

    protected abstract void OnActivate();
    protected abstract void OnDeactivate();
    protected abstract void OnReset();

    // ===== UTILIDADES =====
    protected float GetDistanceToPlayer()
    {
        if (playerTransform == null) return float.MaxValue;
        return Vector3.Distance(transform.position, playerTransform.position);
    }

    protected Vector3 GetDirectionToPlayer()
    {
        if (playerTransform == null) return Vector3.zero;
        return (playerTransform.position - transform.position).normalized;
    }

    protected GameObject[] FindNearbyEnemies(float radius)
    {
        var colliders = Physics2D.OverlapCircleAll(transform.position, radius);
        var enemies = new System.Collections.Generic.List<GameObject>();
        foreach (var col in colliders)
        {
            if (col.CompareTag("Enemy"))
            {
                enemies.Add(col.gameObject);
            }
        }
        return enemies.ToArray();
    }

    protected GameObject FindClosestEnemy(float maxRadius)
    {
        var enemies = FindNearbyEnemies(maxRadius);
        if (enemies.Length == 0) return null;
        GameObject closest = null;
        float closestDistance = float.MaxValue;
        foreach (var enemy in enemies)
        {
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = enemy;
            }
        }
        return closest;
    }

    protected virtual void DamageEnemy(GameObject enemy, int damage)
    {
        if (enemy == null) return;
        var damageable = enemy.GetComponent<IDamageable>() ?? enemy.GetComponentInParent<IDamageable>();
        if (damageable != null && damageable.IsAlive)
        {
            damageable.TakeDamage(damage, enemy.transform.position, transform);
            DebugLog($"{Name} causó {damage} de daño a {enemy.name}");
        }
    }

    // ===== DEBUG =====
    protected void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[KillStreak - {Name}] {message}");
        }
    }

    protected virtual void OnDrawGizmos()
    {
        if (!showDebugGizmos || currentStats == null) return;

        if (currentStats.effectRadius > 0)
        {
            Gizmos.color = isActive ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, currentStats.effectRadius);
        }
        if (playerTransform != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, playerTransform.position);
        }
    }

    protected virtual void OnDestroy()
    {
        OnActivated = null;
        OnDeactivated = null;
    }
}
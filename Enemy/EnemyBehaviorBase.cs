using UnityEngine;

/// <summary>
/// Clase base abstracta para comportamientos de IA de enemigos
/// </summary>
[RequireComponent(typeof(EnemyCore))]
public abstract class EnemyBehaviorBase : MonoBehaviour
{
    [Header("Behavior Settings")]
    public bool enableBehavior = true;
    public float behaviorUpdateRate = 0.1f; // Frecuencia de actualización de la IA
    
    [Header("Debug")]
    public bool showDebugInfo = false;
    public Color debugGizmoColor = Color.cyan;
    
    // Referencias
    protected EnemyCore enemyCore;
    protected EnemyMovementBase movement;
    protected Transform player;
    
    // Control de actualización
    private float lastBehaviorUpdate = 0f;
    
    // Estado del comportamiento
    protected bool isExecutingBehavior = false;
    
    // Eventos
    public System.Action OnBehaviorStarted;
    public System.Action OnBehaviorEnded;
    public System.Action<string> OnStateChanged; // Para debugging
    
    #region Unity Lifecycle
    
    protected virtual void Awake()
    {
        enemyCore = GetComponent<EnemyCore>();
        movement = GetComponent<EnemyMovementBase>();
    }
    
    protected virtual void Start()
    {
        if (enemyCore != null)
        {
            player = enemyCore.player;
            
            // Suscribirse a eventos
            enemyCore.OnDeath += HandleDeath;
            enemyCore.OnDamageReceived += HandleDamageReceived;
        }
        
        OnStart();
    }
    
    protected virtual void Update()
    {
        if (!CanExecuteBehavior()) return;
        
        // Actualizar con la frecuencia especificada
        if (Time.time - lastBehaviorUpdate >= behaviorUpdateRate)
        {
            lastBehaviorUpdate = Time.time;
            UpdateBehavior();
        }
    }
    
    protected virtual void OnDestroy()
    {
        if (enemyCore != null)
        {
            enemyCore.OnDeath -= HandleDeath;
            enemyCore.OnDamageReceived -= HandleDamageReceived;
        }
        
        OnCleanup();
    }
    
    #endregion
    
    #region Abstract Methods
    
    /// <summary>
    /// Inicialización del comportamiento
    /// </summary>
    protected abstract void OnStart();
    
    /// <summary>
    /// Lógica principal del comportamiento (llamado cada behaviorUpdateRate segundos)
    /// </summary>
    protected abstract void UpdateBehavior();
    
    /// <summary>
    /// Limpieza cuando el comportamiento se destruye
    /// </summary>
    protected abstract void OnCleanup();
    
    /// <summary>
    /// Dibuja gizmos específicos del comportamiento
    /// </summary>
    public abstract void DrawBehaviorGizmos();
    
    #endregion
    
    #region Behavior Control
    
    /// <summary>
    /// Verifica si el comportamiento puede ejecutarse
    /// </summary>
    protected virtual bool CanExecuteBehavior()
    {
        if (!enableBehavior) return false;
        if (enemyCore == null || !enemyCore.IsAlive) return false;
        if (enemyCore.IsKnockedBack) return false;
        
        if (player == null)
        {
            player = enemyCore.player;
            if (player == null) return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Inicia la ejecución del comportamiento
    /// </summary>
    protected virtual void StartBehavior()
    {
        if (isExecutingBehavior) return;
        
        isExecutingBehavior = true;
        OnBehaviorStarted?.Invoke();
        
        if (showDebugInfo)
        {
            Debug.Log($"[{GetType().Name}] Comportamiento iniciado en {name}");
        }
    }
    
    /// <summary>
    /// Finaliza la ejecución del comportamiento
    /// </summary>
    protected virtual void EndBehavior()
    {
        if (!isExecutingBehavior) return;
        
        isExecutingBehavior = false;
        OnBehaviorEnded?.Invoke();
        
        if (showDebugInfo)
        {
            Debug.Log($"[{GetType().Name}] Comportamiento finalizado en {name}");
        }
    }
    
    /// <summary>
    /// Cambia el estado y notifica (útil para debugging y sistemas más complejos)
    /// </summary>
    protected void ChangeState(string newState)
    {
        OnStateChanged?.Invoke(newState);
        
        if (showDebugInfo)
        {
            Debug.Log($"[{GetType().Name}] {name} cambió a estado: {newState}");
        }
    }
    
    #endregion
    
    #region Event Handlers
    
    protected virtual void HandleDeath()
    {
        EndBehavior();
        enabled = false;
    }
    
    protected virtual void HandleDamageReceived(Vector3 hitPoint, Transform damageSource)
    {
        // Los comportamientos pueden reaccionar al daño si lo necesitan
    }
    
    #endregion
    
    #region Helper Methods
    
    /// <summary>
    /// Obtiene la distancia al jugador
    /// </summary>
    protected float GetDistanceToPlayer()
    {
        if (player == null) return float.MaxValue;
        return Vector2.Distance(transform.position, player.position);
    }
    
    /// <summary>
    /// Obtiene la dirección hacia el jugador
    /// </summary>
    protected Vector2 GetDirectionToPlayer()
    {
        if (player == null) return Vector2.zero;
        return (player.position - transform.position).normalized;
    }
    
    /// <summary>
    /// Verifica si el jugador está en rango
    /// </summary>
    protected bool IsPlayerInRange(float range)
    {
        return GetDistanceToPlayer() <= range;
    }
    
    /// <summary>
    /// Verifica si hay línea de visión directa al jugador
    /// </summary>
    protected bool HasLineOfSightToPlayer(LayerMask obstacleLayer)
    {
        if (player == null) return false;
        
        Vector2 direction = GetDirectionToPlayer();
        float distance = GetDistanceToPlayer();
        
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, distance, obstacleLayer);
        return hit.collider == null;
    }
    
    #endregion
    
    #region Properties
    
    public bool IsEnabled => enableBehavior;
    public bool IsExecuting => isExecutingBehavior;
    
    #endregion
    
    #region Gizmos
    
    void OnDrawGizmosSelected()
    {
        if (!showDebugInfo) return;
        
        DrawBehaviorGizmos();
    }
    
    #endregion
}
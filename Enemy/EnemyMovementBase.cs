using UnityEngine;

/// <summary>
/// Clase base abstracta para todos los tipos de movimiento de enemigos
/// </summary>
[RequireComponent(typeof(EnemyCore))]
public abstract class EnemyMovementBase : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float stopDistance = 0.1f;
    
    [Header("References")]
    protected EnemyCore enemyCore;
    protected Rigidbody2D rb;
    protected Transform player;
    
    [Header("Debug")]
    public bool showDebugInfo = false;
    
    // Estado
    protected Vector2 moveDirection;
    protected bool canMove = true;
    
    // Eventos
    public System.Action<Vector2> OnMovementDirectionChanged;
    public System.Action OnMovementStarted;
    public System.Action OnMovementStopped;
    
    #region Unity Lifecycle
    
    protected virtual void Awake()
    {
        enemyCore = GetComponent<EnemyCore>();
        rb = GetComponent<Rigidbody2D>();
    }
    
    protected virtual void Start()
    {
        if (enemyCore != null)
        {
            player = enemyCore.player;
            
            // Suscribirse a eventos
            enemyCore.OnKnockbackStart += HandleKnockbackStart;
            enemyCore.OnKnockbackEnd += HandleKnockbackEnd;
            enemyCore.OnDeath += HandleDeath;
        }
    }
    
    protected virtual void Update()
    {
        if (!CanPerformMovement()) return;
        
        UpdateMovement();
    }
    
    protected virtual void FixedUpdate()
    {
        if (!CanPerformMovement()) return;
        
        ApplyMovement();
    }
    
    protected virtual void OnDestroy()
    {
        // Desuscribirse de eventos
        if (enemyCore != null)
        {
            enemyCore.OnKnockbackStart -= HandleKnockbackStart;
            enemyCore.OnKnockbackEnd -= HandleKnockbackEnd;
            enemyCore.OnDeath -= HandleDeath;
        }
    }
    
    #endregion
    
    #region Abstract Methods
    
    /// <summary>
    /// Actualiza la lógica de movimiento (llamado en Update)
    /// </summary>
    protected abstract void UpdateMovement();
    
    /// <summary>
    /// Aplica el movimiento físico (llamado en FixedUpdate)
    /// </summary>
    protected abstract void ApplyMovement();
    
    /// <summary>
    /// Calcula si el enemigo debe moverse hacia el objetivo
    /// </summary>
    public abstract bool ShouldMoveToTarget();
    
    #endregion
    
    #region Movement Control
    
    /// <summary>
    /// Verifica si el enemigo puede moverse
    /// </summary>
    protected virtual bool CanPerformMovement()
    {
        if (enemyCore == null || !enemyCore.IsAlive) return false;
        if (enemyCore.IsKnockedBack) return false;
        if (!canMove) return false;
        if (player == null)
        {
            player = enemyCore.player;
            if (player == null) return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Habilita o deshabilita el movimiento
    /// </summary>
    public virtual void SetCanMove(bool value)
    {
        canMove = value;
        
        if (!value)
        {
            StopMovement();
        }
    }
    
    /// <summary>
    /// Detiene inmediatamente el movimiento
    /// </summary>
    public virtual void StopMovement()
    {
        moveDirection = Vector2.zero;
        
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
        
        OnMovementStopped?.Invoke();
    }
    
    /// <summary>
    /// Fuerza al enemigo a moverse en una dirección específica
    /// </summary>
    public virtual void ForceMove(Vector2 direction, float duration)
    {
        StartCoroutine(ForcedMovement(direction, duration));
    }
    
    protected System.Collections.IEnumerator ForcedMovement(Vector2 direction, float duration)
    {
        bool previousCanMove = canMove;
        canMove = false;
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (rb != null && enemyCore.IsAlive)
            {
                rb.linearVelocity = new Vector2(direction.x * moveSpeed, rb.linearVelocity.y);
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        canMove = previousCanMove;
    }
    
    #endregion
    
    #region Event Handlers
    
    protected virtual void HandleKnockbackStart()
    {
        StopMovement();
    }
    
    protected virtual void HandleKnockbackEnd()
    {
        // El movimiento se reanudará automáticamente en el siguiente Update
    }
    
    protected virtual void HandleDeath()
    {
        StopMovement();
        enabled = false;
    }
    
    #endregion
    
    #region Helper Methods
    
    /// <summary>
    /// Calcula la distancia al jugador
    /// </summary>
    public float GetDistanceToPlayer()
    {
        if (player == null) return float.MaxValue;
        return Vector2.Distance(transform.position, player.position);
    }
    
    /// <summary>
    /// Calcula la dirección hacia el jugador
    /// </summary>
    public Vector2 GetDirectionToPlayer()
    {
        if (player == null) return Vector2.zero;
        return (player.position - transform.position).normalized;
    }
    
    /// <summary>
    /// Voltea el sprite según la dirección de movimiento
    /// </summary>
    protected virtual void UpdateSpriteDirection()
    {
        if (moveDirection.x == 0) return;
        
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * Mathf.Sign(moveDirection.x);
        transform.localScale = scale;
    }
    
    #endregion
    
    #region Properties
    
    public Vector2 MoveDirection => moveDirection;
    public float CurrentSpeed => moveSpeed;
    public bool IsMoving => moveDirection.sqrMagnitude > 0.01f;
    public bool CanMove => canMove;
    
    #endregion
}
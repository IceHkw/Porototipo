using UnityEngine;
using System.Collections;

/// <summary>
/// Comportamiento de ataque melee que hace que el enemigo ataque y luego retroceda
/// </summary>
public class MeleeAttackBehavior : EnemyBehaviorBase
{
    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackDamage = 1f;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float attackDuration = 0.5f; // Duración de la animación de ataque

    [Header("Attack Detection")]
    [SerializeField] private Vector2 attackSize = new Vector2(1f, 1f);
    [SerializeField] private Vector2 attackOffset = new Vector2(0.5f, 0f);
    [SerializeField] private LayerMask playerLayer = -1;

    [Header("Retreat Settings")]
    [SerializeField] private float retreatDistance = 2f;
    [SerializeField] private float retreatDuration = 0.5f;
    [SerializeField] private float retreatWaitTime = 0.3f; // Tiempo de espera después de retroceder

    [Header("Chase Settings")]
    [SerializeField] private float chaseRange = 10f; // Rango máximo para perseguir
    [SerializeField] private float stopChaseRange = 15f; // Si el jugador se aleja mucho, dejar de perseguir

    [Header("Animation")]
    [SerializeField] private string attackAnimationTrigger = "Attack";
    private Animator animator;

    [Header("Effects")]
    [SerializeField] private GameObject attackEffect; // Efecto visual del ataque

    // Estados del comportamiento
    public enum State
    {
        Idle,
        Chasing,
        Attacking,
        Retreating,
        Waiting
    }

    private State currentState = State.Idle;
    private float lastAttackTime = -10f;
    private bool isAttacking = false;
    private Coroutine currentStateCoroutine;

    #region Initialization

    protected override void OnStart()
    {
        animator = GetComponent<Animator>();
        ChangeToState(State.Idle);
    }

    protected override void OnCleanup()
    {
        if (currentStateCoroutine != null)
        {
            StopCoroutine(currentStateCoroutine);
        }
    }

    #endregion

    #region Behavior Update

    protected override void UpdateBehavior()
    {
        // El comportamiento se maneja principalmente a través de coroutines y estados
        // pero verificamos transiciones aquí

        switch (currentState)
        {
            case State.Idle:
                CheckForChaseTransition();
                break;

            case State.Chasing:
                CheckForAttackTransition();
                CheckForIdleTransition();
                break;

            case State.Attacking:
                // El ataque se maneja completamente en la coroutine
                break;

            case State.Retreating:
                // El retroceso se maneja completamente en la coroutine
                break;

            case State.Waiting:
                // La espera se maneja completamente en la coroutine
                break;
        }
    }

    #endregion

    #region State Management

    void ChangeToState(State newState)
    {
        if (currentState == newState) return;

        // Detener coroutine anterior si existe
        if (currentStateCoroutine != null)
        {
            StopCoroutine(currentStateCoroutine);
            currentStateCoroutine = null;
        }

        State previousState = currentState;
        currentState = newState;

        // Notificar cambio de estado
        ChangeState(newState.ToString());

        // Notificar evento global de cambio de estado
        if (enemyCore != null)
        {
            EnemyEvents.TriggerEnemyStateChanged(enemyCore, newState.ToString());
        }

        // Iniciar nuevo estado
        switch (newState)
        {
            case State.Idle:
                HandleIdleState();
                break;

            case State.Chasing:
                HandleChaseState();
                break;

            case State.Attacking:
                currentStateCoroutine = StartCoroutine(AttackCoroutine());
                break;

            case State.Retreating:
                currentStateCoroutine = StartCoroutine(RetreatCoroutine());
                break;

            case State.Waiting:
                currentStateCoroutine = StartCoroutine(WaitCoroutine());
                break;
        }
    }

    #endregion

    #region State Handlers

    void HandleIdleState()
    {
        // Detener movimiento
        if (movement != null)
        {
            movement.SetCanMove(false);
        }

        EndBehavior();

        // Notificar que perdió al jugador
        if (enemyCore != null)
        {
            EnemyEvents.TriggerEnemyLostPlayer(enemyCore);
        }
    }

    void HandleChaseState()
    {
        // Habilitar movimiento hacia el jugador
        if (movement != null)
        {
            movement.SetCanMove(true);
        }

        StartBehavior();

        // Notificar que detectó al jugador
        if (enemyCore != null)
        {
            EnemyEvents.TriggerEnemyDetectedPlayer(enemyCore);
        }
    }

    IEnumerator AttackCoroutine()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        // Detener movimiento
        if (movement != null)
        {
            movement.SetCanMove(false);
        }

        // Reproducir animación de ataque
        if (animator != null)
        {
            animator.SetTrigger(attackAnimationTrigger);
        }

        // Esperar un momento antes de hacer daño (sincronizar con animación)
        yield return new WaitForSeconds(attackDuration * 0.5f);

        // Ejecutar el ataque
        PerformAttack();

        // Esperar a que termine la animación
        yield return new WaitForSeconds(attackDuration * 0.5f);

        isAttacking = false;

        // Transición a retroceso
        ChangeToState(State.Retreating);
    }

    IEnumerator RetreatCoroutine()
    {
        // Calcular dirección de retroceso (alejándose del jugador)
        Vector2 retreatDirection = -GetDirectionToPlayer();

        // Forzar movimiento en dirección de retroceso
        if (movement != null)
        {
            movement.ForceMove(retreatDirection, retreatDuration);
        }

        // Esperar a que termine el retroceso
        yield return new WaitForSeconds(retreatDuration);

        // Transición a espera
        ChangeToState(State.Waiting);
    }

    IEnumerator WaitCoroutine()
    {
        // Mantener al enemigo quieto
        if (movement != null)
        {
            movement.SetCanMove(false);
        }

        // Esperar
        yield return new WaitForSeconds(retreatWaitTime);

        // Volver a perseguir si el jugador sigue en rango
        if (IsPlayerInRange(chaseRange))
        {
            ChangeToState(State.Chasing);
        }
        else
        {
            ChangeToState(State.Idle);
        }
    }

    #endregion

    #region State Transitions

    void CheckForChaseTransition()
    {
        if (IsPlayerInRange(chaseRange) && CanAttack())
        {
            ChangeToState(State.Chasing);
        }
    }

    void CheckForAttackTransition()
    {
        if (IsPlayerInRange(attackRange) && CanAttack())
        {
            ChangeToState(State.Attacking);
        }
    }

    void CheckForIdleTransition()
    {
        if (!IsPlayerInRange(stopChaseRange))
        {
            ChangeToState(State.Idle);
        }
    }

    #endregion

    #region Attack System

    bool CanAttack()
    {
        return Time.time - lastAttackTime >= attackCooldown && !isAttacking;
    }

    void PerformAttack()
    {
        // Calcular posición del ataque
        Vector2 attackPosition = GetAttackPosition();

        // Detectar jugador en el área de ataque
        Collider2D[] hits = Physics2D.OverlapBoxAll(attackPosition, attackSize, 0f, playerLayer);

        foreach (Collider2D hit in hits)
        {
            // Buscar IDamageable en el jugador
            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable == null && hit.transform.parent != null)
            {
                damageable = hit.GetComponentInParent<IDamageable>();
            }

            if (damageable != null && damageable.IsAlive)
            {
                // Aplicar daño
                Vector3 hitPoint = hit.ClosestPoint(attackPosition);
                damageable.TakeDamage((int)attackDamage, hitPoint, transform);

                // Registrar el daño hecho
                if (enemyCore != null)
                {
                    enemyCore.RegisterDamageDealt((int)attackDamage);
                }

                if (showDebugInfo)
                {
                    Debug.Log($"[MeleeAttackBehavior] {name} golpeó al jugador con {attackDamage} de daño");
                }
            }
        }

        // Reproducir efecto de ataque si existe
        if (attackEffect != null)
        {
            Instantiate(attackEffect, attackPosition, Quaternion.identity);
        }
    }

    Vector2 GetAttackPosition()
    {
        // Ajustar offset según la dirección que mira el enemigo
        float direction = transform.localScale.x > 0 ? 1f : -1f;
        Vector2 adjustedOffset = new Vector2(attackOffset.x * direction, attackOffset.y);
        return (Vector2)transform.position + adjustedOffset;
    }

    #endregion

    #region Helper Methods

    protected override void HandleDamageReceived(Vector3 hitPoint, Transform damageSource)
    {
        base.HandleDamageReceived(hitPoint, damageSource);

        // Si estamos esperando o idle y nos atacan, perseguir inmediatamente
        if (currentState == State.Idle || currentState == State.Waiting)
        {
            if (CanAttack())
            {
                ChangeToState(State.Chasing);
            }
        }
    }

    #endregion

    #region Gizmos

    public override void DrawBehaviorGizmos()
    {
        // Rango de persecución
        Gizmos.color = new Color(0, 1, 0, 0.2f);
        Gizmos.DrawWireSphere(transform.position, chaseRange);

        // Rango de ataque
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Área de ataque
        Gizmos.color = Color.red;
        Vector2 attackPos = GetAttackPosition();
        Gizmos.DrawWireCube(attackPos, attackSize);

        // Rango de detener persecución
        Gizmos.color = new Color(1, 1, 0, 0.1f);
        Gizmos.DrawWireSphere(transform.position, stopChaseRange);

        // Estado actual
        Vector3 statePos = transform.position + Vector3.up * 2.5f;
#if UNITY_EDITOR
        UnityEditor.Handles.Label(statePos, $"State: {currentState}");
#endif
    }

    #endregion

    #region Properties

    public State CurrentState => currentState;
    public bool IsAttacking => isAttacking;
    public float AttackCooldownRemaining => Mathf.Max(0, attackCooldown - (Time.time - lastAttackTime));
    public bool CanAttackNow => CanAttack();

    #endregion
}
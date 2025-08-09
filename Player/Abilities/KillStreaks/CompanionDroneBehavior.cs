// Code/Player/Abilities/KillStreaks/CompanionDroneBehavior.cs
using UnityEngine;
using System.Collections;

public class CompanionDroneBehavior : BaseKillStreakBehavior
{
    [Header("═══════════════════════════════════════")]
    [Header("CONFIGURACIÓN DEL DRON")]
    [Header("═══════════════════════════════════════")]

    [Header("Drone Behavior")]
    [SerializeField] private float followDistance = 4f;
    [SerializeField] private float attackApproachDistance = 3f;

    [Header("Animation Settings")]
    [SerializeField] private string attackAnimationTrigger = "Attack";
    [SerializeField] private string deathAnimationTrigger = "Death";
    [SerializeField] private float attackAnimationDuration = 0.5f;
    [SerializeField] private float deathAnimationDuration = 1f;

    [Header("Targeting Visuals")]
    [SerializeField] private GameObject targetMarkerPrefab;

    private enum DroneState { Following, MovingToTarget, Attacking }
    private DroneState currentState = DroneState.Following;
    private Transform currentEnemyTarget;
    private GameObject currentMarkerInstance;
    private float lastAttackTime = -99f;
    private Rigidbody2D rb;
    private Animator animator;
    private int hashIsMoving;

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        hashIsMoving = Animator.StringToHash("isMoving");
    }

    protected override void OnInitialize()
    {
        if (rb != null)
        {
            rb.gravityScale = 0;
            rb.linearDamping = 5f;
        }
        DebugLog("Dron inicializado.");
    }

    protected override void OnActivate()
    {
        currentState = DroneState.Following;
        DebugLog("Dron activado. Empezando a seguir al jugador.");
    }

    protected override void OnDeactivate()
    {
        if (currentMarkerInstance != null) Destroy(currentMarkerInstance);
        if (animator != null && !string.IsNullOrEmpty(deathAnimationTrigger))
        {
            animator.SetTrigger(deathAnimationTrigger);
            Destroy(gameObject, deathAnimationDuration);
        }
        else
        {
            Destroy(gameObject);
        }
        DebugLog("Dron desactivado.");
    }

    protected override void OnReset()
    {
        currentState = DroneState.Following;
        currentEnemyTarget = null;
    }

    void Update()
    {
        if (!isActive || playerTransform == null || currentStats == null) return;
        UpdateMovementAnimation();
        switch (currentState)
        {
            case DroneState.Following:
                HandleFollowingState();
                break;
            case DroneState.MovingToTarget:
                HandleMovingToTargetState();
                break;
            case DroneState.Attacking:
                break;
        }
    }

    private void UpdateMovementAnimation()
    {
        if (animator == null || rb == null) return;
        animator.SetBool(hashIsMoving, rb.linearVelocity.sqrMagnitude > 0.1f);
    }

    private void HandleFollowingState()
    {
        Vector3 followPosition = playerTransform.position + (Vector3.up * followDistance);
        MoveTowards(followPosition);
        if (Time.time > lastAttackTime + (1f / currentStats.rate))
        {
            GameObject closestEnemy = FindClosestEnemy(currentStats.effectRadius);
            if (closestEnemy != null)
            {
                currentEnemyTarget = closestEnemy.transform;
                currentState = DroneState.MovingToTarget;
                DebugLog($"Enemigo encontrado: {closestEnemy.name}. Moviendo para atacar.");
            }
        }
    }

    private void HandleMovingToTargetState()
    {
        if (currentEnemyTarget == null)
        {
            currentState = DroneState.Following;
            return;
        }
        MoveTowards(currentEnemyTarget.position);
        if (Vector3.Distance(transform.position, currentEnemyTarget.position) <= attackApproachDistance)
        {
            StartCoroutine(AttackRoutine());
        }
    }

    private void MoveTowards(Vector3 targetPosition)
    {
        if (rb == null || currentState == DroneState.Attacking || currentStats == null) return;
        Vector2 desiredVelocity = (targetPosition - transform.position).normalized * currentStats.speed;
        Vector2 steeringForce = (desiredVelocity - rb.linearVelocity) * (currentStats.speed * 0.5f);
        rb.AddForce(steeringForce);
        if (rb.linearVelocity.sqrMagnitude > 0.1f)
        {
            float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, 0, angle), Time.deltaTime * 5f);
        }
    }

    private IEnumerator AttackRoutine()
    {
        currentState = DroneState.Attacking;
        lastAttackTime = Time.time;
        rb.linearVelocity = Vector2.zero;
        DebugLog($"Atacando a {currentEnemyTarget.name}.");

        if (targetMarkerPrefab != null && currentEnemyTarget != null)
        {
            currentMarkerInstance = Instantiate(targetMarkerPrefab, currentEnemyTarget.position, Quaternion.identity);
            currentMarkerInstance.transform.SetParent(currentEnemyTarget);
            currentMarkerInstance.transform.localPosition = Vector3.zero;
        }

        if (animator != null) animator.SetTrigger(attackAnimationTrigger);
        yield return new WaitForSeconds(attackAnimationDuration);

        if (currentEnemyTarget != null && currentStats != null)
        {
            DamageEnemy(currentEnemyTarget.gameObject, (int)currentStats.potency);
        }

        if (currentMarkerInstance != null) Destroy(currentMarkerInstance);
        currentState = DroneState.Following;
        DebugLog("Ataque completado. Volviendo a seguir al jugador.");
    }
}
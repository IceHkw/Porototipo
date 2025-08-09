using UnityEngine;

/// <summary>
/// Implementación de movimiento terrestre con detección de obstáculos y saltos automáticos
/// </summary>
public class GroundMovement : EnemyMovementBase
{
    [Header("Jump Settings")]
    public float jumpForce = 8f;
    public float jumpCooldown = 0.5f;
    private float lastJumpTime = -1f;

    [Header("Ground Detection")]
    public float groundCheckDistance = 0.1f;
    public LayerMask groundLayer = -1;
    private bool isGrounded;

    [Header("Obstacle Detection")]
    public float wallCheckDistance = 0.5f;
    [Tooltip("Altura a la que se realiza la comprobación de la pared.")]
    public float wallCheckHeight = 0.5f;
    public float ledgeCheckDistance = 2f;
    private bool wallInFront;
    private bool ledgeInFront;

    [Header("Movement Control")]
    public float airControlMultiplier = 0.7f;
    public bool autoJumpObstacles = true;
    public bool autoJumpLedges = true;

    [Header("Gizmos")]
    public Color groundCheckColor = Color.green;
    public Color wallCheckColor = Color.red;
    public Color ledgeCheckColor = Color.yellow;

    private float facingDirection = 1f;

    #region Movement Implementation

    protected override void UpdateMovement()
    {
        if (player == null) return;

        float distanceToPlayerX = player.position.x - transform.position.x;

        UpdateSpriteDirection(distanceToPlayerX);
        UpdateSensors();

        if (Mathf.Abs(distanceToPlayerX) > stopDistance)
        {
            moveDirection.x = Mathf.Sign(distanceToPlayerX);
            if (moveDirection.x != 0 && !IsMoving) OnMovementStarted?.Invoke();
            OnMovementDirectionChanged?.Invoke(moveDirection);
        }
        else
        {
            if (IsMoving) StopMovement();
            moveDirection.x = 0;
        }

        HandleAutoJump();
    }

    // --- LÓGICA DE SPRITE MODIFICADA ---
    private void UpdateSpriteDirection(float distanceToPlayerX)
    {
        if (Mathf.Abs(distanceToPlayerX) > 0.01f)
        {
            // Determina el signo basado en la nueva opción
            float facingMultiplier = startsFacingLeft ? -1f : 1f;
            facingDirection = Mathf.Sign(distanceToPlayerX);
            transform.localScale = new Vector3(facingDirection * facingMultiplier, 1, 1);
        }
    }

    protected override void ApplyMovement()
    {
        if (rb == null) return;

        float horizontalVelocity = 0f;

        if (wallInFront && isGrounded)
        {
            horizontalVelocity = 0;
        }
        else if (isGrounded)
        {
            horizontalVelocity = moveDirection.x * moveSpeed;
        }
        else
        {
            horizontalVelocity = moveDirection.x * moveSpeed * airControlMultiplier;
        }

        rb.linearVelocity = new Vector2(horizontalVelocity, rb.linearVelocity.y);
    }

    public override bool ShouldMoveToTarget()
    {
        if (player == null) return false;
        float distance = GetDistanceToPlayer();
        return distance > stopDistance;
    }

    #endregion

    #region Sensor System

    void UpdateSensors()
    {
        Vector2 position = transform.position;
        float directionMultiplier = startsFacingLeft ? -1f : 1f;
        Vector2 sensorDirection = new Vector2(transform.localScale.x * directionMultiplier, 0);

        isGrounded = Physics2D.Raycast(position, Vector2.down, groundCheckDistance, groundLayer);

        Vector2 wallCheckStartPos = position + (Vector2.up * wallCheckHeight);
        wallInFront = Physics2D.Raycast(wallCheckStartPos, sensorDirection, wallCheckDistance, groundLayer);

        Vector2 ledgeCheckStartPos = position + (Vector2)(sensorDirection * wallCheckDistance);
        ledgeInFront = !Physics2D.Raycast(ledgeCheckStartPos, Vector2.down, ledgeCheckDistance, groundLayer);
    }

    #endregion

    #region Jump System

    void HandleAutoJump()
    {
        if (!isGrounded || !CanJump()) return;

        bool shouldJump = false;
        if (autoJumpObstacles && wallInFront)
        {
            shouldJump = true;
        }
        else if (autoJumpLedges && ledgeInFront && player.position.y >= transform.position.y - 0.5f)
        {
            shouldJump = true;
        }

        if (shouldJump)
        {
            PerformJump();
        }
    }

    bool CanJump()
    {
        return Time.time - lastJumpTime >= jumpCooldown && Mathf.Abs(rb.linearVelocity.y) < 0.1f;
    }

    void PerformJump()
    {
        if (rb == null) return;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        lastJumpTime = Time.time;
        if (showDebugInfo) Debug.Log($"[GroundMovement] {name} saltó!");
    }

    public void ForceJump()
    {
        if (isGrounded && rb != null) PerformJump();
    }

    #endregion

    #region Properties

    public bool IsGrounded => isGrounded;
    public bool HasWallInFront => wallInFront;
    public bool HasLedgeInFront => ledgeInFront;
    public float TimeSinceLastJump => Time.time - lastJumpTime;

    #endregion

    #region Gizmos

    void OnDrawGizmosSelected()
    {
        Vector3 position = transform.position;
        float direction = Mathf.Sign(transform.localScale.x);

        Gizmos.color = groundCheckColor;
        Gizmos.DrawLine(position, position + Vector3.down * groundCheckDistance);

        Gizmos.color = wallCheckColor;
        Vector3 wallCheckStart = position + (Vector3.up * wallCheckHeight);
        Gizmos.DrawLine(wallCheckStart, wallCheckStart + Vector3.right * direction * wallCheckDistance);

        Gizmos.color = ledgeCheckColor;
        Vector3 ledgeCheckStartPos = position + Vector3.right * direction * wallCheckDistance;
        Gizmos.DrawLine(ledgeCheckStartPos, ledgeCheckStartPos + Vector3.down * ledgeCheckDistance);
    }

    #endregion
}
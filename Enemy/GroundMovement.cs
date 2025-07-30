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
    public float wallCheckHeight = 0.5f; // << NUEVO CAMPO AÑADIDO
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

    // Almacenamos la dirección en la que mira el enemigo para los sensores.
    private float facingDirection = 1f;

    #region Movement Implementation

    protected override void UpdateMovement()
    {
        if (player == null) return;

        // Calcular dirección hacia el jugador
        float distanceToPlayerX = player.position.x - transform.position.x;

        // Actualizar la dirección del sprite primero
        UpdateSpriteDirection(distanceToPlayerX);

        // Actualizar sensores usando la dirección a la que mira el sprite
        UpdateSensors();

        // Solo moverse si estamos más lejos que la distancia de parada
        if (Mathf.Abs(distanceToPlayerX) > stopDistance)
        {
            moveDirection.x = Mathf.Sign(distanceToPlayerX);

            if (moveDirection.x != 0 && !IsMoving)
            {
                OnMovementStarted?.Invoke();
            }

            OnMovementDirectionChanged?.Invoke(moveDirection);
        }
        else
        {
            if (IsMoving)
            {
                StopMovement();
            }
            moveDirection.x = 0;
        }

        // Manejar saltos automáticos
        HandleAutoJump();
    }

    // Se ha modificado UpdateSpriteDirection para que acepte la distancia y actualice la dirección de la cara.
    private void UpdateSpriteDirection(float distanceToPlayerX)
    {
        if (Mathf.Abs(distanceToPlayerX) > 0.01f)
        {
            facingDirection = Mathf.Sign(distanceToPlayerX);
            // Asumiendo que el sprite mira a la derecha por defecto
            transform.localScale = new Vector3(facingDirection, 1, 1);
        }
    }


    protected override void ApplyMovement()
    {
        if (rb == null) return;

        float horizontalVelocity = 0f;

        // No moverse si hay una pared al frente (y no estamos a punto de saltar)
        if (wallInFront && isGrounded)
        {
            horizontalVelocity = 0;
        }
        else if (isGrounded)
        {
            // Movimiento terrestre normal
            horizontalVelocity = moveDirection.x * moveSpeed;
        }
        else
        {
            // Control aéreo reducido
            horizontalVelocity = moveDirection.x * moveSpeed * airControlMultiplier;
        }

        // Aplicar velocidad horizontal preservando la velocidad vertical
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

        // La dirección para los sensores ahora se basa en hacia dónde mira el transform.
        Vector2 sensorDirection = new Vector2(transform.localScale.x, 0);

        // Verificar suelo
        isGrounded = Physics2D.Raycast(position, Vector2.down, groundCheckDistance, groundLayer);

        // << LÓGICA MODIFICADA >>
        // Verificar pared al frente usando la nueva altura
        Vector2 wallCheckStartPos = position + (Vector2.up * wallCheckHeight);
        wallInFront = Physics2D.Raycast(wallCheckStartPos, sensorDirection, wallCheckDistance, groundLayer);
        // << FIN DE LA MODIFICACIÓN >>

        // Verificar borde/precipicio al frente
        Vector2 ledgeCheckStartPos = position + (Vector2)(sensorDirection * wallCheckDistance);
        ledgeInFront = !Physics2D.Raycast(ledgeCheckStartPos, Vector2.down, ledgeCheckDistance, groundLayer);
    }

    #endregion

    #region Jump System

    void HandleAutoJump()
    {
        // La condición de salto ahora es más robusta
        if (!isGrounded || !CanJump()) return;

        bool shouldJump = false;

        // Saltar si hay una pared al frente
        if (autoJumpObstacles && wallInFront)
        {
            shouldJump = true;
        }
        // Saltar si hay un precipicio y el jugador está al mismo nivel o más alto
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
        // Se simplifica la condición, ya que isGrounded se verifica en HandleAutoJump
        return Time.time - lastJumpTime >= jumpCooldown && Mathf.Abs(rb.linearVelocity.y) < 0.1f;
    }

    void PerformJump()
    {
        if (rb == null) return;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        lastJumpTime = Time.time;

        if (showDebugInfo)
        {
            Debug.Log($"[GroundMovement] {name} saltó!");
        }
    }

    public void ForceJump()
    {
        if (isGrounded && rb != null)
        {
            PerformJump();
        }
    }

    #endregion

    #region Unused Original Methods (for reference)

    /*
    * Se eliminan los métodos que ya no se usan o se refactorizaron para evitar confusiones.
    * - DrawDebugRays: Su funcionalidad ahora está integrada en OnDrawGizmosSelected para mejor visualización.
    * - ShouldApplyHorizontalMovement: Su lógica se ha integrado directamente en ApplyMovement.
    * - El antiguo UpdateSpriteDirection: Ahora acepta un parámetro y actualiza la variable facingDirection.
    */

    #endregion

    #region Properties

    public bool IsGrounded => isGrounded;
    public bool HasWallInFront => wallInFront;
    public bool HasLedgeInFront => ledgeInFront;
    public float TimeSinceLastJump => Time.time - lastJumpTime;

    #endregion

    #region Gizmos

    // OnDrawGizmosSelected se ejecuta solo cuando el objeto está seleccionado en el editor.
    // Es ideal para configurar valores.
    void OnDrawGizmosSelected()
    {
        Vector3 position = transform.position;
        // Usa la escala del transform para determinar la dirección, así funciona en el editor sin Play Mode.
        float direction = Mathf.Sign(transform.localScale.x);

        // --- Detección de Suelo ---
        Gizmos.color = groundCheckColor;
        Gizmos.DrawLine(position, position + Vector3.down * groundCheckDistance);

        // << LÓGICA MODIFICADA >>
        // --- Detección de Pared ---
        Gizmos.color = wallCheckColor;
        Vector3 wallCheckStart = position + (Vector3.up * wallCheckHeight);
        Gizmos.DrawLine(wallCheckStart, wallCheckStart + Vector3.right * direction * wallCheckDistance);
        // << FIN DE LA MODIFICACIÓN >>

        // --- Detección de Borde ---
        Gizmos.color = ledgeCheckColor;
        Vector3 ledgeCheckStartPos = position + Vector3.right * direction * wallCheckDistance;
        Gizmos.DrawLine(ledgeCheckStartPos, ledgeCheckStartPos + Vector3.down * ledgeCheckDistance);
    }

    #endregion
}

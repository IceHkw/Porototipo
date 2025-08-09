// Enemy/FlyingMovement.cs
using UnityEngine;

/// <summary>
/// Movimiento para enemigos voladores que orbitan al jugador y respetan el terreno.
/// </summary>
public class FlyingMovement : EnemyMovementBase
{
    [Header("Flying Settings")]
    [SerializeField] private float idealDistanceFromPlayer = 8f;
    [SerializeField] private float movementThreshold = 1f;

    [Header("Natural Flight Physics")]
    [Tooltip("Fuerza con la que el enemigo acelera. Más alto = más ágil.")]
    [SerializeField] private float acceleration = 5f;
    [Tooltip("Fuerza con la que el enemigo frena. Ayuda a evitar que se pase de su objetivo.")]
    [SerializeField] private float damping = 2f;
    [Tooltip("Velocidad a la que el enemigo orbita alrededor del jugador.")]
    [SerializeField] private float orbitSpeed = 3f;

    [Header("Ground Interaction")]
    [Tooltip("Altura ideal que el enemigo intentará mantener sobre el suelo.")]
    [SerializeField] private float idealHeightAboveGround = 4f;
    [Tooltip("Fuerza con la que el enemigo corrige su altura. Más alto = corrección más rápida.")]
    [SerializeField] private float hoverForce = 10f;
    [Tooltip("Distancia máxima hacia abajo para detectar el suelo.")]
    [SerializeField] private float groundCheckDistance = 20f;
    [Tooltip("Capas que se consideran como suelo.")]
    [SerializeField] private LayerMask groundLayer = -1;

    // Estado interno
    private bool horizontalMovementHalted = false;
    private int orbitDirection = 1; // 1 = horario, -1 = antihorario

    protected override void UpdateMovement()
    {
        if (player == null) return;
        UpdateSpriteDirection();
    }

    protected override void ApplyMovement()
    {
        if (player == null || rb == null) return;

        // Si el movimiento está pausado (ej. durante un ataque), solo frena y flota.
        if (horizontalMovementHalted)
        {
            // Frena el movimiento horizontal suavemente
            rb.AddForce(-new Vector2(rb.linearVelocity.x, 0) * damping);
        }
        else
        {
            // Lógica de movimiento normal (acercarse, alejarse u orbitar)
            HandleMovement();
        }

        // La lógica de flotación siempre está activa para que no caiga
        HandleHovering();
    }

    /// <summary>
    /// Decide si acercarse, alejarse u orbitar y aplica las fuerzas correspondientes.
    /// </summary>
    private void HandleMovement()
    {
        Vector2 directionToPlayer = player.position - transform.position;
        float distance = directionToPlayer.magnitude;

        Vector2 targetVelocity = Vector2.zero;

        // Comprobar si está fuera del rango de órbita
        if (distance > idealDistanceFromPlayer + movementThreshold)
        {
            // MUY LEJOS -> Acercarse
            targetVelocity = directionToPlayer.normalized * moveSpeed;
        }
        else if (distance < idealDistanceFromPlayer - movementThreshold)
        {
            // MUY CERCA -> Alejarse
            targetVelocity = -directionToPlayer.normalized * moveSpeed;
        }
        else
        {
            // DISTANCIA IDEAL -> Orbitar
            // Calcular dirección tangencial para la órbita
            Vector2 tangentialDirection = new Vector2(-directionToPlayer.y, directionToPlayer.x).normalized;
            targetVelocity = tangentialDirection * orbitDirection * orbitSpeed;
        }

        // Aplicar fuerza para alcanzar la velocidad objetivo (aceleración y freno)
        Vector2 force = (targetVelocity - rb.linearVelocity) * acceleration;
        force -= rb.linearVelocity * damping;
        rb.AddForce(force);
    }

    private void HandleHovering()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, groundLayer);

        if (hit.collider != null)
        {
            float distanceToGround = hit.distance;
            float heightError = idealHeightAboveGround - distanceToGround;
            float correctiveForce = heightError * hoverForce;
            correctiveForce -= rb.linearVelocity.y * damping;
            rb.AddForce(Vector2.up * correctiveForce);
        }
    }

    /// <summary>
    /// Implementación del nuevo método para pausar el movimiento horizontal.
    /// </summary>
    public override void HaltIntentionalMovement(bool halt)
    {
        horizontalMovementHalted = halt;
        if (!halt && Random.value > 0.5f) // Al reanudar, a veces cambia de dirección de órbita
        {
            orbitDirection *= -1;
        }
    }

    public override bool ShouldMoveToTarget()
    {
        return true; // El enemigo volador siempre está "activo" (flotando u orbitando)
    }

    protected override void UpdateSpriteDirection()
    {
        if (player == null) return;
        float horizontalDirection = player.position.x - transform.position.x;
        if (Mathf.Abs(horizontalDirection) < 0.1f) return;
        float facingMultiplier = startsFacingLeft ? -1f : 1f;
        transform.localScale = new Vector3(Mathf.Sign(horizontalDirection) * facingMultiplier, 1, 1);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!showDebugInfo) return;
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundCheckDistance);
        Vector3 idealHeightPos = transform.position + Vector3.down * idealHeightAboveGround;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(idealHeightPos + Vector3.left * 0.5f, idealHeightPos + Vector3.right * 0.5f);

        // Visualizar el anillo de órbita
        if (player != null)
        {
            Gizmos.color = new Color(1, 0.5f, 0, 0.5f); // Naranja
            Gizmos.DrawWireSphere(player.position, idealDistanceFromPlayer - movementThreshold);
            Gizmos.DrawWireSphere(player.position, idealDistanceFromPlayer + movementThreshold);
        }
    }
#endif
}
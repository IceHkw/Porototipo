using UnityEngine;

/// <summary>
/// AutoStep.cs
/// Adjunta este script al mismo GameObject donde tienes PlayerMovement.cs.
/// Detecta pequeños escalones en el suelo y mueve al jugador hacia arriba automáticamente
/// (hasta un máximo de stepHeight) para evitar que se quede “atorado”. 
/// Si presionas el input de salto en cualquier momento durante el auto‐step, 
/// se cancela el paso para que puedas saltar normalmente.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class AutoStep : MonoBehaviour
{
    [Header("Configuración de Auto‐Step")]
    [Tooltip("Altura máxima (en unidades Unity) que el personaje puede ‘subir’ automáticamente al caminar contra un obstáculo.")]
    public float stepHeight = 0.4f;

    [Tooltip("Distancia horizontal (en unidades Unity) para raycasts al detectar escalones.")]
    public float stepCheckDistance = 0.1f;

    [Tooltip("Capa(s) que se consideran suelo/obstáculo.")]
    public LayerMask groundLayer;

    [Tooltip("Tiempo en segundos para suavizar la transición vertical del auto‐step (si deseas suavizado). " +
             "Si lo dejas en 0, el ajuste de posición es instantáneo.")]
    public float stepSmoothTime = 0f;

    // Referencias internas
    private Rigidbody2D rb;
    private Collider2D col;
    private PlayerMovement pm;


    // Variables auxiliares para suavizar el movimiento vertical
    private float verticalVelocity = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        pm = GetComponent<PlayerMovement>();

        if (pm == null)
        {
            Debug.LogError("AutoStep: no se encontró PlayerMovement en el mismo GameObject.");
        }
    }

    void FixedUpdate()
    {
        // Si PlayerMovement indica que está muerto o saltando, no aplicamos auto‐step
        if (pm == null) return;
        if (pm.EstaMuerto) return;

        // Si justo se presionó salto, cancelamos cualquier intento de auto‐step
        if (Input.GetButton("Jump")) return;

        // Solo intentamos auto‐step si estamos en el suelo y hay input horizontal
        if (!pm.EstaEnSuelo) return;

        float inputH = Input.GetAxisRaw("Horizontal");
        if (Mathf.Abs(inputH) < 0.1f) return;

        // Determinar dirección (1 = derecha, -1 = izquierda)
        int dir = inputH > 0 ? 1 : -1;

        TryAutoStep(dir);
    }

    /// <summary>
    /// Intenta detectar si hay un escalón “pequeño” frente al jugador.
    /// Si lo hay, ajusta la posición vertical para subirlo.
    /// </summary>
    /// <param name="direction">1 para derecha, -1 para izquierda</param>
    void TryAutoStep(int direction)
    {
        // Obtén los bordes del collider
        Bounds bounds = col.bounds;
        Vector2 originBase = new Vector2(bounds.center.x, bounds.min.y + 0.02f);
        Vector2 originTop = originBase + Vector2.up * stepHeight;

        // Offset horizontal del punto de origen para no chocar con el propio collider
        originBase.x += direction * (bounds.extents.x + 0.01f);
        originTop.x += direction * (bounds.extents.x + 0.01f);

        // Raycast desde la base: detecta si hay un obstáculo justo enfrente
        RaycastHit2D hitBase = Physics2D.Raycast(
            originBase,
            Vector2.right * direction,
            stepCheckDistance,
            groundLayer
        );

        if (hitBase.collider == null)
        {
            // No hay nada enfrente a nivel de base, no es un escalón
            return;
        }

        // Raycast a la altura del stepHeight: verifica que haya espacio libre para subir
        RaycastHit2D hitTop = Physics2D.Raycast(
            originTop,
            Vector2.right * direction,
            stepCheckDistance,
            groundLayer
        );

        if (hitTop.collider != null)
        {
            // Hay obstáculo también a la altura del stepHeight → no podemos subir
            return;
        }

        // Si llegamos aquí: hay obstáculo pequeño (escalón) y espacio libre arriba. 
        // Subimos al personaje para “pasar” por encima.

        float targetY = rb.position.y + stepHeight;

        if (stepSmoothTime <= 0f)
        {
            // Ajuste instantáneo (sin suavizado)
            rb.position = new Vector2(rb.position.x, targetY);
        }
        else
        {
            // Suavizamos la transición vertical
            float newY = Mathf.SmoothDamp(
                rb.position.y,
                targetY,
                ref verticalVelocity,
                stepSmoothTime
            );
            rb.MovePosition(new Vector2(rb.position.x, newY));
        }
    }

    // Opcionalmente, puedes dibujar en la escena los raycasts para depuración
    void OnDrawGizmosSelected()
    {
        if (col == null) return;

        Bounds bounds = col.bounds;
        float dirSign = 1f;
#if UNITY_EDITOR
        // Si hay componente PlayerMovement, muévete en la dirección del input
        PlayerMovement possiblePM = GetComponent<PlayerMovement>();
        if (possiblePM != null)
        {
            float inputH = Application.isPlaying
                ? Input.GetAxisRaw("Horizontal")
                : 1f; // por defecto derecha si no está jugando
            dirSign = inputH >= 0 ? 1f : -1f;
        }
        else
        {
            dirSign = 1f;
        }
#endif

        Vector2 originBase = new Vector2(bounds.center.x, bounds.min.y + 0.02f);
        Vector2 originTop = originBase + Vector2.up * stepHeight;

        originBase.x += dirSign * (bounds.extents.x + 0.01f);
        originTop.x += dirSign * (bounds.extents.x + 0.01f);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(originBase, originBase + Vector2.right * dirSign * stepCheckDistance);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(originTop, originTop + Vector2.right * dirSign * stepCheckDistance);
    }
}

using UnityEngine;

/// <summary>
/// Controla un objeto individual que cae del cielo, se mueve en diagonal y daña al impactar.
/// </summary>
public class FallingObject : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Velocidad base de caída del objeto. ¡Puedes ajustar esto!")]
    [SerializeField] private float fallSpeed = 20f;

    [Header("Efectos")]
    [SerializeField] private GameObject impactEffectPrefab;
    [Tooltip("Capas con las que el objeto colisionará.")]
    [SerializeField] private LayerMask collisionLayers;
    [Tooltip("Tiempo de vida máximo en segundos por si no golpea nada.")]
    [SerializeField] private float maxLifetime = 10f;

    // Propiedades
    private int damage;
    private Vector2 velocity;

    void Start()
    {
        Destroy(gameObject, maxLifetime);
    }

    /// <summary>
    /// Inicializa el objeto con sus propiedades de daño y trayectoria.
    /// </summary>
    public void Initialize(int dmg, float angle, float direction)
    {
        this.damage = dmg;

        // Calcular la velocidad basada en el ángulo y la dirección
        float angleRad = Mathf.Deg2Rad * angle;
        float velX = Mathf.Sin(angleRad);
        float velY = Mathf.Cos(angleRad);

        // La velocidad de caída ahora usa la variable fallSpeed
        this.velocity = new Vector2(-direction * velX, -velY) * fallSpeed;

        // Rotar el objeto para que apunte en la dirección de su movimiento
        float rotationAngle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, rotationAngle + 90f);
    }

    void Update()
    {
        // Movimiento simple
        transform.Translate(velocity * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Comprobar si la capa del objeto con el que colisionamos está en nuestra LayerMask
        if ((collisionLayers.value & (1 << other.gameObject.layer)) > 0)
        {
            // Intentar aplicar daño si es un enemigo
            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable != null && damageable.IsAlive)
            {
                damageable.TakeDamage(damage, transform.position, transform);
            }

            // Crear efecto de impacto y destruir el objeto
            TriggerImpact();
        }
    }

    private void TriggerImpact()
    {
        if (impactEffectPrefab != null)
        {
            Instantiate(impactEffectPrefab, transform.position, Quaternion.identity);
        }
        Destroy(gameObject);
    }
}
// Enemy/EnemyProjectile.cs
using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    private float speed;
    private int damage;
    private Vector2 direction;
    private Rigidbody2D rb;

    [Header("Effects")]
    [SerializeField] private GameObject impactEffectPrefab;
    [SerializeField] private float lifetime = 5f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        Destroy(gameObject, lifetime);
    }

    public void Initialize(Vector2 dir, float spd, int dmg)
    {
        this.direction = dir;
        this.speed = spd;
        this.damage = dmg;

        // Rotar el sprite para que mire en la direcci�n del movimiento
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    void FixedUpdate()
    {
        rb.linearVelocity = direction * speed;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            IDamageable playerDamageable = other.GetComponent<IDamageable>();
            if (playerDamageable != null)
            {
                playerDamageable.TakeDamage(damage, transform.position, transform);
            }
            TriggerImpact();
        }
        else if (other.gameObject.layer == LayerMask.NameToLayer("Ground")) // O la capa que uses para el escenario
        {
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
// Code/Player/Combat/ResonantWave.cs
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Controla el comportamiento de la onda de choque del "Combo Resonante".
/// Detecta enemigos en su radio y les aplica daño una sola vez.
/// </summary>
public class ResonantWave : MonoBehaviour
{
    [Header("Efecto de la Onda")]
    [SerializeField] private float radius = 2.5f;
    [SerializeField] private float lifetime = 0.5f;
    [SerializeField] private LayerMask enemyLayer;

    private int damage;
    private Transform playerTransform;
    private HashSet<GameObject> hitEnemies = new HashSet<GameObject>();

    /// <summary>
    /// Inicializa la onda de choque con el daño y la referencia del jugador.
    /// </summary>
    public void Initialize(int waveDamage, Transform sourcePlayer)
    {
        this.damage = waveDamage;
        this.playerTransform = sourcePlayer;
        Destroy(gameObject, lifetime);
        DetectAndDamage();
    }

    private void DetectAndDamage()
    {
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, radius, enemyLayer);

        foreach (var hitCollider in hitColliders)
        {
            if (hitEnemies.Contains(hitCollider.gameObject)) continue;

            IDamageable damageable = hitCollider.GetComponentInParent<IDamageable>();
            if (damageable != null && damageable.IsAlive)
            {
                hitEnemies.Add(hitCollider.gameObject);
                damageable.TakeDamage(damage, hitCollider.transform.position, playerTransform);
            }
        }
    }

    // Dibuja el radio de efecto en el editor para facilitar el ajuste
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
// Enemy/EnemyVFX.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Componente que maneja todos los efectos visuales de los enemigos.
/// Ahora utiliza PREFABS para la fragmentación y busca el SpriteRenderer en hijos.
/// </summary>
[RequireComponent(typeof(EnemyCore))] // Cambiado para requerir EnemyCore en lugar de SpriteRenderer
public class EnemyVFX : MonoBehaviour
{
    [Header("═══════════════════════════════════════")]
    [Header("CONFIGURACIÓN DE FLASH DE DAÑO")]
    [Header("═══════════════════════════════════════")]

    [Tooltip("Duración del flash cuando recibe daño")]
    [SerializeField] private float flashDuration = 0.1f;

    [Tooltip("Color del flash (generalmente blanco)")]
    [SerializeField] private Color flashColor = Color.white;

    [Header("═══════════════════════════════════════")]
    [Header("PARTÍCULAS DE IMPACTO")]
    [Header("═══════════════════════════════════════")]

    [Tooltip("Prefab del sistema de partículas al recibir daño")]
    [SerializeField] private GameObject hitParticlesPrefab;

    [Tooltip("Offset de las partículas respecto al punto de impacto")]
    [SerializeField] private Vector3 particlesOffset = Vector3.zero;

    // --- NUEVA SECCIÓN DE FRAGMENTACIÓN ---
    [Header("═══════════════════════════════════════")]
    [Header("FRAGMENTACIÓN AL MORIR (CON PREFABS)")]
    [Header("═══════════════════════════════════════")]

    [Tooltip("Arrastra aquí los prefabs de los fragmentos que se instanciarán al morir.")]
    [SerializeField] private GameObject[] fragmentPrefabs;

    [Tooltip("Fuerza de explosión aplicada a los fragmentos")]
    [SerializeField] private float explosionForce = 5f;

    [Tooltip("Rango de rotación inicial aleatoria de los fragmentos (en grados)")]
    [SerializeField] private float fragmentRotationRange = 45f;

    [Tooltip("Tiempo antes de desvanecer los fragmentos")]
    [SerializeField] private float fragmentLifetime = 2f;

    [Tooltip("Duración del desvanecimiento")]
    [SerializeField] private float fragmentFadeDuration = 0.5f;

    // Referencias
    private SpriteRenderer spriteRenderer;
    private Material material;
    private EnemyCore enemyCore;

    // Flash
    private Coroutine flashCoroutine;
    private int flashAmountID;


    void Awake()
    {
        InitializeComponents();
    }

    void Start()
    {
        SubscribeToEvents();
    }

    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    void InitializeComponents()
    {
        // === CAMBIO CLAVE AQUÍ ===
        // Buscamos el SpriteRenderer en este componente O en CUALQUIERA de sus hijos.
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        enemyCore = GetComponent<EnemyCore>();

        // Si después de buscar no se encuentra, lanzamos un error claro.
        if (spriteRenderer == null)
        {
            Debug.LogError($"[EnemyVFX] en '{gameObject.name}': No se encontró un SpriteRenderer en este objeto ni en sus hijos. Los efectos visuales no funcionarán.", this);
            return; // Detenemos la inicialización si no hay renderer.
        }

        // Crear instancia del material para que el flash no afecte a otros enemigos
        material = spriteRenderer.material;
        if (!material.name.Contains("Instance"))
        {
            material = new Material(material);
            spriteRenderer.material = material;
        }
        flashAmountID = Shader.PropertyToID("_FlashAmount");
        material.SetFloat(flashAmountID, 0f);
    }

    void SubscribeToEvents()
    {
        if (enemyCore != null)
        {
            enemyCore.OnDamageReceived += HandleDamageReceived;
            enemyCore.OnDeath += HandleDeath;
        }
    }

    void UnsubscribeFromEvents()
    {
        if (enemyCore != null)
        {
            enemyCore.OnDamageReceived -= HandleDamageReceived;
            enemyCore.OnDeath -= HandleDeath;
        }
    }


    #region Damage and Death Handlers

    void HandleDamageReceived(Vector3 hitPoint, Transform damageSource)
    {
        TriggerDamageFlash();
        SpawnHitParticles(hitPoint);
    }

    void HandleDeath()
    {
        // El nuevo método que usa prefabs
        CreateFragmentsFromPrefabs();

        // Ocultar el sprite original
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }
    }

    #endregion


    #region Visual Effects

    void TriggerDamageFlash()
    {
        if (material == null) return;
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashCoroutine());
    }

    IEnumerator FlashCoroutine()
    {
        material.SetFloat(flashAmountID, 1f);
        yield return new WaitForSeconds(flashDuration);
        material.SetFloat(flashAmountID, 0f);
        flashCoroutine = null;
    }

    void SpawnHitParticles(Vector3 hitPoint)
    {
        if (hitParticlesPrefab == null) return;

        Vector3 spawnPosition = hitPoint + particlesOffset;
        GameObject particles = Instantiate(hitParticlesPrefab, spawnPosition, Quaternion.identity);

        ParticleSystem ps = particles.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            Destroy(particles, ps.main.duration + ps.main.startLifetime.constantMax);
        }
        else
        {
            Destroy(particles, 2f);
        }
    }

    void CreateFragmentsFromPrefabs()
    {
        if (fragmentPrefabs == null || fragmentPrefabs.Length == 0) return;

        Vector3 explosionCenter = transform.position;

        foreach (GameObject prefab in fragmentPrefabs)
        {
            if (prefab == null) continue;

            Quaternion randomRotation = Quaternion.Euler(0, 0, Random.Range(-fragmentRotationRange, fragmentRotationRange));
            GameObject fragment = Instantiate(prefab, transform.position, randomRotation);

            FragmentLifecycle lifecycle = fragment.GetComponent<FragmentLifecycle>();
            if (lifecycle != null)
            {
                lifecycle.lifetime = this.fragmentLifetime;
                lifecycle.fadeDuration = this.fragmentFadeDuration;
            }

            Rigidbody2D rb = fragment.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector3 direction = (fragment.transform.position - explosionCenter).normalized + (Vector3)Random.insideUnitCircle * 0.5f;
                rb.AddForce(direction.normalized * explosionForce, ForceMode2D.Impulse);
            }
        }
    }

    #endregion

    /// <summary>
    /// Resetea el estado visual del enemigo. Útil si vas a usar un sistema de Object Pooling.
    /// </summary>
    public void ResetVisualState()
    {
        // --- LÍNEAS AÑADIDAS ---
        // Detener cualquier corutina de flash que pudiera estar activa para evitar errores
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }
        // -------------------------

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }
        if (material != null)
        {
            // Asegurar que el material no se quede "flasheado"
            material.SetFloat(flashAmountID, 0f);
        }
    }
}

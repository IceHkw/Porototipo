using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// KillStreak: Orbe que orbita alrededor del jugador y daña enemigos por contacto
/// </summary>
public class OrbitalOrbBehavior : BaseKillStreakBehavior
{
    [Header("═══════════════════════════════════════")]
    [Header("CONFIGURACIÓN DE ÓRBITA")]
    [Header("═══════════════════════════════════════")]

    [Header("Orbit Settings")]
    [SerializeField] private float orbitRadius = 3f;
    [SerializeField] private float orbitHeight = 0f; // Ahora controla la profundidad en Z
    [SerializeField] private bool randomStartAngle = true;
    [SerializeField] private bool clockwiseRotation = true;

    [Header("Damage Settings")]
    [SerializeField] private float damageCooldown = 0.5f;
    [SerializeField] private bool damageOnlyWhileActive = true;
    [SerializeField] private float contactRadius = 0.5f;

    [Header("Visual Settings")]
    [SerializeField] private bool applyDefinitionColor = true;
    [SerializeField] private bool rotateOrb = true;
    [SerializeField] private float orbRotationSpeed = 360f;
    [SerializeField] private bool pulsateSize = true;
    [SerializeField] private float pulsateSpeed = 2f;
    [SerializeField] private float pulsateAmount = 0.2f;

    // ===== ESTADO INTERNO =====
    private float currentAngle;
    private float currentOrbitSpeed;
    private Dictionary<GameObject, float> damageCooldowns = new Dictionary<GameObject, float>();
    private float baseScale;
    private int actualDamage;

    // ===== COMPONENTES =====
    private TrailRenderer trailRenderer;
    private Light orbLight;

    protected override void Awake()
    {
        base.Awake();

        // Buscar componentes adicionales
        trailRenderer = GetComponent<TrailRenderer>();
        orbLight = GetComponentInChildren<Light>();

        // Guardar escala base
        baseScale = transform.localScale.x;
    }

    protected override void OnInitialize()
    {
        // Establecer posición inicial
        if (randomStartAngle)
        {
            currentAngle = Random.Range(0f, 360f);
        }
        else
        {
            currentAngle = 0f;
        }

        // Obtener valores de la definición
        if (definition != null)
        {
            currentOrbitSpeed = definition.RotationSpeed;
            actualDamage = definition.Damage;
        }

        // Configurar trail renderer si existe
        if (trailRenderer != null)
        {
            trailRenderer.enabled = false;

            // Solo aplicar color si está habilitado
            if (definition != null && applyDefinitionColor)
            {
                var gradient = new Gradient();
                gradient.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(definition.PrimaryColor, 0.0f),
                        new GradientColorKey(definition.PrimaryColor * 0.5f, 1.0f)
                    },
                    new GradientAlphaKey[] {
                        new GradientAlphaKey(1.0f, 0.0f),
                        new GradientAlphaKey(0.0f, 1.0f)
                    }
                );
                trailRenderer.colorGradient = gradient;
            }
        }

        // Configurar luz si existe
        if (orbLight != null)
        {
            orbLight.enabled = false;

            // Solo aplicar color si está habilitado
            if (definition != null && applyDefinitionColor)
            {
                orbLight.color = definition.PrimaryColor;
                orbLight.intensity = 2f;
            }
        }

        DebugLog("OrbitalOrb inicializado");
    }

    protected override void ApplyVisualConfiguration()
    {
        // Solo aplicar si está habilitado
        if (!applyDefinitionColor)
        {
            DebugLog("Aplicación de color deshabilitada");
            return;
        }

        // Llamar al método base para aplicar configuración estándar
        base.ApplyVisualConfiguration();
    }

    protected override void OnActivate()
    {
        // Activar efectos visuales
        if (trailRenderer != null)
            trailRenderer.enabled = true;

        if (orbLight != null)
            orbLight.enabled = true;

        // Posicionar en la órbita inicial
        UpdateOrbitPosition();

        // Iniciar sonido de loop si está configurado
        if (audioSource != null && definition.LoopSound != null)
        {
            audioSource.Play();
        }

        DebugLog("OrbitalOrb activado");
    }

    protected override void OnDeactivate()
    {
        // Desactivar efectos visuales
        if (trailRenderer != null)
            trailRenderer.enabled = false;

        if (orbLight != null)
            orbLight.enabled = false;

        // Limpiar cooldowns
        damageCooldowns.Clear();

        DebugLog("OrbitalOrb desactivado");
    }

    protected override void OnReset()
    {
        currentAngle = 0f;
        damageCooldowns.Clear();
        transform.localScale = Vector3.one * baseScale;
    }

    void Update()
    {
        if (!isActive || playerTransform == null) return;

        // Actualizar órbita
        UpdateOrbit();

        // Actualizar rotación del orbe (ahora en eje Z para 2D)
        if (rotateOrb)
        {
            transform.Rotate(Vector3.forward, orbRotationSpeed * Time.deltaTime);
        }

        // Actualizar pulsación
        if (pulsateSize)
        {
            UpdatePulsation();
        }

        // Actualizar cooldowns de daño
        UpdateDamageCooldowns();

        // Detectar y dañar enemigos cercanos
        if (damageOnlyWhileActive)
        {
            DetectAndDamageEnemies();
        }
    }

    void UpdateOrbit()
    {
        // Actualizar ángulo
        float rotationDirection = clockwiseRotation ? -1f : 1f;
        currentAngle += currentOrbitSpeed * rotationDirection * Time.deltaTime;

        // Mantener el ángulo entre 0-360
        if (currentAngle > 360f) currentAngle -= 360f;
        if (currentAngle < 0f) currentAngle += 360f;

        // Actualizar posición
        UpdateOrbitPosition();
    }

    void UpdateOrbitPosition()
    {
        if (playerTransform == null) return;

        // Calcular posición en la órbita 2D (plano XY)
        float radians = currentAngle * Mathf.Deg2Rad;
        float x = Mathf.Cos(radians) * orbitRadius;
        float y = Mathf.Sin(radians) * orbitRadius;

        // Aplicar posición relativa al jugador con profundidad Z
        Vector3 orbitPosition = new Vector3(
            playerTransform.position.x + x,
            playerTransform.position.y + y,
            playerTransform.position.z + orbitHeight  // Usar para profundidad
        );

        transform.position = orbitPosition;
    }

    void UpdatePulsation()
    {
        float pulsation = 1f + (Mathf.Sin(Time.time * pulsateSpeed) * pulsateAmount);
        transform.localScale = Vector3.one * baseScale * pulsation;

        // Actualizar intensidad de luz con la pulsación
        if (orbLight != null)
        {
            orbLight.intensity = 2f * pulsation;
        }
    }

    void UpdateDamageCooldowns()
    {
        var keysToUpdate = new List<GameObject>(damageCooldowns.Keys);

        foreach (var enemy in keysToUpdate)
        {
            if (enemy == null)
            {
                damageCooldowns.Remove(enemy);
                continue;
            }

            damageCooldowns[enemy] -= Time.deltaTime;

            if (damageCooldowns[enemy] <= 0f)
            {
                damageCooldowns.Remove(enemy);
            }
        }
    }

    void DetectAndDamageEnemies()
    {
        // Buscar enemigos en el radio de contacto
        var enemies = FindNearbyEnemies(contactRadius);

        foreach (var enemy in enemies)
        {
            // Verificar si está en cooldown
            if (damageCooldowns.ContainsKey(enemy)) continue;

            // Aplicar daño
            DamageEnemy(enemy, actualDamage);

            // Agregar cooldown
            damageCooldowns[enemy] = damageCooldown;

            // Efectos visuales de impacto
            CreateImpactEffect(enemy.transform.position);
        }
    }

    void CreateImpactEffect(Vector3 position)
    {
        // Aquí podrías instanciar un efecto de partículas de impacto
        // Por ahora solo un flash de luz
        if (orbLight != null)
        {
            StartCoroutine(FlashLight());
        }
    }

    System.Collections.IEnumerator FlashLight()
    {
        if (orbLight == null) yield break;

        float originalIntensity = orbLight.intensity;
        orbLight.intensity = originalIntensity * 3f;
        yield return new WaitForSeconds(0.1f);
        orbLight.intensity = originalIntensity;
    }

    // Para enemigos que colisionen directamente
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActive || !damageOnlyWhileActive) return;

        if (other.CompareTag("Enemy"))
        {
            HandleEnemyCollision(other.gameObject);
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!isActive || !damageOnlyWhileActive) return;

        if (other.CompareTag("Enemy"))
        {
            HandleEnemyCollision(other.gameObject);
        }
    }

    void HandleEnemyCollision(GameObject enemy)
    {
        // Verificar cooldown
        if (damageCooldowns.ContainsKey(enemy)) return;

        // Aplicar daño
        DamageEnemy(enemy, actualDamage);

        // Agregar cooldown
        damageCooldowns[enemy] = damageCooldown;

        // Efectos
        CreateImpactEffect(enemy.transform.position);
    }

    // ===== DEBUG =====
    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        if (!showDebugGizmos) return;

        // Mostrar órbita
        if (playerTransform != null)
        {
            Gizmos.color = isActive ? Color.cyan : Color.gray;
            DrawOrbitPath();
        }

        // Mostrar radio de contacto
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, contactRadius);
    }

    void DrawOrbitPath()
    {
        int segments = 32;
        float angleStep = 360f / segments;

        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * angleStep * Mathf.Deg2Rad;
            float angle2 = ((i + 1) % segments) * angleStep * Mathf.Deg2Rad;

            Vector3 point1 = new Vector3(
                playerTransform.position.x + Mathf.Cos(angle1) * orbitRadius,
                playerTransform.position.y + Mathf.Sin(angle1) * orbitRadius,
                playerTransform.position.z + orbitHeight
            );

            Vector3 point2 = new Vector3(
                playerTransform.position.x + Mathf.Cos(angle2) * orbitRadius,
                playerTransform.position.y + Mathf.Sin(angle2) * orbitRadius,
                playerTransform.position.z + orbitHeight
            );

            Gizmos.DrawLine(point1, point2);
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Test Damage Effect")]
    public void TestDamageEffect()
    {
        var enemies = FindNearbyEnemies(5f);
        if (enemies.Length > 0)
        {
            CreateImpactEffect(enemies[0].transform.position);
        }
    }

    [ContextMenu("Validate Prefab Setup")]
    public void ValidatePrefabSetup()
    {
        // Verificar que hay algún renderer visible
        var renderers = GetComponentsInChildren<Renderer>();
        bool hasVisibleRenderer = false;

        foreach (var renderer in renderers)
        {
            if (renderer.enabled)
            {
                hasVisibleRenderer = true;

                // Verificar SpriteRenderer
                if (renderer is SpriteRenderer sr)
                {
                    Debug.Log($"SpriteRenderer encontrado: {sr.name}");
                    Debug.Log($"  - Sprite: {(sr.sprite != null ? sr.sprite.name : "NONE")}");
                    Debug.Log($"  - Color: {sr.color}");
                    Debug.Log($"  - Enabled: {sr.enabled}");
                }
                // Verificar MeshRenderer
                else if (renderer is MeshRenderer mr)
                {
                    Debug.Log($"MeshRenderer encontrado: {mr.name}");
                    Debug.Log($"  - Material: {(mr.sharedMaterial != null ? mr.sharedMaterial.name : "NONE")}");
                    Debug.Log($"  - Enabled: {mr.enabled}");
                }
            }
        }

        if (!hasVisibleRenderer)
        {
            Debug.LogWarning("¡No se encontraron renderers visibles! El orbe será invisible.");
        }

        // Verificar collider
        var collider = GetComponent<Collider2D>();
        if (collider == null)
        {
            Debug.LogWarning("No hay Collider2D - el orbe no detectará enemigos");
        }
        else
        {
            Debug.Log($"Collider2D encontrado: {collider.GetType().Name} (Trigger: {collider.isTrigger})");
        }

        // Sugerir configuración
        Debug.Log("\n=== CONFIGURACIÓN SUGERIDA ===");
        Debug.Log("1. Añadir un SpriteRenderer con un sprite circular");
        Debug.Log("2. Añadir un CircleCollider2D en modo Trigger");
        Debug.Log("3. (Opcional) Añadir TrailRenderer para efecto visual");
        Debug.Log("4. (Opcional) Añadir Light2D para brillo");
        Debug.Log("5. Desmarcar 'Apply Definition Color' si quieres mantener colores originales");
    }

    void OnValidate()
    {
        // Validaciones en el editor
        contactRadius = Mathf.Max(0.1f, contactRadius);
        orbitRadius = Mathf.Max(0.5f, orbitRadius);
        damageCooldown = Mathf.Max(0.1f, damageCooldown);
        orbRotationSpeed = Mathf.Max(0f, orbRotationSpeed);
        pulsateSpeed = Mathf.Max(0.1f, pulsateSpeed);
        pulsateAmount = Mathf.Clamp(pulsateAmount, 0f, 1f);
    }
#endif
}
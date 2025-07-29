using UnityEngine;
using System;

/// <summary>
/// Clase base abstracta para todos los comportamientos de KillStreak
/// </summary>
public abstract class BaseKillStreakBehavior : MonoBehaviour, IKillStreakBehavior
{
    [Header("═══════════════════════════════════════")]
    [Header("DEBUG")]
    [Header("═══════════════════════════════════════")]

    [SerializeField] protected bool enableDebugLogs = false;
    [SerializeField] protected bool showDebugGizmos = true;

    // ===== ESTADO =====
    protected KillStreakDefinition definition;
    protected Transform playerTransform;
    protected bool isActive = false;
    protected bool isInitialized = false;

    // ===== COMPONENTES COMUNES =====
    protected AudioSource audioSource;
    protected ParticleSystem[] particleSystems;

    // ===== EVENTOS =====
    public event Action<IKillStreakBehavior> OnActivated;
    public event Action<IKillStreakBehavior> OnDeactivated;

    // ===== IMPLEMENTACIÓN DE INTERFAZ =====
    public string Name => definition != null ? definition.KillStreakName : "Unknown KillStreak";
    public bool IsActive => isActive;
    public Transform PlayerTransform
    {
        get => playerTransform;
        set => playerTransform = value;
    }

    // ===== INICIALIZACIÓN =====
    protected virtual void Awake()
    {
        // Buscar componentes comunes
        audioSource = GetComponent<AudioSource>();
        particleSystems = GetComponentsInChildren<ParticleSystem>();

        // Configurar audio si existe
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
        }
    }

    public virtual void Initialize(KillStreakDefinition killStreakDef, Transform player)
    {
        if (isInitialized) return;

        definition = killStreakDef;
        playerTransform = player;

        // Solo aplicar configuración visual en runtime
        if (Application.isPlaying)
        {
            // Aplicar configuración visual
            ApplyVisualConfiguration();

            // Configurar audio
            SetupAudio();
        }

        // Inicialización específica de cada KillStreak
        OnInitialize();

        isInitialized = true;

        DebugLog($"{Name} inicializado correctamente");
    }

    /// <summary>
    /// Método abstracto para inicialización específica de cada KillStreak
    /// </summary>
    protected abstract void OnInitialize();

    // ===== ACTIVACIÓN/DESACTIVACIÓN =====
    public virtual void Activate()
    {
        if (!isInitialized)
        {
            Debug.LogError($"{Name}: Intentando activar sin inicializar!");
            return;
        }

        if (isActive) return;

        isActive = true;

        // Reproducir sonido de activación
        PlayActivationSound();

        // Activar efectos de partículas
        ActivateParticleEffects();

        // Activación específica
        OnActivate();

        // Disparar evento
        OnActivated?.Invoke(this);

        DebugLog($"{Name} activado!");
    }

    public virtual void Deactivate()
    {
        if (!isActive) return;

        isActive = false;

        // Detener sonidos
        StopSounds();

        // Desactivar efectos de partículas
        DeactivateParticleEffects();

        // Desactivación específica
        OnDeactivate();

        // Disparar evento
        OnDeactivated?.Invoke(this);

        DebugLog($"{Name} desactivado!");
    }

    public virtual void Reset()
    {
        Deactivate();

        // Reset específico
        OnReset();

        DebugLog($"{Name} reseteado!");
    }

    /// <summary>
    /// Métodos abstractos para implementación específica
    /// </summary>
    protected abstract void OnActivate();
    protected abstract void OnDeactivate();
    protected abstract void OnReset();

    // ===== CONFIGURACIÓN VISUAL =====
    protected virtual void ApplyVisualConfiguration()
    {
        // Solo aplicar configuración visual en runtime y si hay definición
        if (!Application.isPlaying || definition == null) return;

        // Aplicar color a todos los renderers (tanto 3D como 2D)
        var renderers = GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            // Skip si el renderer está deshabilitado
            if (!renderer.enabled) continue;

            try
            {
                // Para SpriteRenderer
                if (renderer is SpriteRenderer spriteRenderer)
                {
                    // Verificar que el color tenga alpha visible
                    Color newColor = definition.PrimaryColor;
                    if (newColor.a < 0.1f) newColor.a = 1f;
                    spriteRenderer.color = newColor;
                }
                // Para otros renderers (3D)
                else if (renderer.material != null)
                {
                    // No modificar materiales compartidos en el proyecto
                    if (renderer.sharedMaterial != null &&
                        renderer.sharedMaterial.name.Contains("Default"))
                    {
                        DebugLog($"Saltando material default en {renderer.name}");
                        continue;
                    }

                    // Crear una instancia del material para no modificar el original
                    renderer.material = new Material(renderer.material);

                    // Intentar diferentes propiedades de color según el shader
                    if (renderer.material.HasProperty("_Color"))
                    {
                        Color newColor = definition.PrimaryColor;
                        if (newColor.a < 0.1f) newColor.a = 1f;
                        renderer.material.SetColor("_Color", newColor);
                    }
                    else if (renderer.material.HasProperty("_TintColor"))
                    {
                        renderer.material.SetColor("_TintColor", definition.PrimaryColor);
                    }
                    else if (renderer.material.HasProperty("_BaseColor"))
                    {
                        // Para URP/HDRP
                        Color newColor = definition.PrimaryColor;
                        if (newColor.a < 0.1f) newColor.a = 1f;
                        renderer.material.SetColor("_BaseColor", newColor);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Error aplicando color a {renderer.name}: {e.Message}");
            }
        }

        // Aplicar color a sistemas de partículas
        foreach (var ps in particleSystems)
        {
            var main = ps.main;
            main.startColor = definition.PrimaryColor;
        }
    }

    // ===== AUDIO =====
    protected virtual void SetupAudio()
    {
        if (audioSource == null || definition == null) return;

        audioSource.volume = definition.SoundVolume;

        if (definition.LoopSound != null)
        {
            audioSource.clip = definition.LoopSound;
            audioSource.loop = true;
        }
    }

    protected virtual void PlayActivationSound()
    {
        if (audioSource == null || definition == null || definition.ActivationSound == null)
            return;

        audioSource.PlayOneShot(definition.ActivationSound, definition.SoundVolume);
    }

    protected virtual void StopSounds()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    // ===== EFECTOS DE PARTÍCULAS =====
    protected virtual void ActivateParticleEffects()
    {
        if (!definition.UseParticleEffects) return;

        foreach (var ps in particleSystems)
        {
            ps.Play();
        }
    }

    protected virtual void DeactivateParticleEffects()
    {
        foreach (var ps in particleSystems)
        {
            ps.Stop();
        }
    }

    // ===== UTILIDADES =====

    /// <summary>
    /// Obtiene la distancia al jugador
    /// </summary>
    protected float GetDistanceToPlayer()
    {
        if (playerTransform == null) return float.MaxValue;
        return Vector3.Distance(transform.position, playerTransform.position);
    }

    /// <summary>
    /// Obtiene la dirección hacia el jugador
    /// </summary>
    protected Vector3 GetDirectionToPlayer()
    {
        if (playerTransform == null) return Vector3.zero;
        return (playerTransform.position - transform.position).normalized;
    }

    /// <summary>
    /// Busca enemigos cercanos
    /// </summary>
    protected GameObject[] FindNearbyEnemies(float radius)
    {
        var colliders = Physics2D.OverlapCircleAll(transform.position, radius);
        var enemies = new System.Collections.Generic.List<GameObject>();

        foreach (var col in colliders)
        {
            if (col.CompareTag("Enemy"))
            {
                enemies.Add(col.gameObject);
            }
        }

        return enemies.ToArray();
    }

    /// <summary>
    /// Busca el enemigo más cercano
    /// </summary>
    protected GameObject FindClosestEnemy(float maxRadius)
    {
        var enemies = FindNearbyEnemies(maxRadius);
        if (enemies.Length == 0) return null;

        GameObject closest = null;
        float closestDistance = float.MaxValue;

        foreach (var enemy in enemies)
        {
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = enemy;
            }
        }

        return closest;
    }

    /// <summary>
    /// Aplica daño a un enemigo usando IDamageable
    /// </summary>
    protected virtual void DamageEnemy(GameObject enemy, int damage)
    {
        if (enemy == null) return;

        var damageable = enemy.GetComponent<IDamageable>();
        if (damageable == null)
        {
            damageable = enemy.GetComponentInParent<IDamageable>();
        }

        if (damageable != null && damageable.IsAlive)
        {
            damageable.TakeDamage(damage, enemy.transform.position, transform);
            DebugLog($"{Name} causó {damage} de daño a {enemy.name}");
        }
    }

    // ===== DEBUG =====
    protected void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[KillStreak - {Name}] {message}");
        }
    }

    protected virtual void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        // Mostrar rango si está definido
        if (definition != null && definition.Range > 0)
        {
            Gizmos.color = isActive ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, definition.Range);
        }

        // Línea hacia el jugador
        if (playerTransform != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, playerTransform.position);
        }
    }

    protected virtual void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;

        // Información adicional cuando está seleccionado
        if (definition != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, definition.Range * 1.2f);
        }
    }

    // ===== CLEANUP =====
    protected virtual void OnDestroy()
    {
        // Limpiar eventos
        OnActivated = null;
        OnDeactivated = null;

        // Detener sonidos
        StopSounds();
    }
}
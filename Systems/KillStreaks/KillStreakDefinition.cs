using UnityEngine;

/// <summary>
/// ScriptableObject que define una KillStreak
/// </summary>
[CreateAssetMenu(fileName = "KillStreak_", menuName = "Game/KillStreak Definition", order = 100)]
public class KillStreakDefinition : ScriptableObject
{
    [Header("═══════════════════════════════════════")]
    [Header("INFORMACIÓN BÁSICA")]
    [Header("═══════════════════════════════════════")]

    [Header("Display Info")]
    [SerializeField] private string killStreakName = "New KillStreak";
    [TextArea(2, 4)]
    [SerializeField] private string description = "KillStreak description";
    [SerializeField] private Sprite icon;

    [Header("═══════════════════════════════════════")]
    [Header("CONFIGURACIÓN DE ACTIVACIÓN")]
    [Header("═══════════════════════════════════════")]

    [Header("Activation")]
    [Tooltip("Número de kills consecutivos necesarios para activar")]
    [SerializeField] private int requiredCombo = 5;

    [Tooltip("Prefab que contiene el comportamiento de la KillStreak")]
    [SerializeField] private GameObject behaviorPrefab;

    [Header("═══════════════════════════════════════")]
    [Header("PARÁMETROS ESPECÍFICOS")]
    [Header("═══════════════════════════════════════")]

    [Header("Combat Parameters")]
    [SerializeField] private int damage = 10;
    [SerializeField] private float attackSpeed = 1f;
    [SerializeField] private float range = 5f;

    [Header("Movement Parameters")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 180f;

    [Header("Visual Parameters")]
    [SerializeField] private Color primaryColor = Color.cyan;
    [SerializeField] private bool useParticleEffects = true;

    [Header("═══════════════════════════════════════")]
    [Header("CONFIGURACIÓN AVANZADA")]
    [Header("═══════════════════════════════════════")]

    [Header("Spawning")]
    [SerializeField] private Vector3 spawnOffset = Vector3.zero;
    [SerializeField] private bool parentToPlayer = false;
    [SerializeField] private int maxInstances = 1;

    [Header("Audio")]
    [SerializeField] private AudioClip activationSound;
    [SerializeField] private AudioClip loopSound;
    [SerializeField] private float soundVolume = 1f;

    // ===== PROPIEDADES PÚBLICAS =====
    public string KillStreakName => killStreakName;
    public string Description => description;
    public Sprite Icon => icon;
    public int RequiredCombo => requiredCombo;
    public GameObject BehaviorPrefab => behaviorPrefab;

    // Parámetros de combate
    public int Damage => damage;
    public float AttackSpeed => attackSpeed;
    public float Range => range;

    // Parámetros de movimiento
    public float MoveSpeed => moveSpeed;
    public float RotationSpeed => rotationSpeed;

    // Parámetros visuales
    public Color PrimaryColor => primaryColor;
    public bool UseParticleEffects => useParticleEffects;

    // Configuración de spawn
    public Vector3 SpawnOffset => spawnOffset;
    public bool ParentToPlayer => parentToPlayer;
    public int MaxInstances => maxInstances;

    // Audio
    public AudioClip ActivationSound => activationSound;
    public AudioClip LoopSound => loopSound;
    public float SoundVolume => soundVolume;

    /// <summary>
    /// Crea una instancia del comportamiento de esta KillStreak
    /// </summary>
    public GameObject CreateInstance(Vector3 position, Transform parent = null)
    {
        if (behaviorPrefab == null)
        {
            Debug.LogError($"KillStreakDefinition {killStreakName}: No tiene prefab asignado!");
            return null;
        }

        GameObject instance;
        if (parentToPlayer && parent != null)
        {
            instance = Instantiate(behaviorPrefab, parent);
            instance.transform.localPosition = spawnOffset;
        }
        else
        {
            instance = Instantiate(behaviorPrefab, position + spawnOffset, Quaternion.identity);
        }

        // Aplicar configuración al comportamiento
        var behavior = instance.GetComponent<IKillStreakBehavior>();
        if (behavior != null)
        {
            behavior.Initialize(this, parent);
        }
        else
        {
            Debug.LogError($"KillStreakDefinition {killStreakName}: El prefab no tiene un componente IKillStreakBehavior!");
            Destroy(instance);
            return null;
        }

        return instance;
    }

    /// <summary>
    /// Valida que la definición esté correctamente configurada
    /// </summary>
    public bool IsValid()
    {
        if (string.IsNullOrEmpty(killStreakName))
        {
            Debug.LogError("KillStreakDefinition: Nombre vacío!");
            return false;
        }

        if (behaviorPrefab == null)
        {
            Debug.LogError($"KillStreakDefinition {killStreakName}: No tiene prefab asignado!");
            return false;
        }

        if (requiredCombo <= 0)
        {
            Debug.LogError($"KillStreakDefinition {killStreakName}: Combo requerido debe ser mayor a 0!");
            return false;
        }

        return true;
    }

    // ===== MÉTODOS DE UTILIDAD PARA EL EDITOR =====

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Asegurar valores válidos
        requiredCombo = Mathf.Max(1, requiredCombo);
        damage = Mathf.Max(0, damage);
        attackSpeed = Mathf.Max(0.1f, attackSpeed);
        range = Mathf.Max(0.1f, range);
        moveSpeed = Mathf.Max(0f, moveSpeed);
        rotationSpeed = Mathf.Max(0f, rotationSpeed);
        maxInstances = Mathf.Max(1, maxInstances);
        soundVolume = Mathf.Clamp01(soundVolume);
    }

    [ContextMenu("Log Configuration")]
    public void LogConfiguration()
    {
        Debug.Log($"=== KillStreak: {killStreakName} ===");
        Debug.Log($"Required Combo: {requiredCombo}");
        Debug.Log($"Damage: {damage} | Attack Speed: {attackSpeed} | Range: {range}");
        Debug.Log($"Move Speed: {moveSpeed} | Rotation Speed: {rotationSpeed}");
        Debug.Log($"Has Prefab: {behaviorPrefab != null}");
        Debug.Log($"Has Icon: {icon != null}");
    }
#endif
}
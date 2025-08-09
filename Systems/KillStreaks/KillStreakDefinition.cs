// Code/Systems/KillStreaks/KillStreakDefinition.cs
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Define las estadísticas para un nivel específico de una KillStreak.
/// </summary>
[System.Serializable]
public class KillStreakLevelStats
{
    [Tooltip("Número de kills consecutivos necesarios para alcanzar este nivel.")]
    public int requiredCombo = 5;

    [Header("Generic Stats")]
    [Tooltip("Valor numérico principal de la habilidad (daño, curación, etc.).")]
    public float potency = 10f;

    [Tooltip("El área o tamaño del efecto (radio de explosión, escala de un escudo).")]
    public float effectRadius = 5f;

    [Tooltip("El tiempo que dura un efecto (en segundos). 0 para efectos instantáneos.")]
    public float duration = 0f;

    [Tooltip("Número de instancias o proyectiles (drones, misiles por ráfaga).")]
    public int amount = 1;

    [Tooltip("Velocidad de proyectiles, de un dron, de rotación de un orbe, etc.")]
    public float speed = 5f;

    [Tooltip("El tiempo entre cada 'acción' de la habilidad (disparos, pulsos de curación).")]
    public float cooldown = 1f;

    [Tooltip("La frecuencia o 'velocidad de ataque' de la acción (disparos por segundo, etc.).")]
    public float rate = 1f;
}

/// <summary>
/// ScriptableObject que define una KillStreak de forma genérica.
/// El comportamiento específico y los visuales se definen en el prefab asignado.
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
    [Header("CONFIGURACIÓN DE COMPORTAMIENTO")]
    [Header("═══════════════════════════════════════")]

    [Tooltip("Prefab que contiene el componente IKillStreakBehavior con toda la lógica y visuales.")]
    [SerializeField] private GameObject behaviorPrefab;

    [Header("═══════════════════════════════════════")]
    [Header("NIVELES DE LA KILLSTREAK (Máximo 4)")]
    [Header("═══════════════════════════════════════")]

    [Tooltip("Define las estadísticas para cada nivel de la KillStreak. El nivel 1 es el índice 0.")]
    [SerializeField] private List<KillStreakLevelStats> levels = new List<KillStreakLevelStats>(4);

    // ===== PROPIEDADES PÚBLICAS =====
    public string KillStreakName => killStreakName;
    public string Description => description;
    public Sprite Icon => icon;
    public GameObject BehaviorPrefab => behaviorPrefab;
    public int MaxLevel => levels.Count;

    /// <summary>
    /// Obtiene las estadísticas para un nivel específico.
    /// </summary>
    public KillStreakLevelStats GetStatsForLevel(int level)
    {
        int index = level - 1;
        if (index >= 0 && index < levels.Count)
        {
            return levels[index];
        }

        return null;
    }

    /// <summary>
    /// Crea una instancia del comportamiento de esta KillStreak con un nivel específico.
    /// </summary>
    public GameObject CreateInstance(Vector3 position, Transform parent, int level)
    {
        if (behaviorPrefab == null)
        {
            Debug.LogError($"KillStreakDefinition {killStreakName}: No tiene prefab asignado!");
            return null;
        }

        GameObject instance = Instantiate(behaviorPrefab, position, Quaternion.identity);

        var behavior = instance.GetComponent<IKillStreakBehavior>();
        if (behavior != null)
        {
            // AHORA: Inicializa con la definición y el nivel ESPECIFICADO.
            behavior.Initialize(this, parent, level);
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
    /// Crea una instancia del comportamiento de esta KillStreak (asume Nivel 1 por compatibilidad).
    /// </summary>
    public GameObject CreateInstance(Vector3 position, Transform parent = null)
    {
        // Llama a la versión más detallada con el nivel 1 por defecto.
        return CreateInstance(position, parent, 1);
    }


    /// <summary>
    /// Valida que la definición esté correctamente configurada.
    /// </summary>
    public bool IsValid()
    {
        if (string.IsNullOrEmpty(killStreakName)) return false;
        if (behaviorPrefab == null) return false;
        if (levels.Count == 0)
        {
            Debug.LogError($"KillStreakDefinition {killStreakName}: No tiene niveles configurados!");
            return false;
        }
        return true;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Limitar a 4 niveles
        if (levels.Count > 4)
        {
            levels.RemoveRange(4, levels.Count - 4);
            Debug.LogWarning($"Se ha limitado el número de niveles a 4 para {killStreakName}.");
        }
    }
#endif
}
// Code/Systems/KillStreaks/KillStreakManager.cs
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

/// <summary>
/// Gestor central del sistema de KillStreaks
/// </summary>
public class KillStreakManager : MonoBehaviour
{
    [Header("═══════════════════════════════════════")]
    [Header("CONFIGURACIÓN DE KILLSTREAKS")]
    [Header("═══════════════════════════════════════")]
    [SerializeField] private KillStreakSlot[] killStreakSlots = new KillStreakSlot[3];
    [SerializeField] private Transform playerTransform;
    [SerializeField] private bool findPlayerAutomatically = true;

    [Header("═══════════════════════════════════════")]
    [Header("DEBUG")]
    [Header("═══════════════════════════════════════")]
    [SerializeField] private bool enableDebugLogs = false;
    [SerializeField] private bool showActiveKillStreaks = true;

    private Dictionary<KillStreakDefinition, List<GameObject>> activeKillStreakInstances = new Dictionary<KillStreakDefinition, List<GameObject>>();
    private ComboManager comboManager;
    private bool isInitialized = false;

    public event Action<KillStreakDefinition> OnKillStreakActivated;
    public event Action<KillStreakDefinition> OnKillStreakDeactivated;
    public event Action<KillStreakSlot[]> OnSlotsChanged;

    public static KillStreakManager Instance { get; private set; }

    public KillStreakSlot[] Slots => killStreakSlots;
    public bool HasActiveKillStreaks => activeKillStreakInstances.Count > 0;
    public int ActiveKillStreakCount => activeKillStreakInstances.Count;

    void Awake()
    {
        SetupSingleton();
        ValidateSlots();
    }

    void Start()
    {
        Initialize();
    }

    void SetupSingleton()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    void Initialize()
    {
        if (isInitialized) return;
        if (findPlayerAutomatically && playerTransform == null) FindPlayer();
        ConnectToComboManager();
        isInitialized = true;
     
    }

    void ValidateSlots()
    {
        if (killStreakSlots.Length != 3) { Array.Resize(ref killStreakSlots, 3); }
        for (int i = 0; i < killStreakSlots.Length; i++)
        {
            if (killStreakSlots[i].definition != null && !killStreakSlots[i].definition.IsValid())
            {
                Debug.LogError($"Slot {i}: KillStreakDefinition inválida!");
            }
        }
    }

    void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) { playerTransform = player.transform; }
        else if (LevelManager.Instance != null) { LevelManager.Instance.OnPlayerSpawned += OnPlayerSpawned; }
    }

    void OnPlayerSpawned()
    {
        FindPlayer();
        foreach (var kvp in activeKillStreakInstances)
        {
            foreach (var instance in kvp.Value)
            {
                if (instance != null) instance.GetComponent<IKillStreakBehavior>().PlayerTransform = playerTransform;
            }
        }
    }

    void ConnectToComboManager()
    {
        comboManager = ComboManager.Instance;
        if (comboManager != null)
        {
            comboManager.OnComboChanged += HandleComboChanged;
            comboManager.OnComboReset += HandleComboReset;
        }
    }

    void HandleComboChanged(int currentCombo)
    {
        foreach (var slot in killStreakSlots)
        {
            if (!slot.IsValid()) continue;
            int requiredCombo = slot.GetRequiredComboForNextLevel();
            if (requiredCombo > 0 && currentCombo >= requiredCombo)
            {
                if (slot.isActive) { LevelUpKillStreak(slot); }
                else { ActivateKillStreak(slot); }
            }
        }
    }

    void HandleComboReset() { }

    void ActivateKillStreak(KillStreakSlot slot)
    {
        if (!slot.IsValid() || slot.isActive || playerTransform == null) return;

        slot.isActive = true;
        slot.currentLevel = 1;

        // Limpia la lista por si acaso y crea las instancias para el Nivel 1
        RecreateInstancesForSlot(slot);

        OnKillStreakActivated?.Invoke(slot.definition);
        OnSlotsChanged?.Invoke(killStreakSlots);
    }

    // ===== MÉTODO LevelUpKillStreak (LÓGICA COMPLETAMENTE NUEVA) =====
    void LevelUpKillStreak(KillStreakSlot slot)
    {
        if (!slot.isActive || !slot.IsValid() || slot.currentLevel >= slot.definition.MaxLevel) return;

        slot.currentLevel++;

        // Destruye las instancias antiguas y crea las nuevas sincronizadas
        RecreateInstancesForSlot(slot);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowNotification($"¡{slot.definition.KillStreakName} Nivel {slot.currentLevel}!", 2f);
        }

        OnSlotsChanged?.Invoke(killStreakSlots);
    }

    // ===== MÉTODO NUEVO Y CENTRALIZADO =====
    /// <summary>
    /// Destruye las instancias existentes de un KillStreak y crea las nuevas según el nivel actual del slot.
    /// </summary>
    void RecreateInstancesForSlot(KillStreakSlot slot)
    {
        // 1. Destruir instancias antiguas si existen
        if (activeKillStreakInstances.TryGetValue(slot.definition, out var oldInstances))
        {
            foreach (var oldInstance in oldInstances)
            {
                if (oldInstance != null) Destroy(oldInstance);
            }
            oldInstances.Clear();
        }
        else
        {
            // Si no existía, crea la entrada en el diccionario
            activeKillStreakInstances[slot.definition] = new List<GameObject>();
        }

        // 2. Crear nuevas instancias según el nivel actual
        var stats = slot.definition.GetStatsForLevel(slot.currentLevel);
        if (stats == null) return;

        int amountToCreate = stats.amount;
        if (amountToCreate <= 0) return; // No crear nada si la cantidad es cero o menos

        // --- INICIO DE LA NUEVA LÓGICA ---
        float angleStep = 360f / amountToCreate; // Calcula el espacio angular entre orbes
        float baseAngle = UnityEngine.Random.Range(0f, 360f); // Genera UN ángulo aleatorio para todo el grupo
        // --- FIN DE LA NUEVA LÓGICA ---

        for (int i = 0; i < amountToCreate; i++)
        {
            GameObject newInstance = slot.definition.CreateInstance(playerTransform.position, playerTransform, slot.currentLevel);

            if (newInstance != null)
            {
                var behavior = newInstance.GetComponent<IKillStreakBehavior>();
                if (behavior != null)
                {
                    var orbBehavior = behavior as OrbitalOrbBehavior;
                    if (orbBehavior != null)
                    {
                        // --- LÓGICA MODIFICADA ---
                        // Calculamos el ángulo final para este orbe y se lo pasamos
                        float finalAngle = baseAngle + (i * angleStep);
                        orbBehavior.SetAngleOffset(finalAngle);
                        // --- FIN DE LA MODIFICACIÓN ---
                    }

                    behavior.OnDeactivated += HandleBehaviorDeactivated;
                    behavior.Activate();
                    activeKillStreakInstances[slot.definition].Add(newInstance);
                }
            }
        }
    }




    void HandleBehaviorDeactivated(IKillStreakBehavior behavior)
    {
        // Esta lógica ahora es más compleja, la forma más segura es simplemente buscar a qué definición pertenece
        KillStreakDefinition definitionToRemove = null;
        foreach (var kvp in activeKillStreakInstances)
        {
            foreach (var instance in kvp.Value)
            {
                if (instance != null && instance.GetComponent<IKillStreakBehavior>() == behavior)
                {
                    definitionToRemove = kvp.Key;
                    break;
                }
            }
            if (definitionToRemove != null) break;
        }

        if (definitionToRemove != null)
        {
            DeactivateKillStreak(definitionToRemove);
        }
    }

    void DeactivateKillStreak(KillStreakDefinition definition)
    {
        if (!activeKillStreakInstances.ContainsKey(definition)) return;

        foreach (var instance in activeKillStreakInstances[definition])
        {
            if (instance != null)
            {
                var behavior = instance.GetComponent<IKillStreakBehavior>();
                if (behavior != null) behavior.OnDeactivated -= HandleBehaviorDeactivated;
                Destroy(instance);
            }
        }

        foreach (var slot in killStreakSlots)
        {
            if (slot.definition == definition)
            {
                slot.isActive = false;
                slot.currentLevel = 0;
                break;
            }
        }

        activeKillStreakInstances.Remove(definition);
        OnKillStreakDeactivated?.Invoke(definition);
        OnSlotsChanged?.Invoke(killStreakSlots);
    }

    public void DeactivateAllKillStreaks()
    {
        var definitionsToDeactivate = new List<KillStreakDefinition>(activeKillStreakInstances.Keys);
        foreach (var definition in definitionsToDeactivate)
        {
            DeactivateKillStreak(definition);
        }
    }

    public void ResetSystem()
    {
        DeactivateAllKillStreaks();
        for (int i = 0; i < killStreakSlots.Length; i++)
        {
            killStreakSlots[i].isActive = false;
            killStreakSlots[i].currentLevel = 0;
        }
        OnSlotsChanged?.Invoke(killStreakSlots);
    }

    void OnDestroy()
    {
        if (comboManager != null)
        {
            comboManager.OnComboChanged -= HandleComboChanged;
            comboManager.OnComboReset -= HandleComboReset;
        }
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnPlayerSpawned -= OnPlayerSpawned;
        }
        DeactivateAllKillStreaks();
        if (Instance == this) { Instance = null; }
    }
}


[System.Serializable]
public class KillStreakSlot
{
    public KillStreakDefinition definition;
    [HideInInspector] public bool isActive = false;
    [HideInInspector] public int currentLevel = 0;

    public bool IsValid() { return definition != null && definition.IsValid(); }
    public int GetRequiredCombo() { return IsValid() ? (definition.GetStatsForLevel(1)?.requiredCombo ?? 0) : 0; }
    public int GetRequiredComboForNextLevel()
    {
        if (!IsValid()) return 0;
        if (!isActive) return GetRequiredCombo();
        var stats = definition.GetStatsForLevel(currentLevel + 1);
        return stats?.requiredCombo ?? 0;
    }
}
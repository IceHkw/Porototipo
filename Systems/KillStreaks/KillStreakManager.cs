using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// Gestor central del sistema de KillStreaks
/// </summary>
public class KillStreakManager : MonoBehaviour
{
    [Header("═══════════════════════════════════════")]
    [Header("CONFIGURACIÓN DE KILLSTREAKS")]
    [Header("═══════════════════════════════════════")]

    [Header("KillStreak Slots")]
    [SerializeField] private KillStreakSlot[] killStreakSlots = new KillStreakSlot[3];

    [Header("Player Reference")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private bool findPlayerAutomatically = true;

    [Header("═══════════════════════════════════════")]
    [Header("DEBUG")]
    [Header("═══════════════════════════════════════")]

    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLogs = false;
    [SerializeField] private bool showActiveKillStreaks = true;

    // ===== ESTADO =====
    private Dictionary<KillStreakDefinition, IKillStreakBehavior> activeKillStreaks = new Dictionary<KillStreakDefinition, IKillStreakBehavior>();
    private List<GameObject> killStreakInstances = new List<GameObject>();
    private ComboManager comboManager;
    private bool isInitialized = false;

    // ===== EVENTOS =====
    public event Action<KillStreakDefinition> OnKillStreakActivated;
    public event Action<KillStreakDefinition> OnKillStreakDeactivated;
    public event Action<KillStreakSlot[]> OnSlotsChanged;

    // ===== SINGLETON =====
    public static KillStreakManager Instance { get; private set; }

    // ===== PROPIEDADES PÚBLICAS =====
    public KillStreakSlot[] Slots => killStreakSlots;
    public bool HasActiveKillStreaks => activeKillStreaks.Count > 0;
    public int ActiveKillStreakCount => activeKillStreaks.Count;

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
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Initialize()
    {
        if (isInitialized) return;

        // Buscar jugador si es necesario
        if (findPlayerAutomatically && playerTransform == null)
        {
            FindPlayer();
        }

        // Conectar con ComboManager
        ConnectToComboManager();

        isInitialized = true;

        DebugLog("KillStreakManager inicializado");
    }

    void ValidateSlots()
    {
        // Asegurar que tenemos exactamente 3 slots
        if (killStreakSlots.Length != 3)
        {
            System.Array.Resize(ref killStreakSlots, 3);
            Debug.LogWarning("KillStreakManager: Ajustando a 3 slots");
        }

        // Validar cada slot
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
        if (player != null)
        {
            playerTransform = player.transform;
            DebugLog("Jugador encontrado automáticamente");
        }
        else
        {
            // Suscribirse al evento de spawn del jugador
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.OnPlayerSpawned += OnPlayerSpawned;
            }
        }
    }

    void OnPlayerSpawned()
    {
        // Buscar de nuevo cuando el jugador es spawneado
        FindPlayer();

        // Re-aplicar referencias de jugador a KillStreaks activas
        foreach (var kvp in activeKillStreaks)
        {
            kvp.Value.PlayerTransform = playerTransform;
        }
    }

    void ConnectToComboManager()
    {
        comboManager = ComboManager.Instance;

        if (comboManager != null)
        {
            comboManager.OnComboChanged += HandleComboChanged;
            comboManager.OnComboReset += HandleComboReset;
            DebugLog("Conectado a ComboManager");
        }
        else
        {
            Debug.LogWarning("KillStreakManager: No se encontró ComboManager!");
        }
    }

    // ===== MANEJO DE COMBOS =====
    void HandleComboChanged(int currentCombo)
    {
        // Verificar cada slot para posible activación
        foreach (var slot in killStreakSlots)
        {
            if (!slot.IsValid()) continue;
            if (slot.isActive) continue; // Ya está activa

            // Verificar si alcanzó el combo requerido
            if (currentCombo >= slot.definition.RequiredCombo)
            {
                ActivateKillStreak(slot);
            }
        }
    }

    void HandleComboReset()
    {
        // El combo se resetea pero las KillStreaks activas permanecen
        DebugLog("Combo reseteado - KillStreaks activas se mantienen");
    }

    // ===== ACTIVACIÓN/DESACTIVACIÓN =====
    void ActivateKillStreak(KillStreakSlot slot)
    {
        if (slot.definition == null || slot.isActive) return;

        if (playerTransform == null)
        {
            Debug.LogError("KillStreakManager: No hay referencia al jugador!");
            return;
        }

        DebugLog($"Activando KillStreak: {slot.definition.KillStreakName}");

        // Crear instancia
        Vector3 spawnPosition = playerTransform.position + slot.definition.SpawnOffset;
        GameObject instance = slot.definition.CreateInstance(spawnPosition, playerTransform);

        if (instance == null) return;

        // Obtener comportamiento
        var behavior = instance.GetComponent<IKillStreakBehavior>();
        if (behavior == null)
        {
            Destroy(instance);
            return;
        }

        // Registrar
        activeKillStreaks[slot.definition] = behavior;
        killStreakInstances.Add(instance);
        slot.isActive = true;

        // Suscribirse a eventos del comportamiento
        behavior.OnDeactivated += HandleBehaviorDeactivated;

        // Activar
        behavior.Activate();

        // Disparar eventos
        OnKillStreakActivated?.Invoke(slot.definition);
        OnSlotsChanged?.Invoke(killStreakSlots);

        // Notificación UI
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowNotification($"¡{slot.definition.KillStreakName} ACTIVADA!", 2f);
        }
    }

    void HandleBehaviorDeactivated(IKillStreakBehavior behavior)
    {
        // Encontrar la definición correspondiente
        KillStreakDefinition definitionToRemove = null;
        foreach (var kvp in activeKillStreaks)
        {
            if (kvp.Value == behavior)
            {
                definitionToRemove = kvp.Key;
                break;
            }
        }

        if (definitionToRemove != null)
        {
            DeactivateKillStreak(definitionToRemove);
        }
    }

    void DeactivateKillStreak(KillStreakDefinition definition)
    {
        if (!activeKillStreaks.ContainsKey(definition)) return;

        var behavior = activeKillStreaks[definition];

        // Desactivar comportamiento
        behavior.Deactivate();
        behavior.OnDeactivated -= HandleBehaviorDeactivated;

        // Encontrar y destruir instancia
        GameObject instanceToRemove = null;
        foreach (var instance in killStreakInstances)
        {
            if (instance != null && instance.GetComponent<IKillStreakBehavior>() == behavior)
            {
                instanceToRemove = instance;
                break;
            }
        }

        if (instanceToRemove != null)
        {
            killStreakInstances.Remove(instanceToRemove);
            Destroy(instanceToRemove);
        }

        // Actualizar estado del slot
        foreach (var slot in killStreakSlots)
        {
            if (slot.definition == definition)
            {
                slot.isActive = false;
                break;
            }
        }

        // Remover del diccionario
        activeKillStreaks.Remove(definition);

        // Disparar eventos
        OnKillStreakDeactivated?.Invoke(definition);
        OnSlotsChanged?.Invoke(killStreakSlots);

        DebugLog($"KillStreak desactivada: {definition.KillStreakName}");
    }

    // ===== MÉTODOS PÚBLICOS =====

    /// <summary>
    /// Establece el jugador de referencia
    /// </summary>
    public void SetPlayer(Transform player)
    {
        playerTransform = player;

        // Actualizar referencias en KillStreaks activas
        foreach (var kvp in activeKillStreaks)
        {
            kvp.Value.PlayerTransform = playerTransform;
        }
    }

    /// <summary>
    /// Obtiene información de un slot específico
    /// </summary>
    public KillStreakSlot GetSlot(int index)
    {
        if (index < 0 || index >= killStreakSlots.Length)
            return null;

        return killStreakSlots[index];
    }

    /// <summary>
    /// Cambia la KillStreak en un slot específico
    /// </summary>
    public void SetSlot(int index, KillStreakDefinition definition, int requiredCombo = -1)
    {
        if (index < 0 || index >= killStreakSlots.Length) return;

        // Si hay una activa en ese slot, desactivarla
        if (killStreakSlots[index].isActive)
        {
            DeactivateKillStreak(killStreakSlots[index].definition);
        }

        // Asignar nueva definición
        killStreakSlots[index].definition = definition;

        // Si no se especifica combo, usar el de la definición
        if (requiredCombo < 0 && definition != null)
        {
            requiredCombo = definition.RequiredCombo;
        }

        // Actualizar slot
        if (definition != null)
        {
            killStreakSlots[index] = new KillStreakSlot
            {
                definition = definition,
                requiredCombo = requiredCombo,
                isActive = false
            };
        }

        OnSlotsChanged?.Invoke(killStreakSlots);
    }

    /// <summary>
    /// Desactiva todas las KillStreaks activas
    /// </summary>
    public void DeactivateAllKillStreaks()
    {
        var definitionsToDeactivate = new List<KillStreakDefinition>(activeKillStreaks.Keys);

        foreach (var definition in definitionsToDeactivate)
        {
            DeactivateKillStreak(definition);
        }
    }

    /// <summary>
    /// Resetea el sistema completo
    /// </summary>
    public void ResetSystem()
    {
        DeactivateAllKillStreaks();

        // Resetear estados de slots
        for (int i = 0; i < killStreakSlots.Length; i++)
        {
            killStreakSlots[i].isActive = false;
        }

        OnSlotsChanged?.Invoke(killStreakSlots);

        DebugLog("Sistema de KillStreaks reseteado");
    }

    // ===== HELPERS =====
    void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[KillStreakManager] {message}");
        }
    }

    // ===== CLEANUP =====
    void OnDestroy()
    {
        // Desuscribirse de eventos
        if (comboManager != null)
        {
            comboManager.OnComboChanged -= HandleComboChanged;
            comboManager.OnComboReset -= HandleComboReset;
        }

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnPlayerSpawned -= OnPlayerSpawned;
        }

        // Limpiar KillStreaks activas
        DeactivateAllKillStreaks();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    // ===== DEBUG UI =====
    void OnGUI()
    {
        if (!showActiveKillStreaks || !Application.isPlaying) return;

        GUILayout.BeginArea(new Rect(10, 200, 300, 200));
        GUILayout.Label("=== KILLSTREAKS ACTIVAS ===");

        foreach (var slot in killStreakSlots)
        {
            if (slot.definition != null)
            {
                string status = slot.isActive ? "ACTIVA" : $"Requiere: {slot.requiredCombo} kills";
                GUILayout.Label($"{slot.definition.KillStreakName}: {status}");
            }
        }

        GUILayout.EndArea();
    }

    // ===== MÉTODOS DE TESTING =====
#if UNITY_EDITOR
    [ContextMenu("Force Activate All KillStreaks")]
    public void TestActivateAll()
    {
        foreach (var slot in killStreakSlots)
        {
            if (slot.definition != null && !slot.isActive)
            {
                ActivateKillStreak(slot);
            }
        }
    }

    [ContextMenu("Deactivate All KillStreaks")]
    public void TestDeactivateAll()
    {
        DeactivateAllKillStreaks();
    }

    [ContextMenu("Log Active KillStreaks")]
    public void LogActiveKillStreaks()
    {
        Debug.Log($"=== KillStreaks Activas: {activeKillStreaks.Count} ===");
        foreach (var kvp in activeKillStreaks)
        {
            Debug.Log($"- {kvp.Key.KillStreakName}");
        }
    }
#endif
}

/// <summary>
/// Estructura que representa un slot de KillStreak
/// </summary>
[System.Serializable]
public class KillStreakSlot
{
    [Tooltip("Definición de la KillStreak")]
    public KillStreakDefinition definition;

    [Tooltip("Combo requerido (usa el de la definición si es -1)")]
    public int requiredCombo = -1;

    [HideInInspector]
    public bool isActive = false;

    public bool IsValid()
    {
        return definition != null && definition.IsValid();
    }

    public int GetRequiredCombo()
    {
        if (requiredCombo > 0) return requiredCombo;
        return definition != null ? definition.RequiredCombo : 0;
    }
}
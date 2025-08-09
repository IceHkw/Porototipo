using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;

/// <summary>
/// Manager central para todo el sistema de UI usando UI Toolkit
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("═══════════════════════════════════════")]
    [Header("CONFIGURACIÓN PRINCIPAL")]
    [Header("═══════════════════════════════════════")]

    [Header("UI Document")]
    [SerializeField] private UIDocument mainUIDocument;
    [SerializeField] private PanelSettings panelSettings;

    [Header("UXML Assets")]
    [SerializeField] private VisualTreeAsset hudTemplate;
    //[SerializeField] private VisualTreeAsset pauseMenuTemplate;
    //[SerializeField] private VisualTreeAsset gameOverTemplate;

    [Header("USS Styles")]
    [SerializeField] private StyleSheet mainStyleSheet;
    [SerializeField] private StyleSheet pixelArtStyle;

    [Header("Debug")]
    [SerializeField] private bool enableDebugMode = false;

    // Singleton
    public static UIManager Instance { get; private set; }

    // Referencias a los controladores
    private Dictionary<string, IUIController> uiControllers = new Dictionary<string, IUIController>();

    // Referencias a elementos principales
    private VisualElement root;
    private VisualElement hudContainer;
    private VisualElement menuContainer;
    private VisualElement overlayContainer;

    // Estados
    private bool isInitialized = false;
    private bool hudControllerReady = false;
    private bool playerPendingConnection = false;
    private GameState currentGameState = GameState.Playing;

    // Eventos
    public event Action<GameState> OnGameStateUIChanged;
    public event Action OnUIInitialized;

    void Awake()
    {
        SetupSingleton();

        // SOLUCIÓN CRÍTICA: Inicializar TODO en Awake para estar listos antes que otros sistemas
        InitializeUIDocument();
        InitializeUI();

        // Ahora que el HUD está creado, podemos suscribirnos a eventos
        SubscribeToGameEvents();

        DebugLog("UIManager completamente inicializado en Awake");
    }

    void Start()
    {
        // Verificar si el jugador ya existe y necesita ser conectado
        CheckForExistingPlayer();

        // Si había una conexión pendiente, intentarla ahora
        if (playerPendingConnection)
        {
            DebugLog("Procesando conexión pendiente del jugador...");
            HandlePlayerSpawned();
            playerPendingConnection = false;
        }
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

    void InitializeUIDocument()
    {
        if (mainUIDocument == null)
        {
            mainUIDocument = GetComponent<UIDocument>();
            if (mainUIDocument == null)
            {
                mainUIDocument = gameObject.AddComponent<UIDocument>();
                if (hudTemplate != null)
                {
                    mainUIDocument.visualTreeAsset = hudTemplate;
                }
            }
        }

        if (mainUIDocument.panelSettings == null && panelSettings != null)
        {
            mainUIDocument.panelSettings = panelSettings;
        }
    }

    void InitializeUI()
    {
        if (mainUIDocument == null)
        {
            Debug.LogError("[UIManager] mainUIDocument es null, no se puede inicializar UI!");
            return;
        }

        root = mainUIDocument.rootVisualElement;

        if (root == null)
        {
            Debug.LogError("[UIManager] rootVisualElement es null!");
            return;
        }

        root.Clear();

        if (mainStyleSheet != null) root.styleSheets.Add(mainStyleSheet);
        if (pixelArtStyle != null) root.styleSheets.Add(pixelArtStyle);

        CreateBaseStructure();
        InitializeControllers();

        isInitialized = true;
        OnUIInitialized?.Invoke();
        DebugLog("UI System inicializado correctamente");
    }

    void CreateBaseStructure()
    {
        hudContainer = new VisualElement { name = "hud-container" };
        hudContainer.AddToClassList("ui-container");
        root.Add(hudContainer);

        menuContainer = new VisualElement { name = "menu-container" };
        menuContainer.AddToClassList("ui-container");
        menuContainer.style.display = DisplayStyle.None;
        root.Add(menuContainer);

        overlayContainer = new VisualElement { name = "overlay-container" };
        overlayContainer.AddToClassList("ui-container");
        root.Add(overlayContainer);

        DebugLog("Estructura base de UI creada");
    }

    void InitializeControllers()
    {
        if (hudTemplate != null)
        {
            var hudController = new HUDController();
            RegisterController("HUD", hudController);
            hudController.Initialize(hudContainer, hudTemplate);
            hudControllerReady = true;
            DebugLog("HUDController inicializado y registrado");
        }
        else
        {
            Debug.LogError("[UIManager] hudTemplate es null, no se puede crear HUDController!");
        }
    }

    void SubscribeToGameEvents()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            DebugLog("Suscrito a eventos de GameManager");
        }

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnLevelReady += HandleLevelReady;
            LevelManager.Instance.OnPlayerSpawned += HandlePlayerSpawned;
            DebugLog("Suscrito a eventos de LevelManager");
        }
        else
        {
            Debug.LogWarning("[UIManager] LevelManager.Instance es null en SubscribeToGameEvents");
        }
    }

    void CheckForExistingPlayer()
    {
        GameObject player = null;

        // Primero intentar obtener del LevelManager
        if (LevelManager.Instance != null && LevelManager.Instance.SpawnedPlayer != null)
        {
            player = LevelManager.Instance.SpawnedPlayer;
            DebugLog("Jugador ya existe en LevelManager, conectando...");
        }
        // Fallback: buscar por tag
        else
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                DebugLog("Jugador encontrado por tag, conectando...");
            }
        }

        if (player != null)
        {
            HandlePlayerSpawned();
        }
    }

    public void RegisterController(string name, IUIController controller)
    {
        if (uiControllers.ContainsKey(name))
        {
            Debug.LogWarning($"[UIManager] Controller {name} ya está registrado!");
            return;
        }
        uiControllers[name] = controller;
        DebugLog($"Controller {name} registrado");
    }

    public T GetController<T>(string name) where T : class, IUIController
    {
        if (uiControllers.TryGetValue(name, out var controller))
        {
            return controller as T;
        }

        Debug.LogWarning($"[UIManager] No se encontró controller con nombre: {name}");
        return null;
    }

    public void ShowUI(string containerName)
    {
        var container = GetContainer(containerName);
        if (container != null) container.style.display = DisplayStyle.Flex;
    }

    public void HideUI(string containerName)
    {
        var container = GetContainer(containerName);
        if (container != null) container.style.display = DisplayStyle.None;
    }

    public void ShowNotification(string message, float duration = 2f)
    {
        DebugLog($"Notificación: {message}");
    }

    /// <summary>
    /// Conecta o reconecta al jugador con el HUD.
    /// </summary>
    public void UpdatePlayerHUD(PlayerStats stats)
    {
        var hudController = GetController<HUDController>("HUD");
        if (hudController != null && stats != null)
        {
            var playerController = stats.GetComponent<PlayerController>();
            hudController.ConnectToPlayer(stats, playerController);
            DebugLog("UpdatePlayerHUD ejecutado exitosamente");
        }
        else
        {
            if (hudController == null)
                Debug.LogError("[UIManager] UpdatePlayerHUD: hudController es null!");
            if (stats == null)
                Debug.LogError("[UIManager] UpdatePlayerHUD: stats es null!");
        }
    }

    void HandleGameStateChanged(GameManager.GameState newState)
    {
        currentGameState = (GameState)newState;
        DebugLog($"Estado del juego cambiado a: {newState}");
    }

    void HandleLevelReady()
    {
        DebugLog("Nivel listo, refrescando UI...");
        var hudController = GetController<HUDController>("HUD");
        hudController?.RefreshUI();
    }

    void HandlePlayerSpawned()
    {
        // Si el HUD no está listo todavía, marcar para conexión posterior
        if (!hudControllerReady)
        {
            DebugLog("HUDController no está listo, marcando conexión pendiente...");
            playerPendingConnection = true;
            return;
        }

        DebugLog("HandlePlayerSpawned iniciado");
        StopAllCoroutines();
        StartCoroutine(ConnectPlayerToHUD());
    }

    System.Collections.IEnumerator ConnectPlayerToHUD()
    {
        // Esperar un frame para asegurar que todo esté inicializado
        yield return new WaitForEndOfFrame();

        DebugLog("ConnectPlayerToHUD: Iniciando conexión...");

        GameObject player = null;

        // Primero intentar obtener del LevelManager
        if (LevelManager.Instance != null && LevelManager.Instance.SpawnedPlayer != null)
        {
            player = LevelManager.Instance.SpawnedPlayer;
            DebugLog("ConnectPlayerToHUD: Jugador obtenido del LevelManager");
        }
        // Fallback: buscar por tag
        else
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                DebugLog("ConnectPlayerToHUD: Jugador encontrado por tag");
            }
        }

        if (player == null)
        {
            Debug.LogError("[UIManager] ConnectPlayerToHUD: No se pudo encontrar el GameObject del jugador!");
            yield break;
        }

        // Obtener componentes del jugador
        var stats = player.GetComponent<PlayerStats>();
        var controller = player.GetComponent<PlayerController>();

        // Validar componentes
        if (stats == null)
        {
            Debug.LogError("[UIManager] El jugador no tiene componente PlayerStats!");
            yield break;
        }
        if (controller == null)
        {
            Debug.LogError("[UIManager] El jugador no tiene componente PlayerController!");
            yield break;
        }

        // Obtener HUDController
        var hudController = GetController<HUDController>("HUD");
        if (hudController == null)
        {
            Debug.LogError("[UIManager] No se encontró el HUDController!");

            // Diagnóstico adicional
            DebugLog($"hudControllerReady: {hudControllerReady}");
            DebugLog($"isInitialized: {isInitialized}");
            DebugLog($"Controladores registrados: {uiControllers.Count}");
            foreach (var kvp in uiControllers)
            {
                DebugLog($"  - {kvp.Key}: {kvp.Value?.GetType().Name ?? "null"}");
            }

            yield break;
        }

        // Conectar el HUD principal
        hudController.ConnectToPlayer(stats, controller);
        DebugLog("HUD conectado al jugador exitosamente");

        // Conectar el ComboManager
        if (ComboManager.Instance != null)
        {
            ComboManager.Instance.ConnectToPlayer(stats);
            ComboManager.Instance.ConnectToHUD(hudController);
            DebugLog("ComboManager conectado");
        }

        // Conectar el KillStreakManager al HUD
        if (KillStreakManager.Instance != null)
        {
            hudController.ConnectToKillStreakManager(KillStreakManager.Instance);
            DebugLog("KillStreakManager conectado");
        }

        // Conectar el OverDriveManager al HUD
        if (OverDriveManager.Instance != null)
        {
            hudController.ConnectToOverDriveManager(OverDriveManager.Instance);
            DebugLog("OverDriveManager conectado");
        }

        // Refrescar UI
        hudController.RefreshUI();
        DebugLog("UI refrescada - Conexión completa!");
    }

    VisualElement GetContainer(string name)
    {
        return name switch
        {
            "hud-container" => hudContainer,
            "menu-container" => menuContainer,
            "overlay-container" => overlayContainer,
            _ => root?.Q<VisualElement>(name)
        };
    }

    void DebugLog(string message)
    {
        if (enableDebugMode)
        {
            Debug.Log($"[UIManager] {message}");
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        }
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnLevelReady -= HandleLevelReady;
            LevelManager.Instance.OnPlayerSpawned -= HandlePlayerSpawned;
        }
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // Método público para forzar reconexión manual
    [ContextMenu("Force Connect Player to HUD")]
    public void ForceConnectPlayerToHUD()
    {
        DebugLog("=== FORZANDO CONEXIÓN MANUAL ===");

        // Diagnóstico completo
        DebugLog($"hudControllerReady: {hudControllerReady}");
        DebugLog($"isInitialized: {isInitialized}");
        DebugLog($"Controladores registrados: {uiControllers.Count}");

        if (!hudControllerReady)
        {
            DebugLog("Intentando inicializar HUDController manualmente...");
            InitializeControllers();
        }

        CheckForExistingPlayer();
    }

    [ContextMenu("Debug UI State")]
    public void DebugUIState()
    {
        Debug.Log("=== DEBUG UI STATE ===");
        Debug.Log($"UIManager inicializado: {isInitialized}");
        Debug.Log($"HUDController listo: {hudControllerReady}");
        Debug.Log($"Controladores registrados: {uiControllers.Count}");
        foreach (var kvp in uiControllers)
        {
            Debug.Log($"  - {kvp.Key}: {kvp.Value?.GetType().Name ?? "null"}");
        }
        Debug.Log($"hudContainer: {hudContainer != null}");
        Debug.Log($"hudTemplate asignado: {hudTemplate != null}");
        Debug.Log($"mainUIDocument: {mainUIDocument != null}");
        Debug.Log($"root: {root != null}");
    }

    public enum GameState
    {
        Initializing,
        Playing,
        Paused,
        GameOver
    }
}

/// <summary>
/// Interfaz base para todos los controladores de UI
/// </summary>
public interface IUIController
{
    void Initialize(VisualElement container, VisualTreeAsset template);
    void Show();
    void Hide();
    void RefreshUI();
    void Cleanup();
}
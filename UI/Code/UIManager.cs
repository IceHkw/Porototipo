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
    private GameState currentGameState = GameState.Playing;

    // Eventos
    public event Action<GameState> OnGameStateUIChanged;
    public event Action OnUIInitialized;

    void Awake()
    {
        SetupSingleton();
        InitializeUIDocument();
    }

    void Start()
    {
        InitializeUI();
        SubscribeToGameEvents();
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
        if (mainUIDocument == null) return;
        root = mainUIDocument.rootVisualElement;
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
    }

    void InitializeControllers()
    {
        if (hudTemplate != null)
        {
            var hudController = new HUDController();
            RegisterController("HUD", hudController);
            hudController.Initialize(hudContainer, hudTemplate);
        }
    }

    void SubscribeToGameEvents()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        }
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnLevelReady += HandleLevelReady;
            LevelManager.Instance.OnPlayerSpawned += HandlePlayerSpawned;
        }
    }

    public void RegisterController(string name, IUIController controller)
    {
        if (uiControllers.ContainsKey(name))
        {
            Debug.LogWarning($"Controller {name} ya está registrado!");
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

    // =======================================================
    // ===== INICIO DE SECCIÓN CORREGIDA =====
    // =======================================================

    /// <summary>
    /// Conecta o reconecta al jugador con el HUD.
    /// Este método ahora usa la interfaz correcta del HUDController.
    /// </summary>
    public void UpdatePlayerHUD(PlayerStats stats)
    {
        var hudController = GetController<HUDController>("HUD");
        if (hudController != null && stats != null)
        {
            // Se obtiene el PlayerController desde el componente de stats.
            var playerController = stats.GetComponent<PlayerController>();
            // Se llama al método correcto que sí existe en HUDController.
            hudController.ConnectToPlayer(stats, playerController);
        }
    }

    // =======================================================
    // ===== FIN DE SECCIÓN CORREGIDA =====
    // =======================================================

    void HandleGameStateChanged(GameManager.GameState newState)
    {
        currentGameState = (GameState)newState;
        // Lógica de cambio de estado de la UI...
    }

    void HandleLevelReady()
    {
        var hudController = GetController<HUDController>("HUD");
        hudController?.RefreshUI();
    }

    void HandlePlayerSpawned()
    {
        StartCoroutine(ConnectPlayerToHUD());
    }

    System.Collections.IEnumerator ConnectPlayerToHUD()
    {
        yield return new WaitForSeconds(0.1f);

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var stats = player.GetComponent<PlayerStats>();
            var controller = player.GetComponent<PlayerController>();
            var hudController = GetController<HUDController>("HUD");

            // Conectar el HUD principal
            hudController?.ConnectToPlayer(stats, controller);

            // Conectar el ComboManager
            if (ComboManager.Instance != null)
            {
                ComboManager.Instance.ConnectToPlayer(stats);
                ComboManager.Instance.ConnectToHUD(hudController);
            }

            // Conectar el KillStreakManager al HUD
            if (KillStreakManager.Instance != null && hudController != null)
            {
                // Asumiendo que HUDController tiene un método para esto.
                // Si no, necesitarías añadir: public void ConnectToKillStreakManager(KillStreakManager manager)
                hudController.ConnectToKillStreakManager(KillStreakManager.Instance);
            }

            hudController?.RefreshUI();
        }
    }

    VisualElement GetContainer(string name)
    {
        return name switch
        {
            "hud-container" => hudContainer,
            "menu-container" => menuContainer,
            "overlay-container" => overlayContainer,
            _ => root.Q<VisualElement>(name)
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
        if (Instance == this)
        {
            Instance = null;
        }
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
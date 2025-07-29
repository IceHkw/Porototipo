using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("Estados del Juego")]
    public GameState estadoActual = GameState.Initializing;

    [Header("UI - Pantalla Game Over")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverText;
    public TextMeshProUGUI restartText;

    [Header("UI - Pantalla de Carga (Opcional)")]
    public GameObject loadingPanel;
    public TextMeshProUGUI loadingText;

    [Header("Referencias del Jugador")]
    public PlayerStats playerStats;
    public PlayerMovement playerMovement;

    [Header("Referencias del Nivel")]
    public LevelManager levelManager;

    [Header("Configuración")]
    public KeyCode teclaReinicio = KeyCode.R;
    public float tiempoMinimoGameOver = 1f;

    // Variables privadas
    private float tiempoInicioGameOver;
    private bool puedeReiniciar = false;
    private bool nivelListo = false;

    // Singleton pattern
    public static GameManager Instance { get; private set; }

    // Eventos públicos
    public System.Action<GameState> OnGameStateChanged;
    public System.Action OnGameOver;
    public System.Action OnGameRestart;
    public System.Action OnLevelReady; // NUEVO

    public enum GameState
    {
        Initializing,    // NUEVO: Estado mientras se inicializa el nivel
        Playing,
        GameOver,
        Paused
    }

    void Awake()
    {
        // Configurar Singleton simple
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InicializarComponentes();
    }

    void Start()
    {
        // Esperar a que UI esté lista
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OnUIInitialized += OnUIReady;
        }
        // MODIFICADO: Empezar en estado Initializing
        CambiarEstado(GameState.Initializing);
        SuscribirseAEventosDeNivel();
    }

    void Update()
    {
        switch (estadoActual)
        {
            case GameState.Initializing:
                ActualizarInicializacion();
                break;

            case GameState.Playing:
                ActualizarJuego();
                break;

            case GameState.GameOver:
                ActualizarGameOver();
                break;

            case GameState.Paused:
                ActualizarPausa();
                break;
        }
    }

    void InicializarComponentes()
    {
        // Buscar LevelManager si no está asignado
        if (levelManager == null)
            levelManager = FindFirstObjectByType<LevelManager>();

        // Buscar componentes del jugador (serán asignados después del spawn)
        BuscarComponentesDelJugador();

        // Buscar UI de Game Over
        BuscarGameOverUI();

        // Asegurar que los paneles estén en el estado correcto al inicio
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        // NUEVO: Configurar panel de carga
        ConfigurarPanelDeCarga();
    }

    void OnUIReady()
    {
        Debug.Log("UI System listo!");
    }
    void ConfigurarPanelDeCarga()
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
            if (loadingText != null)
                loadingText.text = "Generando mundo...";
        }
    }

    void SuscribirseAEventosDeNivel()
    {
        if (levelManager != null)
        {
            levelManager.OnLevelReady += OnNivelListo;
            levelManager.OnPlayerSpawned += OnJugadorSpawneado;
        }
    }

    void DesuscribirseDeEventosDeNivel()
    {
        if (levelManager != null)
        {
            levelManager.OnLevelReady -= OnNivelListo;
            levelManager.OnPlayerSpawned -= OnJugadorSpawneado;
        }
    }

    // NUEVO: Estado de inicialización
    void ActualizarInicializacion()
    {
        // Mostrar información de carga si es necesario
        if (loadingText != null && levelManager != null)
        {
            if (levelManager.IsInitializing)
            {
                loadingText.text = "Generando mundo...";
            }
        }

        // No permitir acciones del jugador durante la inicialización
    }

    void ActualizarJuego()
    {
        if (!nivelListo) return; // No procesar si el nivel no está listo

        if (playerStats != null && !playerStats.EstaVivo)
        {
            TriggerGameOver();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            PausarJuego();
        }
    }

    void ActualizarGameOver()
    {
        if (!puedeReiniciar && Time.time - tiempoInicioGameOver >= tiempoMinimoGameOver)
        {
            puedeReiniciar = true;
            MostrarTextoReinicio();
        }

        if (puedeReiniciar && Input.GetKeyDown(teclaReinicio))
        {
            ReiniciarJuego();
        }
    }

    void ActualizarPausa()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ReanudarJuego();
        }
    }

    public void CambiarEstado(GameState nuevoEstado)
    {
        if (estadoActual == nuevoEstado) return;

        GameState estadoAnterior = estadoActual;
        estadoActual = nuevoEstado;

        switch (nuevoEstado)
        {
            case GameState.Initializing:
                ConfigurarEstadoInicializacion();
                break;

            case GameState.Playing:
                ConfigurarEstadoJuego();
                break;

            case GameState.GameOver:
                ConfigurarEstadoGameOver();
                break;

            case GameState.Paused:
                ConfigurarEstadoPausa();
                break;
        }

        OnGameStateChanged?.Invoke(nuevoEstado);
        Debug.Log($"Estado cambiado: {estadoAnterior} → {nuevoEstado}");
    }

    // NUEVO: Configuración del estado de inicialización
    void ConfigurarEstadoInicializacion()
    {
        Time.timeScale = 1f;

        // Ocultar paneles del juego
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        // Mostrar panel de carga si existe
        if (loadingPanel != null)
            loadingPanel.SetActive(true);

        // Deshabilitar movimiento del jugador si existe
        if (playerMovement != null)
            playerMovement.enabled = false;

        nivelListo = false;
    }

    void ConfigurarEstadoJuego()
    {
        Time.timeScale = 1f;

        // Ocultar paneles
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (loadingPanel != null)
            loadingPanel.SetActive(false);

        // Habilitar movimiento del jugador
        if (playerMovement != null)
            playerMovement.enabled = true;
    }

    void ConfigurarEstadoGameOver()
    {
        Time.timeScale = 1f;
        tiempoInicioGameOver = Time.time;
        puedeReiniciar = false;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        else
        {
            // Reintentar buscar UI si no se encontró antes
            BuscarGameOverUI();
            if (gameOverPanel != null)
                gameOverPanel.SetActive(true);
        }

        if (gameOverText != null)
            gameOverText.text = "GAME OVER";

        if (restartText != null)
            restartText.gameObject.SetActive(false);

        if (playerMovement != null)
            playerMovement.enabled = false;

        OnGameOver?.Invoke();
    }

    void ConfigurarEstadoPausa()
    {
        Time.timeScale = 0f;
    }

    // NUEVO: Callbacks de eventos del LevelManager
    void OnNivelListo()
    {
        nivelListo = true;
        Debug.Log("GameManager: Nivel listo, cambiando a estado Playing");

        // Buscar componentes del jugador recién spawneado
        BuscarComponentesDelJugador();

        CambiarEstado(GameState.Playing);
        OnLevelReady?.Invoke();
    }

    void OnJugadorSpawneado()
    {
        Debug.Log("GameManager: Jugador spawneado, buscando componentes...");
        BuscarComponentesDelJugador();
    }

    void BuscarComponentesDelJugador()
    {
        // Buscar componentes del jugador spawneado por el LevelManager
        if (levelManager != null && levelManager.SpawnedPlayer != null)
        {
            GameObject player = levelManager.SpawnedPlayer;

            if (playerStats == null)
                playerStats = player.GetComponent<PlayerStats>();

            if (playerMovement == null)
                playerMovement = player.GetComponent<PlayerMovement>();
        }
        else
        {
            // Buscar por los métodos tradicionales si no hay LevelManager
            if (playerStats == null)
                playerStats = FindFirstObjectByType<PlayerStats>();

            if (playerMovement == null)
                playerMovement = FindFirstObjectByType<PlayerMovement>();
        }
    }

    void BuscarGameOverUI()
    {
        // Buscar panel por nombre en toda la jerarquía
        if (gameOverPanel == null)
        {
            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (GameObject obj in allObjects)
            {
                if (obj.name == "GameOverPanel")
                {
                    gameOverPanel = obj;
                    break;
                }
            }
        }

        // Buscar textos dentro del panel
        if (gameOverPanel != null)
        {
            TextMeshProUGUI[] textos = gameOverPanel.GetComponentsInChildren<TextMeshProUGUI>(true);

            foreach (TextMeshProUGUI texto in textos)
            {
                string contenido = texto.text.ToLower();
                string nombre = texto.name.ToLower();

                if (gameOverText == null && (contenido.Contains("game over") || nombre.Contains("gameover")))
                {
                    gameOverText = texto;
                }
                else if (restartText == null && (contenido.Contains("presiona") || contenido.Contains("restart") || nombre.Contains("restart")))
                {
                    restartText = texto;
                }
            }
        }
    }

    void MostrarTextoReinicio()
    {
        if (restartText != null)
        {
            restartText.gameObject.SetActive(true);
            restartText.text = $"Presiona {teclaReinicio} para reiniciar";
        }
    }

    public void TriggerGameOver()
    {
        if (estadoActual != GameState.GameOver)
        {
            CambiarEstado(GameState.GameOver);
        }
    }

    public void ReiniciarJuego()
    {
        OnGameRestart?.Invoke();

        // Reiniciar sistema de combos
        if (ComboManager.Instance != null)
        {
            ComboManager.Instance.ResetComboSystem();
        }

        // Reiniciar sistema de KillStreaks
        if (KillStreakManager.Instance != null)
        {
            KillStreakManager.Instance.ResetSystem();
        }

        // MODIFICADO: Usar LevelManager para reiniciar si está disponible
        if (levelManager != null)
        {
            CambiarEstado(GameState.Initializing);
            levelManager.RestartLevel();
        }
        else
        {
            // Fallback: recargar escena completa
            int escenaActual = SceneManager.GetActiveScene().buildIndex;
            SceneManager.LoadScene(escenaActual);
        }
    }

    public void PausarJuego()
    {
        if (estadoActual == GameState.Playing)
        {
            CambiarEstado(GameState.Paused);
        }
    }

    public void ReanudarJuego()
    {
        if (estadoActual == GameState.Paused)
        {
            Time.timeScale = 1f;
            CambiarEstado(GameState.Playing);
        }
    }

    void OnDestroy()
    {
        DesuscribirseDeEventosDeNivel();

        if (Instance == this)
        {
            Instance = null;
        }

        Time.timeScale = 1f;
    }

    // Propiedades públicas esenciales
    public bool EstaInicializando => estadoActual == GameState.Initializing;
    public bool EstaJugando => estadoActual == GameState.Playing;
    public bool EstaEnGameOver => estadoActual == GameState.GameOver;
    public bool EstaPausado => estadoActual == GameState.Paused;
    public bool NivelListo => nivelListo;
}
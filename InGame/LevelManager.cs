// ====================================
// LevelManager.cs
// Manager de nivel simplificado con pool genérico
// ====================================

using UnityEngine;
using System.Collections;

/// <summary>
/// Controla la secuencia de inicialización del nivel
/// </summary>
public class LevelManager : MonoBehaviour
{
    [Header("═══════════════════════════════════════")]
    [Header("REFERENCIAS REQUERIDAS")]
    [Header("═══════════════════════════════════════")]

    [Header("Core References")]
    [Tooltip("Generador de terreno que coordinará")]
    public BiomeTerrainGenerator terrainGenerator;

    [Tooltip("Prefab del jugador")]
    public GameObject playerPrefab;

    [Tooltip("Prefab del enemy spawner")]
    public GameObject enemySpawnerPrefab;

    [Header("Camera Integration")]
    [Tooltip("Referencia opcional a la cámara que sigue al jugador")]
    public CMFollowPlayer cameraFollow;

    [Header("═══════════════════════════════════════")]
    [Header("POOLING INTEGRATION")]
    [Header("═══════════════════════════════════════")]

    [Header("Pool Manager")]
    [Tooltip("Object Pool Manager (se busca automáticamente si está vacío)")]
    public ObjectPoolManager poolManager;

    [Tooltip("Esperar a que los pools estén listos antes de continuar")]
    public bool waitForPoolInitialization = true;

    [Tooltip("Timeout máximo para esperar pools (en segundos)")]
    public float poolInitializationTimeout = 10f;

    [Header("═══════════════════════════════════════")]
    [Header("CONFIGURACIÓN DE INICIALIZACIÓN")]
    [Header("═══════════════════════════════════════")]

    [Header("Timing Configuration")]
    [Tooltip("Tiempo de espera después de generar terreno (en segundos)")]
    public float delayAfterTerrain = 0.5f;

    [Tooltip("Tiempo de espera después de inicializar pools")]
    public float delayAfterPools = 0.3f;

    [Tooltip("Tiempo entre spawns (para efectos visuales)")]
    public float delayBetweenSpawns = 0.2f;

    [Header("Cleanup Configuration")]
    [Tooltip("¿Destruir objetos existentes antes de generar nuevos?")]
    public bool clearExistingObjects = true;

    [Tooltip("¿Devolver objetos a pools al reiniciar nivel?")]
    public bool returnToPoolsOnRestart = true;

    [Header("═══════════════════════════════════════")]
    [Header("DEBUG Y VALIDACIÓN")]
    [Header("═══════════════════════════════════════")]

    [Header("Debug")]
    [Tooltip("Mostrar información de debug en consola")]
    public bool enableDebugLogs = true;

    // Referencias a objetos spawneados
    private GameObject spawnedPlayer;
    private GameObject[] spawnedEnemySpawners;

    // Estado del LevelManager
    private bool isInitializing = false;
    private bool levelReady = false;
    private bool poolsReady = false;

    // Pool initialization tracking
    private float poolInitializationStartTime;
    private bool poolTimeoutOccurred = false;

    // Eventos públicos
    public System.Action OnLevelInitializationStart;
    public System.Action OnPoolInitializationStart;
    public System.Action OnPoolInitializationComplete;
    public System.Action OnPlayerSpawned;
    public System.Action OnEnemySpawnersCreated;
    public System.Action OnLevelReady;

    // Singleton simple (opcional)
    public static LevelManager Instance { get; private set; }

    void Awake()
    {
        // Configurar singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Ya existe un LevelManager en la escena!");
            Destroy(gameObject);
            return;
        }

        ValidateAndFindComponents();
    }

    void Start()
    {
        InitializeLevel();
    }

    void ValidateAndFindComponents()
    {
        // Buscar BiomeTerrainGenerator
        if (terrainGenerator == null)
        {
            terrainGenerator = FindFirstObjectByType<BiomeTerrainGenerator>();
            if (terrainGenerator == null)
            {
                Debug.LogError("LevelManager: No se encontró BiomeTerrainGenerator!");
                return;
            }
        }

        // Buscar ObjectPoolManager
        if (poolManager == null)
        {
            poolManager = FindFirstObjectByType<ObjectPoolManager>();

            if (poolManager == null && waitForPoolInitialization)
            {
                Debug.LogWarning("LevelManager: No se encontró ObjectPoolManager pero waitForPoolInitialization está habilitado!");
            }
        }

        // Buscar CMFollowPlayer
        if (cameraFollow == null)
        {
            cameraFollow = FindFirstObjectByType<CMFollowPlayer>();

            if (cameraFollow != null && enableDebugLogs)
            {
                DebugLog("CMFollowPlayer encontrado automáticamente");
            }
        }

        // Validar prefabs
        if (playerPrefab == null)
        {
            Debug.LogError("LevelManager: PlayerPrefab no está asignado!");
        }

        if (enemySpawnerPrefab == null)
        {
            Debug.LogError("LevelManager: EnemySpawnerPrefab no está asignado!");
        }

        DebugLog($"Componentes validados - Pool Manager: {(poolManager != null ? "✓" : "✗")}, Camera: {(cameraFollow != null ? "✓" : "✗")}");
    }

    /// <summary>
    /// Inicia la secuencia completa de inicialización del nivel
    /// </summary>
    public void InitializeLevel()
    {
        if (isInitializing)
        {
            DebugLog("Ya se está inicializando el nivel...");
            return;
        }

        StartCoroutine(InitializationSequence());
    }

    IEnumerator InitializationSequence()
    {
        isInitializing = true;
        levelReady = false;
        poolsReady = false;
        poolTimeoutOccurred = false;

        DebugLog("=== INICIANDO SECUENCIA DE NIVEL ===");

        // Notificar inicio
        OnLevelInitializationStart?.Invoke();

        // 1. Limpiar objetos existentes si es necesario
        if (clearExistingObjects)
        {
            ClearExistingGameObjects();
            yield return new WaitForSeconds(0.1f);
        }

        // 2. Inicializar Object Pools si está habilitado
        if (waitForPoolInitialization && poolManager != null)
        {
            yield return StartCoroutine(WaitForPoolManager());
        }
        else
        {
            poolsReady = true;
            DebugLog("Inicialización de pools saltada o no disponible");
        }

        // 3. Suscribirse a eventos del generador de terreno
        SubscribeToTerrainEvents();

        // 4. Esperar a que el terreno esté listo
        DebugLog("Esperando generación de terreno...");
        yield return new WaitUntil(() => terrainGenerator.IsTerrainReady);

        // 5. Espera adicional para asegurar que todo esté estable
        yield return new WaitForSeconds(delayAfterTerrain);

        // 6. Spawner objetos del juego
        yield return StartCoroutine(SpawnGameObjects());

        // 7. Configuración final
        FinalizeLevel();

        isInitializing = false;
        levelReady = true;

        DebugLog("=== NIVEL LISTO PARA JUGAR ===");
        OnLevelReady?.Invoke();
    }

    IEnumerator WaitForPoolManager()
    {
        DebugLog("=== ESPERANDO POOL MANAGER ===");
        OnPoolInitializationStart?.Invoke();

        poolInitializationStartTime = Time.time;

        // Esperar a que el ObjectPoolManager esté disponible y listo
        while (poolManager == null || !poolManager.IsInitialized)
        {
            // Verificar timeout
            if (Time.time - poolInitializationStartTime > poolInitializationTimeout)
            {
                Debug.LogError($"LevelManager: Timeout esperando inicialización de pools ({poolInitializationTimeout}s)");
                poolTimeoutOccurred = true;
                break;
            }

            // Buscar pool manager si no está asignado
            if (poolManager == null)
            {
                poolManager = FindFirstObjectByType<ObjectPoolManager>();
            }

            float elapsed = Time.time - poolInitializationStartTime;
            DebugLog($"Esperando ObjectPoolManager... ({elapsed:F1}s)");

            yield return new WaitForSeconds(0.2f);
        }

        // Si no hubo timeout, los pools están listos
        if (!poolTimeoutOccurred)
        {
            poolsReady = true;
            DebugLog("=== POOL MANAGER LISTO ===");
        }
        else
        {
            poolsReady = false;
            Debug.LogWarning("LevelManager: Continuando sin pools debido a timeout");
        }

        OnPoolInitializationComplete?.Invoke();

        // Espera adicional después de pools
        yield return new WaitForSeconds(delayAfterPools);
    }

    void SubscribeToTerrainEvents()
    {
        if (terrainGenerator != null)
        {
            terrainGenerator.OnPlayerSpawnPointReady += OnPlayerSpawnPointReceived;
            terrainGenerator.OnEnemySpawnPointsReady += OnEnemySpawnPointsReceived;
        }
    }

    void UnsubscribeFromTerrainEvents()
    {
        if (terrainGenerator != null)
        {
            terrainGenerator.OnPlayerSpawnPointReady -= OnPlayerSpawnPointReceived;
            terrainGenerator.OnEnemySpawnPointsReady -= OnEnemySpawnPointsReceived;
        }
    }

    IEnumerator SpawnGameObjects()
    {
        DebugLog("Comenzando spawn de objetos del juego...");

        // El terreno ya está listo, así que podemos usar sus spawn points calculados
        yield return StartCoroutine(SpawnPlayer());
        yield return new WaitForSeconds(delayBetweenSpawns);

        yield return StartCoroutine(SpawnEnemySpawners());
        yield return new WaitForSeconds(delayBetweenSpawns);
    }

    IEnumerator SpawnPlayer()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("No se puede spawner player: PlayerPrefab es null!");
            yield break;
        }

        Vector3 spawnPoint = terrainGenerator.PlayerSpawnPoint;

        DebugLog($"Spawneando player en: {spawnPoint}");

        spawnedPlayer = Instantiate(playerPrefab, spawnPoint, Quaternion.identity);
        spawnedPlayer.name = "Player";

        // Asegurar que tenga el tag correcto
        if (!spawnedPlayer.CompareTag("Player"))
        {
            spawnedPlayer.tag = "Player";
            DebugLog("Tag 'Player' asignado al jugador spawneado");
        }

        OnPlayerSpawned?.Invoke();
        yield return null;
    }

    IEnumerator SpawnEnemySpawners()
    {
        if (enemySpawnerPrefab == null)
        {
            Debug.LogError("No se puede spawner enemy spawners: EnemySpawnerPrefab es null!");
            yield break;
        }

        // Definir worldWidth si no está definido previamente
        float worldWidth = terrainGenerator != null ? terrainGenerator.worldWidth : 100f;

        // Crear un único spawner dinámico en el centro del mundo
        Vector3 spawnerPosition = new Vector3(worldWidth / 2f, 0f, 0f);

        DebugLog($"Spawneando enemy spawner dinámico en: {spawnerPosition}");

        GameObject spawner = Instantiate(enemySpawnerPrefab, spawnerPosition, Quaternion.identity);
        spawner.name = "DynamicEnemySpawner";

        spawnedEnemySpawners = new GameObject[] { spawner };

        OnEnemySpawnersCreated?.Invoke();

        yield return null;
    }

    void ClearExistingGameObjects()
    {
        DebugLog("Limpiando objetos existentes...");

        // Resetear cámara antes de limpiar
        if (cameraFollow != null)
        {
            cameraFollow.ResetCamera();
            DebugLog("Cámara reseteada");
        }

        // Buscar y destruir players existentes
        GameObject[] existingPlayers = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in existingPlayers)
        {
            DestroyImmediate(player);
        }

        // Buscar y destruir enemy spawners existentes
        GameObject[] existingSpawners = GameObject.FindObjectsOfType<GameObject>();
        foreach (GameObject obj in existingSpawners)
        {
            if (obj.name.Contains("EnemySpawner") || obj.name.Contains("Enemy Spawner"))
            {
                DestroyImmediate(obj);
            }
        }

        // Limpiar enemies existentes
        ClearExistingEnemies();
    }

    void ClearExistingEnemies()
    {
        GameObject[] existingEnemies = GameObject.FindGameObjectsWithTag("Enemy");

        if (existingEnemies.Length == 0) return;

        int poolReturned = 0;
        int destroyed = 0;

        foreach (GameObject enemy in existingEnemies)
        {
            // Intentar devolver al pool si está disponible
            if (poolManager != null && poolsReady)
            {
                poolManager.Return(enemy);
                poolReturned++;
            }
            else
            {
                // Destruir normalmente
                DestroyImmediate(enemy);
                destroyed++;
            }
        }

        if (poolReturned > 0 || destroyed > 0)
        {
            DebugLog($"Enemies limpiados: {poolReturned} devueltos al pool, {destroyed} destruidos");
        }
    }

    void FinalizeLevel()
    {
        DebugLog("Finalizando configuración del nivel...");

        // Comunicar al GameManager que el nivel está listo
        if (GameManager.Instance != null)
        {
            // Asegurar que el juego esté en estado Playing
            if (!GameManager.Instance.EstaJugando)
            {
                GameManager.Instance.CambiarEstado(GameManager.GameState.Playing);
            }
        }

        // Configuración final adicional
        ConfigureCameraIfNeeded();

        // Log estadísticas finales
        LogFinalStatistics();
    }

    void ConfigureCameraIfNeeded()
    {
        // Si hay un player spawneado, configurar la cámara para seguirlo
        if (spawnedPlayer != null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                // Configurar seguimiento de cámara si existe el componente
                if (cameraFollow != null)
                {
                    // La cámara debería auto-detectar al player con el tag "Player"
                    DebugLog("Cámara configurada para seguir al jugador");
                }
            }
        }
    }

    void LogFinalStatistics()
    {
        if (!enableDebugLogs) return;

        DebugLog("=== ESTADÍSTICAS FINALES DEL NIVEL ===");
        DebugLog($"Player spawneado: {(spawnedPlayer != null ? "✓" : "✗")}");
        DebugLog($"Enemy spawners: {(spawnedEnemySpawners != null ? spawnedEnemySpawners.Length : 0)}");
        DebugLog($"Pools inicializados: {(poolsReady ? "✓" : "✗")}");
        DebugLog($"Terreno generado: {(terrainGenerator != null && terrainGenerator.IsTerrainReady ? "✓" : "✗")}");
        DebugLog($"Cámara integrada: {(cameraFollow != null ? "✓" : "✗")}");

        if (poolManager != null && poolsReady)
        {
            DebugLog($"Pool Manager disponible y listo");
        }
    }

    // Callbacks de eventos del TerrainGenerator
    void OnPlayerSpawnPointReceived(Vector3 spawnPoint)
    {
        DebugLog($"Spawn point del player recibido: {spawnPoint}");
    }

    void OnEnemySpawnPointsReceived(Vector3[] spawnPoints)
    {
        DebugLog($"Spawn points de enemies recibidos: {spawnPoints.Length} puntos");
    }

    void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[LevelManager] {message}");
        }
    }

    // Métodos públicos para control externo
    public void RestartLevel()
    {
        if (isInitializing)
        {
            DebugLog("No se puede reiniciar: nivel en inicialización");
            return;
        }

        StartCoroutine(RestartSequence());
    }

    IEnumerator RestartSequence()
    {
        DebugLog("Reiniciando nivel...");

        // Limpiar objetos actuales
        ClearExistingGameObjects();

        // Devolver objetos a pools si está habilitado
        if (returnToPoolsOnRestart && poolManager != null)
        {
            poolManager.ReturnAllObjects();
            DebugLog("Todos los objetos devueltos a pools");
        }

        yield return new WaitForSeconds(0.2f);

        // Regenerar terreno si es necesario
        if (terrainGenerator != null && terrainGenerator.currentBiome != null)
        {
            terrainGenerator.ChangeBiome(terrainGenerator.currentBiome);
        }

        // Reinicializar
        InitializeLevel();
    }

    // Método público para integración con cámara
    public void NotifyCameraReset()
    {
        if (cameraFollow != null)
        {
            cameraFollow.ResetCamera();
            DebugLog("Cámara notificada para reset");
        }
    }

    // Métodos públicos para pooling
    public void ForceReturnAllEnemiesToPools()
    {
        if (poolManager != null)
        {
            // Devolver todos los enemies activos con tag "Enemy"
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            int returned = 0;

            foreach (GameObject enemy in enemies)
            {
                poolManager.Return(enemy);
                returned++;
            }

            DebugLog($"Forzado retorno de {returned} enemies a pools");
        }
    }

    // Context Menus para testing
    [ContextMenu("Restart Level")]
    public void TestRestartLevel()
    {
        RestartLevel();
    }

    [ContextMenu("Return All Enemies To Pool")]
    public void TestReturnAllEnemiesToPool()
    {
        ForceReturnAllEnemiesToPools();
    }

    [ContextMenu("Log Final Statistics")]
    public void TestLogFinalStatistics()
    {
        LogFinalStatistics();
    }

    [ContextMenu("Reset Camera")]
    public void TestResetCamera()
    {
        NotifyCameraReset();
    }

    // Propiedades públicas
    public bool IsInitializing => isInitializing;
    public bool IsLevelReady => levelReady;
    public bool ArePoolsReady => poolsReady;
    public bool PoolTimeoutOccurred => poolTimeoutOccurred;
    public GameObject SpawnedPlayer => spawnedPlayer;
    public GameObject[] SpawnedEnemySpawners => spawnedEnemySpawners;
    public ObjectPoolManager PoolManager => poolManager;
    public CMFollowPlayer CameraFollow => cameraFollow;

    void OnDestroy()
    {
        UnsubscribeFromTerrainEvents();

        if (Instance == this)
        {
            Instance = null;
        }
    }
}
using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class EnemySpawner : MonoBehaviour
{
    [Header("═══════════════════════════════════════")]
    [Header("CONFIGURACIÓN DE SPAWN DINÁMICO")]
    [Header("═══════════════════════════════════════")]

    [Header("Camera Reference")]
    [Tooltip("Cámara principal para calcular área de spawn")]
    public Camera mainCamera;

    [Header("Spawn Ring Configuration")]
    [Tooltip("Distancia mínima desde el borde de la cámara para spawn")]
    public float minSpawnDistanceFromCamera = 2f;

    [Tooltip("Distancia máxima desde el borde de la cámara para spawn")]
    public float maxSpawnDistanceFromCamera = 8f;

    [Tooltip("Altura máxima para buscar superficie sólida")]
    public float maxGroundCheckDistance = 20f;

    [Tooltip("Offset vertical sobre la superficie para spawn")]
    public float spawnHeightOffset = 0.5f;

    [Header("Spawn Behavior")]
    [Tooltip("Intentar spawn en ambos lados del jugador")]
    public bool spawnBothSides = true;

    [Tooltip("Preferir spawn en la dirección de movimiento")]
    public bool preferForwardSpawn = true;

    [Tooltip("Probabilidad de spawn adelante vs atrás (0.5 = igual)")]
    [Range(0f, 1f)]
    public float forwardSpawnBias = 0.7f;

    [Header("Surface Detection")]
    [Tooltip("Capas que cuentan como superficie sólida")]
    public LayerMask groundLayers = -1;

    [Tooltip("Radio para verificar espacio libre al spawnear")]
    public float spawnCheckRadius = 0.5f;

    [Header("═══════════════════════════════════════")]
    [Header("CONFIGURACIÓN GENERAL")]
    [Header("═══════════════════════════════════════")]

    [Header("Spawn Timing")]
    public float spawnInterval = 2f;
    public int maxEnemies = 10;

    [Header("═══════════════════════════════════════")]
    [Header("ENEMY SPAWN CONFIGURATION")]
    [Header("═══════════════════════════════════════")]

    [SerializeField]
    private List<EnemySpawnConfig> enemyConfigs = new List<EnemySpawnConfig>();

    [System.Serializable]
    public class EnemySpawnConfig
    {
        [Header("Enemy Settings")]
        [Tooltip("ID del pool para este enemigo")]
        public string poolID = "";

        [Tooltip("Peso de spawn (mayor = más frecuente)")]
        [Range(0f, 100f)]
        public float spawnWeight = 10f;

        [Tooltip("Habilitar spawn de este tipo")]
        public bool enabled = true;

        [Header("Optional Overrides")]
        [Tooltip("Configuración específica al spawnear (opcional)")]
        public EnemyConfigOverride configOverride;
    }

    [System.Serializable]
    public class EnemyConfigOverride
    {
        public bool overrideStats = false;
        public int health = 3;
        public float moveSpeed = 3.5f;
        public int attackDamage = 1;
        public float attackRange = 1.5f;
        public float detectionRange = 8f;
        public float attackCooldown = 2f;
    }

    [Header("═══════════════════════════════════════")]
    [Header("CONFIGURACIÓN OPCIONAL")]
    [Header("═══════════════════════════════════════")]

    [Header("Startup")]
    public bool spawnOnStart = true;
    public bool waitForLevelReady = true;
    public float delayAfterLevelReady = 1f;

    [Header("Game State")]
    public bool pauseOnGameOver = true;

    [Header("Debug")]
    public bool enableDebugLogs = true;
    public bool showSpawnGizmos = true;

    // Variables privadas
    private float nextSpawnTime;
    private int currentEnemyCount;
    private bool levelReady = false;
    private bool canSpawnEnemies = false;
    private Transform playerTransform;
    private float cameraHalfWidth;
    private float cameraHalfHeight;

    // Referencias del sistema
    private GameManager gameManager;
    private LevelManager levelManager;
    private ObjectPoolManager poolManager;

    // Estadísticas
    private int totalSpawned = 0;
    private int failedSpawnAttempts = 0;
    private Dictionary<string, int> spawnedByType = new Dictionary<string, int>();
    private float totalSpawnWeight = 0f;

    // Cache para rendimiento
    private List<Vector2> validSpawnPoints = new List<Vector2>(10);
    private Collider2D[] overlapResults = new Collider2D[10];

    void Start()
    {
        InitializeSpawner();
    }

    void InitializeSpawner()
    {
        // Buscar referencias del sistema
        gameManager = GameManager.Instance;
        levelManager = LevelManager.Instance;
        poolManager = ObjectPoolManager.Instance;

        // Buscar cámara si no está asignada
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError($"{gameObject.name}: No se encontró cámara principal!");
            }
        }

        // Validar configuración
        ValidateSpawnConfiguration();
        CalculateTotalSpawnWeight();

        // Inicializar estadísticas
        foreach (var config in enemyConfigs)
        {
            if (!string.IsNullOrEmpty(config.poolID))
            {
                spawnedByType[config.poolID] = 0;
            }
        }

        // Suscribirse a eventos del sistema
        SubscribeToSystemEvents();

        // Configurar estado inicial
        SetupInitialState();

        string poolStatus = poolManager != null ? "Pool Manager encontrado" : "Pool Manager no encontrado";
        DebugLog($"EnemySpawner dinámico inicializado | {poolStatus}");
    }

    void Update()
    {
        // Actualizar referencias de cámara
        UpdateCameraInfo();

        // Buscar jugador si no lo tenemos
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }

        // Verificar estado del juego antes de procesar
        if (!ShouldProcessSpawning()) return;

        // Verificar si es tiempo de spawnear un nuevo enemigo
        if (Time.time >= nextSpawnTime && CanSpawn())
        {
            AttemptDynamicSpawn();
            nextSpawnTime = Time.time + spawnInterval;
        }

        // Ya no actualizamos el conteo aquí - se maneja por eventos
    }

    void UpdateCameraInfo()
    {
        if (mainCamera == null) return;

        // Calcular dimensiones de la cámara en unidades del mundo
        cameraHalfHeight = mainCamera.orthographicSize;
        cameraHalfWidth = cameraHalfHeight * mainCamera.aspect;
    }

    void AttemptDynamicSpawn()
    {
        if (playerTransform == null || mainCamera == null) return;

        // Calcular puntos de spawn válidos
        CalculateValidSpawnPoints();

        if (validSpawnPoints.Count == 0)
        {
            DebugLog("No se encontraron puntos de spawn válidos");
            failedSpawnAttempts++;
            return;
        }

        // Seleccionar un punto aleatorio de los válidos
        Vector2 spawnPosition = validSpawnPoints[Random.Range(0, validSpawnPoints.Count)];

        // Seleccionar tipo de enemigo
        EnemySpawnConfig selectedConfig = SelectEnemyType();
        if (selectedConfig == null || string.IsNullOrEmpty(selectedConfig.poolID))
        {
            DebugLog("No se pudo seleccionar tipo de enemigo válido");
            failedSpawnAttempts++;
            return;
        }

        // Intentar spawnear desde pool
        GameObject newEnemy = poolManager.SpawnWithConfig(
            selectedConfig.poolID,
            spawnPosition,
            Quaternion.identity,
            (obj) => ConfigureSpawnedEnemy(obj, selectedConfig)
        );

        if (newEnemy != null)
        {
            currentEnemyCount++;
            totalSpawned++;
            spawnedByType[selectedConfig.poolID]++;

            DebugLog($"Enemigo '{selectedConfig.poolID}' spawneado en: {spawnPosition} " +
                    $"(Total: {currentEnemyCount}/{maxEnemies})");
        }
        else
        {
            failedSpawnAttempts++;
            Debug.LogWarning($"No se pudo spawnear enemigo '{selectedConfig.poolID}' desde pool");
        }
    }

    void ConfigureSpawnedEnemy(GameObject enemy, EnemySpawnConfig config)
    {
        // Asegurar que tenga el tag correcto
        if (!enemy.CompareTag("Enemy"))
        {
            enemy.tag = "Enemy";
        }

    }

    void CalculateValidSpawnPoints()
    {
        validSpawnPoints.Clear();

        Vector3 cameraPos = mainCamera.transform.position;
        Vector3 playerPos = playerTransform.position;

        // Calcular límites del anillo de spawn
        float leftCameraEdge = cameraPos.x - cameraHalfWidth;
        float rightCameraEdge = cameraPos.x + cameraHalfWidth;
        float topCameraEdge = cameraPos.y + cameraHalfHeight;
        float bottomCameraEdge = cameraPos.y - cameraHalfHeight;

        // Determinar dirección del jugador (para bias de spawn)
        float playerVelocityX = 0f;
        Rigidbody2D playerRb = playerTransform.GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            playerVelocityX = playerRb.linearVelocity.x;
        }

        // Probar puntos en ambos lados si está habilitado
        if (spawnBothSides || Mathf.Abs(playerVelocityX) < 0.1f)
        {
            // Lado izquierdo
            TryAddSpawnPointsOnSide(
                leftCameraEdge - maxSpawnDistanceFromCamera,
                leftCameraEdge - minSpawnDistanceFromCamera,
                bottomCameraEdge,
                topCameraEdge,
                playerVelocityX < 0 ? forwardSpawnBias : 1f - forwardSpawnBias
            );

            // Lado derecho
            TryAddSpawnPointsOnSide(
                rightCameraEdge + minSpawnDistanceFromCamera,
                rightCameraEdge + maxSpawnDistanceFromCamera,
                bottomCameraEdge,
                topCameraEdge,
                playerVelocityX > 0 ? forwardSpawnBias : 1f - forwardSpawnBias
            );
        }
        else if (playerVelocityX > 0)
        {
            // Solo spawn adelante (derecha)
            TryAddSpawnPointsOnSide(
                rightCameraEdge + minSpawnDistanceFromCamera,
                rightCameraEdge + maxSpawnDistanceFromCamera,
                bottomCameraEdge,
                topCameraEdge,
                1f
            );
        }
        else
        {
            // Solo spawn adelante (izquierda)
            TryAddSpawnPointsOnSide(
                leftCameraEdge - maxSpawnDistanceFromCamera,
                leftCameraEdge - minSpawnDistanceFromCamera,
                bottomCameraEdge,
                topCameraEdge,
                1f
            );
        }
    }

    void TryAddSpawnPointsOnSide(float xMin, float xMax, float yMin, float yMax, float probability)
    {
        // Saltar este lado si la probabilidad no se cumple
        if (Random.value > probability) return;

        // Intentar varios puntos en este lado
        int attemptsPerSide = 5;
        for (int i = 0; i < attemptsPerSide; i++)
        {
            float x = Random.Range(xMin, xMax);
            float y = Random.Range(yMin, yMax + maxGroundCheckDistance);

            Vector2 testPoint = new Vector2(x, y);
            Vector2? validPoint = FindValidGroundPoint(testPoint);

            if (validPoint.HasValue)
            {
                validSpawnPoints.Add(validPoint.Value);
            }
        }
    }

    Vector2? FindValidGroundPoint(Vector2 startPoint)
    {
        // Raycast hacia abajo para encontrar superficie
        RaycastHit2D hit = Physics2D.Raycast(
            startPoint,
            Vector2.down,
            maxGroundCheckDistance,
            groundLayers
        );

        if (hit.collider != null)
        {
            Vector2 groundPoint = hit.point + Vector2.up * spawnHeightOffset;

            // Verificar que hay espacio libre para spawnear
            int overlaps = Physics2D.OverlapCircleNonAlloc(
                groundPoint,
                spawnCheckRadius,
                overlapResults,
                ~groundLayers // Todo excepto las capas de suelo
            );

            if (overlaps == 0)
            {
                return groundPoint;
            }
        }

        return null;
    }

    void ValidateSpawnConfiguration()
    {
        if (enemyConfigs == null || enemyConfigs.Count == 0)
        {
            Debug.LogError($"{gameObject.name}: No hay configuraciones de enemigos definidas!");
            return;
        }

        bool hasValidType = false;
        foreach (var config in enemyConfigs)
        {
            if (config.enabled && !string.IsNullOrEmpty(config.poolID))
            {
                hasValidType = true;
            }
        }

        if (!hasValidType)
        {
            Debug.LogError($"{gameObject.name}: No hay tipos de enemigos válidos configurados!");
        }

        if (poolManager == null)
        {
            Debug.LogError($"{gameObject.name}: ObjectPoolManager no encontrado!");
        }
    }

    void CalculateTotalSpawnWeight()
    {
        totalSpawnWeight = 0f;
        foreach (var config in enemyConfigs)
        {
            if (config.enabled && config.spawnWeight > 0)
            {
                totalSpawnWeight += config.spawnWeight;
            }
        }
        DebugLog($"Total spawn weight calculado: {totalSpawnWeight}");
    }

    void SubscribeToSystemEvents()
    {
        if (gameManager != null)
        {
            gameManager.OnGameStateChanged += OnGameStateChanged;
            gameManager.OnLevelReady += OnLevelReady;
        }

        if (levelManager != null)
        {
            levelManager.OnLevelReady += OnLevelReady;
        }

        if (poolManager != null)
        {
            poolManager.OnPoolManagerInitialized += OnPoolManagerReady;
        }

        // NUEVO: Suscribirse a eventos de muerte de enemigos
        EnemyEvents.OnEnemyDeath += OnEnemyDeath;
        EnemyEvents.OnEnemyReturnedToPool += OnEnemyReturnedToPool;
    }

    void UnsubscribeFromSystemEvents()
    {
        if (gameManager != null)
        {
            gameManager.OnGameStateChanged -= OnGameStateChanged;
            gameManager.OnLevelReady -= OnLevelReady;
        }

        if (levelManager != null)
        {
            levelManager.OnLevelReady -= OnLevelReady;
        }

        if (poolManager != null)
        {
            poolManager.OnPoolManagerInitialized -= OnPoolManagerReady;
        }

        // NUEVO: Desuscribirse de eventos de enemigos
        EnemyEvents.OnEnemyDeath -= OnEnemyDeath;
        EnemyEvents.OnEnemyReturnedToPool -= OnEnemyReturnedToPool;
    }

    // NUEVO: Manejar muerte de enemigos
    void OnEnemyDeath(EnemyCore enemy)
    {
        if (currentEnemyCount > 0)
        {
            currentEnemyCount--;
            DebugLog($"Enemigo murió. Contador actualizado: {currentEnemyCount}/{maxEnemies}");
        }
    }

    // NUEVO: Manejar retorno al pool
    void OnEnemyReturnedToPool(EnemyCore enemy)
    {
        // Este evento se dispara después de la muerte, así que no necesitamos hacer nada aquí
        // ya que el contador se actualizó en OnEnemyDeath
    }

    void SetupInitialState()
    {
        if (waitForLevelReady)
        {
            canSpawnEnemies = false;
            DebugLog("Esperando a que el nivel esté listo...");
        }
        else
        {
            canSpawnEnemies = spawnOnStart;
            if (canSpawnEnemies)
            {
                nextSpawnTime = Time.time + spawnInterval;
                DebugLog("Spawner activado inmediatamente");
            }
        }
    }

    bool ShouldProcessSpawning()
    {
        if (!canSpawnEnemies) return false;

        if (gameManager != null)
        {
            if (gameManager.EstaInicializando) return false;
            if (pauseOnGameOver && gameManager.EstaEnGameOver) return false;
            if (gameManager.EstaPausado) return false;
        }

        return true;
    }

    bool CanSpawn()
    {
        if (currentEnemyCount >= maxEnemies) return false;
        if (!HasValidSpawnTypes()) return false;
        if (!levelReady && waitForLevelReady) return false;
        if (playerTransform == null) return false;
        if (mainCamera == null) return false;

        return true;
    }

    bool HasValidSpawnTypes()
    {
        // Si el pool manager no está listo, no podemos spawnear.
        if (poolManager == null || !poolManager.IsInitialized)
        {
            return false;
        }

        // Verificamos si existe AL MENOS un tipo de enemigo configurado,
        // habilitado y con un peso de spawn válido.
        foreach (var config in enemyConfigs)
        {
            if (config.enabled && !string.IsNullOrEmpty(config.poolID) && config.spawnWeight > 0)
            {
                // Con que uno sea válido, es suficiente para intentar el spawn.
                // El ObjectPoolManager se encargará de expandir si es necesario.
                return true;
            }
        }

        // Si ningún tipo de enemigo está habilitado o configurado, entonces no se puede spawnear.
        return false;
    }

    EnemySpawnConfig SelectEnemyType()
    {
        // Crear lista de configs válidas
        List<EnemySpawnConfig> validConfigs = new List<EnemySpawnConfig>();

        foreach (var config in enemyConfigs)
        {
            if (config.enabled && !string.IsNullOrEmpty(config.poolID) &&
                config.spawnWeight > 0 && IsSpawnTypeAvailable(config))
            {
                validConfigs.Add(config);
            }
        }

        if (validConfigs.Count == 0) return null;

        // Selección aleatoria basada en peso
        float totalWeight = 0f;
        foreach (var config in validConfigs)
        {
            totalWeight += config.spawnWeight;
        }

        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (var config in validConfigs)
        {
            currentWeight += config.spawnWeight;
            if (randomValue <= currentWeight)
            {
                return config;
            }
        }

        return validConfigs[validConfigs.Count - 1]; // Fallback
    }

    bool IsSpawnTypeAvailable(EnemySpawnConfig config)
    {
        if (string.IsNullOrEmpty(config.poolID)) return false;

        if (poolManager != null && poolManager.IsInitialized)
        {
            // MODIFICADO: Si permite expansión, siempre está disponible
            var poolConfig = poolManager.GetPoolConfiguration(config.poolID);
            if (poolConfig != null && poolConfig.allowExpansion)
            {
                return true;
            }

            // Si no permite expansión, verificar disponibilidad
            return poolManager.GetAvailableCount(config.poolID) > 0;
        }

        return false;
    }

    void UpdateEnemyCount()
    {
        int previousCount = currentEnemyCount;
        currentEnemyCount = GameObject.FindGameObjectsWithTag("Enemy").Length;

        if (Mathf.Abs(currentEnemyCount - previousCount) > 0 && enableDebugLogs)
        {
            DebugLog($"Conteo de enemigos actualizado: {currentEnemyCount}/{maxEnemies}");
        }
    }

    // ===== CALLBACKS DE EVENTOS =====

    void OnGameStateChanged(GameManager.GameState newState)
    {
        switch (newState)
        {
            case GameManager.GameState.Initializing:
                canSpawnEnemies = false;
                DebugLog("Estado: Inicializando - Spawner pausado");
                break;

            case GameManager.GameState.Playing:
                if (levelReady)
                {
                    EnableSpawning();
                }
                break;

            case GameManager.GameState.GameOver:
                if (pauseOnGameOver)
                {
                    canSpawnEnemies = false;
                    DebugLog("Estado: Game Over - Spawner pausado");
                }
                break;
        }
    }

    void OnLevelReady()
    {
        levelReady = true;
        DebugLog("Nivel listo detectado");

        if (spawnOnStart)
        {
            Invoke(nameof(EnableSpawning), delayAfterLevelReady);
        }
    }

    void OnPoolManagerReady()
    {
        DebugLog("ObjectPoolManager listo");
    }

    void EnableSpawning()
    {
        if (!levelReady && waitForLevelReady)
        {
            DebugLog("No se puede activar spawner: nivel no está listo");
            return;
        }

        canSpawnEnemies = true;
        nextSpawnTime = Time.time + spawnInterval;

        // NUEVO: Sincronizar contador inicial
        UpdateEnemyCount();

        DebugLog($"✅ Spawner dinámico activado - Primer spawn en {spawnInterval} segundos");
    }

    // ===== MÉTODOS PÚBLICOS =====

    public void StartSpawning()
    {
        spawnOnStart = true;

        if (levelReady || !waitForLevelReady)
        {
            EnableSpawning();
        }
    }

    public void StopSpawning()
    {
        canSpawnEnemies = false;
        nextSpawnTime = float.MaxValue;
        DebugLog("Spawner detenido manualmente");
    }

    public void SpawnEnemyNow()
    {
        if (CanSpawn() && ShouldProcessSpawning())
        {
            AttemptDynamicSpawn();
            DebugLog("Enemigo spawneado manualmente");
        }
    }

    public void ClearAllEnemies()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        int poolReturned = 0;

        foreach (GameObject enemy in enemies)
        {
            // Intentar devolver al pool
            if (poolManager != null)
            {
                poolManager.Return(enemy);
                poolReturned++;
            }
            else
            {
                // Fallback: destruir si no hay pool manager
                Destroy(enemy);
            }
        }

        currentEnemyCount = 0;
        DebugLog($"Todos los enemigos limpiados: {poolReturned} devueltos al pool");
    }

    void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[DynamicSpawner-{gameObject.name}] {message}");
        }
    }

    // ===== DEBUG VISUAL =====

    void OnDrawGizmosSelected()
    {
        if (!showSpawnGizmos) return;

        if (mainCamera != null && Application.isPlaying)
        {
            UpdateCameraInfo();

            Vector3 cameraPos = mainCamera.transform.position;

            // Dibujar límites de cámara
            Gizmos.color = Color.white;
            Vector3 topLeft = new Vector3(cameraPos.x - cameraHalfWidth, cameraPos.y + cameraHalfHeight, 0);
            Vector3 topRight = new Vector3(cameraPos.x + cameraHalfWidth, cameraPos.y + cameraHalfHeight, 0);
            Vector3 bottomLeft = new Vector3(cameraPos.x - cameraHalfWidth, cameraPos.y - cameraHalfHeight, 0);
            Vector3 bottomRight = new Vector3(cameraPos.x + cameraHalfWidth, cameraPos.y - cameraHalfHeight, 0);

            Gizmos.DrawLine(topLeft, topRight);
            Gizmos.DrawLine(topRight, bottomRight);
            Gizmos.DrawLine(bottomRight, bottomLeft);
            Gizmos.DrawLine(bottomLeft, topLeft);

            // Dibujar anillo de spawn mínimo
            Gizmos.color = Color.yellow;
            DrawSpawnRing(cameraPos, minSpawnDistanceFromCamera);

            // Dibujar anillo de spawn máximo
            Gizmos.color = Color.red;
            DrawSpawnRing(cameraPos, maxSpawnDistanceFromCamera);

            // Dibujar puntos de spawn válidos
            Gizmos.color = Color.green;
            foreach (Vector2 point in validSpawnPoints)
            {
                Gizmos.DrawWireSphere(point, spawnCheckRadius);
            }
        }

        // Estado del spawner
        if (Application.isPlaying)
        {
            Color statusColor = canSpawnEnemies ? Color.green : Color.red;
            Gizmos.color = statusColor;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 2, Vector3.one * 0.5f);

#if UNITY_EDITOR
            string status = canSpawnEnemies ? "Active" : "Inactive";
            UnityEditor.Handles.Label(transform.position + Vector3.up * 3f,
                $"Dynamic Spawner: {status}\nEnemies: {currentEnemyCount}/{maxEnemies}\nValid Points: {validSpawnPoints.Count}");
#endif
        }
    }

    void DrawSpawnRing(Vector3 center, float distance)
    {
        Vector3 topLeft = new Vector3(center.x - cameraHalfWidth - distance, center.y + cameraHalfHeight + distance, 0);
        Vector3 topRight = new Vector3(center.x + cameraHalfWidth + distance, center.y + cameraHalfHeight + distance, 0);
        Vector3 bottomLeft = new Vector3(center.x - cameraHalfWidth - distance, center.y - cameraHalfHeight - distance, 0);
        Vector3 bottomRight = new Vector3(center.x + cameraHalfWidth + distance, center.y - cameraHalfHeight - distance, 0);

        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
        Gizmos.DrawLine(bottomLeft, topLeft);
    }

    void OnDestroy()
    {
        UnsubscribeFromSystemEvents();
    }

    // ===== PROPIEDADES PÚBLICAS =====
    public bool IsSpawning => canSpawnEnemies;
    public bool IsLevelReady => levelReady;
    public int CurrentEnemyCount => currentEnemyCount;
    public int TotalSpawned => totalSpawned;

    // Context Menus para testing
    [ContextMenu("Test Spawn Enemy Now")]
    public void TestSpawnEnemyNow()
    {
        SpawnEnemyNow();
    }

    [ContextMenu("Clear All Enemies")]
    public void TestClearAllEnemies()
    {
        ClearAllEnemies();
    }
}
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // Necesario para RemoveAll

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
    [Header("SISTEMA DE DESATASCO (UNSTUCK)")]
    [Header("═══════════════════════════════════════")]

    [Tooltip("Habilita el sistema que despawnea enemigos atascados fuera de pantalla.")]
    [SerializeField] private bool enableUnstuckSystem = true;

    [Tooltip("Tiempo máximo que un enemigo puede estar fuera de la vista antes de ser despawneado.")]
    [SerializeField] private float maxTimeOffScreen = 10f;

    [Tooltip("Intervalo en segundos para verificar los enemigos fuera de pantalla.")]
    [SerializeField] private float unstuckCheckInterval = 2f;

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

    // Variables para desatasco
    private List<GameObject> activeEnemies = new List<GameObject>();
    private Dictionary<GameObject, float> offScreenTimers = new Dictionary<GameObject, float>();
    private float nextUnstuckCheck;

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
        gameManager = GameManager.Instance;
        levelManager = LevelManager.Instance;
        poolManager = ObjectPoolManager.Instance;

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError($"{gameObject.name}: No se encontró cámara principal!");
            }
        }

        ValidateSpawnConfiguration();
        CalculateTotalSpawnWeight();

        foreach (var config in enemyConfigs)
        {
            if (!string.IsNullOrEmpty(config.poolID))
            {
                spawnedByType[config.poolID] = 0;
            }
        }

        SubscribeToSystemEvents();
        SetupInitialState();
        nextUnstuckCheck = Time.time + unstuckCheckInterval;

        string poolStatus = poolManager != null ? "Pool Manager encontrado" : "Pool Manager no encontrado";
        DebugLog($"EnemySpawner dinámico inicializado | {poolStatus}");
    }

    void Update()
    {
        UpdateCameraInfo();

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }

        if (!ShouldProcessSpawning()) return;

        if (Time.time >= nextSpawnTime && CanSpawn())
        {
            AttemptDynamicSpawn();
            nextSpawnTime = Time.time + spawnInterval;
        }

        if (enableUnstuckSystem && Time.time >= nextUnstuckCheck)
        {
            CheckForOffScreenEnemies();
            nextUnstuckCheck = Time.time + unstuckCheckInterval;
        }
    }

    void UpdateCameraInfo()
    {
        if (mainCamera == null) return;

        cameraHalfHeight = mainCamera.orthographicSize;
        cameraHalfWidth = cameraHalfHeight * mainCamera.aspect;
    }

    void AttemptDynamicSpawn()
    {
        if (playerTransform == null || mainCamera == null) return;

        CalculateValidSpawnPoints();

        if (validSpawnPoints.Count == 0)
        {
            DebugLog("No se encontraron puntos de spawn válidos");
            failedSpawnAttempts++;
            return;
        }

        Vector2 spawnPosition = validSpawnPoints[Random.Range(0, validSpawnPoints.Count)];
        EnemySpawnConfig selectedConfig = SelectEnemyType();
        if (selectedConfig == null || string.IsNullOrEmpty(selectedConfig.poolID))
        {
            DebugLog("No se pudo seleccionar tipo de enemigo válido");
            failedSpawnAttempts++;
            return;
        }

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
            activeEnemies.Add(newEnemy);

            DebugLog($"Enemigo '{selectedConfig.poolID}' spawneado en: {spawnPosition} " +
                    $"(Total: {currentEnemyCount}/{maxEnemies})");
        }
        else
        {
            failedSpawnAttempts++;
            Debug.LogWarning($"No se pudo spawnear enemigo '{selectedConfig.poolID}' desde pool");
        }
    }

    void CheckForOffScreenEnemies()
    {
        if (mainCamera == null) return;

        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(mainCamera);

        // Se itera sobre una copia para poder modificar la lista original de forma segura si es necesario.
        foreach (GameObject enemy in new List<GameObject>(activeEnemies))
        {
            // Si el enemigo es nulo (fue destruido por otro medio), se salta.
            if (enemy == null || !enemy.activeInHierarchy)
            {
                continue;
            }

            Collider2D enemyCollider = enemy.GetComponent<Collider2D>();
            if (enemyCollider == null) continue;

            // Comprueba si el enemigo está en pantalla
            if (GeometryUtility.TestPlanesAABB(planes, enemyCollider.bounds))
            {
                // Si está en pantalla, nos aseguramos de que no tenga un temporizador activo.
                // Se comprueba si la clave existe antes de intentar eliminarla.
                if (offScreenTimers.ContainsKey(enemy))
                {
                    offScreenTimers.Remove(enemy);
                }
            }
            else
            {
                // Si está fuera de pantalla, se actualiza su temporizador.
                if (!offScreenTimers.ContainsKey(enemy))
                {
                    offScreenTimers[enemy] = 0f;
                }
                offScreenTimers[enemy] += unstuckCheckInterval;

                // Si el tiempo supera el máximo, se devuelve al pool.
                if (offScreenTimers[enemy] >= maxTimeOffScreen)
                {
                    DebugLog($"Enemigo '{enemy.name}' despawneado por estar fuera de pantalla mucho tiempo.");
                    poolManager.Return(enemy); // Esto activará OnEnemyDeath, que lo eliminará de las listas.
                }
            }
        }

        // Limpieza final: elimina cualquier referencia nula de la lista de activos.
        activeEnemies.RemoveAll(item => item == null);
    }

    void ConfigureSpawnedEnemy(GameObject enemy, EnemySpawnConfig config)
    {
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

        float leftCameraEdge = cameraPos.x - cameraHalfWidth;
        float rightCameraEdge = cameraPos.x + cameraHalfWidth;
        float topCameraEdge = cameraPos.y + cameraHalfHeight;
        float bottomCameraEdge = cameraPos.y - cameraHalfHeight;

        float playerVelocityX = 0f;
        Rigidbody2D playerRb = playerTransform.GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            playerVelocityX = playerRb.linearVelocity.x;
        }

        if (spawnBothSides || Mathf.Abs(playerVelocityX) < 0.1f)
        {
            TryAddSpawnPointsOnSide(
                leftCameraEdge - maxSpawnDistanceFromCamera,
                leftCameraEdge - minSpawnDistanceFromCamera,
                bottomCameraEdge, topCameraEdge,
                playerVelocityX < 0 ? forwardSpawnBias : 1f - forwardSpawnBias
            );
            TryAddSpawnPointsOnSide(
                rightCameraEdge + minSpawnDistanceFromCamera,
                rightCameraEdge + maxSpawnDistanceFromCamera,
                bottomCameraEdge, topCameraEdge,
                playerVelocityX > 0 ? forwardSpawnBias : 1f - forwardSpawnBias
            );
        }
        else if (playerVelocityX > 0)
        {
            TryAddSpawnPointsOnSide(
                rightCameraEdge + minSpawnDistanceFromCamera,
                rightCameraEdge + maxSpawnDistanceFromCamera,
                bottomCameraEdge, topCameraEdge, 1f
            );
        }
        else
        {
            TryAddSpawnPointsOnSide(
                leftCameraEdge - maxSpawnDistanceFromCamera,
                leftCameraEdge - minSpawnDistanceFromCamera,
                bottomCameraEdge, topCameraEdge, 1f
            );
        }
    }

    void TryAddSpawnPointsOnSide(float xMin, float xMax, float yMin, float yMax, float probability)
    {
        if (Random.value > probability) return;

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
        RaycastHit2D hit = Physics2D.Raycast(startPoint, Vector2.down, maxGroundCheckDistance, groundLayers);

        if (hit.collider != null)
        {
            Vector2 groundPoint = hit.point + Vector2.up * spawnHeightOffset;
            int overlaps = Physics2D.OverlapCircleNonAlloc(groundPoint, spawnCheckRadius, overlapResults, ~groundLayers);
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

        EnemyEvents.OnEnemyDeath -= OnEnemyDeath;
        EnemyEvents.OnEnemyReturnedToPool -= OnEnemyReturnedToPool;
    }

    void OnEnemyDeath(EnemyCore enemy)
    {
        if (currentEnemyCount > 0)
        {
            currentEnemyCount--;
            DebugLog($"Enemigo murió. Contador actualizado: {currentEnemyCount}/{maxEnemies}");
        }

        if (enemy != null)
        {
            activeEnemies.Remove(enemy.gameObject);
            offScreenTimers.Remove(enemy.gameObject);
        }
    }

    void OnEnemyReturnedToPool(EnemyCore enemy)
    {
        // El contador ya se actualiza en OnEnemyDeath
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
        if (poolManager == null || !poolManager.IsInitialized) return false;
        foreach (var config in enemyConfigs)
        {
            if (config.enabled && !string.IsNullOrEmpty(config.poolID) && config.spawnWeight > 0) return true;
        }
        return false;
    }

    EnemySpawnConfig SelectEnemyType()
    {
        List<EnemySpawnConfig> validConfigs = new List<EnemySpawnConfig>();
        foreach (var config in enemyConfigs)
        {
            if (config.enabled && !string.IsNullOrEmpty(config.poolID) && config.spawnWeight > 0 && IsSpawnTypeAvailable(config))
            {
                validConfigs.Add(config);
            }
        }

        if (validConfigs.Count == 0) return null;

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
        return validConfigs[validConfigs.Count - 1];
    }

    bool IsSpawnTypeAvailable(EnemySpawnConfig config)
    {
        if (string.IsNullOrEmpty(config.poolID)) return false;
        if (poolManager != null && poolManager.IsInitialized)
        {
            var poolConfig = poolManager.GetPoolConfiguration(config.poolID);
            if (poolConfig != null && poolConfig.allowExpansion) return true;
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

    void OnGameStateChanged(GameManager.GameState newState)
    {
        switch (newState)
        {
            case GameManager.GameState.Initializing:
                canSpawnEnemies = false;
                DebugLog("Estado: Inicializando - Spawner pausado");
                break;
            case GameManager.GameState.Playing:
                if (levelReady) EnableSpawning();
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
        UpdateEnemyCount();
        DebugLog($"✅ Spawner dinámico activado - Primer spawn en {spawnInterval} segundos");
    }

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
            if (poolManager != null)
            {
                poolManager.Return(enemy);
                poolReturned++;
            }
            else
            {
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

    void OnDrawGizmosSelected()
    {
        if (!showSpawnGizmos) return;

        if (mainCamera != null && Application.isPlaying)
        {
            UpdateCameraInfo();
            Vector3 cameraPos = mainCamera.transform.position;

            Gizmos.color = Color.white;
            Vector3 topLeft = new Vector3(cameraPos.x - cameraHalfWidth, cameraPos.y + cameraHalfHeight, 0);
            Vector3 topRight = new Vector3(cameraPos.x + cameraHalfWidth, cameraPos.y + cameraHalfHeight, 0);
            Vector3 bottomLeft = new Vector3(cameraPos.x - cameraHalfWidth, cameraPos.y - cameraHalfHeight, 0);
            Vector3 bottomRight = new Vector3(cameraPos.x + cameraHalfWidth, cameraPos.y - cameraHalfHeight, 0);
            Gizmos.DrawLine(topLeft, topRight);
            Gizmos.DrawLine(topRight, bottomRight);
            Gizmos.DrawLine(bottomRight, bottomLeft);
            Gizmos.DrawLine(bottomLeft, topLeft);

            Gizmos.color = Color.yellow;
            DrawSpawnRing(cameraPos, minSpawnDistanceFromCamera);

            Gizmos.color = Color.red;
            DrawSpawnRing(cameraPos, maxSpawnDistanceFromCamera);

            Gizmos.color = Color.green;
            foreach (Vector2 point in validSpawnPoints)
            {
                Gizmos.DrawWireSphere(point, spawnCheckRadius);
            }
        }

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

    public bool IsSpawning => canSpawnEnemies;
    public bool IsLevelReady => levelReady;
    public int CurrentEnemyCount => currentEnemyCount;
    public int TotalSpawned => totalSpawned;

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
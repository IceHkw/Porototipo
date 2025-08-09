// ====================================
// ObjectPoolManager.cs
// Sistema de Object Pooling Genérico
// ====================================

using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

/// <summary>
/// Manager genérico de object pooling que soporta cualquier tipo de GameObject
/// </summary>
public class ObjectPoolManager : MonoBehaviour
{
    [Header("═══════════════════════════════════════")]
    [Header("CONFIGURACIÓN DE POOLS")]
    [Header("═══════════════════════════════════════")]

    [SerializeField] private List<PoolCategory> poolCategories = new List<PoolCategory>();

    [Header("General Settings")]
    [SerializeField] private Transform poolParent;
    [SerializeField] private bool createParentIfMissing = true;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    [SerializeField] private bool showPoolStats = false;

    // Singleton
    public static ObjectPoolManager Instance { get; private set; }

    // Diccionarios para acceso rápido
    private Dictionary<string, Queue<GameObject>> availablePools = new Dictionary<string, Queue<GameObject>>();
    private Dictionary<string, List<GameObject>> allObjectsInPool = new Dictionary<string, List<GameObject>>();
    private Dictionary<string, PoolItem> poolConfigurations = new Dictionary<string, PoolItem>();
    private Dictionary<string, Transform> categoryParents = new Dictionary<string, Transform>();
    private Dictionary<GameObject, string> objectToPoolID = new Dictionary<GameObject, string>();

    // Estado
    private bool isInitialized = false;

    // Eventos
    public System.Action OnPoolManagerInitialized;
    public System.Action<string, GameObject> OnObjectSpawned;
    public System.Action<string, GameObject> OnObjectReturned;

    void Awake()
    {
        SetupSingleton();
        ValidatePoolParent();
    }

    void Start()
    {
        InitializeAllPools();
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
            Debug.LogWarning("Ya existe un ObjectPoolManager en la escena!");
            Destroy(gameObject);
        }
    }

    void ValidatePoolParent()
    {
        if (poolParent == null && createParentIfMissing)
        {
            GameObject parentObj = new GameObject("PooledObjects");
            parentObj.transform.SetParent(transform);
            poolParent = parentObj.transform;
        }
    }

    /// <summary>
    /// Inicializa todos los pools configurados
    /// </summary>
    void InitializeAllPools()
    {
        DebugLog("=== INICIANDO POOLS ===");

        foreach (var category in poolCategories)
        {
            if (string.IsNullOrEmpty(category.categoryName))
                category.categoryName = "Sin Categoría";

            // Crear parent para la categoría
            GameObject categoryObj = new GameObject($"Pool_{category.categoryName}");
            categoryObj.transform.SetParent(poolParent);
            categoryParents[category.categoryName] = categoryObj.transform;

            // Inicializar pools de la categoría
            foreach (var poolItem in category.pools)
            {
                if (ValidatePoolItem(poolItem))
                {
                    InitializePool(poolItem, category.categoryName);
                }
            }
        }

        isInitialized = true;
        OnPoolManagerInitialized?.Invoke();

        DebugLog($"=== POOLS INICIALIZADOS - Total: {poolConfigurations.Count} ===");
        if (showPoolStats) LogPoolStatistics();
    }

    /// <summary>
    /// Valida que un pool item esté correctamente configurado
    /// </summary>
    bool ValidatePoolItem(PoolItem item)
    {
        if (string.IsNullOrEmpty(item.poolID))
        {
            Debug.LogError("Pool sin ID asignado!");
            return false;
        }

        if (item.prefab == null)
        {
            Debug.LogError($"Pool '{item.poolID}' no tiene prefab asignado!");
            return false;
        }

        if (poolConfigurations.ContainsKey(item.poolID))
        {
            Debug.LogError($"ID duplicado: '{item.poolID}'. Los IDs deben ser únicos!");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Inicializa un pool individual
    /// </summary>
    void InitializePool(PoolItem poolItem, string categoryName)
    {
        string poolID = poolItem.poolID;

        // Guardar configuración
        poolConfigurations[poolID] = poolItem;

        // Crear estructuras de datos
        availablePools[poolID] = new Queue<GameObject>(poolItem.initialSize);
        allObjectsInPool[poolID] = new List<GameObject>(poolItem.initialSize);

        Transform parent = categoryParents[categoryName];

        // Pre-instanciar objetos
        for (int i = 0; i < poolItem.initialSize; i++)
        {
            GameObject obj = CreatePooledObject(poolItem, parent);
            if (obj != null)
            {
                availablePools[poolID].Enqueue(obj);
                allObjectsInPool[poolID].Add(obj);
                objectToPoolID[obj] = poolID;
            }
        }

        DebugLog($"Pool '{poolID}' inicializado con {poolItem.initialSize} objetos en categoría '{categoryName}'");
    }

    /// <summary>
    /// Obtiene la configuración de un pool específico
    /// </summary>
    public PoolItem GetPoolConfiguration(string poolID)
    {
        return poolConfigurations.ContainsKey(poolID) ? poolConfigurations[poolID] : null;
    }

    /// <summary>
    /// Crea un objeto para el pool
    /// </summary>
    GameObject CreatePooledObject(PoolItem poolItem, Transform parent)
    {
        GameObject obj = Instantiate(poolItem.prefab, parent);
        obj.name = $"{poolItem.prefab.name}_Pooled";

        // Intentar obtener componente IPoolable
        IPoolable poolable = obj.GetComponent<IPoolable>();

        // Desactivar el objeto
        obj.SetActive(false);

        // Si tiene IPoolable, notificar que está en pool
        if (poolable != null)
        {
            poolable.OnReturnToPool();
        }

        return obj;
    }

    /// <summary>
    /// Spawn un objeto del pool
    /// </summary>
    public GameObject Spawn(string poolID, Vector3 position, Quaternion rotation)
    {
        if (!isInitialized)
        {
            Debug.LogError("ObjectPoolManager no está inicializado!");
            return null;
        }

        if (!poolConfigurations.ContainsKey(poolID))
        {
            Debug.LogError($"No existe pool con ID: '{poolID}'");
            return null;
        }

        GameObject obj = GetAvailableObject(poolID);

        if (obj == null)
        {
            DebugLog($"No hay objetos disponibles en pool '{poolID}'");
            return null;
        }

        // Configurar transform
        obj.transform.position = position;
        obj.transform.rotation = rotation;

        // Activar objeto
        obj.SetActive(true);

        // Si tiene IPoolable, notificar spawn
        IPoolable poolable = obj.GetComponent<IPoolable>();
        if (poolable != null)
        {
            poolable.OnSpawnFromPool(position, rotation);
        }

        OnObjectSpawned?.Invoke(poolID, obj);
        DebugLog($"Objeto spawneado desde pool '{poolID}'");

        return obj;
    }

    /// <summary>
    /// Spawn con configuración adicional
    /// </summary>
    public GameObject SpawnWithConfig(string poolID, Vector3 position, Quaternion rotation, System.Action<GameObject> configAction)
    {
        GameObject obj = Spawn(poolID, position, rotation);

        if (obj != null && configAction != null)
        {
            configAction(obj);
        }

        return obj;
    }

    /// <summary>
    /// Devuelve un objeto al pool
    /// </summary>
    public void Return(GameObject obj)
    {
        if (obj == null) return;

        // Buscar a qué pool pertenece
        if (!objectToPoolID.TryGetValue(obj, out string poolID))
        {
            Debug.LogWarning($"Objeto {obj.name} no pertenece a ningún pool");
            return;
        }

        // Verificar que el pool existe
        if (!availablePools.ContainsKey(poolID))
        {
            Debug.LogError($"Pool '{poolID}' no existe");
            return;
        }

        // Si tiene IPoolable, notificar return
        IPoolable poolable = obj.GetComponent<IPoolable>();
        if (poolable != null)
        {
            poolable.OnReturnToPool();
        }

        // Desactivar y devolver al pool
        obj.SetActive(false);
        availablePools[poolID].Enqueue(obj);

        OnObjectReturned?.Invoke(poolID, obj);
        DebugLog($"Objeto devuelto al pool '{poolID}'");
    }

    /// <summary>
    /// Obtiene un objeto disponible del pool, expandiendo si es necesario
    /// </summary>
    GameObject GetAvailableObject(string poolID)
    {
        Queue<GameObject> pool = availablePools[poolID];
        PoolItem config = poolConfigurations[poolID];

        // Si hay objetos disponibles, usar uno
        if (pool.Count > 0)
        {
            return pool.Dequeue();
        }

        // Si no hay objetos y se permite expansión
        if (config.allowExpansion)
        {
            ExpandPool(poolID, config.expansionAmount);

            if (pool.Count > 0)
            {
                return pool.Dequeue();
            }
        }

        return null;
    }

    /// <summary>
    /// Expande un pool creando más objetos
    /// </summary>
    void ExpandPool(string poolID, int amount)
    {
        if (!poolConfigurations.ContainsKey(poolID)) return;

        PoolItem config = poolConfigurations[poolID];

        // Buscar parent de la categoría
        Transform parent = poolParent;
        foreach (var category in poolCategories)
        {
            if (category.pools.Any(p => p.poolID == poolID))
            {
                if (categoryParents.TryGetValue(category.categoryName, out Transform catParent))
                {
                    parent = catParent;
                }
                break;
            }
        }

        int expandedCount = 0;
        for (int i = 0; i < amount; i++)
        {
            GameObject obj = CreatePooledObject(config, parent);
            if (obj != null)
            {
                availablePools[poolID].Enqueue(obj);
                allObjectsInPool[poolID].Add(obj);
                objectToPoolID[obj] = poolID;
                expandedCount++;
            }
        }

        DebugLog($"Pool '{poolID}' expandido en {expandedCount} objetos");
    }

    /// <summary>
    /// Devuelve todos los objetos activos de un pool específico
    /// </summary>
    public void ReturnAll(string poolID)
    {
        if (!allObjectsInPool.ContainsKey(poolID)) return;

        List<GameObject> poolObjects = allObjectsInPool[poolID];
        int returnedCount = 0;

        foreach (var obj in poolObjects)
        {
            if (obj != null && obj.activeInHierarchy)
            {
                Return(obj);
                returnedCount++;
            }
        }

        DebugLog($"Devueltos {returnedCount} objetos al pool '{poolID}'");
    }

    /// <summary>
    /// Devuelve todos los objetos activos de todos los pools
    /// </summary>
    public void ReturnAllObjects()
    {
        foreach (string poolID in allObjectsInPool.Keys)
        {
            ReturnAll(poolID);
        }

        DebugLog("Todos los objetos devueltos a sus pools");
    }

    /// <summary>
    /// Devuelve todos los objetos de una categoría específica
    /// </summary>
    public void ReturnAllInCategory(string categoryName)
    {
        var category = poolCategories.FirstOrDefault(c => c.categoryName == categoryName);
        if (category == null) return;

        foreach (var poolItem in category.pools)
        {
            ReturnAll(poolItem.poolID);
        }

        DebugLog($"Todos los objetos de la categoría '{categoryName}' devueltos");
    }

    // ===== MÉTODOS DE INFORMACIÓN =====

    public int GetPoolSize(string poolID)
    {
        return allObjectsInPool.ContainsKey(poolID) ? allObjectsInPool[poolID].Count : 0;
    }

    public int GetAvailableCount(string poolID)
    {
        return availablePools.ContainsKey(poolID) ? availablePools[poolID].Count : 0;
    }

    public int GetActiveCount(string poolID)
    {
        return GetPoolSize(poolID) - GetAvailableCount(poolID);
    }

    public bool PoolExists(string poolID)
    {
        return poolConfigurations.ContainsKey(poolID);
    }

    public List<string> GetAllPoolIDs()
    {
        return new List<string>(poolConfigurations.Keys);
    }

    public List<string> GetPoolIDsInCategory(string categoryName)
    {
        var category = poolCategories.FirstOrDefault(c => c.categoryName == categoryName);
        if (category == null) return new List<string>();

        return category.pools.Select(p => p.poolID).ToList();
    }

    // ===== DEBUG =====

    void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[ObjectPoolManager] {message}");
        }
    }

    void LogPoolStatistics()
    {
        Debug.Log("=== ESTADÍSTICAS DE POOLS ===");

        foreach (var category in poolCategories)
        {
            Debug.Log($"\n--- {category.categoryName} ---");

            foreach (var poolItem in category.pools)
            {
                string id = poolItem.poolID;
                int total = GetPoolSize(id);
                int available = GetAvailableCount(id);
                int active = GetActiveCount(id);

                Debug.Log($"{id}: Total={total} | Activos={active} | Disponibles={available}");
            }
        }
    }

    [ContextMenu("Log Pool Statistics")]
    public void LogStats()
    {
        LogPoolStatistics();
    }

    [ContextMenu("Return All Objects")]
    public void TestReturnAll()
    {
        ReturnAllObjects();
    }

    // ===== CLEANUP =====

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // ===== PROPIEDADES PÚBLICAS =====
    public bool IsInitialized => isInitialized;
}

// ====================================
// CLASES DE CONFIGURACIÓN
// ====================================

[System.Serializable]
public class PoolCategory
{
    [Header("Category Settings")]
    public string categoryName = "Nueva Categoría";

    [HideInInspector]
    public bool isExpanded = true;

    [Space]
    public List<PoolItem> pools = new List<PoolItem>();
}

[System.Serializable]
public class PoolItem
{
    [Header("Pool Configuration")]
    [Tooltip("ID único para identificar este pool")]
    public string poolID = "";

    [Tooltip("Prefab a poolear")]
    public GameObject prefab;

    [Header("Size Settings")]
    [Tooltip("Cantidad inicial de objetos")]
    [Min(1)]
    public int initialSize = 10;

    [Tooltip("¿Permitir expansión dinámica?")]
    public bool allowExpansion = true;

    [Tooltip("Cantidad a expandir cuando se agote")]
    [Min(1)]
    public int expansionAmount = 5;
}
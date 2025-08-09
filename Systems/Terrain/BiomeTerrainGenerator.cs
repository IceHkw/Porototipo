using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Tilemap))]
public class BiomeTerrainGenerator : MonoBehaviour
{
    [Header("Configuración del Bioma")]
    [Tooltip("Datos del bioma a generar")]
    public BiomeData currentBiome;

    [Header("Configuración del Mundo")]
    [Tooltip("Ancho del mundo en tiles")]
    public int worldWidth = 100;

    [Tooltip("Seed para la generación aleatoria")]
    public int seed = 12345;

    [Header("Referencias")]
    [Tooltip("Tilemap donde se generará el terreno")]
    public Tilemap terrainTilemap;

    [Tooltip("Tilemap para decoraciones (opcional)")]
    public Tilemap decorationTilemap;

    [Tooltip("Cámara principal para ajustar colores")]
    public Camera mainCamera;

    private System.Random random;
    private List<Vector3Int> surfacePositions = new List<Vector3Int>();
    private bool isGenerating = false;
    private GameObject currentBackgroundInstance; // <--- AÑADIDO

    // NUEVOS EVENTOS Y PROPIEDADES PARA LEVEL MANAGER
    public System.Action OnTerrainGenerationComplete;
    public System.Action<Vector3> OnPlayerSpawnPointReady;
    public System.Action<Vector3[]> OnEnemySpawnPointsReady;

    [Header("Configuración de Spawn")]
    [Tooltip("Altura adicional sobre la superficie para spawns")]
    public float spawnHeightOffset = 1f;

    [Tooltip("Distancia desde los bordes para enemy spawners")]
    public float enemySpawnerEdgeOffset = 5f;

    // Propiedades públicas para acceso externo
    public Vector3 PlayerSpawnPoint { get; private set; }
    public Vector3[] EnemySpawnPoints { get; private set; }
    public bool IsTerrainReady { get; private set; } = false;

    void Start()
    {
        if (currentBiome == null)
        {
            Debug.LogError("No se ha asignado ningún BiomeData!");
            return;
        }

        StartCoroutine(GenerateTerrainSequence());
    }

    /// <summary>
    /// Secuencia completa de generación del terreno
    /// </summary>
    IEnumerator GenerateTerrainSequence()
    {
        isGenerating = true;
        IsTerrainReady = false;

        InitializeGeneration();
        GenerateBackground(); // <--- AÑADIDO
        GenerateTerrain();
        GenerateDecorations();
        ApplyBiomeAmbience();

        // NUEVO: Calcular puntos de spawn después de generar terreno
        CalculateSpawnPoints();

        // Finalizar generación
        FinishTerrainGeneration();

        isGenerating = false;
        IsTerrainReady = true;

        // NUEVO: Notificar que la generación está completa
        OnTerrainGenerationComplete?.Invoke();

        yield return null;
    }

    void InitializeGeneration()
    {
        random = new System.Random(seed);

        if (terrainTilemap == null)
            terrainTilemap = GetComponent<Tilemap>();

        surfacePositions.Clear();
    }

    // <--- AÑADIDO --- >
    void GenerateBackground()
    {
        if (currentBackgroundInstance != null)
        {
            Destroy(currentBackgroundInstance);
        }

        if (currentBiome.backgroundPrefab != null)
        {
            currentBackgroundInstance = Instantiate(currentBiome.backgroundPrefab, transform);
        }
    }


    void GenerateTerrain()
    {
        int yBottom = currentBiome.baseHeight - currentBiome.depth;

        for (int x = 0; x < worldWidth; x++)
        {
            // Calcular altura de superficie con ruido
            float perlinSurface = Mathf.PerlinNoise(x * currentBiome.surfaceNoiseScale, seed);
            int heightOffset = Mathf.RoundToInt(
                Mathf.Lerp(-currentBiome.heightVariation, currentBiome.heightVariation, perlinSurface)
            );
            int yMax = currentBiome.baseHeight + heightOffset;

            // Guardar posición de superficie para decoraciones
            surfacePositions.Add(new Vector3Int(x, yMax, 0));

            // Colocar tile de superficie
            if (currentBiome.surfaceTile != null)
            {
                Vector3Int surfacePos = new Vector3Int(x, yMax, 0);
                terrainTilemap.SetTile(surfacePos, currentBiome.surfaceTile);
            }

            // Calcular grosor de capa intermedia
            float perlinMiddle = Mathf.PerlinNoise(x * currentBiome.middleLayerNoiseScale, seed + 100f);
            int middleThickness = Mathf.RoundToInt(
                Mathf.Lerp(currentBiome.minMiddleThickness, currentBiome.maxMiddleThickness, perlinMiddle)
            );

            int yMiddleBottom = yMax - middleThickness;

            // Rellenar capa intermedia
            if (currentBiome.middleTile != null)
            {
                for (int y = yMax - 1; y >= Mathf.Max(yMiddleBottom, yBottom); y--)
                {
                    Vector3Int middlePos = new Vector3Int(x, y, 0);
                    terrainTilemap.SetTile(middlePos, currentBiome.middleTile);
                }
            }

            // Rellenar capa profunda
            if (currentBiome.deepTile != null && yMiddleBottom > yBottom)
            {
                for (int y = yMiddleBottom - 1; y >= yBottom; y--)
                {
                    Vector3Int deepPos = new Vector3Int(x, y, 0);
                    terrainTilemap.SetTile(deepPos, currentBiome.deepTile);

                    // Generar minerales si están configurados
                    GenerateOres(x, y);
                }
            }

            // Generar líquidos si están configurados
            GenerateLiquids(x, yBottom);
        }
    }

    // NUEVO: Método para calcular puntos de spawn automáticamente
    void CalculateSpawnPoints()
    {
        if (surfacePositions.Count == 0)
        {
            Debug.LogError("No hay posiciones de superficie para calcular spawn points!");
            return;
        }

        // 1. Calcular spawn point del player (centro del mapa)
        int centerX = worldWidth / 2;
        Vector3Int centerSurfacePos = surfacePositions[centerX];

        // Convertir a posición del mundo y añadir offset de altura
        Vector3 playerWorldPos = terrainTilemap.CellToWorld(centerSurfacePos);
        playerWorldPos.y += spawnHeightOffset;
        PlayerSpawnPoint = playerWorldPos;

        // 2. Calcular spawn points de enemy spawners (extremos)
        List<Vector3> enemyPoints = new List<Vector3>();

        // Spawner izquierdo
        int leftX = Mathf.RoundToInt(enemySpawnerEdgeOffset);
        if (leftX < surfacePositions.Count)
        {
            Vector3Int leftSurfacePos = surfacePositions[leftX];
            Vector3 leftWorldPos = terrainTilemap.CellToWorld(leftSurfacePos);
            leftWorldPos.y += spawnHeightOffset;
            enemyPoints.Add(leftWorldPos);
        }

        // Spawner derecho
        int rightX = worldWidth - 1 - Mathf.RoundToInt(enemySpawnerEdgeOffset);
        if (rightX >= 0 && rightX < surfacePositions.Count)
        {
            Vector3Int rightSurfacePos = surfacePositions[rightX];
            Vector3 rightWorldPos = terrainTilemap.CellToWorld(rightSurfacePos);
            rightWorldPos.y += spawnHeightOffset;
            enemyPoints.Add(rightWorldPos);
        }

        EnemySpawnPoints = enemyPoints.ToArray();

        // 3. Notificar los puntos calculados
        OnPlayerSpawnPointReady?.Invoke(PlayerSpawnPoint);
        OnEnemySpawnPointsReady?.Invoke(EnemySpawnPoints);

        Debug.Log($"Spawn points calculados - Player: {PlayerSpawnPoint}, Enemies: {EnemySpawnPoints.Length}");
    }

    void GenerateOres(int x, int y)
    {
        if (currentBiome.specialConfig.oreTiles == null ||
            currentBiome.specialConfig.oreTiles.Length == 0) return;

        float oreRoll = (float)random.NextDouble();
        float cumulativeChance = 0f;

        for (int i = 0; i < currentBiome.specialConfig.oreTiles.Length; i++)
        {
            if (i < currentBiome.specialConfig.oreChances.Length)
            {
                cumulativeChance += currentBiome.specialConfig.oreChances[i];
                if (oreRoll <= cumulativeChance)
                {
                    Vector3Int orePos = new Vector3Int(x, y, 0);
                    terrainTilemap.SetTile(orePos, currentBiome.specialConfig.oreTiles[i]);
                    break;
                }
            }
        }
    }

    void GenerateLiquids(int x, int yBottom)
    {
        if (currentBiome.specialConfig.liquidTile == null) return;

        int liquidLevel = currentBiome.specialConfig.liquidLevel;
        if (liquidLevel >= yBottom)
        {
            for (int y = yBottom; y <= liquidLevel; y++)
            {
                Vector3Int liquidPos = new Vector3Int(x, y, 0);
                if (terrainTilemap.GetTile(liquidPos) == null)
                {
                    terrainTilemap.SetTile(liquidPos, currentBiome.specialConfig.liquidTile);
                }
            }
        }
    }

    void GenerateDecorations()
    {
        if (currentBiome.surfaceDecorations == null) return;

        int lastDecorationX = -1000; // Para controlar espaciado

        foreach (Vector3Int surfacePos in surfacePositions)
        {
            foreach (BiomeDecoration decoration in currentBiome.surfaceDecorations)
            {
                if (decoration.prefab == null) continue;

                // Verificar espaciado mínimo
                if (surfacePos.x - lastDecorationX < decoration.minSpacing) continue;

                // Verificar altura válida
                int heightAboveSurface = surfacePos.y - currentBiome.baseHeight;
                if (heightAboveSurface < decoration.heightRange.x ||
                    heightAboveSurface > decoration.heightRange.y) continue;

                // Verificar probabilidad
                if (random.NextDouble() <= decoration.spawnChance)
                {
                    Vector3 worldPos = terrainTilemap.CellToWorld(surfacePos);
                    worldPos.y += terrainTilemap.cellSize.y; // Colocar encima del tile

                    GameObject instance = Instantiate(decoration.prefab, worldPos, Quaternion.identity);
                    instance.transform.SetParent(this.transform);

                    lastDecorationX = surfacePos.x;
                }
            }
        }
    }

    void ApplyBiomeAmbience()
    {
        // Cambiar color del cielo
        if (mainCamera != null)
        {
            mainCamera.backgroundColor = currentBiome.skyColor;
        }

        // Cambiar luz ambiental
        RenderSettings.ambientLight = currentBiome.ambientLight;

        // Activar efectos climáticos si están configurados
        if (currentBiome.specialConfig.hasWeather &&
            currentBiome.specialConfig.weatherEffects != null)
        {
            foreach (GameObject weatherEffect in currentBiome.specialConfig.weatherEffects)
            {
                if (weatherEffect != null)
                {
                    GameObject instance = Instantiate(weatherEffect, transform);
                    // Configurar el efecto según necesites
                }
            }
        }
    }

    /// <summary>
    /// Finaliza la generación del terreno
    /// </summary>
    void FinishTerrainGeneration()
    {
        Debug.Log("Terreno generado completamente.");

        // Forzar actualización del tilemap
        if (terrainTilemap != null)
        {
            terrainTilemap.CompressBounds();
        }

        // Notificar que la generación ha terminado
        OnTerrainGenerationComplete?.Invoke(); // Cambiado para invocar el evento correctamente
    }


    /// <summary>
    /// Cambia el bioma actual y regenera el terreno
    /// </summary>
    public void ChangeBiome(BiomeData newBiome)
    {
        if (newBiome == null) return;
        if (isGenerating) return; // Evitar cambios durante la generación

        currentBiome = newBiome;

        // Limpiar terreno actual
        terrainTilemap.SetTilesBlock(
            new BoundsInt(-worldWidth, -currentBiome.depth, 0, worldWidth * 2, currentBiome.depth * 2, 1),
            new TileBase[worldWidth * 2 * currentBiome.depth * 2]
        );

        // Regenerar con nuevo bioma
        StartCoroutine(GenerateTerrainSequence());
    }

    /// <summary>
    /// Verifica si el terreno está siendo generado actualmente
    /// </summary>
    public bool IsGenerating()
    {
        return isGenerating;
    }

    // NUEVO: Método para obtener altura de superficie en una posición X específica
    public float GetSurfaceHeightAtX(int x)
    {
        if (x >= 0 && x < surfacePositions.Count)
        {
            Vector3 worldPos = terrainTilemap.CellToWorld(surfacePositions[x]);
            return worldPos.y;
        }
        return 0f;
    }
}
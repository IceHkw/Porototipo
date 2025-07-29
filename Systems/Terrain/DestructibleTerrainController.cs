using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class DestructibleTerrainController : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("El Tilemap sobre el que trabajamos")]
    public Tilemap terrainTilemap;

    [Tooltip("El tile que usarás para dar más bloque (al colocar)")]
    public TileBase groundTile;

    [Header("Parámetros de destrucción")]
    [Tooltip("Radio (en tiles) de destrucción si quieres un efecto circular (ej: explosión)")]
    public float destroyRadius = 1.0f;

    [Tooltip("Capa que define 'terreno' para el raycast de destrucción")]
    public LayerMask groundLayer;


    private Dictionary<Vector3Int, int> tileHealth = new Dictionary<Vector3Int, int>();
    public static DestructibleTerrainController Instance { get; private set; }


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple DestructibleTerrainController instances detected!");
            Destroy(gameObject);
        }

        if (terrainTilemap == null)
        {
            terrainTilemap = GetComponent<Tilemap>();
        }

    }

    private void OnEnable()
    {
        RegisterInstance();
    }

    private void OnDisable()
    {
        UnregisterInstance();
    }

    private void RegisterInstance()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Debug.LogWarning("Multiple DestructibleTerrainController instances detected!");
            Destroy(gameObject);
        }
    }

    private void UnregisterInstance()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    void Update()
    {
        // Ejemplo: clic izquierdo => destruir -> un solo tile
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            TryDestroySingleTile(worldPoint);
        }

        // Ejemplo: clic derecho => colocar bloque
        if (Input.GetMouseButtonDown(1))
        {
            Vector3 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            TryPlaceSingleTile(worldPoint);
        }

        // Ejemplo: pulsar E => destrucción circular (explosión)
        if (Input.GetKeyDown(KeyCode.E))
        {
            Vector3 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            DestroyTilesCircle(worldPoint, destroyRadius);
        }
    }

    /// <summary>
    /// Aplica daño a un tile específico
    /// </summary>
    public void DamageTile(Vector3 worldPos, int damage = 1)
    {
        Vector3Int cell = terrainTilemap.WorldToCell(worldPos);
        TileBase tile = terrainTilemap.GetTile(cell);

        if (tile == null) return;

        // Inicializar salud si es primera vez
        if (!tileHealth.ContainsKey(cell))
        {
            if (tile is DestructibleTile destructibleTile)
            {
                tileHealth[cell] = destructibleTile.maxHealth;
            }
            else
            {
                tileHealth[cell] = 1; // Valor por defecto para tiles normales
            }
        }

        // Aplicar daño
        tileHealth[cell] -= damage;

        // Actualizar apariencia
        UpdateTileAppearance(cell, tileHealth[cell]);

        // Destruir si salud <= 0
        if (tileHealth[cell] <= 0)
        {
            terrainTilemap.SetTile(cell, null);
            tileHealth.Remove(cell);
        }

        terrainTilemap.RefreshTile(cell);
    }

    private void UpdateTileAppearance(Vector3Int cell, int currentHealth)
    {
        TileBase tile = terrainTilemap.GetTile(cell);

        if (tile is DestructibleTile destructibleTile)
        {
            // Cambiar sprite según daño
            if (destructibleTile.damageSprites != null &&
                destructibleTile.damageSprites.Length > 0)
            {
                int spriteIndex = Mathf.Clamp(
                    destructibleTile.damageSprites.Length - currentHealth,
                    0,
                    destructibleTile.damageSprites.Length - 1
                );

                destructibleTile.sprite = destructibleTile.damageSprites[spriteIndex];
            }
        }
    }

    /// <summary>
    /// Intenta destruir únicamente el tile exacto donde está el cursor (clic izquierdo).
    /// </summary>
    public void TryDestroySingleTile(Vector3 worldPos)
    {
        Vector3Int cell = terrainTilemap.WorldToCell(worldPos);
        TileBase tile = terrainTilemap.GetTile(cell);

        if (tile != null)
        {
            terrainTilemap.SetTile(cell, null);
            // Si usas Composite Collider 2D, asegúrate de llamar a:
            terrainTilemap.RefreshTile(cell);
        }


    }

    /// <summary>
    /// Intenta colocar un bloque en la celda apuntada (clic derecho),
    /// siempre que esa celda esté vacía (null).
    /// </summary>
    // Modificar TryPlaceSingleTile para resetear salud
    void TryPlaceSingleTile(Vector3 worldPos)
    {
        Vector3Int cell = terrainTilemap.WorldToCell(worldPos);
        TileBase tile = terrainTilemap.GetTile(cell);

        if (tile == null)
        {
            terrainTilemap.SetTile(cell, groundTile);

            // Resetear salud al colocar nuevo tile
            if (tileHealth.ContainsKey(cell))
            {
                tileHealth.Remove(cell);
            }

            terrainTilemap.RefreshTile(cell);
        }
    }

    /// <summary>
    /// Destruye (pone a null) todos los tiles dentro de un radio (en tiles) del punto dado.
    /// </summary>
    /// <param name="worldPos">Posición en mundo donde ocurre la 'explosión'</param>
    /// <param name="radiusInTiles">Radio aproximado en número de tiles</param>
    void DestroyTilesCircle(Vector3 worldPos, float radiusInTiles)
    {
        Vector3Int centerCell = terrainTilemap.WorldToCell(worldPos);
        int intRadius = Mathf.CeilToInt(radiusInTiles);


        for (int dx = -intRadius; dx <= intRadius; dx++)
        {
            for (int dy = -intRadius; dy <= intRadius; dy++)
            {
                Vector3Int offset = new Vector3Int(dx, dy, 0);
                Vector3Int cell = centerCell + offset;

                float dist = new Vector2(dx, dy).magnitude;
                if (dist <= radiusInTiles)
                {
                    if (terrainTilemap.GetTile(cell) != null)
                    {
                        terrainTilemap.SetTile(cell, null);
                        terrainTilemap.RefreshTile(cell);

                    }
                }
            }
        }


    }

    /// <summary>
    /// Destruye el terreno en una posición con radio
    /// </summary>
    public void DestroyTerrainAt(Vector3 worldPos, float radius, int damage = 1)
    {
        if (radius <= 0)
        {
            DamageTile(worldPos, damage);
        }
        else
        {
            DamageTilesCircle(worldPos, radius, damage);
        }
    }

    void DamageTilesCircle(Vector3 worldPos, float radiusInTiles, int damage)
    {
        Vector3Int centerCell = terrainTilemap.WorldToCell(worldPos);
        int intRadius = Mathf.CeilToInt(radiusInTiles);

        for (int dx = -intRadius; dx <= intRadius; dx++)
        {
            for (int dy = -intRadius; dy <= intRadius; dy++)
            {
                Vector3Int cell = centerCell + new Vector3Int(dx, dy, 0);
                float dist = new Vector2(dx, dy).magnitude;

                if (dist <= radiusInTiles)
                {
                    DamageTile(terrainTilemap.GetCellCenterWorld(cell), damage);
                }
            }
        }
    }


    /// <summary>
    /// Método para crear túneles horizontales (útil para debugging)
    /// </summary>
    [ContextMenu("Crear Túnel Horizontal")]
    public void CreateHorizontalTunnel()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int startCell = terrainTilemap.WorldToCell(mousePos);

        // Crear túnel de 10 tiles de ancho
        for (int x = startCell.x - 5; x <= startCell.x + 5; x++)
        {
            for (int y = startCell.y - 1; y <= startCell.y + 1; y++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                if (terrainTilemap.GetTile(cell) != null)
                {
                    terrainTilemap.SetTile(cell, null);
                    terrainTilemap.RefreshTile(cell);
                }
            }
        }
    }

    /// <summary>
    /// Método para crear túneles verticales (útil para debugging)
    /// </summary>
    [ContextMenu("Crear Túnel Vertical")]
    public void CreateVerticalTunnel()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int startCell = terrainTilemap.WorldToCell(mousePos);

        // Crear túnel hacia abajo
        for (int y = startCell.y; y >= startCell.y - 20; y--)
        {
            for (int x = startCell.x - 1; x <= startCell.x + 1; x++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                if (terrainTilemap.GetTile(cell) != null)
                {
                    terrainTilemap.SetTile(cell, null);
                    terrainTilemap.RefreshTile(cell);
                }
            }
        }
    }
}
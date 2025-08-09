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

    /// <summary>
    /// Aplica daño a un tile específico en una posición del mundo.
    /// </summary>
    public void DamageTile(Vector3 worldPos, int damage = 1)
    {
        Vector3Int cell = terrainTilemap.WorldToCell(worldPos);
        TileBase tile = terrainTilemap.GetTile(cell);

        if (tile == null) return;

        // Inicializar salud si es la primera vez que se golpea esta tile
        if (!tileHealth.ContainsKey(cell))
        {
            if (tile is DestructibleTile destructibleTile)
            {
                tileHealth[cell] = destructibleTile.maxHealth;
            }
            else
            {
                tileHealth[cell] = 1; // Salud por defecto para tiles no configurados como destructibles
            }
        }

        // Aplicar daño
        tileHealth[cell] -= damage;

        // Actualizar la apariencia de la tile si es necesario
        UpdateTileAppearance(cell, tileHealth[cell]);

        // Destruir la tile si su salud llega a cero
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
            // Cambiar el sprite según el daño recibido
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
    /// Destruye el terreno en una posición con un radio determinado.
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
}
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "New Biome", menuName = "Terrain/Biome Data")]
public class BiomeData : ScriptableObject
{
    [Header("Identificación del Bioma")]
    public string biomeName = "Bioma Básico";
    [TextArea(2, 4)]
    public string description = "Descripción del bioma";

    [Header("Tiles del Bioma")]
    [Tooltip("Tile para la superficie (césped, arena, nieve, etc.)")]
    public TileBase surfaceTile;

    [Tooltip("Tile para la capa intermedia (tierra, arena, etc.)")]
    public TileBase middleTile;

    [Tooltip("Tile para la capa profunda (piedra, roca volcánica, etc.)")]
    public TileBase deepTile;

    [Header("Configuración de Generación")]
    [Tooltip("Altura base del terreno")]
    public int baseHeight = 0;

    [Tooltip("Profundidad mínima del terreno")]
    public int depth = 20;

    [Header("Configuración de Superficie")]
    [Tooltip("Escala del ruido para la superficie")]
    public float surfaceNoiseScale = 0.1f;

    [Tooltip("Variación máxima de altura")]
    public int heightVariation = 5;

    [Header("Configuración de Capa Intermedia")]
    [Tooltip("Escala del ruido para la capa intermedia")]
    public float middleLayerNoiseScale = 0.2f;

    [Tooltip("Grosor mínimo de la capa intermedia")]
    public int minMiddleThickness = 4;

    [Tooltip("Grosor máximo de la capa intermedia")]
    public int maxMiddleThickness = 12;

    [Header("Colores y Ambiente")]
    [Tooltip("Color del cielo para este bioma")]
    public Color skyColor = Color.cyan;

    [Tooltip("Color de la luz ambiental")]
    public Color ambientLight = Color.white;

    [Tooltip("Prefab del fondo con el efecto parallax")]
    public GameObject backgroundPrefab; // <--- AÑADIDO

    [Header("Decoraciones y Estructuras")]
    [Tooltip("Prefabs que pueden aparecer en la superficie")]
    public BiomeDecoration[] surfaceDecorations;

    [Tooltip("Prefabs que pueden aparecer bajo tierra")]
    public BiomeDecoration[] undergroundStructures;

    [Header("Configuraciones Especiales")]
    [Tooltip("Configuraciones específicas del bioma")]
    public BiomeSpecialConfig specialConfig;
}

[System.Serializable]
public class BiomeDecoration
{
    public GameObject prefab;
    [Range(0f, 1f)]
    public float spawnChance = 0.1f;
    public Vector2Int heightRange = new Vector2Int(0, 5); // Rango de altura donde puede aparecer
    public int minSpacing = 3; // Espaciado mínimo entre decoraciones
}

[System.Serializable]
public class BiomeSpecialConfig
{
    [Header("Líquidos")]
    public TileBase liquidTile; // Agua, lava, etc.
    public int liquidLevel = -5; // Nivel donde aparece el líquido

    [Header("Minerales")]
    public TileBase[] oreTiles; // Diferentes minerales
    public float[] oreChances; // Probabilidad de cada mineral

    [Header("Clima")]
    public bool hasWeather = false;
    public GameObject[] weatherEffects; // Partículas de lluvia, nieve, etc.
}
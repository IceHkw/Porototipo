using UnityEngine;

/// <summary>
/// Gestor de biomas que permite cambiar entre diferentes tipos de mundo
/// </summary>
public class BiomeManager : MonoBehaviour
{
    [Header("Biomas Disponibles")]
    [Tooltip("Lista de todos los biomas disponibles en el juego")]
    public BiomeData[] availableBiomes;

    [Header("Configuración")]
    [Tooltip("Bioma seleccionado por defecto")]
    public int defaultBiomeIndex = 0;

    [Tooltip("Generador de terreno que usará los biomas")]
    public BiomeTerrainGenerator terrainGenerator;

    [Header("UI (Opcional)")]
    [Tooltip("Texto para mostrar el bioma actual")]
    public UnityEngine.UI.Text biomeNameText;

    private int currentBiomeIndex = 0;

    void Start()
    {
        if (availableBiomes.Length == 0)
        {
            Debug.LogError("No hay biomas configurados en el BiomeManager!");
            return;
        }

        currentBiomeIndex = Mathf.Clamp(defaultBiomeIndex, 0, availableBiomes.Length - 1);
        LoadBiome(currentBiomeIndex);
    }

    void Update()
    {
        // Cambiar biomas con teclas (para testing)
        if (Input.GetKeyDown(KeyCode.B))
        {
            NextBiome();
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            PreviousBiome();
        }
    }

    /// <summary>
    /// Carga un bioma específico por índice
    /// </summary>
    public void LoadBiome(int biomeIndex)
    {
        if (biomeIndex < 0 || biomeIndex >= availableBiomes.Length)
        {
            Debug.LogWarning($"Índice de bioma inválido: {biomeIndex}");
            return;
        }

        currentBiomeIndex = biomeIndex;
        BiomeData selectedBiome = availableBiomes[currentBiomeIndex];

        if (terrainGenerator != null)
        {
            terrainGenerator.ChangeBiome(selectedBiome);
        }

        UpdateUI();

        Debug.Log($"Bioma cambiado a: {selectedBiome.biomeName}");
    }

    /// <summary>
    /// Cambia al siguiente bioma en la lista
    /// </summary>
    public void NextBiome()
    {
        int nextIndex = (currentBiomeIndex + 1) % availableBiomes.Length;
        LoadBiome(nextIndex);
    }

    /// <summary>
    /// Cambia al bioma anterior en la lista
    /// </summary>
    public void PreviousBiome()
    {
        int prevIndex = (currentBiomeIndex - 1 + availableBiomes.Length) % availableBiomes.Length;
        LoadBiome(prevIndex);
    }

    /// <summary>
    /// Selecciona un bioma aleatorio
    /// </summary>
    public void RandomBiome()
    {
        int randomIndex = Random.Range(0, availableBiomes.Length);
        LoadBiome(randomIndex);
    }

    /// <summary>
    /// Obtiene el bioma actual
    /// </summary>
    public BiomeData GetCurrentBiome()
    {
        if (currentBiomeIndex >= 0 && currentBiomeIndex < availableBiomes.Length)
        {
            return availableBiomes[currentBiomeIndex];
        }
        return null;
    }

    void UpdateUI()
    {
        if (biomeNameText != null && GetCurrentBiome() != null)
        {
            biomeNameText.text = $"Bioma: {GetCurrentBiome().biomeName}";
        }
    }
}

// ==========================================
// EJEMPLOS DE CONFIGURACIÓN DE BIOMAS
// ==========================================

/*
EJEMPLO 1: BIOMA DE PRADERA
- Superficie: Césped verde
- Intermedio: Tierra marrón
- Profundo: Piedra gris
- Decoraciones: Árboles, flores, arbustos
- Ambiente: Cielo azul, luz natural

EJEMPLO 2: BIOMA DESÉRTICO
- Superficie: Arena
- Intermedio: Arenisca
- Profundo: Roca sedimentaria
- Decoraciones: Cactus, rocas, huesos
- Ambiente: Cielo amarillento, luz cálida

EJEMPLO 3: BIOMA NEVADO
- Superficie: Nieve
- Intermedio: Tierra congelada
- Profundo: Roca con hielo
- Decoraciones: Pinos, rocas nevadas
- Ambiente: Cielo gris, luz fría

EJEMPLO 4: BIOMA VOLCÁNICO
- Superficie: Roca volcánica
- Intermedio: Ceniza volcánica
- Profundo: Magma solidificado
- Decoraciones: Géiseres, rocas ardientes
- Ambiente: Cielo rojizo, lava como líquido
- Minerales: Obsidiana, azufre

EJEMPLO 5: BIOMA SUBMARINO
- Superficie: Arena del fondo marino
- Intermedio: Sedimento marino
- Profundo: Roca submarina
- Decoraciones: Corales, algas, plantas marinas
- Ambiente: Agua como líquido principal
- Minerales: Perlas, minerales marinos
*/

/// <summary>
/// Ejemplo de un selector de bioma para menú principal
/// </summary>
public class BiomeSelector : MonoBehaviour
{
    [System.Serializable]
    public class BiomeOption
    {
        public BiomeData biome;
        public Sprite previewImage;
        public string displayName;
        [TextArea(2, 4)]
        public string description;
    }

    [Header("Opciones de Bioma")]
    public BiomeOption[] biomeOptions;

    [Header("Referencias UI")]
    public UnityEngine.UI.Image previewImage;
    public UnityEngine.UI.Text biomeNameText;
    public UnityEngine.UI.Text biomeDescriptionText;
    public UnityEngine.UI.Button confirmButton;

    private int selectedBiomeIndex = 0;

    void Start()
    {
        UpdatePreview();
    }

    public void SelectNextBiome()
    {
        selectedBiomeIndex = (selectedBiomeIndex + 1) % biomeOptions.Length;
        UpdatePreview();
    }

    public void SelectPreviousBiome()
    {
        selectedBiomeIndex = (selectedBiomeIndex - 1 + biomeOptions.Length) % biomeOptions.Length;
        UpdatePreview();
    }

    public void ConfirmSelection()
    {
        BiomeData selectedBiome = biomeOptions[selectedBiomeIndex].biome;

        // Aquí puedes guardar la selección y pasar al generador de mundo
        PlayerPrefs.SetString("SelectedBiome", selectedBiome.name);

        // Cargar escena del juego
        UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
    }

    void UpdatePreview()
    {
        if (selectedBiomeIndex >= 0 && selectedBiomeIndex < biomeOptions.Length)
        {
            BiomeOption current = biomeOptions[selectedBiomeIndex];

            if (previewImage != null)
                previewImage.sprite = current.previewImage;

            if (biomeNameText != null)
                biomeNameText.text = current.displayName;

            if (biomeDescriptionText != null)
                biomeDescriptionText.text = current.description;
        }
    }
}
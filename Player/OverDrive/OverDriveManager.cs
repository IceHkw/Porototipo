// Code/Systems/InGame/OverDriveManager.cs
using UnityEngine;
using System.Collections;
using System;

public class OverDriveManager : MonoBehaviour
{
    [Header("═══════════════════════════════════════")]
    [Header("CONFIGURACIÓN DE NIVELES Y CARGA")]
    [Header("═══════════════════════════════════════")]

    [Tooltip("Número máximo de niveles de OverDrive.")]
    [SerializeField] private int maxLevel = 9;
    [Tooltip("Carga necesaria para subir de nivel. Aumenta en cada nivel.")]
    [SerializeField] private float baseChargePerLevel = 100f;
    [Tooltip("Cuánto aumenta la carga necesaria por cada nivel (porcentual).")]
    [SerializeField] private float chargeMultiplierPerLevel = 0.1f;

    [Header("═══════════════════════════════════════")]
    [Header("DECAY (DESCARGA DE LA BARRA)")]
    [Header("═══════════════════════════════════════")]

    [Tooltip("Tiempo en segundos sin hacer daño antes de que la barra empiece a descargarse.")]
    [SerializeField] private float decayDelay = 2f;
    [Tooltip("Velocidad a la que se descarga la barra por segundo.")]
    [SerializeField] private float decayRate = 25f;

    [Header("═══════════════════════════════════════")]
    [Header("EVOLUCIONES DE ARMA")]
    [Header("═══════════════════════════════════════")]
    [Tooltip("La evolución que se aplica en los niveles 3, 6 y 9 para el arma actual.")]
    [SerializeField] private WeaponEvolution currentWeaponEvolution;

    // Referencia al spawner del jugador
    private ClickAttackSpawner playerAttackSpawner;

    // --- ESTADO INTERNO ---
    private int currentLevel = 0;
    private float currentCharge = 0;
    private float currentMaxCharge = 100f;
    private float decayTimer = 0f;
    private bool isDecaying = false;

    // --- EVENTOS PÚBLICOS ---
    public event Action<int, int> OnLevelChanged;
    public event Action<float, float> OnChargeChanged;
    public event Action<int> OnWeaponEvolution;

    // --- SINGLETON ---
    public static OverDriveManager Instance { get; private set; }

    // --- PROPIEDADES PÚBLICAS ---
    public int CurrentLevel => currentLevel;
    public float ChargePercent => currentMaxCharge > 0 ? currentCharge / currentMaxCharge : 0f;
    public float CurrentCharge => currentCharge;
    public float CurrentMaxCharge => currentMaxCharge;
    public int MaxLevel => maxLevel;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        ResetSystem();
        SubscribeToEvents();
        FindPlayerSpawner();
    }

    void Update()
    {
        HandleDecay();
    }

    private void SubscribeToEvents()
    {
        EnemyEvents.OnEnemyDamaged += HandleEnemyDamaged;
    }

    private void UnsubscribeFromEvents()
    {
        EnemyEvents.OnEnemyDamaged -= HandleEnemyDamaged;
    }

    void FindPlayerSpawner()
    {
        if (playerAttackSpawner == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerAttackSpawner = player.GetComponent<ClickAttackSpawner>();
                if (playerAttackSpawner == null)
                {
                    Debug.LogError("OverDriveManager: El objeto del jugador no tiene el componente ClickAttackSpawner.");
                }
            }
        }
    }

    private void HandleEnemyDamaged(EnemyCore enemy, int damage, Vector3 hitPoint, Transform damageSource)
    {
        AddCharge(10f);
    }

    public void AddCharge(float amount)
    {
        if (currentLevel >= maxLevel) return;

        decayTimer = decayDelay;
        isDecaying = false;

        currentCharge += amount;

        if (currentCharge >= currentMaxCharge)
        {
            LevelUp();
        }

        OnChargeChanged?.Invoke(currentCharge, currentMaxCharge);
    }

    private void LevelUp()
    {
        float remainingCharge = currentCharge - currentMaxCharge;
        currentLevel++;
        currentCharge = remainingCharge;
        currentMaxCharge = baseChargePerLevel * (1 + (currentLevel * chargeMultiplierPerLevel));

        OnLevelChanged?.Invoke(currentLevel, maxLevel);
        Debug.Log($"¡OVERDRIVE LEVEL UP! Nuevo nivel: {currentLevel}");

        if (currentWeaponEvolution != null)
        {
            if (currentLevel == 3 || currentLevel == 6 || currentLevel == 9)
            {
                if (playerAttackSpawner == null) FindPlayerSpawner();

                if (playerAttackSpawner != null)
                {
                    if (currentLevel == 3)
                    {
                        playerAttackSpawner.SetEvolution(currentWeaponEvolution);
                    }
                    else
                    {
                        playerAttackSpawner.UpgradeEvolution(currentLevel);
                    }
                }
                OnWeaponEvolution?.Invoke(currentLevel);
            }
        }
    }

    private void HandleDecay()
    {
        if (currentLevel >= maxLevel) return;

        decayTimer -= Time.deltaTime;

        if (decayTimer <= 0)
        {
            isDecaying = true;
        }

        if (isDecaying && currentCharge > 0)
        {
            currentCharge -= decayRate * Time.deltaTime;
            currentCharge = Mathf.Max(0, currentCharge);
            OnChargeChanged?.Invoke(currentCharge, currentMaxCharge);
        }
    }

    public void ResetSystem()
    {
        currentLevel = 0;
        currentCharge = 0;
        currentMaxCharge = baseChargePerLevel;
        decayTimer = decayDelay;
        isDecaying = false;

        OnLevelChanged?.Invoke(currentLevel, maxLevel);
        OnChargeChanged?.Invoke(currentCharge, currentMaxCharge);

        if (playerAttackSpawner != null)
        {
            playerAttackSpawner.SetEvolution(null);
        }
    }

    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
}
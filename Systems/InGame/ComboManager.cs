using UnityEngine;
using System.Collections;

/// <summary>
/// Gestiona el sistema de combo del jugador
/// </summary>
public class ComboManager : MonoBehaviour
{
    [Header("═══════════════════════════════════════")]
    [Header("CONFIGURACIÓN DE COMBO")]
    [Header("═══════════════════════════════════════")]

    [Header("Visual Settings")]
    [SerializeField] private float scaleIncreasePerCombo = 0.02f;
    [SerializeField] private float maxScale = 2f;
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Combo Settings")]
    [SerializeField] private bool onlyCountPlayerKills = true; // Solo contar muertes causadas por el jugador
    [SerializeField] private float comboTimeWindow = 5f; // Tiempo máximo entre kills para mantener combo
    [SerializeField] private bool useComboTimer = false; // Si usar timer para resetear combo

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    // Estado del combo
    private int currentCombo = 0;
    private int highestCombo = 0; // Para esta sesión
    private float lastKillTime = 0f;

    // Referencias
    private PlayerStats playerStats;
    private HUDController hudController;

    // Coroutines
    private Coroutine animationCoroutine;
    private Coroutine comboTimerCoroutine;

    // Eventos
    public System.Action<int> OnComboChanged;
    public System.Action<int> OnComboIncreased;
    public System.Action OnComboReset;
    public System.Action<int> OnNewHighCombo;

    // Singleton
    public static ComboManager Instance { get; private set; }

    // Propiedades públicas
    public int CurrentCombo => currentCombo;
    public int HighestCombo => highestCombo;
    public bool HasCombo => currentCombo > 0;
    public float TimeSinceLastKill => Time.time - lastKillTime;

    void Awake()
    {
        // Configurar singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        SubscribeToEvents();
    }

    void SubscribeToEvents()
    {
        // Suscribirse a eventos del nuevo sistema de enemigos
        if (onlyCountPlayerKills)
        {
            EnemyEvents.OnEnemyKilledByPlayer += HandleEnemyKilledByPlayer;
        }
        else
        {
            EnemyEvents.OnEnemyDeath += HandleEnemyDeath;
        }

        // Buscar referencias iniciales
        FindReferences();
    }

    void FindReferences()
    {
        // Buscar PlayerStats
        playerStats = FindFirstObjectByType<PlayerStats>();
        if (playerStats != null)
        {
            ConnectToPlayer(playerStats);
        }

        // La referencia al HUD se establecerá desde UIManager
    }

    /// <summary>
    /// Conecta el ComboManager con el jugador
    /// </summary>
    public void ConnectToPlayer(PlayerStats stats)
    {
        // Desconectar eventos anteriores si existen
        if (playerStats != null)
        {
            playerStats.OnDamageTaken -= HandlePlayerDamaged;
            playerStats.OnPlayerDeath -= HandlePlayerDeath;
        }

        playerStats = stats;

        if (playerStats != null)
        {
            playerStats.OnDamageTaken += HandlePlayerDamaged;
            playerStats.OnPlayerDeath += HandlePlayerDeath;

            DebugLog("ComboManager conectado al jugador");
        }
    }

    /// <summary>
    /// Conecta el ComboManager con el HUD
    /// </summary>
    public void ConnectToHUD(HUDController hud)
    {
        hudController = hud;

        if (hudController != null)
        {
            // Actualizar display inicial
            UpdateComboDisplay();
            DebugLog("ComboManager conectado al HUD");
        }
    }

    void HandleEnemyDeath(EnemyCore enemy)
    {
        DebugLog($"Enemigo {enemy.name} murió");
        ProcessEnemyKill(enemy);
    }

    void HandleEnemyKilledByPlayer(EnemyCore enemy, Transform killer)
    {
        DebugLog($"Enemigo {enemy.name} eliminado por el jugador");
        ProcessEnemyKill(enemy);
    }

    void ProcessEnemyKill(EnemyCore enemy)
    {
        // Verificar ventana de tiempo si está habilitada
        if (useComboTimer && currentCombo > 0)
        {
            float timeSinceLastKill = Time.time - lastKillTime;
            if (timeSinceLastKill > comboTimeWindow)
            {
                DebugLog($"Combo perdido: {timeSinceLastKill}s desde el último kill (máximo: {comboTimeWindow}s)");
                ResetCombo();
                return;
            }
        }

        lastKillTime = Time.time;
        IncreaseCombo();

        // Reiniciar timer si está habilitado
        if (useComboTimer)
        {
            if (comboTimerCoroutine != null)
            {
                StopCoroutine(comboTimerCoroutine);
            }
            comboTimerCoroutine = StartCoroutine(ComboTimerCoroutine());
        }
    }

    IEnumerator ComboTimerCoroutine()
    {
        yield return new WaitForSeconds(comboTimeWindow);

        DebugLog($"Combo expirado después de {comboTimeWindow}s sin kills");
        ResetCombo();
        comboTimerCoroutine = null;
    }

    void HandlePlayerDamaged(int damage)
    {
        DebugLog($"Jugador recibió daño, reseteando combo");
        ResetCombo();
    }

    void HandlePlayerDeath()
    {
        DebugLog("Jugador murió, reseteando combo");
        ResetCombo();
    }

    /// <summary>
    /// Incrementa el combo
    /// </summary>
    public void IncreaseCombo()
    {
        currentCombo++;

        // Verificar si es un nuevo récord de sesión
        if (currentCombo > highestCombo)
        {
            highestCombo = currentCombo;
            OnNewHighCombo?.Invoke(highestCombo);
        }

        // Disparar eventos
        OnComboIncreased?.Invoke(currentCombo);
        OnComboChanged?.Invoke(currentCombo);

        // Actualizar display
        UpdateComboDisplay();

        // Iniciar animación
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
        animationCoroutine = StartCoroutine(AnimateComboIncrease());

        DebugLog($"Combo aumentado a: {currentCombo}");
    }

    /// <summary>
    /// Reinicia el combo a 0
    /// </summary>
    public void ResetCombo()
    {
        if (currentCombo == 0) return;

        int previousCombo = currentCombo;
        currentCombo = 0;

        // Detener timer si existe
        if (comboTimerCoroutine != null)
        {
            StopCoroutine(comboTimerCoroutine);
            comboTimerCoroutine = null;
        }

        // Disparar eventos
        OnComboReset?.Invoke();
        OnComboChanged?.Invoke(currentCombo);

        // Actualizar display
        UpdateComboDisplay();

        // Detener animaciones en curso
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }

        DebugLog($"Combo reiniciado (era: {previousCombo})");
    }

    /// <summary>
    /// Actualiza el display del combo en el HUD
    /// </summary>
    void UpdateComboDisplay()
    {
        if (hudController != null && hudController.GetComboDisplay() != null)
        {
            hudController.GetComboDisplay().UpdateCombo(currentCombo);
        }
    }

    /// <summary>
    /// Anima el incremento del combo
    /// </summary>
    IEnumerator AnimateComboIncrease()
    {
        if (hudController == null || hudController.GetComboDisplay() == null)
            yield break;

        var comboDisplay = hudController.GetComboDisplay();

        // Calcular escala objetivo basada en el combo actual
        float targetScale = 1f + (currentCombo * scaleIncreasePerCombo);
        targetScale = Mathf.Min(targetScale, maxScale);

        // Animar la escala
        float elapsedTime = 0f;
        float startScale = 1f;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / animationDuration;

            // Usar curve para hacer la animación más dinámica
            float curveValue = scaleCurve.Evaluate(t);

            // Escala va de normal a target y vuelve a normal
            float currentScale;
            if (t < 0.5f)
            {
                // Primera mitad: crecer
                currentScale = Mathf.Lerp(startScale, targetScale, curveValue * 2f);
            }
            else
            {
                // Segunda mitad: volver a normal
                currentScale = Mathf.Lerp(targetScale, startScale, (curveValue - 0.5f) * 2f);
            }

            comboDisplay.SetScale(currentScale);

            yield return null;
        }

        // Asegurar que vuelve a escala normal
        comboDisplay.SetScale(1f);

        animationCoroutine = null;
    }

    /// <summary>
    /// Reinicia completamente el sistema de combos
    /// </summary>
    public void ResetComboSystem()
    {
        currentCombo = 0;
        highestCombo = 0;
        lastKillTime = 0f;

        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }

        if (comboTimerCoroutine != null)
        {
            StopCoroutine(comboTimerCoroutine);
            comboTimerCoroutine = null;
        }

        UpdateComboDisplay();

        DebugLog("Sistema de combos reiniciado completamente");
    }

    void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[ComboManager] {message}");
        }
    }

    void OnDestroy()
    {
        // Limpiar suscripciones
        if (onlyCountPlayerKills)
        {
            EnemyEvents.OnEnemyKilledByPlayer -= HandleEnemyKilledByPlayer;
        }
        else
        {
            EnemyEvents.OnEnemyDeath -= HandleEnemyDeath;
        }

        if (playerStats != null)
        {
            playerStats.OnDamageTaken -= HandlePlayerDamaged;
            playerStats.OnPlayerDeath -= HandlePlayerDeath;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    // ===== MÉTODOS DE DEBUG =====

    [ContextMenu("Test Increase Combo")]
    public void TestIncreaseCombo()
    {
        IncreaseCombo();
    }

    [ContextMenu("Test Reset Combo")]
    public void TestResetCombo()
    {
        ResetCombo();
    }

    [ContextMenu("Test Combo x10")]
    public void TestComboX10()
    {
        for (int i = 0; i < 10; i++)
        {
            IncreaseCombo();
        }
    }

    [ContextMenu("Log Event Subscribers")]
    public void LogEventSubscribers()
    {
        EnemyEvents.LogEventSubscribers();
    }
}
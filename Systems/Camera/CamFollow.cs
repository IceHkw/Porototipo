using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

public class CMFollowPlayer : MonoBehaviour
{
    [Header("═══════════════════════════════════════")]
    [Header("CONFIGURACIÓN DE TARGETS")]
    [Header("═══════════════════════════════════════")]

    [Header("Target Settings")]
    [Tooltip("Tag del jugador principal (siempre el target por defecto)")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("Tag del target virtual para apuntado temporal")]
    [SerializeField] private string virtualTargetTag = "VirtualTarget";

    [Header("═══════════════════════════════════════")]
    [Header("CONTROLES")]
    [Header("═══════════════════════════════════════")]

    [Header("Input Settings")]
    [Tooltip("Tecla para cambiar temporalmente al VirtualTarget")]
    [SerializeField] private KeyCode switchTargetKey = KeyCode.LeftAlt;

    [Header("═══════════════════════════════════════")]
    [Header("TRANSICIONES")]
    [Header("═══════════════════════════════════════")]

    [Header("Transition Settings")]
    [Tooltip("¿Usar transiciones suaves entre targets?")]
    [SerializeField] private bool useSmoothTransitions = true;

    [Tooltip("Velocidad de transición entre targets")]
    [SerializeField] private float transitionSpeed = 5f;

    [Header("═══════════════════════════════════════")]
    [Header("DEBUG")]
    [Header("═══════════════════════════════════════")]

    [Header("Debug")]
    [Tooltip("Mostrar logs de debug")]
    [SerializeField] private bool enableDebugLogs = true;

    // Referencias principales
    private CinemachineCamera vcam;
    private LevelManager levelManager;

    // Targets
    private Transform playerTarget;
    private Transform virtualTarget;
    private Transform currentTarget;

    // Estados
    private bool isUsingVirtualTarget = false;
    private bool playerReady = false;
    private bool isTransitioning = false;

    // Transiciones suaves
    private Coroutine transitionCoroutine;
    private Vector3 lastKnownPlayerPosition;

    void Awake()
    {
        vcam = GetComponent<CinemachineCamera>();

        if (vcam == null)
        {
            Debug.LogError("CMFollowPlayer: No se encontró CinemachineCamera en este GameObject!");
            enabled = false;
            return;
        }

        FindLevelManager();
    }

    void Start()
    {
        SubscribeToLevelEvents();

        // Intentar encontrar targets existentes
        FindExistingTargets();

        // Si ya hay un player en escena, usarlo inmediatamente
        if (playerTarget != null)
        {
            SetPlayerAsTarget();
            playerReady = true;
        }
    }

    void Update()
    {
        if (!playerReady) return;

        HandleTargetSwitching();
        UpdateTargetReferences();
    }

    void FindLevelManager()
    {
        levelManager = LevelManager.Instance;

        if (levelManager == null)
        {
            levelManager = FindFirstObjectByType<LevelManager>();
        }

        if (levelManager == null && enableDebugLogs)
        {
            Debug.LogWarning("CMFollowPlayer: No se encontró LevelManager. La integración será limitada.");
        }
    }

    void SubscribeToLevelEvents()
    {
        if (levelManager != null)
        {
            levelManager.OnPlayerSpawned += OnPlayerSpawned;
            levelManager.OnLevelReady += OnLevelReady;

            DebugLog("Suscrito a eventos del LevelManager");
        }
    }

    void UnsubscribeFromLevelEvents()
    {
        if (levelManager != null)
        {
            levelManager.OnPlayerSpawned -= OnPlayerSpawned;
            levelManager.OnLevelReady -= OnLevelReady;
        }
    }

    void FindExistingTargets()
    {
        // Buscar Player
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
        {
            playerTarget = playerObj.transform;
            DebugLog($"Player encontrado: {playerObj.name}");
        }

        // Buscar VirtualTarget
        UpdateVirtualTargetReference();
    }

    void UpdateVirtualTargetReference()
    {
        GameObject virtualObj = GameObject.FindGameObjectWithTag(virtualTargetTag);
        if (virtualObj != null)
        {
            virtualTarget = virtualObj.transform;
            if (virtualTarget != null && enableDebugLogs)
            {
                DebugLog($"VirtualTarget encontrado: {virtualObj.name}");
            }
        }
        else
        {
            virtualTarget = null;
        }
    }

    void UpdateTargetReferences()
    {
        // Actualizar referencia del VirtualTarget periódicamente
        // Solo si no estamos en transición para evitar interferencias
        if (!isTransitioning && Time.frameCount % 60 == 0) // Cada segundo aprox
        {
            UpdateVirtualTargetReference();
        }
    }

    void HandleTargetSwitching()
    {
        bool shouldUseVirtualTarget = Input.GetKey(switchTargetKey);

        // Si el estado cambió, procesar el cambio
        if (shouldUseVirtualTarget != isUsingVirtualTarget)
        {
            if (shouldUseVirtualTarget)
            {
                // Intentar cambiar a VirtualTarget
                TrySwitchToVirtualTarget();
            }
            else
            {
                // Volver al Player
                SwitchToPlayer();
            }
        }
    }

    void TrySwitchToVirtualTarget()
    {
        // Asegurarse de tener la referencia más actual
        UpdateVirtualTargetReference();

        if (virtualTarget != null)
        {
            SwitchTarget(virtualTarget, true);
            DebugLog("Cambiado a VirtualTarget");
        }
        else
        {
            DebugLog("No se encontró VirtualTarget - manteniendo Player como target");
            isUsingVirtualTarget = false; // Mantener el estado consistente
        }
    }

    void SwitchToPlayer()
    {
        if (playerTarget != null)
        {
            SwitchTarget(playerTarget, false);
            DebugLog("Regresado a Player");
        }
    }

    void SwitchTarget(Transform newTarget, bool usingVirtual)
    {
        if (newTarget == currentTarget) return;

        Transform previousTarget = currentTarget;
        currentTarget = newTarget;
        isUsingVirtualTarget = usingVirtual;

        if (useSmoothTransitions && previousTarget != null)
        {
            StartSmoothTransition(newTarget);
        }
        else
        {
            SetCameraTarget(newTarget);
        }
    }

    void StartSmoothTransition(Transform newTarget)
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }

        transitionCoroutine = StartCoroutine(SmoothTransitionCoroutine(newTarget));
    }

    IEnumerator SmoothTransitionCoroutine(Transform targetTransform)
    {
        isTransitioning = true;

        Vector3 startPosition = vcam.transform.position;
        Transform originalTarget = vcam.Follow;

        // Crear un objeto temporal para la transición si es necesario
        GameObject tempTarget = new GameObject("CameraTransitionTarget");
        tempTarget.transform.position = startPosition;

        vcam.Follow = tempTarget.transform;

        float elapsedTime = 0f;
        float transitionDuration = 1f / transitionSpeed;

        while (elapsedTime < transitionDuration && targetTransform != null)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / transitionDuration;

            // Usar una curva suave para la transición
            t = Mathf.SmoothStep(0f, 1f, t);

            Vector3 targetPosition = targetTransform.position;
            Vector3 currentPosition = Vector3.Lerp(startPosition, targetPosition, t);

            tempTarget.transform.position = currentPosition;

            yield return null;
        }

        // Finalizar transición
        SetCameraTarget(targetTransform);

        // Limpiar objeto temporal
        if (tempTarget != null)
        {
            DestroyImmediate(tempTarget);
        }

        isTransitioning = false;
        transitionCoroutine = null;
    }

    void SetCameraTarget(Transform target)
    {
        if (target != null && vcam != null)
        {
            vcam.Follow = target;
            // vcam.LookAt = target; // Descomentar si necesitas LookAt
        }
    }

    void SetPlayerAsTarget()
    {
        if (playerTarget != null)
        {
            SwitchTarget(playerTarget, false);
            DebugLog("Player asignado como target principal");
        }
    }

    // Callbacks de eventos del LevelManager
    void OnPlayerSpawned()
    {
        DebugLog("Evento OnPlayerSpawned recibido");

        // Obtener referencia del player spawneado
        if (levelManager != null && levelManager.SpawnedPlayer != null)
        {
            playerTarget = levelManager.SpawnedPlayer.transform;
            lastKnownPlayerPosition = playerTarget.position;

            // Asignar inmediatamente como target
            SetPlayerAsTarget();
            playerReady = true;

            DebugLog($"Player spawneado asignado: {playerTarget.name}");
        }
        else
        {
            // Fallback: buscar por tag
            FindExistingTargets();
            if (playerTarget != null)
            {
                SetPlayerAsTarget();
                playerReady = true;
            }
        }
    }

    void OnLevelReady()
    {
        DebugLog("Evento OnLevelReady recibido");

        // Asegurar que tenemos las referencias correctas
        if (playerTarget == null)
        {
            OnPlayerSpawned(); // Intentar encontrar el player
        }

        // Forzar regreso al Player al completar el nivel
        if (playerTarget != null)
        {
            SwitchToPlayer();
        }

        // Actualizar referencias de VirtualTarget
        UpdateVirtualTargetReference();
    }

    // Métodos públicos para control externo
    public void ForceReturnToPlayer()
    {
        if (playerTarget != null)
        {
            SwitchToPlayer();
        }
    }

    public void ResetCamera()
    {
        DebugLog("Reseteando cámara...");

        // Detener transiciones
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
            isTransitioning = false;
        }

        // Reset estado
        isUsingVirtualTarget = false;
        playerReady = false;

        // Limpiar referencias
        currentTarget = null;
        vcam.Follow = null;

        // Buscar nuevas referencias
        FindExistingTargets();

        if (playerTarget != null)
        {
            SetPlayerAsTarget();
            playerReady = true;
        }
    }

    public void SetTransitionSpeed(float speed)
    {
        transitionSpeed = Mathf.Max(0.1f, speed);
    }

    // Context Menu para testing
    [ContextMenu("Force Return To Player")]
    public void TestForceReturnToPlayer()
    {
        ForceReturnToPlayer();
    }

    [ContextMenu("Reset Camera")]
    public void TestResetCamera()
    {
        ResetCamera();
    }

    [ContextMenu("Log Camera Status")]
    public void TestLogCameraStatus()
    {
        LogCameraStatus();
    }

    void LogCameraStatus()
    {
        DebugLog("=== ESTADO DE LA CÁMARA ===");
        DebugLog($"Player Ready: {playerReady}");
        DebugLog($"Current Target: {(currentTarget != null ? currentTarget.name : "None")}");
        DebugLog($"Player Target: {(playerTarget != null ? playerTarget.name : "None")}");
        DebugLog($"Virtual Target: {(virtualTarget != null ? virtualTarget.name : "None")}");
        DebugLog($"Using Virtual Target: {isUsingVirtualTarget}");
        DebugLog($"Is Transitioning: {isTransitioning}");
        DebugLog($"Vcam Follow: {(vcam.Follow != null ? vcam.Follow.name : "None")}");
    }

    void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[CMFollowPlayer] {message}");
        }
    }

    // Propiedades públicas
    public bool IsPlayerReady => playerReady;
    public bool IsUsingVirtualTarget => isUsingVirtualTarget;
    public bool IsTransitioning => isTransitioning;
    public Transform CurrentTarget => currentTarget;
    public Transform PlayerTarget => playerTarget;
    public Transform VirtualTarget => virtualTarget;

    void OnDestroy()
    {
        UnsubscribeFromLevelEvents();

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }
    }
}
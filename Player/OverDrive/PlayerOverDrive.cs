// Code/Player/Core/PlayerOverDrive.cs
using UnityEngine;

/// <summary>
/// Gestiona la aplicación de las mejoras del sistema OverDrive
/// a las estadísticas y habilidades del jugador.
/// </summary>
[RequireComponent(typeof(PlayerStats))]
public class PlayerOverDrive : MonoBehaviour
{
    [Header("═══════════════════════════════════════")]
    [Header("REFERENCIAS A COMPONENTES DEL JUGADOR")]
    [Header("═══════════════════════════════════════")]

    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private ClickAttackSpawner attackSpawner;
    [SerializeField] private DashAbility dashAbility;

    [Header("═══════════════════════════════════════")]
    [Header("CONFIGURACIÓN DE MEJORAS POR NIVEL")]
    [Header("═══════════════════════════════════════")]

    [Tooltip("Aumento de velocidad de movimiento por nivel de OverDrive (ej. 0.05 = 5%).")]
    [SerializeField] private float speedBoostPerLevel = 0.05f;

    [Tooltip("Reducción de cooldown del Dash por nivel (ej. 0.05 = 5%).")]
    [SerializeField] private float dashCooldownReductionPerLevel = 0.05f;

    [Tooltip("Aumento de daño base por nivel (a partir de la Fase 2).")]
    [SerializeField] private float damageBoostPerLevel = 0.1f; // 10%

    [Tooltip("Aumento de velocidad de ataque por nivel (ej. 0.03 = 3% más rápido).")]
    [SerializeField] private float attackSpeedBoostPerLevel = 0.03f; // 3% por nivel

    [Header("═══════════════════════════════════════")]
    [Header("CONFIGURACIÓN DE FASES")]
    [Header("═══════════════════════════════════════")]

    [Tooltip("Nivel en el que empieza el boost de daño")]
    [SerializeField] private int damageBoostStartLevel = 4;

    [Tooltip("Nivel en el que empieza el boost de velocidad de ataque")]
    [SerializeField] private int attackSpeedBoostStartLevel = 2;

    // Almacenamiento de las estadísticas originales para poder calcular las mejoras
    private float originalMoveSpeed;
    private float originalDashCooldown;
    private float originalAttackSpeed;


    void Start()
    {
        // Guardamos las estadísticas originales del jugador antes de cualquier mejora
        CacheOriginalStats();

        // Nos suscribimos a los eventos del OverDriveManager
        if (OverDriveManager.Instance != null)
        {
            OverDriveManager.Instance.OnLevelChanged += HandleLevelChanged;
            // Aplicar el estado inicial si ya hay un nivel
            if (OverDriveManager.Instance.CurrentLevel > 0)
            {
                HandleLevelChanged(OverDriveManager.Instance.CurrentLevel, OverDriveManager.Instance.MaxLevel);
            }
        }
        else
        {
            Debug.LogError("PlayerOverDrive no pudo encontrar una instancia de OverDriveManager.");
            enabled = false;
        }
    }

    /// <summary>
    /// Guarda los valores iniciales de las estadísticas del jugador.
    /// </summary>
    private void CacheOriginalStats()
    {
        if (playerMovement != null)
        {
            originalMoveSpeed = playerMovement.velocidadMaxima;
        }
        if (dashAbility != null)
        {
            originalDashCooldown = dashAbility.MaxCooldown;
        }
        if (attackSpawner != null)
        {
            originalAttackSpeed = attackSpawner.attackSpeed;
        }
    }

    /// <summary>
    /// Se ejecuta cada vez que el nivel de OverDrive cambia.
    /// </summary>
    private void HandleLevelChanged(int newLevel, int maxLevel)
    {
        ApplyStatBoosts(newLevel);

        // Log para debugging
        Debug.Log($"[OverDrive] Nivel {newLevel}/{maxLevel} - Mejoras aplicadas:");
        if (playerMovement != null)
            Debug.Log($"  • Velocidad de movimiento: {playerMovement.velocidadMaxima:F1} ({(playerMovement.velocidadMaxima / originalMoveSpeed - 1) * 100:F0}% boost)");
        if (attackSpawner != null && newLevel >= attackSpeedBoostStartLevel)
            Debug.Log($"  • Velocidad de ataque: x{attackSpawner.TotalAttackSpeed:F2}");
        if (attackSpawner != null && newLevel >= damageBoostStartLevel)
            Debug.Log($"  • Multiplicador de daño: x{1 + ((newLevel - (damageBoostStartLevel - 1)) * damageBoostPerLevel):F2}");
    }

    /// <summary>
    /// Aplica todas las mejoras de estadísticas acumulativas basadas en el nivel actual.
    /// </summary>
    private void ApplyStatBoosts(int currentLevel)
    {
        // --- FASE 1 en adelante: Mejoras de Movilidad ---
        if (playerMovement != null)
        {
            float speedMultiplier = 1 + (currentLevel * speedBoostPerLevel);
            playerMovement.velocidadMaxima = originalMoveSpeed * speedMultiplier;
        }

        if (dashAbility != null)
        {
            float cooldownMultiplier = 1 - (currentLevel * dashCooldownReductionPerLevel);
            cooldownMultiplier = Mathf.Max(0.3f, cooldownMultiplier); // Mínimo 30% del cooldown original
            dashAbility.SetMaxCooldown(originalDashCooldown * cooldownMultiplier);
        }

        // --- FASE 2 en adelante: Mejoras de Velocidad de Ataque ---
        if (currentLevel >= attackSpeedBoostStartLevel && attackSpawner != null)
        {
            // Calculamos el boost de velocidad de ataque basado en los niveles desde que empieza
            int levelsWithAttackSpeed = currentLevel - (attackSpeedBoostStartLevel - 1);
            float attackSpeedMultiplier = 1 + (levelsWithAttackSpeed * attackSpeedBoostPerLevel);
            attackSpawner.SetAttackSpeedMultiplier(attackSpeedMultiplier);
        }
        else if (attackSpawner != null)
        {
            // Si bajamos de nivel (en un futuro), reseteamos el multiplicador
            attackSpawner.SetAttackSpeedMultiplier(1.0f);
        }

        // --- FASE 3 en adelante: Mejoras de Daño ---
        if (currentLevel >= damageBoostStartLevel && attackSpawner != null)
        {
            int levelsWithDamage = currentLevel - (damageBoostStartLevel - 1);
            float damageMultiplier = 1 + (levelsWithDamage * damageBoostPerLevel);
            attackSpawner.SetDamageMultiplier(damageMultiplier);
        }
        else if (attackSpawner != null)
        {
            // Si bajamos del nivel de daño, reseteamos el multiplicador
            attackSpawner.SetDamageMultiplier(1.0f);
        }
    }

    /// <summary>
    /// Permite ajustar manualmente la velocidad de ataque base (para efectos temporales, items, etc.)
    /// </summary>
    public void ModifyBaseAttackSpeed(float newSpeed)
    {
        if (attackSpawner != null)
        {
            originalAttackSpeed = newSpeed;
            attackSpawner.SetBaseAttackSpeed(newSpeed);
            // Reaplicar los boosts actuales
            if (OverDriveManager.Instance != null)
            {
                HandleLevelChanged(OverDriveManager.Instance.CurrentLevel, OverDriveManager.Instance.MaxLevel);
            }
        }
    }

    void OnDestroy()
    {
        // Es importante desuscribirse para evitar errores
        if (OverDriveManager.Instance != null)
        {
            OverDriveManager.Instance.OnLevelChanged -= HandleLevelChanged;
        }
    }
}
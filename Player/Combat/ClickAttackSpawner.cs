// Player/Combat/ClickAttackSpawner.cs
using UnityEngine;
using System.Collections;

public class ClickAttackSpawner : MonoBehaviour
{
    [Header("Attack Settings")]
    public GameObject attackPrefab;

    [Header("Base Attack Durations (at 1.0x speed)")]
    [Tooltip("Duración base del primer ataque. Se ajustará con attackSpeed")]
    public float baseFirstAttackDuration = 0.5f;
    [Tooltip("Duración base del segundo ataque. Se ajustará con attackSpeed")]
    public float baseSecondAttackDuration = 0.7f;

    [Header("Attack Speed")]
    [Tooltip("Multiplicador de velocidad de ataque. 1.0 = normal, 2.0 = doble de rápido")]
    [Range(0.5f, 3.0f)]
    public float attackSpeed = 1.0f;

    [Header("Spawn Settings")]
    public Transform playerTransform;
    public float meleeSpawnRadius = 1.5f;

    [Header("Evolución de Arma")]
    [Tooltip("La evolución actualmente activa. Es asignada por el OverDriveManager.")]
    public WeaponEvolution currentEvolution;

    [Header("OverDrive Multipliers")]
    private float damageMultiplier = 1.0f;
    private float sizeMultiplier = 1.0f;
    private float attackSpeedMultiplier = 1.0f; // NUEVO: Multiplicador adicional del OverDrive

    private SwordAttack prefabAttackStats;

    [Header("Component References")]
    [SerializeField] private PlayerAnimatorController animatorController;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Animator playerAnimator; // NUEVO: Referencia directa al Animator

    private bool isAttacking = false;
    private int comboStep = 0;
    private bool waitingForAnimationEnd = false; // NUEVO: Flag para esperar el fin de la animación

    // Propiedades calculadas
    public float CurrentFirstAttackDuration => baseFirstAttackDuration / TotalAttackSpeed;
    public float CurrentSecondAttackDuration => baseSecondAttackDuration / TotalAttackSpeed;
    public float TotalAttackSpeed => attackSpeed * attackSpeedMultiplier;

    void Awake()
    {
        if (animatorController == null)
            animatorController = GetComponentInChildren<PlayerAnimatorController>();

        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        // NUEVO: Buscar el Animator en los hijos
        if (playerAnimator == null)
            playerAnimator = GetComponentInChildren<Animator>();

        if (attackPrefab != null)
        {
            prefabAttackStats = attackPrefab.GetComponent<SwordAttack>();
        }

        // NUEVO: Suscribirse al evento de fin de animación
        if (animatorController != null)
        {
            animatorController.OnAttackAnimationFinished += HandleAttackAnimationFinished;
        }
    }

    void OnDestroy()
    {
        // NUEVO: Desuscribirse del evento
        if (animatorController != null)
        {
            animatorController.OnAttackAnimationFinished -= HandleAttackAnimationFinished;
        }
    }

    public void HandleAttackInput(Vector2 moveInput)
    {
        if (!isAttacking)
        {
            StartCoroutine(PerformAttack(moveInput));
        }
    }

    private IEnumerator PerformAttack(Vector2 moveInput)
    {
        isAttacking = true;
        waitingForAnimationEnd = true;

        if (animatorController != null)
            animatorController.SetAttacking(true);

        // NUEVO: Ajustar la velocidad de reproducción del Animator
        if (playerAnimator != null)
        {
            playerAnimator.speed = TotalAttackSpeed;
        }

        Vector2 attackDirection = GetAttackDirection(moveInput);

        if (playerMovement != null && Mathf.Abs(moveInput.x) > 0.1f)
        {
            playerMovement.ForzarFlip(moveInput.x);
        }

        // No permitir ataques hacia abajo en el suelo
        if (attackDirection.y < 0 && playerMovement != null && playerMovement.EstaEnSuelo)
        {
            if (animatorController != null) animatorController.SetAttacking(false);
            if (playerAnimator != null) playerAnimator.speed = 1f; // Restaurar velocidad
            isAttacking = false;
            waitingForAnimationEnd = false;
            yield break;
        }

        // Calcular la duración actual basada en la velocidad de ataque
        float currentAttackDuration = (comboStep == 0) ? CurrentFirstAttackDuration : CurrentSecondAttackDuration;

        // 1. Spawneamos el ataque base
        GameObject attackInstance = SpawnSwordAttack(comboStep, attackDirection);

        // 2. Si hay una evolución activa, le pedimos que aplique sus efectos
        if (currentEvolution != null && attackInstance != null)
        {
            currentEvolution.OnAttackSpawn(attackInstance, comboStep);
        }

        // Disparar la animación correspondiente
        TriggerAnimation(comboStep, attackDirection);

        // NUEVO: Sistema híbrido - esperamos el menor tiempo entre:
        // 1. La duración calculada del ataque
        // 2. El evento de fin de animación
        float elapsedTime = 0f;
        while (elapsedTime < currentAttackDuration && waitingForAnimationEnd)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Avanzar el combo
        comboStep = 1 - comboStep;

        // Restaurar la velocidad normal del Animator
        if (playerAnimator != null)
        {
            playerAnimator.speed = 1f;
        }

        if (animatorController != null)
            animatorController.SetAttacking(false);

        isAttacking = false;
        waitingForAnimationEnd = false;
    }

    // NUEVO: Método llamado cuando la animación de ataque termina
    private void HandleAttackAnimationFinished()
    {
        waitingForAnimationEnd = false;
    }

    private Vector2 GetAttackDirection(Vector2 moveInput)
    {
        if (Mathf.Abs(moveInput.y) > 0.5f)
        {
            return new Vector2(0, Mathf.Sign(moveInput.y));
        }

        if (Mathf.Abs(moveInput.x) > 0.1f)
        {
            return new Vector2(Mathf.Sign(moveInput.x), 0);
        }

        return playerMovement != null ? (playerMovement.MirandoDerecha ? Vector2.right : Vector2.left) : Vector2.right;
    }

    private void TriggerAnimation(int step, Vector2 direction)
    {
        if (animatorController == null) return;

        if (Mathf.Abs(direction.y) > 0f)
        {
            if (direction.y > 0) animatorController.IniciarAnimacionAtaqueArriba();
            else animatorController.IniciarAnimacionAtaqueAbajo();
        }
        else
        {
            animatorController.IniciarAnimacionAtaqueHorizontal(step);
        }
    }

    GameObject SpawnSwordAttack(int currentComboStep, Vector2 direction)
    {
        if (attackPrefab == null || playerTransform == null || prefabAttackStats == null) return null;

        Vector3 attackDirection = direction;
        Vector3 spawnPos = playerTransform.position + attackDirection * meleeSpawnRadius;
        float angle = Mathf.Atan2(attackDirection.y, attackDirection.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.Euler(0, 0, angle);

        GameObject attackInstance = Instantiate(attackPrefab, spawnPos, rotation);
        SwordAttack swordScript = attackInstance.GetComponent<SwordAttack>();
        if (swordScript != null)
        {
            swordScript.SetComboStep(currentComboStep);
            swordScript.damage = Mathf.CeilToInt(prefabAttackStats.damage * this.damageMultiplier);
            swordScript.attackSize = prefabAttackStats.attackSize * this.sizeMultiplier;

            if (direction.y < -0.5f)
            {
                swordScript.esAtaqueHaciaAbajo = true;
            }
        }

        Transform visuals = attackInstance.transform.Find("Visuals");
        if (visuals != null && playerMovement != null && !playerMovement.MirandoDerecha)
        {
            Vector3 visualScale = visuals.localScale;
            visualScale.y = -1f;
            visuals.localScale = visualScale;
        }

        attackInstance.transform.SetParent(playerTransform);

        // Usar la duración ajustada por velocidad
        float duration = (currentComboStep == 0) ? CurrentFirstAttackDuration : CurrentSecondAttackDuration;
        Destroy(attackInstance, duration);

        return attackInstance;
    }

    // --- Métodos para gestionar la evolución ---
    public void SetEvolution(WeaponEvolution evolution)
    {
        if (currentEvolution != null)
        {
            currentEvolution.Deactivate(this);
        }
        currentEvolution = evolution;
        if (currentEvolution != null)
        {
            currentEvolution.Activate(this);
        }
    }

    public void UpgradeEvolution(int level)
    {
        if (currentEvolution != null)
        {
            currentEvolution.Upgrade(this, level);
        }
    }

    public void SetDamageMultiplier(float newMultiplier)
    {
        this.damageMultiplier = Mathf.Max(0.1f, newMultiplier);
    }

    public void SetSizeMultiplier(float newMultiplier)
    {
        this.sizeMultiplier = Mathf.Max(0.1f, newMultiplier);
    }

    // NUEVO: Método para establecer el multiplicador de velocidad de ataque
    public void SetAttackSpeedMultiplier(float newMultiplier)
    {
        this.attackSpeedMultiplier = Mathf.Max(0.1f, newMultiplier);
    }

    // NUEVO: Método para modificar la velocidad de ataque base
    public void SetBaseAttackSpeed(float newSpeed)
    {
        this.attackSpeed = Mathf.Clamp(newSpeed, 0.5f, 3.0f);
    }

    public bool IsAttacking => isAttacking;
}
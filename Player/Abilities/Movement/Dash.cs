using UnityEngine;
using System.Collections;
using System;

/// <summary>
/// Habilidad de movimiento: Dash
/// Permite al jugador hacer un dash rápido en la dirección de movimiento
/// </summary>
public class DashAbility : MonoBehaviour, IMovementAbility
{
    [Header("Dash Settings")]
    [SerializeField] private float dashDistance = 5f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;
    [SerializeField] private AnimationCurve dashCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Visual Settings")]
    [SerializeField] private bool createAfterImage = true;
    [SerializeField] private int afterImageCount = 5;
    [SerializeField] private float afterImageInterval = 0.05f;
    [SerializeField] private Color afterImageColor = new Color(1f, 1f, 1f, 0.5f);

    [Header("Physics Settings")]
    [SerializeField] private bool ignoreGravityDuringDash = true;
    [SerializeField] private LayerMask obstacleLayer = -1;
    [SerializeField] private float obstacleCheckRadius = 0.5f;

    [Header("UI")]
    [SerializeField] private Sprite abilityIcon;

    // Referencias
    private PlayerMovement playerMovement;
    private Rigidbody2D rb;
    private Collider2D playerCollider;
    private SpriteRenderer spriteRenderer;
    private PlayerAnimatorController animatorController;
    private PlayerStats playerStats; // --- NUEVA LÍNEA --- Referencia a PlayerStats

    // Estado
    private bool isReady = true;
    private bool isActive = false;
    private float currentCooldown = 0f;
    private float originalGravityScale;
    private Coroutine dashCoroutine;
    private Coroutine afterImageCoroutine;

    // Implementación de IMovementAbility
    public string AbilityName => "Dash";
    public Sprite AbilityIcon => abilityIcon;
    public bool IsReady => isReady && !isActive;
    public bool IsActive => isActive;
    public float CurrentCooldown => currentCooldown;
    public float MaxCooldown => dashCooldown;

    public event Action<float, float, float> OnCooldownChanged;

    void Start()
    {
        InitializeComponents();
    }

    void InitializeComponents()
    {
        playerMovement = GetComponent<PlayerMovement>();
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        animatorController = GetComponent<PlayerAnimatorController>();
        playerStats = GetComponent<PlayerStats>(); // --- NUEVA LÍNEA --- Obtener el componente PlayerStats

        if (rb != null)
            originalGravityScale = rb.gravityScale;

        // --- NUEVA SECCIÓN ---
        // Nos suscribimos al evento OnDamageTaken de PlayerStats.
        // Esto nos permite reaccionar cuando el jugador recibe daño.
        if (playerStats != null)
        {
            playerStats.OnDamageTaken += HandleDamageTaken;
        }
        // --- FIN DE NUEVA SECCIÓN ---
    }

    // --- NUEVA SECCIÓN ---
    // Este método se ejecutará cada vez que PlayerStats invoque el evento OnDamageTaken.
    private void HandleDamageTaken(int damageAmount)
    {
        // Si el dash está activo cuando recibimos daño, lo cancelamos.
        if (isActive)
        {
            ForceStop();
        }
    }

    // --- NUEVA SECCIÓN ---
    // Es una buena práctica desuscribirse de los eventos cuando el objeto se destruye
    // para evitar fugas de memoria.
    void OnDestroy()
    {
        if (playerStats != null)
        {
            playerStats.OnDamageTaken -= HandleDamageTaken;
        }
    }
    // --- FIN DE NUEVA SECCIÓN ---

    void Update()
    {
        UpdateCooldown();
    }

    void UpdateCooldown()
    {
        // El cooldown solo avanza si no estamos listos, no estamos activos, Y estamos en el suelo
        if (!isReady && !isActive && playerMovement != null && playerMovement.EstaEnSuelo)
        {
            currentCooldown -= Time.deltaTime;
            if (currentCooldown <= 0f)
            {
                currentCooldown = 0f;
                isReady = true;
            }
            float cooldownPercent = dashCooldown > 0 ? currentCooldown / dashCooldown : 0f;
            OnCooldownChanged?.Invoke(currentCooldown, dashCooldown, cooldownPercent);
        }
    }

    public bool TryActivate()
    {
        if (!IsReady || playerMovement == null || rb == null) return false;

        Vector2 dashDirection = GetDashDirection();
        if (dashDirection == Vector2.zero || !CanDashInDirection(dashDirection)) return false;

        StartDash(dashDirection);
        return true;
    }

    /// <summary>
    /// Establece un nuevo valor para el cooldown máximo de la habilidad.
    /// </summary>
    /// <param name="newCooldown">El nuevo tiempo de cooldown.</param>
    public void SetMaxCooldown(float newCooldown)
    {
        dashCooldown = Mathf.Max(0.1f, newCooldown); // Evitamos un cooldown de cero o negativo
    }

    Vector2 GetDashDirection()
    {
        // MODIFICADO: El dash ahora es solo horizontal basado en la dirección del jugador.
        return playerMovement.MirandoDerecha ? Vector2.right : Vector2.left;
    }

    bool CanDashInDirection(Vector2 direction)
    {
        if (playerCollider == null) return true;
        RaycastHit2D hit = Physics2D.CircleCast(transform.position, obstacleCheckRadius, direction, dashDistance, obstacleLayer);
        return hit.collider == null;
    }

    void StartDash(Vector2 direction)
    {
        isActive = true;
        isReady = false;
        if (dashCoroutine != null) StopCoroutine(dashCoroutine);
        dashCoroutine = StartCoroutine(DashCoroutine(direction));

        if (createAfterImage && spriteRenderer != null)
        {
            if (afterImageCoroutine != null) StopCoroutine(afterImageCoroutine);
            afterImageCoroutine = StartCoroutine(CreateAfterImages());
        }
    }

    IEnumerator DashCoroutine(Vector2 direction)
    {
        float originalDrag = rb.linearDamping;
        if (ignoreGravityDuringDash) rb.gravityScale = 0f;
        rb.linearDamping = 0f;

        if (animatorController != null)
        {
            animatorController.SetDashing(true);
        }

        Vector2 startPosition = transform.position;
        Vector2 targetPosition = startPosition + (direction * dashDistance);
        float elapsedTime = 0f;

        while (elapsedTime < dashDuration)
        {
            float t = elapsedTime / dashDuration;
            float curveValue = dashCurve.Evaluate(t);
            rb.MovePosition(Vector2.Lerp(startPosition, targetPosition, curveValue));
            elapsedTime += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        rb.linearDamping = originalDrag;
        if (ignoreGravityDuringDash) rb.gravityScale = originalGravityScale;

        if (animatorController != null)
        {
            animatorController.SetDashing(false);
        }

        isActive = false;
        currentCooldown = dashCooldown; // Inicia el cooldown
        OnCooldownChanged?.Invoke(currentCooldown, dashCooldown, 1f);

        // Si estamos en el aire al terminar, el dash no estará listo hasta que se resetee
        if (playerMovement != null && !playerMovement.EstaEnSuelo)
        {
            isReady = false;
        }
    }

    IEnumerator CreateAfterImages()
    {
        int imagesCreated = 0;
        while (isActive && imagesCreated < afterImageCount)
        {
            CreateSingleAfterImage();
            imagesCreated++;
            yield return new WaitForSeconds(afterImageInterval);
        }
    }

    void CreateSingleAfterImage()
    {
        if (spriteRenderer == null) return;
        GameObject afterImage = new GameObject("DashAfterImage");
        afterImage.transform.position = transform.position;
        afterImage.transform.rotation = transform.rotation;
        // --- AJUSTE CLAVE AQUÍ ---
        // Usamos la escala del "spriteRenderer" que sí se invierte,
        // en lugar de la escala del objeto padre que no lo hace.
        // Usamos "lossyScale" para obtener la escala final en el mundo.
        afterImage.transform.localScale = spriteRenderer.transform.lossyScale;
        SpriteRenderer afterImageRenderer = afterImage.AddComponent<SpriteRenderer>();
        afterImageRenderer.sprite = spriteRenderer.sprite;
        afterImageRenderer.color = afterImageColor;
        afterImageRenderer.sortingLayerName = spriteRenderer.sortingLayerName;
        afterImageRenderer.sortingOrder = spriteRenderer.sortingOrder - 1;
        StartCoroutine(FadeAndDestroyAfterImage(afterImageRenderer));
    }

    IEnumerator FadeAndDestroyAfterImage(SpriteRenderer afterImageRenderer)
    {
        float fadeTime = 0.5f;
        float elapsedTime = 0f;
        Color startColor = afterImageRenderer.color;
        while (elapsedTime < fadeTime)
        {
            float alpha = Mathf.Lerp(startColor.a, 0f, elapsedTime / fadeTime);
            afterImageRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        Destroy(afterImageRenderer.gameObject);
    }

    public void ForceStop()
    {
        if (isActive)
        {
            if (dashCoroutine != null) StopCoroutine(dashCoroutine);
            if (afterImageCoroutine != null) StopCoroutine(afterImageCoroutine);

            if (rb != null) rb.gravityScale = originalGravityScale;
            if (animatorController != null) animatorController.SetDashing(false);

            isActive = false;
            currentCooldown = dashCooldown;
        }
    }

    public void ResetCooldown()
    {
        currentCooldown = 0f;
        isReady = true;
        OnCooldownChanged?.Invoke(0f, dashCooldown, 0f);
    }

    public void OnPlayerDeath()
    {
        ForceStop();
    }

    public void OnPlayerRespawn()
    {
        ResetCooldown();
    }
}
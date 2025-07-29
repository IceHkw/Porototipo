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

        if (rb != null)
            originalGravityScale = rb.gravityScale;
    }

    void Update()
    {
        UpdateCooldown();
    }

    void UpdateCooldown()
    {
        if (!isReady && !isActive)
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

    Vector2 GetDashDirection()
    {
        Vector2 inputDirection = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;

        if (inputDirection != Vector2.zero)
        {
            return inputDirection;
        }
        else
        {
            return playerMovement.MirandoDerecha ? Vector2.right : Vector2.left;
        }
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
        currentCooldown = dashCooldown;
        OnCooldownChanged?.Invoke(currentCooldown, dashCooldown, 1f);
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
        afterImage.transform.localScale = transform.localScale;
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
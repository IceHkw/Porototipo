using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class EnergyWaveUltimate : MonoBehaviour, IUltimateAbility
{
    [Header("Ultimate Settings")]
    [SerializeField] private string abilityName = "Energy Wave";
    [SerializeField] private string abilityDescription = "Lanza una onda de energía devastadora.";
    [SerializeField] private Sprite abilityIcon;

    [Header("Execution Settings")]
    [SerializeField] private float chargeDuration = 0.5f;
    [SerializeField] private float expansionDuration = 0.3f;
    [SerializeField] private float maxHorizontalRange = 15f;

    [Header("Charge Settings")]
    [SerializeField] private float maxCharge = 100f;
    private float currentCharge = 0f;

    [Header("Damage Settings")]
    [SerializeField] private int damage = 100;
    [SerializeField] private LayerMask enemyLayer = -1;

    // Referencias
    private PlayerController playerController;
    private PlayerMovement playerMovement;
    private PlayerAnimatorController animatorController;

    // Estado
    private bool isActive = false;
    private float activeTimer = 0f;
    private HashSet<GameObject> hitEnemies = new HashSet<GameObject>();
    private GameObject waveObject;

    // --- EVENTOS Y PROPIEDADES DE LA INTERFAZ IMPLEMENTADOS ---
    public event Action<float> OnChargeChanged;
    public event Action<bool, float> OnActiveStateChanged;

    public string AbilityName => abilityName;
    public string AbilityDescription => abilityDescription;
    public Sprite AbilityIcon => abilityIcon;
    public bool IsReady => currentCharge >= maxCharge && !isActive;
    public bool IsActive => isActive;
    public float ChargePercent => maxCharge > 0 ? currentCharge / maxCharge : 0f;
    public float Duration => chargeDuration + expansionDuration;
    public float RemainingTime => isActive ? (Duration - activeTimer) : 0f;

    void Start()
    {
        InitializeComponents();
        // Para pruebas, la ultimate empieza cargada. Cámbialo a 0 si es necesario.
        currentCharge = maxCharge;
        OnChargeChanged?.Invoke(ChargePercent);
    }

    void InitializeComponents()
    {
        playerController = GetComponent<PlayerController>();
        playerMovement = GetComponent<PlayerMovement>();
        animatorController = GetComponentInChildren<PlayerAnimatorController>();
    }

    void Update()
    {
        if (isActive)
        {
            activeTimer += Time.deltaTime;
        }
    }

    public bool TryActivate()
    {
        if (!IsReady) return false;
        StartCoroutine(ExecuteUltimate());
        return true;
    }

    IEnumerator ExecuteUltimate()
    {
        isActive = true;
        activeTimer = 0f;
        hitEnemies.Clear();
        // Notifica que la ultimate está activa y su duración
        OnActiveStateChanged?.Invoke(true, Duration);

        if (playerController) playerController.SetInputEnabled(false);
        if (playerMovement) playerMovement.SetControlHabilitado(false);
        if (animatorController) animatorController.SetChargingUltimate(true);

        yield return new WaitForSeconds(chargeDuration);

        if (animatorController)
        {
            animatorController.SetChargingUltimate(false);
            animatorController.SetReleasingUltimate(true);
        }

        yield return StartCoroutine(ExpandWave());

        EndUltimate();
    }

    void EndUltimate()
    {
        isActive = false;
        currentCharge = 0f;
        OnChargeChanged?.Invoke(ChargePercent);
        // Notifica que la ultimate ha terminado
        OnActiveStateChanged?.Invoke(false, 0f);

        if (animatorController) animatorController.SetReleasingUltimate(false);
        if (playerController) playerController.SetInputEnabled(true);
        if (playerMovement) playerMovement.SetControlHabilitado(true);
    }

    IEnumerator ExpandWave()
    {
        waveObject = new GameObject("EnergyWaveCollider");
        waveObject.transform.position = transform.position;
        waveObject.transform.SetParent(transform, false);
        BoxCollider2D waveCollider = waveObject.AddComponent<BoxCollider2D>();
        waveCollider.isTrigger = true;

        float elapsedTime = 0f;
        while (elapsedTime < expansionDuration)
        {
            float t = elapsedTime / expansionDuration;
            float currentWidth = Mathf.Lerp(1f, maxHorizontalRange * 2, t);
            waveCollider.size = new Vector2(currentWidth, 5f);
            DetectAndDamageEnemies(waveCollider);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        Destroy(waveObject);
    }

    void DetectAndDamageEnemies(BoxCollider2D waveCollider)
    {
        Collider2D[] overlaps = Physics2D.OverlapBoxAll(waveCollider.bounds.center, waveCollider.bounds.size, 0f, enemyLayer);
        foreach (Collider2D col in overlaps)
        {
            if (hitEnemies.Contains(col.gameObject)) continue;
            hitEnemies.Add(col.gameObject);

            // Aquí iría la lógica para aplicar daño al enemigo
            Debug.Log($"Hit {col.name} with Energy Wave!");
        }
    }

    public void AddCharge(float amount)
    {
        if (isActive || IsReady) return;
        currentCharge = Mathf.Min(currentCharge + amount, maxCharge);
        OnChargeChanged?.Invoke(ChargePercent);
    }

    public void ForceStop()
    {
        if (isActive)
        {
            StopAllCoroutines();
            if (waveObject != null) Destroy(waveObject);
            EndUltimate();
        }
    }

    public void ResetCharge()
    {
        currentCharge = 0f;
        OnChargeChanged?.Invoke(ChargePercent);
    }

    public void OnPlayerDeath()
    {
        ForceStop();
    }

    public void OnPlayerRespawn()
    {
        ResetCharge();
    }
}
// Player/Abilities/KillStreaks/OrbitalOrb.cs
using UnityEngine;
using System.Collections.Generic;

public class OrbitalOrbBehavior : BaseKillStreakBehavior
{
    [Header("Orbit Settings")]
    [SerializeField] private float orbitHeight = 0f;
    [SerializeField] private bool randomStartAngle = true;
    [SerializeField] private bool clockwiseRotation = true;

    [Header("Visual Settings")]
    [SerializeField] private bool rotateOrb = true;
    [SerializeField] private float orbRotationSpeed = 360f;
    [SerializeField] private bool pulsateSize = true;
    [SerializeField] private float pulsateSpeed = 2f;
    [SerializeField] private float pulsateAmount = 0.2f;

    private float currentAngle;
    private Dictionary<GameObject, float> damageCooldowns = new Dictionary<GameObject, float>();
    private float baseScale;
    private TrailRenderer trailRenderer;
    private Light orbLight;

    // --- NUEVA VARIABLE ---
    // Guardará el desfase angular para que no todos los orbes empiecen en el mismo sitio.
    private float angleOffset = 0f;

    protected override void Awake()
    {
        base.Awake();
        trailRenderer = GetComponent<TrailRenderer>();
        orbLight = GetComponentInChildren<Light>();
        baseScale = transform.localScale.x;
    }

    // --- NUEVO MÉTODO PÚBLICO ---
    /// <summary>
    /// Establece un desfase angular para este orbe, usado para sincronizar múltiples orbes.
    /// </summary>
    public void SetAngleOffset(float offset)
    {
        this.angleOffset = offset;
        // Directamente establecemos el ángulo que nos pasa el manager.
        this.currentAngle = this.angleOffset;
        // Ya no necesitamos que sea aleatorio.
        this.randomStartAngle = false;
    }

    protected override void OnInitialize()
    {
        // Ya no se calcula ningún ángulo aquí. El manager se encargará de todo.
        if (trailRenderer != null) trailRenderer.enabled = false;
        if (orbLight != null) orbLight.enabled = false;
        DebugLog("OrbitalOrb inicializado.");
    }


    protected override void OnActivate()
    {
        // La lógica de ángulo aleatorio ya no es necesaria aquí.
        if (trailRenderer != null) trailRenderer.enabled = true;
        if (orbLight != null) orbLight.enabled = true;
        UpdateOrbitPosition();
        DebugLog("OrbitalOrb activado con ángulo: " + currentAngle);
    }

    protected override void OnDeactivate()
    {
        if (trailRenderer != null) trailRenderer.enabled = false;
        if (orbLight != null) orbLight.enabled = false;
        damageCooldowns.Clear();
        DebugLog("OrbitalOrb desactivado");
    }

    protected override void OnReset()
    {
        currentAngle = 0f + angleOffset; // Reseteamos al ángulo inicial con el desfase
        damageCooldowns.Clear();
        transform.localScale = Vector3.one * baseScale;
    }

    void Update()
    {
        if (!isActive || playerTransform == null || currentStats == null) return;
        UpdateOrbit();
        if (rotateOrb) transform.Rotate(Vector3.forward, orbRotationSpeed * Time.deltaTime);
        if (pulsateSize) UpdatePulsation();
        UpdateDamageCooldowns();
        DetectAndDamageEnemies();
    }

    void UpdateOrbit()
    {
        float rotationDirection = clockwiseRotation ? -1f : 1f;
        currentAngle += currentStats.speed * rotationDirection * Time.deltaTime;
        if (currentAngle > 360f) currentAngle -= 360f;
        if (currentAngle < 0f) currentAngle += 360f;
        UpdateOrbitPosition();
    }

    void UpdateOrbitPosition()
    {
        if (playerTransform == null || currentStats == null) return;
        float radians = currentAngle * Mathf.Deg2Rad;
        float x = Mathf.Cos(radians) * currentStats.effectRadius;
        float y = Mathf.Sin(radians) * currentStats.effectRadius;
        transform.position = new Vector3(
            playerTransform.position.x + x,
            playerTransform.position.y + y,
            playerTransform.position.z + orbitHeight
        );
    }

    void UpdatePulsation()
    {
        float pulsation = 1f + (Mathf.Sin(Time.time * pulsateSpeed) * pulsateAmount);
        transform.localScale = Vector3.one * baseScale * pulsation;
        if (orbLight != null) orbLight.intensity = 2f * pulsation;
    }

    void UpdateDamageCooldowns()
    {
        var keysToUpdate = new List<GameObject>(damageCooldowns.Keys);
        foreach (var enemy in keysToUpdate)
        {
            if (enemy == null)
            {
                damageCooldowns.Remove(enemy);
                continue;
            }
            damageCooldowns[enemy] -= Time.deltaTime;
            if (damageCooldowns[enemy] <= 0f) damageCooldowns.Remove(enemy);
        }
    }

    void DetectAndDamageEnemies()
    {
        if (currentStats == null) return;
        var enemies = FindNearbyEnemies(0.5f);
        foreach (var enemy in enemies)
        {
            HandleEnemyCollision(enemy);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActive) return;
        if (other.CompareTag("Enemy")) HandleEnemyCollision(other.gameObject);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!isActive) return;
        if (other.CompareTag("Enemy")) HandleEnemyCollision(other.gameObject);
    }

    void HandleEnemyCollision(GameObject enemy)
    {
        if (currentStats == null || damageCooldowns.ContainsKey(enemy)) return;
        DamageEnemy(enemy, (int)currentStats.potency);
        damageCooldowns[enemy] = currentStats.cooldown;
    }
}
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SwordAttack : MonoBehaviour
{
    [Header("Animation Settings")]
    public string firstAttackAnimation = "Ataque1";
    public string secondAttackAnimation = "Ataque2";

    [Header("Damage Settings")]
    public int damage = 1;
    public float knockbackForce = 8f;

    [Header("Recoil Settings")]
    [Tooltip("Fuerza del pequeño empujón hacia atrás que recibe el jugador al golpear.")]
    public float recoilForce = 4f;

    [Header("Pogo Jump Settings")]
    [Tooltip("Fuerza del rebote hacia arriba al golpear un enemigo desde arriba.")]
    public float pogoForce = 12f;

    [Header("Attack Detection")]
    public LayerMask enemyLayer = -1;
    public LayerMask groundLayer = -1;
    public Vector2 attackSize = new Vector2(1.5f, 1.5f);
    public Vector2 attackOffset = new Vector2(0.75f, 0f);

    [Header("Game Feel")]
    [Tooltip("Duración de la micropausa al impactar (hit-stop).")]
    public float hitStopDuration = 0.05f;
    [Tooltip("Fuerza del shake de cámara al impactar.")]
    public float shakeForceOnHit = 1.2f;
    [Tooltip("Duración del shake de cámara al impactar.")]
    public float shakeDurationOnHit = 0.15f;

    // Flag para identificar el ataque hacia abajo
    public bool esAtaqueHaciaAbajo = false;

    private Animator animator;
    private GameObject playerObject;
    private List<GameObject> hitEnemies = new List<GameObject>();
    public int comboStep = 0;
    private PlayerMovement playerMovement;
    private bool recoilApplied = false;
    private bool timeStopped = false;

    // NUEVO: Referencia a la habilidad de Dash
    private DashAbility dashAbility;

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        playerObject = GameObject.FindGameObjectWithTag("Player");

        if (animator == null)
        {
            Debug.LogError("¡SwordAttack no pudo encontrar el Animator en sus hijos!", this.gameObject);
        }

        if (playerObject != null)
        {
            playerMovement = playerObject.GetComponent<PlayerMovement>();
            // NUEVO: Obtener el componente DashAbility del jugador
            dashAbility = playerObject.GetComponent<DashAbility>();
        }
    }

    public void SetComboStep(int step)
    {
        comboStep = step;
    }

    void Start()
    {
        PlayCorrectAnimation();
    }

    void PlayCorrectAnimation()
    {
        if (animator == null) return;

        string animToPlay = (comboStep == 0) ? firstAttackAnimation : secondAttackAnimation;
        animator.Play(animToPlay);
    }

    public void OnAttackHit()
    {
        DetectAndDamage();
    }

    void DetectAndDamage()
    {
        Vector2 attackPosition = (Vector2)transform.position + GetAttackPositionOffset();
        bool hasHitSomething = false;

        Collider2D[] hitColliders = Physics2D.OverlapBoxAll(attackPosition, attackSize, transform.eulerAngles.z, enemyLayer);

        foreach (var hitCollider in hitColliders)
        {
            if (hitEnemies.Contains(hitCollider.gameObject)) continue;

            IDamageable damageable = hitCollider.GetComponentInParent<IDamageable>();
            if (damageable != null && damageable.IsAlive)
            {
                hitEnemies.Add(hitCollider.gameObject);
                Vector3 hitPoint = hitCollider.ClosestPoint(transform.position);
                Transform damageSource = playerObject != null ? playerObject.transform : transform;
                damageable.TakeDamage(damage, hitPoint, damageSource);

                // Fix: Use the static method to trigger the event instead of directly invoking it
                EnemyEvents.TriggerEnemyDamaged(hitCollider.GetComponentInParent<EnemyCore>(), damage, hitPoint, damageSource);

                hasHitSomething = true;
            }
        }

        // 2. Detectar terreno si no se ha golpeado a un enemigo en este frame
        if (!hasHitSomething)
        {
            Collider2D[] groundHits = Physics2D.OverlapBoxAll(attackPosition, attackSize, 0, groundLayer);
            if (groundHits.Length > 0)
            {
                if (DestructibleTerrainController.Instance != null)
                {
                    DestructibleTerrainController.Instance.DamageTile(attackPosition, damage);
                    hasHitSomething = true;
                }
            }
        }

        // 3. Aplicar Game Feel, Pogo Jump y Recoil si se golpeó algo
        if (hasHitSomething)
        {
            // NUEVO: Si golpeamos a un enemigo mientras estamos en el aire, reseteamos el dash.
            if (hitColliders.Length > 0 && playerMovement != null && !playerMovement.EstaEnSuelo && dashAbility != null)
            {
                dashAbility.ResetCooldown();
            }

            // Lógica de Game Feel (Hit-Stop y Screen Shake)
            if (CameraShakeManager.Instance != null)
            {
                CameraShakeManager.Instance.Shake(shakeForceOnHit, shakeDurationOnHit);
            }
            if (!timeStopped)
            {
                StartCoroutine(HitStopCoroutine());
            }

            // Lógica de Pogo Jump y Retroceso
            bool pogoRealizado = false;
            // Si es un ataque hacia abajo y golpeamos a un enemigo, hacemos Pogo Jump
            if (esAtaqueHaciaAbajo && playerMovement != null && hitColliders.Length > 0)
            {
                playerMovement.AplicarPogoJump(pogoForce);
                pogoRealizado = true;
            }

            // 4. Aplicar retroceso SOLO si NO se hizo un pogo jump
            if (playerMovement != null && !recoilApplied && !pogoRealizado)
            {
                // Calculamos una dirección de retroceso que también tenga un componente vertical.
                Vector2 recoilDirection = new Vector2(-transform.right.x, 0.5f).normalized;

                // Nos aseguramos de aplicar la fuerza solo si hay dirección horizontal
                if (recoilDirection.sqrMagnitude > 0)
                {
                    playerMovement.AplicarImpulsoDeAtaque(recoilDirection, recoilForce);
                }

                recoilApplied = true;
            }
        }
    }

    private IEnumerator HitStopCoroutine()
    {
        timeStopped = true;
        Time.timeScale = 0.0f;
        yield return new WaitForSecondsRealtime(hitStopDuration);
        Time.timeScale = 1.0f;
        timeStopped = false;
    }

    private Vector2 GetAttackPositionOffset()
    {
        return transform.rotation * attackOffset;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
        Gizmos.matrix = rotationMatrix;
        Gizmos.DrawWireCube(attackOffset, attackSize);
    }
}
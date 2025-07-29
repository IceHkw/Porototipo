using UnityEngine;
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
    [Tooltip("Fuerza del peque�o empuj�n hacia atr�s que recibe el jugador al golpear.")]
    public float recoilForce = 4f;

    [Header("Attack Detection")]
    public LayerMask enemyLayer = -1;
    public Vector2 attackSize = new Vector2(1.5f, 1.5f);
    public Vector2 attackOffset = new Vector2(0.75f, 0f);

    private Animator animator;
    private GameObject playerObject;
    private List<GameObject> hitEnemies = new List<GameObject>();
    private int comboStep = 0;
    private PlayerMovement playerMovement;
    private bool recoilApplied = false;

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        playerObject = GameObject.FindGameObjectWithTag("Player");

        if (animator == null)
        {
            Debug.LogError("�SwordAttack no pudo encontrar el Animator en sus hijos!", this.gameObject);
        }

        if (playerObject != null)
        {
            playerMovement = playerObject.GetComponent<PlayerMovement>();
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
        DetectAndDamageEnemies();
    }

    void DetectAndDamageEnemies()
    {
        Vector2 attackPosition = (Vector2)transform.position + GetAttackPositionOffset();
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

                if (playerMovement != null && !recoilApplied)
                {
                    Vector2 recoilDirection = -transform.right;
                    // MODIFICADO: Llamamos al nuevo m�todo que inicia la corutina en PlayerMovement.
                    playerMovement.IniciarRetroceso(recoilDirection, recoilForce);
                    recoilApplied = true;
                }
            }
        }
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
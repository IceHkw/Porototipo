using UnityEngine;
using System.Collections;

public class ClickAttackSpawner : MonoBehaviour
{
    [Header("Attack Settings")]
    public GameObject attackPrefab;
    public float firstAttackDuration = 0.5f;
    public float secondAttackDuration = 0.7f;

    [Header("Spawn Settings")]
    public Transform playerTransform;
    public float meleeSpawnRadius = 1.5f;

    // YA NO SE NECESITA LA CONFIGURACIÓN DE COMBO
    // [Header("Combo Settings")]
    // public bool comboEnabled = true;
    // public float comboWindow = 1.2f;

    [Header("Component References")]
    [SerializeField] private PlayerAnimatorController animatorController;
    [SerializeField] private PlayerMovement playerMovement;

    private bool isAttacking = false;
    private int comboStep = 0; // 0 para el primer ataque, 1 para el segundo

    void Awake()
    {
        if (animatorController == null)
            animatorController = GetComponentInChildren<PlayerAnimatorController>();

        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();
    }

    public void HandleAttackInput()
    {
        // Solo podemos atacar si no estamos atacando ya.
        if (!isAttacking)
        {
            StartCoroutine(PerformAttack());
        }
    }

    private IEnumerator PerformAttack()
    {
        isAttacking = true;

        // Deshabilitamos el control del jugador pero mantenemos la inercia (momentum).
        SetPlayerMovementControl(false);
        if (animatorController != null) animatorController.SetAttacking(true);

        // Determinamos la duración del ataque actual.
        float currentAttackDuration = (comboStep == 0) ? firstAttackDuration : secondAttackDuration;

        // Obtenemos la dirección y generamos el ataque.
        Vector2 attackDirection = GetAttackDirection();
        SpawnSwordAttack(comboStep, attackDirection);
        TriggerAnimation(comboStep, attackDirection);

        // Esperamos a que la animación del ataque termine.
        yield return new WaitForSeconds(currentAttackDuration);

        // Preparamos el siguiente ataque en la secuencia.
        comboStep = 1 - comboStep; // Alterna entre 0 y 1.

        // Devolvemos el control al jugador.
        SetPlayerMovementControl(true);
        if (animatorController != null) animatorController.SetAttacking(false);

        isAttacking = false;
    }

    private void SetPlayerMovementControl(bool enabled)
    {
        if (playerMovement != null)
        {
            playerMovement.SetControlHabilitado(enabled);
        }
    }

    private Vector2 GetAttackDirection()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        if (Mathf.Abs(verticalInput) > 0.1f) return new Vector2(0, Mathf.Sign(verticalInput));
        if (Mathf.Abs(horizontalInput) > 0.1f) return new Vector2(Mathf.Sign(horizontalInput), 0);

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

    void SpawnSwordAttack(int currentComboStep, Vector2 direction)
    {
        if (attackPrefab == null || playerTransform == null) return;

        Vector3 attackDirection = direction;
        Vector3 spawnPos = playerTransform.position + attackDirection * meleeSpawnRadius;
        float angle = Mathf.Atan2(attackDirection.y, attackDirection.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.Euler(0, 0, angle);

        GameObject attackInstance = Instantiate(attackPrefab, spawnPos, rotation);
        SwordAttack swordScript = attackInstance.GetComponent<SwordAttack>();
        if (swordScript != null)
        {
            swordScript.SetComboStep(currentComboStep);
        }

        Transform visuals = attackInstance.transform.Find("Visuals");
        if (visuals != null && playerMovement != null && !playerMovement.MirandoDerecha)
        {
            Vector3 visualScale = visuals.localScale;
            visualScale.y = -1f;
            visuals.localScale = visualScale;
        }

        attackInstance.transform.SetParent(playerTransform);
        float duration = (currentComboStep == 0) ? firstAttackDuration : secondAttackDuration;
        Destroy(attackInstance, duration);
    }

    // Renombramos la propiedad para mayor claridad.
    public bool IsAttacking => isAttacking;
}
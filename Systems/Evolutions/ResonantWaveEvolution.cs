// Code/Systems/Evolutions/ResonantWaveEvolution.cs
using UnityEngine;

[CreateAssetMenu(fileName = "Evo_ResonantWave", menuName = "Evolutions/Sword/Resonant Wave")]
public class ResonantWaveEvolution : WeaponEvolution
{
    [Header("Configuraci�n de Onda Resonante")]
    [SerializeField] private GameObject resonantWavePrefab;
    [Tooltip("El da�o de la onda como un porcentaje del da�o del ataque principal.")]
    [SerializeField][Range(0f, 2f)] private float damageMultiplier = 0.5f; // Nivel 3: 50%
    [SerializeField][Range(0f, 2f)] private float upgradeDamageMultiplier = 0.75f; // Nivel 6: 75%
    [SerializeField][Range(0f, 2f)] private float finalDamageMultiplier = 1.0f; // Nivel 9: 100%

    private float currentDamageMultiplier;

    public override void Activate(ClickAttackSpawner spawner)
    {
        currentDamageMultiplier = damageMultiplier;
        Debug.Log("Evoluci�n ONDA RESONANTE [Nivel 1] activada!");
    }

    public override void Upgrade(ClickAttackSpawner spawner, int level)
    {
        if (level == 6)
        {
            currentDamageMultiplier = upgradeDamageMultiplier;
            Debug.Log("ONDA RESONANTE mejorada a [Nivel 2]!");
        }
        else if (level == 9)
        {
            currentDamageMultiplier = finalDamageMultiplier;
            Debug.Log("ONDA RESONANTE mejorada a [Nivel 3]!");
        }
    }

    public override void OnAttackSpawn(GameObject attackInstance, int comboStep)
    {
        // La onda resonante solo se activa en el segundo golpe del combo.
        if (comboStep != 1) return;

        SwordAttack swordAttack = attackInstance.GetComponent<SwordAttack>();
        if (swordAttack == null || resonantWavePrefab == null) return;

        // Instanciamos la onda en la posici�n del ataque.
        GameObject waveInstance = Instantiate(resonantWavePrefab, attackInstance.transform.position, Quaternion.identity);
        ResonantWave waveScript = waveInstance.GetComponent<ResonantWave>();

        if (waveScript != null)
        {
            // Calculamos el da�o de la onda basado en el da�o actual del ataque y el multiplicador de la evoluci�n.
            int waveDamage = Mathf.CeilToInt(swordAttack.damage * currentDamageMultiplier);
            waveScript.Initialize(waveDamage, swordAttack.transform.parent); // El "padre" es el jugador.
        }
    }

    public override void Deactivate(ClickAttackSpawner spawner)
    {
        // No necesita hacer nada especial al desactivarse.
        Debug.Log("Evoluci�n ONDA RESONANTE desactivada.");
    }
}
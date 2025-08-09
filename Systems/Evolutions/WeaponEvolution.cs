// Code/Systems/Evolutions/WeaponEvolution.cs
using UnityEngine;

/// <summary>
/// Clase base abstracta para todas las evoluciones de armas.
/// Se usa como un ScriptableObject para definir el comportamiento de una evoluci�n.
/// </summary>
public abstract class WeaponEvolution : ScriptableObject
{
    [Header("Informaci�n de la Evoluci�n")]
    public string evolutionName = "Nueva Evoluci�n";
    [TextArea(2, 4)]
    public string description = "Descripci�n de la evoluci�n.";

    /// <summary>
    /// Se llama cuando la evoluci�n se activa por primera vez (Nivel 3 de OverDrive).
    /// </summary>
    /// <param name="spawner">Referencia al spawner de ataques del jugador.</param>
    public abstract void Activate(ClickAttackSpawner spawner);

    /// <summary>
    /// Se llama para mejorar la evoluci�n en niveles superiores (6 y 9).
    /// </summary>
    /// <param name="spawner">Referencia al spawner de ataques del jugador.</param>
    /// <param name="level">El nivel de OverDrive (6 o 9).</param>
    public abstract void Upgrade(ClickAttackSpawner spawner, int level);

    /// <summary>
    /// Se llama justo despu�s de que el spawner crea una instancia de ataque.
    /// Aqu� es donde se a�aden los efectos visuales o de gameplay de la evoluci�n.
    /// </summary>
    /// <param name="attackInstance">La instancia del prefab de ataque reci�n creada.</param>
    /// <param name="comboStep">El paso del combo actual (0 para el primero, 1 para el segundo).</param>
    public abstract void OnAttackSpawn(GameObject attackInstance, int comboStep);

    /// <summary>
    /// Se llama cuando la evoluci�n debe ser desactivada o reseteada.
    /// </summary>
    /// <param name="spawner">Referencia al spawner de ataques del jugador.</param>
    public abstract void Deactivate(ClickAttackSpawner spawner);
}
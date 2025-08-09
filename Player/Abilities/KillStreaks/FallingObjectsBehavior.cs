// Code/Player/Abilities/KillStreaks/FallingObjectsBehavior.cs
using UnityEngine;
using System.Collections;

public class FallingObjectsBehavior : BaseKillStreakBehavior
{
    [Header("═══════════════════════════════════════")]
    [Header("CONFIGURACIÓN DE OBJETOS CAYENDO")]
    [Header("═══════════════════════════════════════")]

    [Header("Spawning")]
    [SerializeField] private GameObject fallingObjectPrefab;
    [SerializeField] private float heightAboveCamera = 5f;
    [SerializeField] private float horizontalOffset = 15f;

    [Header("Trajectory")]
    [SerializeField] private float fallAngle = 30f;

    private Camera mainCamera;
    private Coroutine spawnCoroutine;

    protected override void OnInitialize()
    {
        if (fallingObjectPrefab == null)
        {
            Debug.LogError($"[FallingObjectsBehavior] en '{gameObject.name}': No se ha asignado 'fallingObjectPrefab'.");
            return;
        }
        mainCamera = Camera.main;
    }

    protected override void OnActivate()
    {
        if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
        spawnCoroutine = StartCoroutine(SpawnObjectsRoutine());
        DebugLog("Iniciada la lluvia de objetos.");
    }

    protected override void OnDeactivate()
    {
        if (spawnCoroutine != null && gameObject.activeInHierarchy) StopCoroutine(spawnCoroutine);
        spawnCoroutine = null;
        DebugLog("Detenida la lluvia de objetos.");
    }

    private IEnumerator SpawnObjectsRoutine()
    {
        while (isActive && currentStats != null)
        {
            SpawnFallingObject();
            // Usa el cooldown de las stats para el intervalo
            yield return new WaitForSeconds(currentStats.cooldown);
        }
    }

    private void SpawnFallingObject()
    {
        if (mainCamera == null || currentStats == null) return;
        float spawnSide = Random.Range(0, 2) * 2 - 1;
        float cameraHeight = mainCamera.orthographicSize;
        float cameraWidth = cameraHeight * mainCamera.aspect;
        Vector3 cameraPos = mainCamera.transform.position;
        float spawnX = cameraPos.x + (spawnSide * (cameraWidth + horizontalOffset));
        float spawnY = cameraPos.y + cameraHeight + heightAboveCamera;
        Vector3 spawnPosition = new Vector3(spawnX, spawnY, 0);

        GameObject objInstance = Instantiate(fallingObjectPrefab, spawnPosition, Quaternion.identity);
        FallingObject fallingObj = objInstance.GetComponent<FallingObject>();

        if (fallingObj != null)
        {
            // Usa la potencia de las stats para el daño
            fallingObj.Initialize((int)currentStats.potency, fallAngle, spawnSide);
        }
    }

    protected override void OnReset() { }
}
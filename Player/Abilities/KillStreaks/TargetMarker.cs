using UnityEngine;

/// <summary>
/// Controla el ciclo de vida del marcador de objetivo (crosshair).
/// Se destruye a s� mismo si su objeto padre (el enemigo) es destruido.
/// </summary>
public class TargetMarker : MonoBehaviour
{
    private Transform parentTransform;

    void Start()
    {
        parentTransform = transform.parent;
        if (parentTransform == null)
        {
            // Si por alguna raz�n no tiene padre, se autodestruye.
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // Si el enemigo fue destruido, el padre ser� nulo.
        if (parentTransform == null)
        {
            Destroy(gameObject);
        }
    }
}
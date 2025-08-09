using UnityEngine;

/// <summary>
/// Controla el ciclo de vida del marcador de objetivo (crosshair).
/// Se destruye a sí mismo si su objeto padre (el enemigo) es destruido.
/// </summary>
public class TargetMarker : MonoBehaviour
{
    private Transform parentTransform;

    void Start()
    {
        parentTransform = transform.parent;
        if (parentTransform == null)
        {
            // Si por alguna razón no tiene padre, se autodestruye.
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // Si el enemigo fue destruido, el padre será nulo.
        if (parentTransform == null)
        {
            Destroy(gameObject);
        }
    }
}
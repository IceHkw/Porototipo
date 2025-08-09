using UnityEngine;

/// <summary>
/// Controla el efecto parallax de forma robusta. Espera a que la cámara esté
/// lista antes de empezar a moverse.
/// </summary>
public class ParallaxBackground : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("La cámara principal que sigue al jugador. Se encontrará automáticamente si se deja en blanco.")]
    [SerializeField] private Camera mainCamera;

    [Header("Configuración de Parallax")]
    [Tooltip("Factor de parallax. 0 = el fondo no se mueve, 1 = se mueve solidario a la cámara.")]
    [Range(0f, 1f)]
    [SerializeField] private float parallaxFactorX = 0.5f;

    // Estado interno
    private float lastCameraX;
    private float spriteWidth;
    private bool isReady = false; // Flag para controlar si estamos listos para movernos

    void LateUpdate()
    {
        // Si aún no estamos listos, intentamos inicializarnos en cada frame.
        if (!isReady)
        {
            // 1. Buscar la cámara si no la tenemos.
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            // 2. Si YA tenemos cámara y un SpriteRenderer, completamos la inicialización.
            if (mainCamera != null && GetComponent<SpriteRenderer>() != null)
            {
                spriteWidth = GetComponent<SpriteRenderer>().bounds.size.x;
                lastCameraX = mainCamera.transform.position.x;
                isReady = true; // ¡Ahora sí estamos listos!
            }
            else
            {
                // Si no se cumplen las condiciones, salimos y lo reintentamos en el siguiente frame.
                return;
            }
        }

        // --- A partir de aquí, el código solo se ejecuta si isReady es true ---

        // Calculamos cuánto se movió la cámara desde el último frame
        float deltaX = mainCamera.transform.position.x - lastCameraX;

        // Movemos el fondo
        transform.position += new Vector3(deltaX * parallaxFactorX, 0, 0);

        // Actualizamos la última posición conocida de la cámara
        lastCameraX = mainCamera.transform.position.x;

        // Lógica de bucle infinito
        // Si el borde de la cámara ha sobrepasado el borde del fondo, lo reposicionamos.
        float cameraViewRightEdge = mainCamera.transform.position.x + (mainCamera.orthographicSize * mainCamera.aspect);
        float cameraViewLeftEdge = mainCamera.transform.position.x - (mainCamera.orthographicSize * mainCamera.aspect);

        float backgroundRightEdge = transform.position.x + (spriteWidth / 2);
        float backgroundLeftEdge = transform.position.x - (spriteWidth / 2);

        if (cameraViewLeftEdge > backgroundRightEdge) // La cámara se movió tanto a la derecha que el fondo quedó atrás
        {
            transform.position = new Vector3(transform.position.x + spriteWidth, transform.position.y, transform.position.z);
        }
        else if (cameraViewRightEdge < backgroundLeftEdge) // La cámara se movió tanto a la izquierda...
        {
            transform.position = new Vector3(transform.position.x - spriteWidth, transform.position.y, transform.position.z);
        }
    }
}
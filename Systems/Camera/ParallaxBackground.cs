using UnityEngine;

/// <summary>
/// Controla el efecto parallax de forma robusta. Espera a que la c�mara est�
/// lista antes de empezar a moverse.
/// </summary>
public class ParallaxBackground : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("La c�mara principal que sigue al jugador. Se encontrar� autom�ticamente si se deja en blanco.")]
    [SerializeField] private Camera mainCamera;

    [Header("Configuraci�n de Parallax")]
    [Tooltip("Factor de parallax. 0 = el fondo no se mueve, 1 = se mueve solidario a la c�mara.")]
    [Range(0f, 1f)]
    [SerializeField] private float parallaxFactorX = 0.5f;

    // Estado interno
    private float lastCameraX;
    private float spriteWidth;
    private bool isReady = false; // Flag para controlar si estamos listos para movernos

    void LateUpdate()
    {
        // Si a�n no estamos listos, intentamos inicializarnos en cada frame.
        if (!isReady)
        {
            // 1. Buscar la c�mara si no la tenemos.
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            // 2. Si YA tenemos c�mara y un SpriteRenderer, completamos la inicializaci�n.
            if (mainCamera != null && GetComponent<SpriteRenderer>() != null)
            {
                spriteWidth = GetComponent<SpriteRenderer>().bounds.size.x;
                lastCameraX = mainCamera.transform.position.x;
                isReady = true; // �Ahora s� estamos listos!
            }
            else
            {
                // Si no se cumplen las condiciones, salimos y lo reintentamos en el siguiente frame.
                return;
            }
        }

        // --- A partir de aqu�, el c�digo solo se ejecuta si isReady es true ---

        // Calculamos cu�nto se movi� la c�mara desde el �ltimo frame
        float deltaX = mainCamera.transform.position.x - lastCameraX;

        // Movemos el fondo
        transform.position += new Vector3(deltaX * parallaxFactorX, 0, 0);

        // Actualizamos la �ltima posici�n conocida de la c�mara
        lastCameraX = mainCamera.transform.position.x;

        // L�gica de bucle infinito
        // Si el borde de la c�mara ha sobrepasado el borde del fondo, lo reposicionamos.
        float cameraViewRightEdge = mainCamera.transform.position.x + (mainCamera.orthographicSize * mainCamera.aspect);
        float cameraViewLeftEdge = mainCamera.transform.position.x - (mainCamera.orthographicSize * mainCamera.aspect);

        float backgroundRightEdge = transform.position.x + (spriteWidth / 2);
        float backgroundLeftEdge = transform.position.x - (spriteWidth / 2);

        if (cameraViewLeftEdge > backgroundRightEdge) // La c�mara se movi� tanto a la derecha que el fondo qued� atr�s
        {
            transform.position = new Vector3(transform.position.x + spriteWidth, transform.position.y, transform.position.z);
        }
        else if (cameraViewRightEdge < backgroundLeftEdge) // La c�mara se movi� tanto a la izquierda...
        {
            transform.position = new Vector3(transform.position.x - spriteWidth, transform.position.y, transform.position.z);
        }
    }
}
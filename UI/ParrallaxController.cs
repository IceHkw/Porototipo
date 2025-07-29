using UnityEngine;

public class SimpleParallaxRepeating : MonoBehaviour
{
    [Header("Parallax Settings")]
    [Range(0f, 1f)]
    public float parallaxSpeed = 0.5f;
    public Transform cameraTransform;

    [Header("Infinite Scroll")]
    public bool enableInfiniteScroll = true;
    public float backgroundWidth = 10f; // Ajusta este valor manualmente

    private Vector3 startPos;
    private float lastCameraX;

    void Start()
    {
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;

        startPos = transform.position;
        lastCameraX = cameraTransform.position.x;

        // Auto-calcular el ancho si es posible
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            backgroundWidth = sr.bounds.size.x;
            Debug.Log($"Background width auto-detected: {backgroundWidth}");
        }
    }

    void Update()
    {
        // Calcular movimiento de la cámara
        float deltaX = cameraTransform.position.x - lastCameraX;

        // Aplicar parallax
        Vector3 newPos = transform.position;
        newPos.x += deltaX * parallaxSpeed;
        transform.position = newPos;

        // Infinite scroll
        if (enableInfiniteScroll)
        {
            float distanceFromCamera = transform.position.x - cameraTransform.position.x;

            // Si el fondo se ha alejado demasiado, reposicionarlo
            if (distanceFromCamera > backgroundWidth)
            {
                Vector3 resetPos = transform.position;
                resetPos.x = cameraTransform.position.x - backgroundWidth;
                transform.position = resetPos;
            }
            else if (distanceFromCamera < -backgroundWidth)
            {
                Vector3 resetPos = transform.position;
                resetPos.x = cameraTransform.position.x + backgroundWidth;
                transform.position = resetPos;
            }
        }

        lastCameraX = cameraTransform.position.x;
    }

    // Método para ajustar manualmente el ancho en runtime
    [ContextMenu("Set Background Width From Sprite")]
    void SetWidthFromSprite()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            backgroundWidth = sr.bounds.size.x;
            Debug.Log($"Background width set to: {backgroundWidth}");
        }
    }
}
using UnityEngine;

public class MouseFollower : MonoBehaviour
{
    [Header("Configuración del Área")]
    [Tooltip("Tamaño del área rectangular donde el objeto puede moverse")]
    public Vector2 areaSize = new Vector2(5f, 5f);

    [Tooltip("Centro del área de movimiento (relativo a la posición inicial del objeto)")]
    public Vector2 areaCenter = Vector2.zero;

    [Header("Configuración de Movimiento")]
    [Tooltip("Velocidad de suavizado para seguir el mouse (más alto = más rápido)")]
    [Range(1f, 20f)]
    public float followSpeed = 8f;

    [Tooltip("Distancia mínima para detener el movimiento y evitar deslizamiento")]
    [Range(0.001f, 0.1f)]
    public float stopThreshold = 0.01f;

    [Header("Gizmos")]
    [Tooltip("Mostrar el área de movimiento en la vista de escena")]
    public bool showAreaGizmo = true;

    [Tooltip("Color del gizmo del área")]
    public Color gizmoColor = Color.yellow;

    // Variables privadas
    private Vector3 initialLocalPosition;
    private Vector3 targetLocalPosition;
    private Camera mainCamera;

    void Start()
    {
        // Guardar la posición local inicial como centro de referencia
        initialLocalPosition = transform.localPosition;
        targetLocalPosition = initialLocalPosition;

        // Obtener la cámara principal
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }
    }

    void Update()
    {
        if (mainCamera == null) return;

        HandleMouseInput();
        UpdateMovement();
    }

    void HandleMouseInput()
    {
        // Convertir posición del mouse a coordenadas del mundo
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = transform.position.z; // Mantener la Z del objeto

        // Calcular la posición del centro del área en coordenadas del mundo
        // Usar la posición actual del padre + offset local
        Vector3 parentWorldPos = transform.parent != null ? transform.parent.position : Vector3.zero;
        Vector3 worldAreaCenter = parentWorldPos + (Vector3)initialLocalPosition + (Vector3)areaCenter;

        // Limitar la posición del mouse dentro del área definida
        Vector3 clampedWorldPosition = new Vector3(
            Mathf.Clamp(mouseWorldPos.x,
                worldAreaCenter.x - areaSize.x / 2f,
                worldAreaCenter.x + areaSize.x / 2f),
            Mathf.Clamp(mouseWorldPos.y,
                worldAreaCenter.y - areaSize.y / 2f,
                worldAreaCenter.y + areaSize.y / 2f),
            worldAreaCenter.z
        );

        // Convertir la posición del mundo a posición local relativa al padre
        Vector3 clampedLocalPosition;
        if (transform.parent != null)
        {
            clampedLocalPosition = transform.parent.InverseTransformPoint(clampedWorldPosition);
        }
        else
        {
            clampedLocalPosition = clampedWorldPosition;
        }

        // Actualizar la posición objetivo
        targetLocalPosition = clampedLocalPosition;
    }

    void UpdateMovement()
    {
        // Verificar si ya estamos lo suficientemente cerca del objetivo
        float distance = Vector3.Distance(transform.localPosition, targetLocalPosition);

        if (distance > stopThreshold)
        {
            // Mover suavemente hacia la posición objetivo usando coordenadas locales
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocalPosition,
                followSpeed * Time.deltaTime);
        }
        else
        {
            // Si estamos muy cerca, fijar directamente la posición para evitar deslizamiento
            transform.localPosition = targetLocalPosition;
        }
    }

    // Métodos públicos para control externo
    public void ResetToCenter()
    {
        Vector3 centerLocalPosition = initialLocalPosition + (Vector3)areaCenter;
        targetLocalPosition = centerLocalPosition;
    }

    public void SetAreaSize(Vector2 newSize)
    {
        areaSize = newSize;
    }

    public void SetAreaCenter(Vector2 newCenter)
    {
        areaCenter = newCenter;
    }

    // Dibujar gizmos en la vista de escena
    void OnDrawGizmos()
    {
        if (!showAreaGizmo) return;

        // Calcular la posición base dependiendo del estado y si tiene padre
        Vector3 basePosition;
        if (Application.isPlaying)
        {
            // En tiempo de ejecución, usar la posición del padre + posición local inicial
            if (transform.parent != null)
            {
                basePosition = transform.parent.position + initialLocalPosition;
            }
            else
            {
                basePosition = transform.position + (Vector3)(-initialLocalPosition);
            }
        }
        else
        {
            // En el editor, usar la posición actual del objeto
            basePosition = transform.position;
        }

        Vector3 worldAreaCenter = basePosition + (Vector3)areaCenter;

        // Configurar el color del gizmo
        Gizmos.color = gizmoColor;

        // Dibujar el contorno del área
        Vector3 topLeft = new Vector3(worldAreaCenter.x - areaSize.x / 2f,
            worldAreaCenter.y + areaSize.y / 2f, worldAreaCenter.z);
        Vector3 topRight = new Vector3(worldAreaCenter.x + areaSize.x / 2f,
            worldAreaCenter.y + areaSize.y / 2f, worldAreaCenter.z);
        Vector3 bottomLeft = new Vector3(worldAreaCenter.x - areaSize.x / 2f,
            worldAreaCenter.y - areaSize.y / 2f, worldAreaCenter.z);
        Vector3 bottomRight = new Vector3(worldAreaCenter.x + areaSize.x / 2f,
            worldAreaCenter.y - areaSize.y / 2f, worldAreaCenter.z);

        // Dibujar las líneas del rectángulo
        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
        Gizmos.DrawLine(bottomLeft, topLeft);

        // Dibujar una cruz en el centro
        float crossSize = 0.2f;
        Gizmos.DrawLine(worldAreaCenter + Vector3.up * crossSize,
            worldAreaCenter + Vector3.down * crossSize);
        Gizmos.DrawLine(worldAreaCenter + Vector3.left * crossSize,
            worldAreaCenter + Vector3.right * crossSize);

        // Dibujar área semitransparente
        Color fillColor = gizmoColor;
        fillColor.a = 0.1f;
        Gizmos.color = fillColor;
        Gizmos.DrawCube(worldAreaCenter, new Vector3(areaSize.x, areaSize.y, 0.01f));
    }
}
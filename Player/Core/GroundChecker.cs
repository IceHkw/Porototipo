using UnityEngine;

public class GroundChecker : MonoBehaviour
{
    [Header("Configuraci�n Raycast")]
    [Tooltip("Distancia hacia abajo para detectar suelo")]
    public float distanciaDeteccion = 0.1f;

    [Tooltip("N�mero de raycast horizontales (mayor precisi�n)")]
    public int numeroRaycast = 3;

    [Tooltip("Ancho del �rea de detecci�n")]
    public float anchoDeteccion = 0.8f;

    [Tooltip("Capas que cuentan como suelo")]
    public LayerMask capaSuelo = -1;

    // Estado
    private bool enSuelo = false;
    private bool estadoAnterior = false;

    // Eventos
    public System.Action<bool> OnGroundStateChanged;
    public System.Action OnGroundEntered;
    public System.Action OnGroundExited;

    void Update()
    {
        CheckGround();
    }

    void CheckGround()
    {
        bool sueloDetectado = false;
        Vector2 posicion = transform.position;

        // Hacer m�ltiples raycast para mayor precisi�n
        for (int i = 0; i < numeroRaycast; i++)
        {
            float offset = 0f;
            if (numeroRaycast > 1)
            {
                // Distribuir raycast a lo ancho del jugador
                float porcentaje = (float)i / (numeroRaycast - 1); // 0 a 1
                offset = (porcentaje - 0.5f) * anchoDeteccion; // -ancho/2 a +ancho/2
            }

            Vector2 origenRay = posicion + Vector2.right * offset;
            RaycastHit2D hit = Physics2D.Raycast(origenRay, Vector2.down, distanciaDeteccion, capaSuelo);

            if (hit.collider != null)
            {
                sueloDetectado = true;
                break; // Con que uno detecte, ya hay suelo
            }

            // Debug visual (opcional)
            Debug.DrawRay(origenRay, Vector2.down * distanciaDeteccion,
                         sueloDetectado ? Color.green : Color.red, 0.1f);
        }

        // Actualizar estado solo si cambi�
        if (sueloDetectado != estadoAnterior)
        {
            enSuelo = sueloDetectado;
            estadoAnterior = sueloDetectado;

            OnGroundStateChanged?.Invoke(enSuelo);

            if (enSuelo)
                OnGroundEntered?.Invoke();
            else
                OnGroundExited?.Invoke();
        }
    }

    // Propiedades p�blicas
    public bool EnSuelo => enSuelo;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = enSuelo ? Color.green : Color.red;
        Vector3 pos = transform.position;

        // Dibujar �rea de detecci�n
        for (int i = 0; i < numeroRaycast; i++)
        {
            float offset = 0f;
            if (numeroRaycast > 1)
            {
                float porcentaje = (float)i / (numeroRaycast - 1);
                offset = (porcentaje - 0.5f) * anchoDeteccion;
            }

            Vector3 inicio = pos + Vector3.right * offset;
            Vector3 fin = inicio + Vector3.down * distanciaDeteccion;
            Gizmos.DrawLine(inicio, fin);
        }
    }
}
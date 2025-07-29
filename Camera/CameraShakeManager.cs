using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Maneja el screenshake usando Cinemachine Impulse
/// Requiere: CinemachineImpulseSource component
/// </summary>
[RequireComponent(typeof(CinemachineImpulseSource))]
public class CameraShakeManager : MonoBehaviour
{
    [Header("Shake Settings")]
    [SerializeField] private float projectileShakeForce = 0.5f;  // Fuerza del shake al disparar
    [SerializeField] private float shakeDuration = 0.2f;         // Duraci�n del shake

    [Header("Advanced Settings (Optional)")]
    [SerializeField] private bool useRandomDirection = true;     // Si true, el shake es en direcci�n aleatoria
    [SerializeField] private Vector3 shakeDirection = Vector3.one; // Direcci�n del shake si no es aleatorio

    // Componente de Cinemachine
    private CinemachineImpulseSource impulseSource;

    // Singleton simple para f�cil acceso
    private static CameraShakeManager instance;
    public static CameraShakeManager Instance => instance;

    void Awake()
    {
        // Configurar singleton
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Obtener componente
        impulseSource = GetComponent<CinemachineImpulseSource>();

        // Configurar valores por defecto del ImpulseSource
        ConfigureImpulseSource();
    }

    void ConfigureImpulseSource()
    {
        // Configurar la definici�n del impulso para que sea suave
        if (impulseSource.ImpulseDefinition != null)
        {
            // Configurar la duraci�n
            impulseSource.ImpulseDefinition.TimeEnvelope.AttackTime = 0.05f;  // Tiempo para llegar al m�ximo
            impulseSource.ImpulseDefinition.TimeEnvelope.SustainTime = shakeDuration * 0.3f; // Tiempo en el m�ximo
            impulseSource.ImpulseDefinition.TimeEnvelope.DecayTime = shakeDuration * 0.7f;   // Tiempo para decaer

            // Usar el perfil de disipaci�n predeterminado
            impulseSource.ImpulseDefinition.ImpulseDuration = shakeDuration;
        }
    }

    /// <summary>
    /// Activa el screenshake cuando se dispara un proyectil
    /// </summary>
    public void ShakeOnProjectileFire()
    {
        ShakeOnProjectileFire(projectileShakeForce);
    }

    /// <summary>
    /// Activa el screenshake con una fuerza espec�fica
    /// </summary>
    public void ShakeOnProjectileFire(float customForce)
    {
        if (impulseSource == null) return;

        Vector3 velocity;

        if (useRandomDirection)
        {
            // Generar direcci�n aleatoria en 2D (solo X e Y)
            velocity = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                0f
            ).normalized * customForce;
        }
        else
        {
            // Usar direcci�n configurada
            velocity = shakeDirection.normalized * customForce;
        }

        // Generar el impulso
        impulseSource.GenerateImpulse(velocity);
    }

    /// <summary>
    /// M�todo gen�rico para shake con par�metros personalizados
    /// </summary>
    public void Shake(float force, float duration, Vector3? direction = null)
    {
        // Actualizar duraci�n temporalmente
        float originalDuration = shakeDuration;
        shakeDuration = duration;
        ConfigureImpulseSource();

        // Aplicar shake
        if (direction.HasValue)
        {
            bool originalUseRandom = useRandomDirection;
            useRandomDirection = false;
            shakeDirection = direction.Value;
            ShakeOnProjectileFire(force);
            useRandomDirection = originalUseRandom;
        }
        else
        {
            ShakeOnProjectileFire(force);
        }

        // Restaurar duraci�n original
        shakeDuration = originalDuration;
        ConfigureImpulseSource();
    }

    // M�todos de utilidad para debugging
    void OnValidate()
    {
        // Asegurar valores m�nimos
        projectileShakeForce = Mathf.Max(0f, projectileShakeForce);
        shakeDuration = Mathf.Max(0.05f, shakeDuration);
    }

#if UNITY_EDITOR
    [ContextMenu("Test Projectile Shake")]
    void TestProjectileShake()
    {
        ShakeOnProjectileFire();
    }
#endif
}
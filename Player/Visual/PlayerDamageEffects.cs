using System.Collections;
using UnityEngine;

public class PlayerDamageEffects : MonoBehaviour
{
    [Header("Configuración de Invulnerabilidad")]
    public float tiempoInvulnerabilidad = 1.5f;
    public float intervaloBlink = 0.1f;

    [Header("Efectos Visuales")]
    public Color colorDaño = Color.red;
    public bool cambiarColor = true;
    public float intensidadColorDaño = 0.7f;

    [Header("Referencias")]
    public PlayerStats playerStats;
    public SpriteRenderer spriteRenderer;

    // Estado del sistema
    private bool esInvulnerable = false;
    private Color colorOriginal;

    // Eventos públicos
    public System.Action OnInvulnerabilityStart;
    public System.Action OnInvulnerabilityEnd;

    void Start()
    {
        InicializarComponentes();
    }

    void InicializarComponentes()
    {
        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
            colorOriginal = spriteRenderer.color;
    }

    public void TriggerDamageEffect()
    {
        if (!esInvulnerable)
        {
            StartCoroutine(EfectoDañoCoroutine());
        }
    }

    public bool PuedeRecibirDaño()
    {
        return !esInvulnerable;
    }

    IEnumerator EfectoDañoCoroutine()
    {
        esInvulnerable = true;
        OnInvulnerabilityStart?.Invoke();

        float tiempoTranscurrido = 0f;
        bool spriteVisible = true;

        // Cambiar color inicial si está habilitado
        if (cambiarColor)
        {
            Color colorMezclado = Color.Lerp(colorOriginal, colorDaño, intensidadColorDaño);
            spriteRenderer.color = colorMezclado;
        }

        // Efecto de parpadeo
        while (tiempoTranscurrido < tiempoInvulnerabilidad)
        {
            spriteVisible = !spriteVisible;

            if (cambiarColor)
            {
                Color colorActual = spriteVisible ?
                    Color.Lerp(colorOriginal, colorDaño, intensidadColorDaño) :
                    colorOriginal;
                spriteRenderer.color = colorActual;
            }
            else
            {
                Color colorActual = colorOriginal;
                colorActual.a = spriteVisible ? 1f : 0.3f;
                spriteRenderer.color = colorActual;
            }

            yield return new WaitForSeconds(intervaloBlink);
            tiempoTranscurrido += intervaloBlink;
        }

        // Restaurar estado normal
        spriteRenderer.color = colorOriginal;
        esInvulnerable = false;
        OnInvulnerabilityEnd?.Invoke();
    }

    public void DetenerEfectoDaño()
    {
        StopAllCoroutines();
        spriteRenderer.color = colorOriginal;
        esInvulnerable = false;
        OnInvulnerabilityEnd?.Invoke();
    }

    void OnDestroy()
    {
        StopAllCoroutines();
    }

    // Propiedades públicas esenciales
    public bool EsInvulnerable => esInvulnerable;
    public float TiempoInvulnerabilidad => tiempoInvulnerabilidad;
    public Color ColorOriginal => colorOriginal;
}
// Enemy/Fragments.cs

using UnityEngine;
using System.Collections;

public class FragmentLifecycle : MonoBehaviour
{
    // Estos valores serán transferidos desde EnemyVFX
    public float lifetime = 2f;
    public float fadeDuration = 0.5f;

    private SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            StartCoroutine(FadeAndDestroy());
        }
        else
        {
            // Si no hay renderer, simplemente destruir después del tiempo de vida
            Destroy(gameObject, lifetime);
        }
    }

    private IEnumerator FadeAndDestroy()
    {
        // 1. Esperar el tiempo de vida antes de desvanecer
        yield return new WaitForSeconds(lifetime);

        // 2. Desvanecer gradualmente
        float elapsed = 0f;
        Color startColor = spriteRenderer.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);

            spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

            yield return null;
        }

        // 3. Destruir el objeto
        Destroy(gameObject);
    }
}
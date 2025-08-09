// DamageIndicator.cs
using UnityEngine;
using TMPro;
using System.Collections;

public class DamageIndicator : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private TextMeshPro tmpText;

    [Header("Animación")]
    [SerializeField] private float lifetime = 1f;
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float fadeSpeed = 2f;
    [SerializeField] private Vector3 moveDirection = new Vector3(0.5f, 1, 0); // Movimiento diagonal ligero

    private Color startColor;

    void Awake()
    {
        if (tmpText == null)
        {
            tmpText = GetComponentInChildren<TextMeshPro>();
        }
    }

    /// <summary>
    /// Inicializa el indicador con el texto de daño.
    /// </summary>
    public void Initialize(int damageAmount)
    {
        if (tmpText != null)
        {
            tmpText.text = damageAmount.ToString();
            startColor = tmpText.color;
        }

        // Inicia la animación y la autodestrucción
        StartCoroutine(AnimateAndDestroy());
    }

    private IEnumerator AnimateAndDestroy()
    {
        float timer = 0f;

        while (timer < lifetime)
        {
            // Mover el texto hacia arriba
            transform.position += moveDirection * moveSpeed * Time.deltaTime;

            // Desvanecer el texto
            float alpha = Mathf.Lerp(1f, 0f, timer / lifetime);
            tmpText.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

            timer += Time.deltaTime;
            yield return null;
        }

        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.Return(gameObject);
        }
        else
        {
            Destroy(gameObject); // Fallback por si el pool no existe
        }
    }
}
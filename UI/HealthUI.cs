using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    public Image hearthPrefab;
    public Sprite fullHearthSprite;
    public Sprite emptyHearthSprite;

    private List<Image> hearths = new List<Image>();

    public int SetMaxHearths(int maxHearths)
    {
        foreach (Image hearth in hearths)
        {
            Destroy(hearth.gameObject);
        }

        hearths.Clear();

        for (int i = 0; i < maxHearths; i++)
        {
            Image newHearth = Instantiate(hearthPrefab, transform);
            newHearth.sprite = fullHearthSprite;
            newHearth.color = Color.white;
            hearths.Add(newHearth);
        }

        return maxHearths;
    }

    public void UpdateHearts(int currentHealth) 
    {
        for (int i = 0; i < hearths.Count; i++)
        {
            if (i < currentHealth)
            {
                hearths[i].sprite = fullHearthSprite;
                hearths[i].color = Color.white;
            }
            else
            {
                hearths[i].sprite = emptyHearthSprite;
                hearths[i].color = Color.black;
            }
        }
    }

}

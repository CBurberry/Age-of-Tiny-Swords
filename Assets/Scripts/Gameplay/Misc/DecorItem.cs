using NaughtyAttributes;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class DecorItem : MonoBehaviour
{
    public DecorItemData Data;

    [OnValueChanged("Inspector_OnValueChanged")]
    [Dropdown("GetSpriteNames")]
    public string Selected;

    [SerializeField]
    private SpriteRenderer spriteRenderer;

    private List<string> GetSpriteNames()
    {
        if (Data == null || Data.Decorations.Count == 0) 
        {
            return new List<string>() { string.Empty };
        }

        List<string> names = new List<string>();
        foreach (var sprite in Data.Decorations) 
        {
            names.Add(sprite.name);
        }

        return names;
    }

    private void Inspector_OnValueChanged()
    {
        if (Data == null || Data.Decorations.Count == 0)
        {
            spriteRenderer.sprite = null;
        }

        spriteRenderer.sprite = Data.Decorations.FirstOrDefault(x => x.name == Selected);
    }
}

using NaughtyAttributes;
using UnityEngine;

/// <summary>
/// Used to show/hide/set a sprite above a pawn's head when resources are being carried. Visual only.
/// </summary>
public class HeldResourcesVisual : MonoBehaviour
{
    [OnValueChanged("Inspector_OnResourceTypeChanged")]
    [SerializeField]
    private ResourceType resourceType;

    [SerializeField]
    private SpriteRenderer spriteRenderer;

    //POLISH/TODO: Add option to display a stack of multiple items (same or multiple carried, like on the TinySwords page)

    [Foldout("Config")]
    [SerializeField]
    private Sprite foodSingle;

    [Foldout("Config")]
    [SerializeField]
    private Vector3 foodSingleOffset;

    [Foldout("Config")]
    [SerializeField]
    private Vector3 foodSingleEuler;

    [Foldout("Config")]
    [SerializeField]
    private Sprite goldSingle;

    [Foldout("Config")]
    [SerializeField]
    private Vector3 goldSingleOffset;

    [Foldout("Config")]
    [SerializeField]
    private Vector3 goldSingleEuler;

    [Foldout("Config")]
    [SerializeField]
    private Sprite woodSingle;

    [Foldout("Config")]
    [SerializeField]
    private Vector3 woodSingleOffset;

    [Foldout("Config")]
    [SerializeField]
    private Vector3 woodSingleEuler;

    private void Awake()
    {
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    private void Start()
    {
        SetResource(resourceType);
    }

    public void SetResource(ResourceType resourceType)
    {
        switch (resourceType)
        {
            case ResourceType.Food:
                spriteRenderer.sprite = foodSingle;
                transform.localPosition = foodSingleOffset;
                transform.localEulerAngles = foodSingleEuler;
                break;
            case ResourceType.Gold:
                spriteRenderer.sprite = goldSingle;
                transform.localPosition = goldSingleOffset;
                transform.localEulerAngles = goldSingleEuler;
                break;
            case ResourceType.Wood:
                spriteRenderer.sprite = woodSingle;
                transform.localPosition = woodSingleOffset;
                transform.localEulerAngles = woodSingleEuler;
                break;
            default: 
                break;
        }
    }

    public void Show()
    {
        spriteRenderer.enabled = true;
    }

    public void Hide()
    {
        spriteRenderer.enabled = false;
    }

    private void Inspector_OnResourceTypeChanged() => SetResource(resourceType);
}

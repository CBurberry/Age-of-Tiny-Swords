using AYellowpaper.SerializedCollections;
using NaughtyAttributes;
using UnityEngine;

/// <summary>
/// Represents a resource item stack in the world. When dropped by pawns or other entities.
/// When picked up by an entity, it should be destroyed when empty.
/// </summary>
public class ResourceItem : AUnitInteractableNonUnit, IResourceSource
{
    //Maximum amount of a resource that this one item can hold
    public const int MaxResourceAmount = 10;
    public int ResourceCount => currentResourceAmount;
    public ResourceType ResourceType => resourceType;

    public bool IsDepleted => currentResourceAmount == 0;

    [OnValueChanged("Inspector_OnResourceTypeChanged")]
    [SerializeField]
    private ResourceType resourceType;

    [SerializeField]
    private bool spawnOnStart;

    [SerializeField]
    private SerializedDictionary<ResourceType, RuntimeAnimatorController> animationControllers;

    [SerializeField]
    private SerializedDictionary<ResourceType, Sprite> resourceSprite;

    [SerializeField]
    private Animator animator;

    [SerializeField]
    private SpriteRenderer spriteRenderer;

    [SerializeField]
    private int currentResourceAmount;

    private void Awake()
    {
        spriteRenderer.enabled = false;
        animator.runtimeAnimatorController = null;
    }

    private void Start()
    {
        if (spawnOnStart) 
        {
            Spawn(resourceType, MaxResourceAmount);
        }
    }

    public override UnitInteractContexts GetApplicableContexts(SimpleUnit unit)
    {
        if (unit is not PawnUnit)
        {
            return UnitInteractContexts.None;
        }
        else
        {
            PawnUnit pawn = unit as PawnUnit;
            if (!pawn.CanCarryResources)
            {
                return UnitInteractContexts.None;
            }
            else
            {
                return UnitInteractContexts.Gather;
            }
        }
    }

    public void Spawn(ResourceType type, int amount)
    {
        if (amount == 0) 
        {
            return;
        }

        //TODO: Add some vfx to make the spawning a bit less strange

        gameObject.name = "ResourceStack_" + type;

        spriteRenderer.sprite = null;
        spriteRenderer.enabled = true;
        animator.StopPlayback();
        animator.runtimeAnimatorController = animationControllers[type];

        resourceType = type;
        currentResourceAmount = amount;
    }

    public int Collect(int requestedAmount)
    {
        if (!spriteRenderer.enabled) 
        {
            return 0;
        }

        if (requestedAmount >= currentResourceAmount)
        {
            spriteRenderer.enabled = false;
            Destroy(gameObject);
            return currentResourceAmount;
        }
        else 
        {
            currentResourceAmount -= requestedAmount;
            return requestedAmount;
        }
    }

    private void Inspector_OnResourceTypeChanged()
    {
        //Update the preview sprite if the animation is not playing aka edit mode
        if (!Application.isPlaying)
        {
            spriteRenderer.sprite = resourceSprite[resourceType];
        }
        else 
        {
            animator.StopPlayback();
            animator.runtimeAnimatorController = animationControllers[resourceType];
        }
    }

    [Button("Spawn (PlayMode)", EButtonEnableMode.Playmode)]
    private void SpawnMax()
    {
        Spawn(resourceType, MaxResourceAmount);
    }
}

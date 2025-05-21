using UnityEngine;

public class Tree : ABaseUnitInteractable, IResourceSource
{
    public bool IsDepleted => currentWood == 0;

    [SerializeField]
    private Animator animator;

    [SerializeField]
    private int maxWood = 100;

    [SerializeField]
    private int currentWood;

    private const string ANIMATION_BOOL_DEAD = "IsDead";
    private const string ANIMATION_TRIGGER_HIT= "OnHit";

    private void Awake()
    {
        currentWood = maxWood;
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

    /// <summary>
    /// Gather wood from this tree each chop.
    /// </summary>
    /// <param name="maxAmount">Max amount gathered per hit</param>
    /// <returns>Actual amount recieved (e.g. if tree is near dead)</returns>
    public int Chopped(int maxAmount)
    {
        animator.SetTrigger(ANIMATION_TRIGGER_HIT);

        int gathered = currentWood > maxAmount ? maxAmount : currentWood;
        currentWood = Mathf.Clamp(currentWood - maxAmount, 0, maxWood);

        if (IsDepleted) 
        {
            animator.SetBool(ANIMATION_BOOL_DEAD, true);
        }

        return gathered;
    }
}

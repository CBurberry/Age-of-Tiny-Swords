using NaughtyAttributes;
using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class Sheep : AUnitInteractableUnit
{
    [SerializeField]
    private GameObject resourcePrefab;

    [SerializeField]
    private bool randomizeMaxFoodAmount;

    [HideIf("randomizeMaxFoodAmount")]
    [SerializeField]
    private int maxFoodAmount;

    [SerializeField]
    private int foodAmount;

    [ShowIf("randomizeMaxFoodAmount")]
    [SerializeField]
    [MinMaxSlider(80, 200)]
    private Vector2Int maxFoundAmountRange;

    protected override void Awake()
    {
        base.Awake();
        currentHp = maxHp;
        foodAmount = randomizeMaxFoodAmount ? Random.Range(maxFoundAmountRange.x, maxFoundAmountRange.y) : maxFoodAmount;
    }

    protected override void Update()
    {
        //TODO: Add a movement routine such that sheeps randomly pick a point near them to move to every so often
        //      Also to move away from an attacker when attacked
    }

    //Sheep drops a food prefab(s) when killed
    public override UnitInteractContexts GetApplicableContexts(SimpleUnit unit)
        => UnitInteractContexts.Attack;

    //POLISH/TODO: Sheep should randmly decide to move about short distances
    public void RandomMove()
    {
        throw new NotImplementedException();
    }

    //POLISH/TODO: Sheep should try to move away from an attacker
    public void MoveAwayFrom(Vector3 position) 
    {
        throw new NotImplementedException();
    }

    [Button("TriggerDeath - Sheep (PlayMode)", EButtonEnableMode.Playmode)]
    protected override void TriggerDeath()
    {
        //Hide self
        spriteRenderer.enabled = false;

        GameObject gameObject = Instantiate(resourcePrefab, transform.position, Quaternion.identity, transform.parent);
        ResourceItem resourceItem = gameObject.GetComponent<ResourceItem>();
        resourceItem.Spawn(ResourceType.Food, foodAmount);
        Destroy(this.gameObject);
    }
}

using NaughtyAttributes;
using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class Sheep : AUnitInteractableUnit
{
    private const float IDLE_MOVE_TIME = 25f;

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

    private UnitIdleTimer idleTimer;
    private float randomOffset;

    protected override void Awake()
    {
        base.Awake();
        currentHp = maxHp;
        foodAmount = randomizeMaxFoodAmount ? Random.Range(maxFoundAmountRange.x, maxFoundAmountRange.y) : maxFoodAmount;
        idleTimer = GetComponent<UnitIdleTimer>();
        randomOffset = Random.Range(0f, 10f);
    }

    protected override void Update()
    {
        if (idleTimer.Timer > IDLE_MOVE_TIME + randomOffset) 
        {
            RandomMove();
        }
    }

    public override bool IsAttacking() => false;

    //Sheep drops a food prefab(s) when killed
    public override UnitInteractContexts GetApplicableContexts(SimpleUnit unit)
        => UnitInteractContexts.Attack;

    public void RandomMove()
    {
        Vector3 randomPoint = Random.insideUnitCircle;
        MoveTo(transform.position + new Vector3(randomPoint.x, randomPoint.y, 0f));
    }

    //Sheep should try to move away from an attacker
    public void MoveAwayFrom(Vector3 position)
    {
        Debug.Log("Baaaa!");
        Vector3 direction = (transform.position - position).normalized;
        MoveTo(transform.position + (direction * 0.5f));
    }

    [Button("TriggerDeath - Sheep (PlayMode)", EButtonEnableMode.Playmode)]
    protected override void TriggerDeath()
    {
        //Hide self
        spriteRenderer.enabled = false;

        GameObject gameObject = Instantiate(resourcePrefab, transform.position, Quaternion.identity,  GameManager.Instance.ResourcesParent);
        ResourceItem resourceItem = gameObject.GetComponent<ResourceItem>();
        resourceItem.Spawn(ResourceType.Food, foodAmount);
        Destroy(this.gameObject);
    }

    protected override void OnDamaged(IDamageable attacker)
    {
        base.OnDamaged(attacker);
        if (attacker != null) 
        {
            MoveAwayFrom((attacker as MonoBehaviour).transform.position);
        }
    }
}

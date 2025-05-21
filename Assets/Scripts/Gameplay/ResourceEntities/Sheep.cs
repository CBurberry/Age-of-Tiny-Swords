using NaughtyAttributes;
using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class Sheep : ABaseUnitInteractable, IDamageable
{
    public float HpAlpha => (float)currentHp / maxHp;

    [SerializeField]
    private UnitHealthBar healthBar;

    [SerializeField]
    private SpriteRenderer spriteRenderer;

    [SerializeField]
    private GameObject resourcePrefab;

    [SerializeField]
    private float moveSpeed;

    [SerializeField]
    private int maxHp;

    [SerializeField]
    private int currentHp;

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

    private void Awake()
    {
        currentHp = maxHp;
        foodAmount = randomizeMaxFoodAmount ? Random.Range(maxFoundAmountRange.x, maxFoundAmountRange.y) : maxFoodAmount;
    }

    private void Update()
    {
        //TODO: Add a movement routine such that sheeps randomly pick a point near them to move to every so often
        //      Also to move away from an attacker when attacked
    }

    //Sheep drops a food prefab(s) when killed
    public override UnitInteractContexts GetApplicableContexts(SimpleUnit unit)
        => UnitInteractContexts.Attack;

    public Vector3 GetClosestPosition(Vector3 position)
        => spriteRenderer.bounds.ClosestPoint(position);

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

    public void ApplyDamage(int value)
    {
        if (value == 0f)
        {
            return;
        }

        //NOTE: Should there be some kind of 'took damage' animation or effect? If so, play here.

        currentHp = Math.Clamp(currentHp - value, 0, maxHp);

        //Update health bar
        bool shouldShow = currentHp < maxHp && currentHp > 0f;
        healthBar.SetValue(HpAlpha);
        healthBar.gameObject.SetActive(shouldShow);

        if (currentHp <= 0f)
        {
            TriggerDeath();
        }
    }

    [Button("TriggerDeath (PlayMode)", EButtonEnableMode.Playmode)]
    private void TriggerDeath()
    {
        //Hide self
        spriteRenderer.enabled = false;

        //Replace this prefab with a spawned instance of the resource prefab
        GameObject gameObject = Instantiate(resourcePrefab, transform.position, Quaternion.identity, transform.parent);
        ResourceItem resourceItem = gameObject.GetComponent<ResourceItem>();
        resourceItem.Spawn(ResourceType.Food, foodAmount);
        Destroy(this.gameObject);
    }
}

using AYellowpaper.SerializedCollections;
using NaughtyAttributes;
using RuntimeStatics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GoldMine : AUnitInteractableNonUnit, IResourceSource
{
    public enum Status
    {
        Inactive,
        Active,
        Destroyed
    }

    public event Action OnDepleted;

    public bool IsDepleted => currentGold == 0;
    public bool IsBeingMined => state == Status.Active;
    public Status State => state;

    public SpriteRenderer SpriteRenderer;

    [OnValueChanged("Inspector_OnStateChanged")]
    [SerializeField]
    private Status state;

    [SerializeField]
    private int maxGold = 2000;

    [SerializeField]
    private int currentGold;

    [SerializeField]
    [Tooltip("How many times must the mine be hit to forcibly eject all pawns?")]
    [MinValue(3)]
    private int numberOfHitsToEjectPawns;

    [SerializeField]
    private SerializedDictionary<Status, Sprite> sprites;

    private int hitCount;
    private float miningCount => miners.Count;
    private List<SimpleUnit> miners;

    private void Awake()
    {
        miners = new List<SimpleUnit>();
        currentGold = maxGold;
        state = Status.Inactive;
    }

    private void Start()
    {
        hitCount = 0;
        SpriteRenderer.sprite = sprites[state];
    }

    public override UnitInteractContexts GetApplicableContexts(SimpleUnit unit)
    {
        if (unit is not PawnUnit)
        {
            //Hardcoding goblins as only one faction can mine
            if (unit.Faction == Player.Faction.Goblins && state == Status.Active)
            {
                //Attack to force exit of pawns
                return UnitInteractContexts.Attack;
            }
            else 
            {
                return UnitInteractContexts.None;
            }
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

    public void Attack(int hitCount = 1)
    {
        this.hitCount += hitCount;
        if (hitCount >= numberOfHitsToEjectPawns) 
        {
            DisplaceAllMiners();
            state = IsDepleted ? Status.Destroyed : Status.Inactive;
            SpriteRenderer.sprite = sprites[state];
            hitCount = 0;
        }
    }

    public void EnterMine(SimpleUnit unit)
    {
        if (IsDepleted) 
        {
            throw new InvalidOperationException($"[{nameof(GoldMine)}.{nameof(EnterMine)}]: Cannot enter a depleted mine!");
        }

        if (miners.Contains(unit)) 
        {
            throw new InvalidOperationException($"[{nameof(GoldMine)}.{nameof(EnterMine)}]: Miner is trying to enter the same mine it's already in!");
        }

        miners.Add(unit);
        state = Status.Active;
        SpriteRenderer.sprite = sprites[state];
    }

    public void ExitMine(SimpleUnit unit)
    {
        if (state == Status.Destroyed) 
        {
            return;
        }

        if (!miners.Contains(unit))
        {
            throw new InvalidOperationException($"[{nameof(GoldMine)}.{nameof(ExitMine)}]: Unit is trying to exit a mine its not inside of!");
        }

        miners.Remove(unit);
        if (miningCount == 0 && !IsDepleted) 
        {
            state = Status.Inactive;
            SpriteRenderer.sprite = sprites[state];
        }
    }

    /// <summary>
    /// Gather gold from this mine each call.
    /// </summary>
    /// <param name="maxAmount">Max amount gathered per hit</param>
    /// <returns>Actual amount recieved (e.g. if tree is near dead)</returns>
    public int Mined(int maxAmount)
    {
        if (maxAmount == 0) 
        {
            return 0;
        }

        if (IsDepleted) 
        {
            throw new InvalidOperationException($"[{nameof(GoldMine)}.{nameof(Mined)}]: Cannot mine a goldmine in '{state}' state!");
        }

        int gathered = currentGold > maxAmount ? maxAmount : currentGold;
        currentGold = Mathf.Clamp(currentGold - maxAmount, 0, maxGold);

        if (IsDepleted)
        {
            state = Status.Destroyed;
            DisplaceAllMiners();
            SpriteRenderer.sprite = sprites[state];
            OnDepleted?.Invoke();
        }

        return gathered;
    }

    [Button("DestroyMine (PlayMode)", EButtonEnableMode.Playmode)]
    public void DestroyMine()
    {
        state = Status.Destroyed;
        DisplaceAllMiners();
        StartCoroutine(KillAllMinersInMineCollapse());
        SpriteRenderer.sprite = sprites[state];
    }

    private IEnumerator KillAllMinersInMineCollapse()
    {
        yield return new WaitUntil(() => miners.All(x => x.IsRendererActive));
        foreach (var miner in miners) 
        {
            miner.ApplyDamage(1000, null);
        }
    }

    private void DisplaceAllMiners()
    {
        foreach (var unit in miners) 
        {
            Vector3 randomPoint = SpriteRenderer.bounds.GetRandomPointOnEdge();

            //N.B. Keep the same z position as we don't want to affect rendering
            unit.transform.position = new Vector3(randomPoint.x, randomPoint.y, unit.transform.position.z);
        }
    }

    private void Inspector_OnStateChanged()
    {
        SpriteRenderer.sprite = sprites[state];
    }
}

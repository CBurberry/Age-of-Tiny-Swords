using AYellowpaper.SerializedCollections;
using NaughtyAttributes;
using RuntimeStatics;
using System;
using System.Collections.Generic;
using UnityEngine;

public class GoldMine : ABaseUnitInteractable, IResourceSource
{
    public enum Status
    {
        Inactive,
        Active,
        Destroyed
    }

    public event Action OnDepleted;

    public bool IsDepleted => currentGold == 0;
    public Status State => state;

    public int MaxGatherers => throw new NotImplementedException();

    [OnValueChanged("Inspector_OnStateChanged")]
    [SerializeField]
    private Status state;

    [SerializeField]
    private int maxGold = 2000;

    [SerializeField]
    private int currentGold;

    [SerializeField]
    private SerializedDictionary<Status, Sprite> sprites;

    [SerializeField]
    private SpriteRenderer spriteRenderer;

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
        spriteRenderer.sprite = sprites[state];
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
        spriteRenderer.sprite = sprites[state];
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
            spriteRenderer.sprite = sprites[state];
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
            Depleted();
        }

        return gathered;
    }

    private void Depleted()
    {
        state = Status.Destroyed;
        spriteRenderer.sprite = sprites[state];
        DisplaceAllMiners();
        OnDepleted?.Invoke();
    }

    private void DisplaceAllMiners()
    {
        foreach (var unit in miners) 
        {
            Vector3 randomPoint = spriteRenderer.bounds.GetRandomPointOnEdge();

            //N.B. Keep the same z position as we don't want to affect rendering
            unit.transform.position = new Vector3(randomPoint.x, randomPoint.y, unit.transform.position.z);
        }
    }

    private void Inspector_OnStateChanged()
    {
        spriteRenderer.sprite = sprites[state];
    }
}

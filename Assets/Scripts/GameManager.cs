using System;
using UniRx;
using UnityEngine;
using static Player;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public Faction CurrentPlayerFaction => Faction.Knights;
    public Player Knights;
    public Player Goblins;
    public IObservable<bool> ObserveGameOver() => _gameOver;

    Subject<bool> _gameOver = new();

    [SerializeField]
    private bool enableGoblinAI;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else 
        {
            Debug.LogWarning($"[{nameof(GameManager)}.Awake]: Singleton duplicate!");
            Destroy(this);
        }
    }

    private void Start()
    {
        if (Knights == null || Goblins == null) 
        {
            throw new InvalidOperationException();
        }

        Knights.OnPlayerDied += OnPlayerDied;
        Goblins.OnPlayerDied += OnPlayerDied;

        if (Goblins.TryGetComponent(out GoblinAI AI)) 
        {
            AI.enabled = enableGoblinAI;
        }
    }

    private void OnPlayerDied(Faction diedFaction)
    {
        _gameOver.OnNext(diedFaction == Faction.Goblins);
    }

    public static Player GetPlayer(Faction faction)
    {
        switch (faction)
        {
            case Faction.Knights:
                return Instance.Knights;
            case Faction.Goblins:
                return Instance.Goblins;
            case Faction.None:
                return null;
            default:
                throw new ArgumentException($"{nameof(GameManager)}.{nameof(GetPlayer)}: Player for faction '{faction}' not found!");
        }
    }
}

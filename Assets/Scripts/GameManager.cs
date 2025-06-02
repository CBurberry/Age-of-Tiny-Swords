using Cysharp.Threading.Tasks;
using System;
using UniRx;
using UnityEngine;
using static Player;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public bool IsPaused => _paused.Value;
    public Faction CurrentPlayerFaction => Faction.Knights;
    public Player Knights;
    public Player Goblins;
    public IObservable<bool> ObserveGameOver() => _gameOver;
    public IObservable<bool> ObservePause() => _paused;

    Subject<bool> _gameOver = new();
    BehaviorSubject<bool> _paused = new(false);

    public Transform UnitsParent;
    public Transform BuildingsParent;
    public Transform ResourcesParent;

    [SerializeField]
    private bool enableGoblinAI;

    [SerializeField] 
    private Transform cameraResetTransform;

    [SerializeField]
    private FogOfWarManager fogOfWarManager;

    [SerializeField]
    private float initialRevealRadius;

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

        RevealStartingArea();
    }

    public void TogglePause()
    {
        if (!_paused.Value)
        {
            Time.timeScale = 0f;
        }
        else 
        {
            Time.timeScale = 1f;
        }

        _paused.OnNext(!_paused.Value);
    }

    private void OnPlayerDied(Faction diedFaction)
    {
        _gameOver.OnNext(diedFaction == Faction.Goblins);
    }

    private void RevealStartingArea()
    {
        if (initialRevealRadius > 0f) 
        {
            fogOfWarManager.UpdateArea(cameraResetTransform.position, initialRevealRadius).Forget();
        }
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

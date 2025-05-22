using System;
using UnityEngine;
using static Player;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public Player Knights;
    public Player Goblins;

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

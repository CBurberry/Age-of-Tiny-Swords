using System.Linq;
using UnityEngine;

public class Player : MonoBehaviour
{
    public enum Faction
    {
        None,
        Knights,
        Goblins
    }

    private const string PLAYER_TAG = "Player";

    public Faction Team => faction;

    [HideInInspector]
    public ResourceManager Resources;

    [SerializeField]
    private Faction faction;

    private void Awake()
    {
        Resources = GetComponent<ResourceManager>();
    }

    public static Player GetPlayerByName(string name)
        => GameObject.FindGameObjectsWithTag(PLAYER_TAG)
        .FirstOrDefault(x => x.name == name)
        .GetComponent<Player>();
}

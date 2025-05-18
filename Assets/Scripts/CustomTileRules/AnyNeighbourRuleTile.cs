
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Custom Rule Tile", menuName = "Tiles/Custom Rule Tile")]
public class AnyNeighbourRuleTile : RuleTile<AnyNeighbourRuleTile.Neighbor>
{
    public class Neighbor : RuleTile.TilingRule.Neighbor
    {
        public const int AnyTile = 3;  // Custom value for "any tile"
    }

    public override bool RuleMatch(int neighbor, TileBase tile)
    {
        switch (neighbor)
        {
            case Neighbor.This:
                return tile == this;
            case Neighbor.NotThis:
                return tile != this;
            case Neighbor.AnyTile:
                return tile != null;  // Matches any tile present
            default:
                return base.RuleMatch(neighbor, tile);
        }
    }
}

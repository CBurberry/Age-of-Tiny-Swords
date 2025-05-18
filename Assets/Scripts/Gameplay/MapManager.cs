using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using static UnityEditor.PlayerSettings;

public class MapManager : MonoBehaviour
{
    [SerializeField, Layer] int _waterLayer;
    [SerializeField, Layer] int _landLayer;
    RaycastHit2D[] _hits;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            worldPos.z = 0;
            _hits = Physics2D.RaycastAll(worldPos, Vector2.zero);

            CheckIsLand(worldPos);
            CheckIsWater(worldPos);
        }
    }

    public bool CheckIsWater(Vector3 pos)
    {
        _hits = Physics2D.RaycastAll(pos, Vector2.zero, 100f, 1 << _waterLayer);
        return _hits.Length > 0;
    }

    public bool CheckIsLand(Vector3 pos)
    {
        _hits = Physics2D.RaycastAll(pos, Vector2.zero, 100f, 1 << _landLayer);
        return _hits.Length > 0;
    }
}

using System;
using UnityEngine;

public class AProjectile : MonoBehaviour
{
    public Vector3 TravelVector;
    public float Speed;
    public Action OnComplete;
    public SpriteRenderer SpriteRenderer;
}

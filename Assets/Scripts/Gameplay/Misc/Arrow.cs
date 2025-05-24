using System;
using UnityEngine;

public class Arrow : AProjectile
{
    public float Ttl;
    public Vector3 Direction;
    public float Speed;

    public Action OnComplete;

    private float elapsedTime;

    [SerializeField]
    private SpriteRenderer spriteRenderer;

    private void OnEnable()
    {
        elapsedTime = 0f;
        spriteRenderer.enabled = true;
    }

    private void Update()
    {
        if (elapsedTime < Ttl)
        {
            transform.Translate(Direction * Speed * Time.deltaTime);
            elapsedTime += Time.deltaTime;
        }
        else 
        {
            spriteRenderer.enabled = false;
            OnComplete?.Invoke();
        }
    }
}

using Animancer;
using System;
using System.Linq;
using UnityEngine;

public class TNT : AProjectile
{
    public float Ttl;
    public Vector3 Direction;
    public float Speed;
    public int Damage;
    public float ExplosionRadius;

    public Action OnComplete;

    private float elapsedTime;

    [SerializeField]
    private float rotationSpeed;

    [SerializeField]
    private SpriteRenderer spriteRenderer;

    [SerializeField]
    private SoloAnimation soloAnimation;

    private void OnEnable()
    {
        elapsedTime = 0f;
        spriteRenderer.enabled = true;
        soloAnimation.Play();
    }

    private void Update()
    {
        if (elapsedTime < Ttl)
        {
            transform.Rotate(new Vector3(0f, 0f, (spriteRenderer.flipX ? 1f : -1f)) * Time.deltaTime * rotationSpeed, Space.Self);
            transform.Translate(Direction * Speed * Time.deltaTime, Space.World);
            elapsedTime += Time.deltaTime;
        }
        else if (spriteRenderer.enabled)
        {
            Explode();
            spriteRenderer.enabled = false;
        }
    }

    private void Explode()
    {
        //TODO: Play explosion animation, scale to the damage radius

        //Get all enemies in a radius around this unit and apply damage to them
        var damageables = Physics2D.OverlapCircleAll(transform.position, ExplosionRadius)
            .Select(x => x.gameObject.GetComponent<IDamageable>())
            .Where(x => x != null);

        foreach (var element in damageables)
        {
            element.ApplyDamage(Damage);
        }

        OnComplete?.Invoke();
    }
}

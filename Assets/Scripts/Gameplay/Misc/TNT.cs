using Animancer;
using System.Linq;
using UnityEngine;

public class TNT : AProjectile
{
    public int Damage;
    public float ExplosionRadius;

    [SerializeField]
    private float rotationSpeed;

    [SerializeField]
    private SoloAnimation soloAnimation;

    private Vector3 startPos;
    private float alpha;

    private void OnEnable()
    {
        alpha = 0f;
        startPos = transform.position;
        SpriteRenderer.enabled = true;
        soloAnimation.Play();
    }

    private void Update()
    {
        if (alpha < 1f)
        {
            transform.Rotate(new Vector3(0f, 0f, (SpriteRenderer.flipX ? 1f : -1f)) * Time.deltaTime * rotationSpeed, Space.Self);
            alpha = Mathf.Clamp(alpha + (Time.deltaTime * Speed), 0f, 1f);
            transform.position = startPos + (TravelVector * alpha);
        }
        else
        {
            OnComplete?.Invoke();
            SpriteRenderer.enabled = false;
        }
    }

    public void Explode()
    {
        //TODO: Play explosion animation, scale to the damage radius

        //Get all enemies in a radius around this unit and apply damage to them
        var damageables = Physics2D.OverlapCircleAll(transform.position, ExplosionRadius)
            .Select(x => x.gameObject.GetComponent<IDamageable>())
            .Where(x => x != null);

        foreach (var element in damageables)
        {
            element?.ApplyDamage(Damage);
        }
    }
}

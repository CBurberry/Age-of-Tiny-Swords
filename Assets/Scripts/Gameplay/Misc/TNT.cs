using Animancer;
using System.Linq;
using UnityEngine;

public class TNT : AProjectile
{
    public int Damage;
    public float ExplosionRadius;

    [SerializeField]
    private Explosion explosionPrefab;

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
        //Play explosion animation
        Instantiate(explosionPrefab, transform.position, Quaternion.identity, transform.parent);

        //Get all enemies in a radius around this unit and apply damage to them
        var hitTargets = Physics2D.OverlapCircleAll(transform.position, ExplosionRadius)
            .Where(x => x != null && (x.gameObject.TryGetComponent<IDamageable>(out _) || x.gameObject.TryGetComponent<GoldMine>(out _)));

        foreach (var colliders in hitTargets)
        {
            if (colliders.gameObject.TryGetComponent(out IDamageable damageable))
            {
                damageable.ApplyDamage(Damage);
            }

            if (colliders.gameObject.TryGetComponent(out GoldMine mine))
            {
                if (mine.IsBeingMined) 
                {
                    //Setting a higher value to force miners out in one hit
                    mine.Attack(5);
                }
            }
        }
    }
}

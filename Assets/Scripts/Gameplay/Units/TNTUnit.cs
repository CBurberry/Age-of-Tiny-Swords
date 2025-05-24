using UnityEngine;

public class TNTUnit : RangedUnit
{
    //Get the facing direction of the unit (based on animation state)
    //return the center boundary of the side representing the facing direction
    protected override void SetProjectileSpawnPoint(AProjectile projectile)
    {
        //Get the facing direction of the unit (based on flipX)
        //return the center boundary of the side representing the facing direction
        float xPos = 
            spriteRenderer.flipX ? transform.position.x - spriteRenderer.bounds.extents.x 
            : transform.position.x + spriteRenderer.bounds.extents.x;

        projectile.transform.position = new Vector3(xPos, spriteRenderer.bounds.center.y, spriteRenderer.transform.position.z);
    }

    //Set rotation either through facing enum switch or flip
    protected override void SetProjectileRotation(AProjectile projectile, Vector3 direction)
    {
        //NOTE: We assume that our unit has already turned to face the target
        //Check our sprite renderer, apply the same FlipX value.
        projectile.SpriteRenderer.flipX = spriteRenderer.flipX;
    }

    //Set properties like damage radius etc which aren't common to base projectile
    protected override void SetOtherProjectileProperties(AProjectile projectile)
    {
        TNT dynamite = projectile as TNT;
        dynamite.Damage = data.BaseAttackDamage;
        dynamite.OnComplete = () =>
        {
            dynamite.Explode();
            prefabsPool.Release(projectile);
        };
    }
}

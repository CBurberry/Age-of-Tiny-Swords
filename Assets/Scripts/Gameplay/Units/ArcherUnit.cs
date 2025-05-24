using UnityEngine;

public class ArcherUnit : RangedUnit
{
    private const string ANIMATION_INT_FACING = "FacingDirection";

    //Represents a 1-1 match to the int value in the animator (use flipX for left facing)
    private enum FacingDirection
    {
        Front = 0,
        Down = 1,
        Up = 2,
        DiagonalDown = 3,
        DiagonalUp = 4
    }

    //Get the facing direction of the unit (based on animation state)
    //return the center boundary of the side representing the facing direction
    protected override void SetProjectileSpawnPoint(AProjectile projectile)
    {
        Vector3 offset = Vector3.zero;
        Bounds localBounds = spriteRenderer.localBounds;
        bool isRightFacing = !spriteRenderer.flipX;
        FacingDirection animFacing = (FacingDirection)animator.GetInteger(ANIMATION_INT_FACING);
        if (isRightFacing)
        {
            switch (animFacing)
            {
                case FacingDirection.Down:
                    offset = new Vector3(localBounds.center.x, localBounds.center.y - localBounds.extents.y, 0f);
                    break;
                case FacingDirection.Up:
                    offset = new Vector3(localBounds.center.x, localBounds.center.y + localBounds.extents.y, 0f);
                    break;
                case FacingDirection.Front:
                    offset = new Vector3(localBounds.center.x + localBounds.extents.x, localBounds.center.y, 0f);
                    break;
                case FacingDirection.DiagonalDown:
                    offset = new Vector3(localBounds.center.x + localBounds.extents.x, localBounds.center.y - localBounds.extents.y, 0f);
                    break;
                case FacingDirection.DiagonalUp:
                    offset = new Vector3(localBounds.center.x + localBounds.extents.x, localBounds.center.y + localBounds.extents.y, 0f);
                    break;
            }
        }
        else 
        {
            switch (animFacing)
            {
                case FacingDirection.Down:
                    offset = new Vector3(localBounds.center.x, transform.position.y - localBounds.extents.y, 0f);
                    break;
                case FacingDirection.Up:
                    offset = new Vector3(localBounds.center.x, transform.position.y + localBounds.extents.y, 0f);
                    break;
                case FacingDirection.Front:
                    offset = new Vector3(localBounds.center.x - localBounds.extents.x, localBounds.center.y, 0f);
                    break;
                case FacingDirection.DiagonalDown:
                    offset = new Vector3(localBounds.center.x - localBounds.extents.x, localBounds.center.y - localBounds.extents.y, 0f);
                    break;
                case FacingDirection.DiagonalUp:
                    offset = new Vector3(localBounds.center.x - localBounds.extents.x, localBounds.center.y + localBounds.extents.y, 0f);
                    break;
            }
        }

        projectile.transform.position = transform.position + offset;
    }

    //Set properties like damage radius etc which aren't common to base projectile
    protected override void SetOtherProjectileProperties(AProjectile projectile)
    {
        projectile.OnComplete = () =>
        {
            (interactionTarget as IDamageable)?.ApplyDamage(data.BaseAttackDamage);
            prefabsPool.Release(projectile);
        };
    }
}
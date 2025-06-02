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
                    offset = new Vector3(localBounds.center.x, localBounds.center.y - localBounds.extents.y, 0f);
                    break;
                case FacingDirection.Up:
                    offset = new Vector3(localBounds.center.x, localBounds.center.y + localBounds.extents.y, 0f);
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
            if (IUnitInteractable.IsValid(interactionTarget))
            {
                (interactionTarget as IDamageable)?.ApplyDamage(data.BaseAttackDamage, this);
            }
            projectile.enabled = false;
            prefabsPool.Release(projectile);
        };
    }

    protected override void FaceTarget(float angle)
    {
        FacingDirection facing = (FacingDirection)animator.GetInteger(ANIMATION_INT_FACING);
        if (angle > -22.5f && angle <= 22.5f)
        {
            //Forward
            spriteRenderer.flipX = false;
            facing = FacingDirection.Front;
        }
        else if (angle > 22.5f && angle <= 67.5f)
        {
            //Forward diagonal up
            spriteRenderer.flipX = false;
            facing = FacingDirection.DiagonalUp;
        }
        else if (angle > 67.5f && angle <= 112.5f)
        {
            //Up
            spriteRenderer.flipX = false;
            facing = FacingDirection.Up;
        }
        else if (angle > 112.5f && angle <= 157.5f)
        {
            //Flipped Forward diagonal up
            spriteRenderer.flipX = true;
            facing = FacingDirection.DiagonalUp;
        }
        else if (angle > 157.5f && angle <= -157.5f)
        {
            //Flipped Forward
            spriteRenderer.flipX = true;
            facing = FacingDirection.Front;
        }
        else if (angle > -157.5f && angle <= -112.5f)
        {
            //Flipped Forward diagonal down
            spriteRenderer.flipX = true;
            facing = FacingDirection.DiagonalDown;
        }
        else if (angle > -112.5f && angle <= -67.5f)
        {
            //Down
            spriteRenderer.flipX = false;
            facing = FacingDirection.Down;
        }
        else if (angle > -67.5f && angle <= -22.5f)
        {
            //Forward diagonal down
            spriteRenderer.flipX = false;
            facing = FacingDirection.DiagonalDown;
        }

        animator.SetInteger(ANIMATION_INT_FACING, (int)facing);
    }
}
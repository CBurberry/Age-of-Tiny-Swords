using RuntimeStatics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Player;

public class GarrisonedRangedUnit : MonoBehaviour
{
    protected const string ANIMATION_BOOL_ATTACKING = "IsAttacking";

    public bool IsAttacking() => attackTarget != null;

    public int Damage;

    [SerializeField]
    protected AProjectile projectilePrefab;

    [SerializeField]
    protected Animator animator;

    [SerializeField]
    protected float projectileSpeed;

    [SerializeField]
    protected float attackSpeed;

    [SerializeField]
    protected float attackRange;

    [SerializeField]
    protected Faction faction;

    [SerializeField]
    protected float delayForTargetAcquisition;

    protected PrefabsPool<AProjectile> prefabsPool;

    protected IDamageable attackTarget;

    private float targetAcquireTimer;

    protected virtual void Awake()
    {
        targetAcquireTimer = 0f;
        prefabsPool = new PrefabsPool<AProjectile>(projectilePrefab, transform, 10);
    }

    protected virtual void Update()
    {
        //Check for enemies in the area, attack them until they leave range (or are killed)
        if (!IsAttacking()) 
        {
            targetAcquireTimer += Time.deltaTime;
            if (targetAcquireTimer > delayForTargetAcquisition)
            {
                CheckForNewEnemyTarget();
                targetAcquireTimer = 0f;
            }
        }
    }

    protected virtual void StartAttacking()
    {
        attackTarget.OnDeath += OnTargetKilled;
        StartCoroutine(Attacking());
    }

    protected virtual IEnumerator Attacking()
    {
        yield return null;
    }

    protected void CheckForNewEnemyTarget()
    {
        IEnumerable<IDamageable> potentialTargets = DamageableUtilities.GetDamageablesInArea(transform.position, attackRange, 
            (x) => x.Faction != Faction.None && x.Faction != faction && !x.IsKilled);

        //Select the closest one (underlying implemenation uses pre-sorted results)
        attackTarget = potentialTargets.FirstOrDefault();
        if (attackTarget != null) 
        {
            StartAttacking();
        }
    }

    protected bool IsTargetWithinRange(IDamageable target, out Vector3 closestPosition)
    {
        closestPosition = target.GetClosestPosition(transform.position);
        float magnitude = (closestPosition - transform.position).magnitude;
        return magnitude <= attackRange;
    }

    protected virtual void OnTargetKilled()
    {
        attackTarget = null;
        animator.SetBool(ANIMATION_BOOL_ATTACKING, false);
    }
}

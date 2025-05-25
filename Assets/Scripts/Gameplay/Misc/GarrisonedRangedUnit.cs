using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GarrisonedRangedUnit : MonoBehaviour
{
    protected const string ANIMATION_BOOL_ATTACKING = "IsAttacking";

    [SerializeField]
    protected Animator animator;

    public bool IsAttacking() => animator.GetBool(ANIMATION_BOOL_ATTACKING);
}

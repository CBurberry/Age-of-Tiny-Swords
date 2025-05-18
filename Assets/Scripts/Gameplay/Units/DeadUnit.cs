using Animancer;
using NaughtyAttributes;
using UnityEngine;

[RequireComponent(typeof(Animator), typeof(SpriteRenderer), typeof(SoloAnimation))]
public class DeadUnit : MonoBehaviour
{
    //For UI to show what died on selection
    public string UnitName { get; set; }

    [SerializeField]
    [Tooltip("Play burying animation after time and destroy?")]
    private bool buryAfterTime = true;

    [SerializeField]
    private AnimationClip deadClip;

    [ShowIf("buryAfterTime")]
    [SerializeField]
    private AnimationClip buriedClip;

    [ShowIf("buryAfterTime")]
    [SerializeField]
    [Tooltip("How long before a skull that has appeared then sinks into the ground and disappears?")]
    private float timeToBuried;

    private SoloAnimation soloAnimation;
    private float elapsedTime;

    private void Awake()
    {
        soloAnimation = GetComponent<SoloAnimation>();
    }

    private void Start()
    {
        elapsedTime = 0f;
        soloAnimation.Play(deadClip);
    }

    private void Update()
    {
        if (!buryAfterTime) 
        {
            return;
        }

        if (soloAnimation.Clip == deadClip)
        {
            if (soloAnimation.IsPlaying == false)
            {
                soloAnimation.Clip = buriedClip;
            }
        }
        else if (elapsedTime >= 0f)
        {
            elapsedTime += Time.deltaTime;
            if (elapsedTime > timeToBuried)
            {
                soloAnimation.Play(buriedClip);
                elapsedTime = -1f;
            }
        }
        else 
        {
            if (soloAnimation.IsPlaying == false) 
            {
                Destroy(gameObject);
            }
        }
    }
}
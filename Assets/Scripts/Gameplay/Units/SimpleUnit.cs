using NaughtyAttributes;
using Pathfinding;
using RuntimeStatics;
using System;
using System.Linq;
using UniRx;
using UnityEngine;
using static Player;
using static UnityEngine.GraphicsBuffer;


[RequireComponent(typeof(Seeker))]
[RequireComponent(typeof(AILerp))]
/// <summary>
/// Base class governing behaviours that all units share e.g. Movement & Death.
/// 
/// This class should coordinate between the prefab's Animator, setting triggers, and processing Movement control 
/// </summary>
public class SimpleUnit : MonoBehaviour, IDamageable
{
    public static event Action<Faction> OnAnyUnitSpawned;
    public static event Action<Faction> OnAnyUnitDied;
    public event Action OnDeath;

    protected const string ANIMATION_BOOL_MOVING = "IsMoving";

    BehaviorSubject<Vector3?> _targetPos = new(null);

    public Faction Faction => faction;
    public bool IsKilled => currentHp == 0;
    public float HpAlpha => (float)currentHp / maxHp;
    public bool IsRendererActive => spriteRenderer.enabled;

    [SerializeField]
    [Expandable]
    protected UnitData data;

    [SerializeField]
    private UnitHealthBar healthBar;

    [SerializeField]
    private bool spawnDeadPrefab;

    [ShowIf("spawnDeadPrefab")]
    [SerializeField]
    private DeadUnit deadPrefab;

    [SerializeField]
    private GameObject unitSelectedMarker;

    private Faction faction;
    protected float moveSpeed => data.MovementSpeed;
    protected int maxHp => data.MaxHp;
    [ShowNonSerializedField]
    protected int currentHp;
    protected bool isMoving { get; private set; }
    protected IUnitInteractable interactionTarget;

    //These components should be on the prefab root, along with this script
    protected Animator animator;
    protected SpriteRenderer spriteRenderer;

    [SerializeField]
    [Tooltip("FOR TESTING PURPOSES, BUTTON BELOW")]
    protected Transform moveToTargetTransform;    //FOR TESTING PURPOSES

    private Action onMoveToComplete;
    private CompositeDisposable _disposables = new();
    private AILerp _pathfinder;

    public GameObject UnitSelectedMarker => unitSelectedMarker;

    public IObservable<Vector3?> ObserveTargetPos() => _targetPos;

    protected virtual void Awake()
    {
        currentHp = maxHp;
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        //To allow for later overriding if needed
        faction = data.Faction;
        SetupPathfinder();
    }

    protected virtual void Start()
    {
        OnAnyUnitSpawned?.Invoke(faction);
    }

    protected virtual void OnDestroy()
    {
        _disposables.Clear();
        _pathfinder.onSearchPath -= UpdateDestintation;
    }

    protected virtual void Update()
    {
    }

    public virtual bool MoveTo(Transform target, Action onComplete = null, bool clearTarget = true, bool stopAtAttackDistance = false)
    {
        return MoveTo(target.transform.position, onComplete, clearTarget, stopAtAttackDistance);
    }

    public virtual bool MoveTo(Vector3 worldPosition, Action onComplete = null, bool clearTarget = true, bool stopAtAttackDistance = false)
    {
        if (clearTarget) 
        {
            //Clear the interaction target as we may be cancelling another action
            interactionTarget = null;
        }

        if (worldPosition == transform.position 
            || stopAtAttackDistance && (worldPosition - transform.position).magnitude <= data.AttackDistance) 
        {
            onComplete?.Invoke();
            Debug.Log($"Complete");
            return true;
        }


        onMoveToComplete = onComplete;
        _pathfinder.destination = worldPosition;
        _pathfinder.StopDistance = stopAtAttackDistance ? data.AttackDistance : AILerp.DEFAULT_STOP_DISTANCE;
        _targetPos.OnNext(worldPosition);
        _pathfinder.SearchPath();
        return true;
    }

    public virtual Vector3 GetClosestPosition(Vector3 position)
        => spriteRenderer.bounds.ClosestPoint(position);

    public virtual void EnterBuilding()
    {
        var collider = GetComponent<Collider2D>();
        if (collider != null) 
        {
            collider.enabled = false;
        }
        spriteRenderer.enabled = false;
    }

    public virtual void ExitBuilding()
    {
        var collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = true;
        }
        spriteRenderer.enabled = true;
    }

    public virtual void ApplyDamage(int value)
    {
        if (value == 0f)
        {
            return;
        }

        //NOTE: Should there be some kind of 'took damage' animation or effect? If so, play here.

        currentHp = Math.Clamp(currentHp - value, 0, maxHp);

        //Update health bar
        bool shouldShow = currentHp < maxHp && currentHp > 0f;
        healthBar.SetValue(HpAlpha);
        healthBar.gameObject.SetActive(shouldShow);

        if (currentHp <= 0f)
        {
            TriggerDeath();
        }
    }

    [Button("MoveToTransform (PlayMode)", EButtonEnableMode.Playmode)]
    protected virtual void MoveToTransform()
    {
        if (moveToTargetTransform != null)
        {
            MoveTo(moveToTargetTransform);
        }
        else
        {
            Debug.LogWarning("Transform reference is not set!");
        }
    }

    [Button("TriggerDeath (PlayMode)", EButtonEnableMode.Playmode)]
    protected virtual void TriggerDeath()
    {
        if (!spriteRenderer.enabled) 
        {
            return;
        }

        //Hide self
        spriteRenderer.enabled = false;

        if (spawnDeadPrefab) 
        {
            //Replace this prefab with a spawned instance of the death prefab
            DeadUnit deadUnit = Instantiate(deadPrefab, transform.position, Quaternion.identity, transform.parent);
            deadUnit.name = deadUnit.UnitName = name;
        }

        OnDeath?.Invoke();
        OnAnyUnitDied?.Invoke(faction);
        Destroy(gameObject);
    }

    void UpdateDestintation()
    {
        if (isMoving && interactionTarget != null)
        {
            try
            {
                _targetPos.OnNext((interactionTarget as MonoBehaviour).transform.position);
                _pathfinder.destination = _targetPos.Value.Value;
            }
            catch
            {
                interactionTarget = null;
            }
        }
    }

    void SetupPathfinder()
    {
        _pathfinder = GetComponent<AILerp>();
        _pathfinder.onSearchPath += UpdateDestintation;
        _pathfinder.speed = data.MovementSpeed;

        _pathfinder.ObservePathReached().Subscribe(pathReached =>
        {
            if (pathReached)
            {
                isMoving = false;
                animator.SetBool(ANIMATION_BOOL_MOVING, false);
                if (onMoveToComplete != null)
                {
                    var tempAction = onMoveToComplete;
                    onMoveToComplete = null;
                    tempAction?.Invoke();
                }
                _targetPos.OnNext(null);
            }
            else
            {
                isMoving = true;
                animator.SetBool(ANIMATION_BOOL_MOVING, true);
            }
        }).AddTo(_disposables);

        Observable.CombineLatest(
            _pathfinder.ObservePathReached().DistinctUntilChanged(),
            _pathfinder.ObserveMovementDirection().DistinctUntilChanged(),
            (pathReached, moveDirection) => { return pathReached ? Vector3.zero : moveDirection; }
        )
        .DistinctUntilChanged().Subscribe(direction =>
        {
            if (direction.sqrMagnitude == 0f)
                return;

            // prevent weird flipping while walking
            if (Mathf.Abs(direction.x) > 0.01f)
            {
                spriteRenderer.flipX = direction.x < 0f;
            }
        }).AddTo(_disposables);
    }

    Vector3 GetClosestPoint(Transform target)
    {
        //Get the pivot of the sprite renderer of the object, we need to filter by name as there may be others for VFX animations
        var spriteRenderers = target.GetComponentsInChildren<SpriteRenderer>(true);
        //Smelly code I know. We assume bottom center pivot (or similarily defined custom pivot)
        var spriteRenderer = spriteRenderers.FirstOrDefault(x => x.gameObject.name == "Visuals");
        if (spriteRenderer == null)
        {
            //Backup check for non-conforming objects
            spriteRenderer = spriteRenderers.FirstOrDefault();
        }

        Bounds bounds = spriteRenderer.bounds;

        //Set to bottom of bounds for simplicity
        bounds.center = new Vector3(bounds.center.x, bounds.center.y - bounds.extents.y, spriteRenderer.transform.position.z);
        bounds.extents = new Vector3(bounds.extents.x, 0f, 0f);
        return bounds.ClosestPoint(transform.position);
    }
}

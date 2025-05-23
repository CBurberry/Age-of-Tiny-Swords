using Cysharp.Threading.Tasks;
using UniDi;
using UniRx;
using UnityEngine;

public class PlayerInteractionManager : MonoBehaviour
{
    [Inject] InputManager _inputManager;
    [SerializeField] GameObject _targetIndicator;

    CompositeDisposable _disposables = new();
    CompositeDisposable _selectedUnitDisposable = new();
    RaycastHit2D[] _hits;
    BehaviorSubject<AUnitInteractableUnit> _selectedUnit = new(null);
    

    // Update is called once per frame
    void Awake()
    {
        SetupTargetIndicator();

        _inputManager.ObserveSingleSelect().Subscribe(TrySelectUnit).AddTo(_disposables);
        _inputManager.ObserveInteract().Subscribe(TryInteract).AddTo(_disposables);
        
    }

    void OnDestroy()
    {
        _disposables.Clear();
    }

    void TrySelectUnit(Vector2 pos)
    {
        _hits = Physics2D.RaycastAll(pos, Vector2.zero);
        _selectedUnit.OnNext(GetUnitFromHits());
    }

    void TryInteract(Vector2 pos)
    {
        if (_selectedUnit.Value == null)
            return;

        _hits = Physics2D.RaycastAll(pos, Vector2.zero);
        var interactableUnit = GetInteractableFromHits();

        if (interactableUnit != null)
        {
            _selectedUnit.Value.Interact(interactableUnit, _selectedUnit.Value.GetContexts());
            _selectedUnit.OnNext(null);
        }
        else
        {
            _selectedUnit.Value.MoveTo(pos);
        }
    }

    AUnitInteractableUnit GetUnitFromHits()
    {
        foreach (RaycastHit2D hit in _hits)
        {
            if (hit.transform.TryGetComponent<AUnitInteractableUnit>(out var unit))
            {
                return unit;
            }
        }

        return null;
    }

    IUnitInteractable GetInteractableFromHits()
    {
        foreach (RaycastHit2D hit in _hits)
        {
            if (hit.transform.TryGetComponent<IUnitInteractable>(out var interactable))
            {
                return interactable;
            }
        }

        return null;
    }

    void TrySelectNonUnit(Vector2 pos)
    {
        // TODO
    }


    void SetupTargetIndicator()
    {
        if (_targetIndicator == null)
            return;

        _selectedUnit.Subscribe(selectedUnit =>
        {
            _selectedUnitDisposable.Clear();
            if (selectedUnit)
            {
                selectedUnit.ObserveTargetPos().DistinctUntilChanged().Subscribe(targetPos =>
                {
                    if (targetPos != null)
                    {
                        _targetIndicator.transform.position = targetPos.Value;
                        _targetIndicator.gameObject.SetActive(true);
                    }
                    else
                    {
                        _targetIndicator.gameObject.SetActive(false);
                    }

                }).AddTo(_selectedUnitDisposable);
            }
            else
            {
                _targetIndicator.gameObject.SetActive(false);
            }
        }).AddTo(_disposables);
    }
}

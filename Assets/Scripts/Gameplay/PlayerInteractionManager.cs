using Cysharp.Threading.Tasks;
using System;
using UniDi;
using UniRx;
using UnityEngine;

public class PlayerInteractionManager : MonoBehaviour
{
    [Inject] InputManager _inputManager;
    [SerializeField] GameObject _targetIndicator;
    [SerializeField] bool _allowSelectingEnemies;

    CompositeDisposable _disposables = new();
    CompositeDisposable _selectedUnitDisposable = new();
    RaycastHit2D[] _hits;
    BehaviorSubject<AUnitInteractableUnit> _selectedUnit = new(null);
    BehaviorSubject<SimpleBuilding> _selectedBuilding = new(null);

    public IObservable<SimpleBuilding> ObserveSelectedBuilding() => _selectedBuilding;

    // Update is called once per frame
    void Awake()
    {
        SetupTargetIndicator();

        _inputManager.ObserveSingleSelect().Subscribe(TrySelect).AddTo(_disposables);
        _inputManager.ObserveInteract().Subscribe(TryInteract).AddTo(_disposables);
    }

    void OnDestroy()
    {
        _disposables.Clear();
    }

    void TrySelect(Vector2 pos)
    {
        _hits = Physics2D.RaycastAll(pos, Vector2.zero);
        SetSlectedUnitMarkerActive(false);

        _selectedUnit.OnNext(GetUnitFromHits());

        if (_selectedUnit.Value == null)
        {
            _selectedBuilding.OnNext(GetBuildingFromHits());
        }

        SetSlectedUnitMarkerActive(true);
    }

    void SetSlectedUnitMarkerActive(bool active)
    {
        if (_selectedUnit.Value != null && _selectedUnit.Value.UnitSelectedMarker)
        {
            _selectedUnit.Value.UnitSelectedMarker.gameObject.SetActive(active);
        }

        //if (_selectedBuilding.Value != null && _selectedBuilding.Value.UnitSelectedMarker)
        //{
        //    _selectedUnit.Value.UnitSelectedMarker.gameObject.SetActive(active);
        //}
    }

    void TryInteract(Vector2 pos)
    {
        if (_selectedUnit.Value == null)
            return;

        _hits = Physics2D.RaycastAll(pos, Vector2.zero);
        var interactableUnit = GetInteractableFromHits();

        if (interactableUnit != null && _selectedUnit.Value.Interact(interactableUnit, _selectedUnit.Value.GetContexts()))
        {
            SetSlectedUnitMarkerActive(false);
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
            if (hit.transform.TryGetComponent<AUnitInteractableUnit>(out var unit) 
                && (_allowSelectingEnemies || unit.Faction == GameManager.Instance.CurrentPlayerFaction))
            {
                return unit;
            }
        }

        return null;
    }
    SimpleBuilding GetBuildingFromHits()
    {
        foreach (RaycastHit2D hit in _hits)
        {
            if (hit.transform.TryGetComponent<SimpleBuilding>(out var building)
                && building.Faction == GameManager.Instance.CurrentPlayerFaction)
            {
                return building;
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

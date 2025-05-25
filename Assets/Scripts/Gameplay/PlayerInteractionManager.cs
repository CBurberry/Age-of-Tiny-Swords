using Cysharp.Threading.Tasks;
using System;
using System.Linq;
using UniDi;
using UniRx;
using UniRx.Triggers;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerInteractionManager : MonoBehaviour
{
    [Inject] InputManager _inputManager;
    [SerializeField] GameObject _targetIndicator;
    [SerializeField] bool _allowSelectingEnemies;

    CompositeDisposable _disposables = new();
    CompositeDisposable _selectedUnitDisposable = new();
    CompositeDisposable _clearSelectedUnitDisposable = new();
    CompositeDisposable _clearBuildAreaDisposable = new();
    CompositeDisposable _clearBuildingDisposable = new();
    RaycastHit2D[] _hits;
    BehaviorSubject<AUnitInteractableUnit> _selectedUnit = new(null);
    BehaviorSubject<SimpleBuilding> _selectedBuilding = new(null);
    BehaviorSubject<BuildArea> _selectedBuildArea = new(null);

    public IObservable<SimpleBuilding> ObserveSelectedBuilding() => _selectedBuilding;
    public IObservable<BuildArea> ObserveSelectedBuildArea() => _selectedBuildArea;

    // Update is called once per frame
    void Awake()
    {
        SetupTargetIndicator();
        SetupClearOnDisable();

        _inputManager.ObserveSingleSelect().Subscribe(TrySelect).AddTo(_disposables);
        _inputManager.ObserveInteract().Subscribe(TryInteract).AddTo(_disposables);
    }

    void OnDestroy()
    {
        _disposables.Clear();
        _selectedUnitDisposable.Clear();
        _clearBuildAreaDisposable.Clear();
        _clearSelectedUnitDisposable.Clear();
        _clearBuildingDisposable.Clear();
    }

    void TrySelect(Vector2 pos)
    {
        _hits = Physics2D.RaycastAll(pos, Vector2.zero);
        SetSlectedUnitMarkerActive(false);

        AUnitInteractableUnit hitUnit = GetUnitFromHits();
        SimpleBuilding hitBuilding = null;
        BuildArea hitBuildArea = null;

        if (hitUnit == null)
        {
            hitBuilding = GetBuildingFromHits();
            if (hitBuilding == null)
            {
                hitBuildArea = GetBuildAreaFromHits();
            }
        }

        _selectedUnit.OnNext(hitUnit);
        _selectedBuilding.OnNext(hitBuilding);
        _selectedBuildArea.OnNext(hitBuildArea);

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

    BuildArea GetBuildAreaFromHits()
    {
        foreach (RaycastHit2D hit in _hits)
        {
            if (hit.transform.TryGetComponent<BuildArea>(out var buildArea))
            {
                return buildArea;
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

    void SetupClearOnDisable()
    {
        _selectedUnit.Subscribe(selectedUnit =>
        {
            _clearSelectedUnitDisposable.Clear();
            if (selectedUnit)
            {
                selectedUnit.gameObject.OnDisableAsObservable()
                   .Subscribe(x => _selectedUnit.OnNext(null))
                   .AddTo(_clearSelectedUnitDisposable);
            }
        }).AddTo(_disposables);

        _selectedBuildArea.Subscribe(buildArea =>
        {
            _clearBuildAreaDisposable.Clear();
            if (buildArea)
            {
                buildArea.gameObject.OnDisableAsObservable()
                   .Subscribe(x => _selectedBuildArea.OnNext(null))
                   .AddTo(_clearBuildAreaDisposable);
            }
        }).AddTo(_disposables);

        _selectedBuilding.Subscribe(buidling =>
        {
            _clearBuildingDisposable.Clear();
            if (buidling)
            {
                buidling.gameObject.OnDisableAsObservable()
                   .Subscribe(x => _selectedBuilding.OnNext(null))
                   .AddTo(_clearBuildingDisposable);
                buidling.ObserveBuildingState()
                    .Select(x => x == BuildingStates.Destroyed)
                    .Where(x => x)
                    .Subscribe(x => _selectedBuilding.OnNext(null))
                    .AddTo(_clearBuildingDisposable);
            }
        }).AddTo(_disposables);
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

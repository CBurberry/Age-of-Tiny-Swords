using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using UniDi;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

public class PlayerInteractionManager : MonoBehaviour
{
    [Inject] InputManager _inputManager;
    [SerializeField, Layer] int _charactersLayer;
    [SerializeField] GameObject _targetIndicator;
    [SerializeField] bool _allowSelectingEnemies;

    CompositeDisposable _disposables = new();
    CompositeDisposable _selectedUnitDisposable = new();
    CompositeDisposable _clearSelectedUnitDisposable = new();
    CompositeDisposable _clearBuildAreaDisposable = new();
    CompositeDisposable _clearBuildingDisposable = new();
    RaycastHit2D[] _hits;
    BehaviorSubject<List<AUnitInteractableUnit>> _selectedUnits = new(new List<AUnitInteractableUnit>());
    BehaviorSubject<SimpleBuilding> _selectedBuilding = new(null);
    BehaviorSubject<BuildArea> _selectedBuildArea = new(null);

    public IObservable<SimpleBuilding> ObserveSelectedBuilding() => _selectedBuilding;
    public IObservable<BuildArea> ObserveSelectedBuildArea() => _selectedBuildArea;

    void Awake()
    {
        SetupTargetIndicator();
        SetupClearOnDisable();

        _inputManager.ObserveSingleSelect().Subscribe(TrySelect).AddTo(_disposables);
        _inputManager.ObserveInteract().Subscribe(TryInteract).AddTo(_disposables);
        _inputManager.ObserveGroupSelect().Subscribe(TrySelectGroup).AddTo(_disposables);
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
        SetAllMarkersActive(false);

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

        var selectedUnits = _selectedUnits.Value;
        selectedUnits.Clear();
        if (hitUnit)
        {
            selectedUnits.Add(hitUnit);
            _selectedUnits.OnNext(selectedUnits);
        }

        _selectedBuilding.OnNext(hitBuilding);
        _selectedBuildArea.OnNext(hitBuildArea);

        SetAllMarkersActive(true);
    }

    void SetAllMarkersActive(bool active)
    {
        foreach (var selectedUnit in _selectedUnits.Value)
        {
            SetSlectedUnitMarkerActive(selectedUnit, active);
        }
    }

    void SetSlectedUnitMarkerActive(AUnitInteractableUnit selectedUnit, bool active)
    {
        if (selectedUnit.UnitSelectedMarker)
        {
            selectedUnit.UnitSelectedMarker.gameObject.SetActive(active);
        }
    }

    void TryInteract(Vector2 pos)
    {
        if (_selectedUnits.Value.Count == 0)
            return;

        _hits = Physics2D.RaycastAll(pos, Vector2.zero);
        var interactable = GetInteractableFromHits();
        var selectedUnits = _selectedUnits.Value;
        if (interactable != null)
        {
            for (int i = selectedUnits.Count - 1; i >= 0; i--)
            {
                var selectedUnit = selectedUnits[i];
                if (selectedUnit.Interact(interactable, selectedUnit.GetContexts()))
                {
                    selectedUnits.RemoveAt(i);
                    SetSlectedUnitMarkerActive(selectedUnit, false);
                }
            }
            _selectedUnits.OnNext(selectedUnits);
        }
        else
        {
            foreach (var selectedUnit in selectedUnits)
            {
                selectedUnit.MoveTo(pos);
            }
        }
    }

    (Vector2, Vector2) _cachedSelectionArea;

    void TrySelectGroup((Vector2, Vector2) selectionArea)
    {
        _cachedSelectionArea = selectionArea;
        Vector2 startPos = selectionArea.Item1;
        Vector2 endPos = selectionArea.Item2;
        SetAllMarkersActive(false);
        var size = endPos - startPos;
        size = new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.y));
        var colliders = Physics2D.OverlapBoxAll((startPos + endPos)/2f, size, 0, 1 << _charactersLayer);
        var colliders2 = Physics2D.OverlapBoxAll((startPos + endPos)/2f, size, 0);
        _hits = Physics2D.BoxCastAll(startPos, size, 0, Vector3.zero,1 << _charactersLayer);
        var selectedUnits = new List<AUnitInteractableUnit>();
        foreach (var collider in colliders)
        {
            if (collider.TryGetComponent<AUnitInteractableUnit>(out var unit) 
                && (_allowSelectingEnemies || unit.Faction == GameManager.Instance.CurrentPlayerFaction))
            {
                selectedUnits.Add(unit);
            }
        }
        _selectedUnits.OnNext(selectedUnits);
        _selectedBuilding.OnNext(null);
        _selectedBuildArea.OnNext(null);
        SetAllMarkersActive(true);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Vector2 startPos = _cachedSelectionArea.Item1;
        Vector2 endPos = _cachedSelectionArea.Item2;

        Gizmos.DrawLine(startPos, new Vector2(startPos.x, endPos.y));
        Gizmos.DrawLine(startPos, new Vector2(endPos.x, startPos.y));
        Gizmos.DrawLine(endPos, new Vector2(startPos.x, endPos.y));
        Gizmos.DrawLine(endPos, new Vector2(endPos.x, startPos.y));
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
        _selectedUnits.Subscribe(selectedUnits =>
        {
            _clearSelectedUnitDisposable.Clear();
            foreach(var selectedUnit in selectedUnits)
            {
                if (selectedUnit)
                {
                    selectedUnit.gameObject.OnDisableAsObservable()
                       .Subscribe(x =>
                       {
                           var currentUnits = _selectedUnits.Value;
                           currentUnits.Remove(selectedUnit);
                           _selectedUnits.OnNext(currentUnits);
                        })
                       .AddTo(_clearSelectedUnitDisposable);
                }
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

        _selectedUnits.Subscribe(selectedUnits =>
        {
            _selectedUnitDisposable.Clear();
            if (selectedUnits.Count != 1)
            {
                _targetIndicator.gameObject.SetActive(false);
                return;
            }

            var selectedUnit = selectedUnits[0];
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
        }).AddTo(_disposables);
    }
}

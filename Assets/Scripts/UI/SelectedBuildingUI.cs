using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UniDi;
using UniRx;
using UnityEngine;

public class SelectedBuildingUI : MonoBehaviour
{
    [Inject] PlayerInteractionManager _playerInteractionManager;

    [SerializeField] UnitCostUI _unitCostUIPrefab;
    [SerializeField] Transform _unitsParent;
    [SerializeField] Transform _buildQueueParent;
    [SerializeField] QueuedUnitUI _firstQueuedUnit;
    [SerializeField] QueuedUnitUI _queuedUnitUIPrefab;

    PrefabsPool<UnitCostUI> _unitCostPool;
    CompositeDisposable _disposables = new();
    CompositeDisposable _selectedBuildingDisposable = new();
    List<QueuedUnitUI> _queuedUnitUIItems = new();

    void Awake()
    {
        _unitCostPool = new(_unitCostUIPrefab, _unitsParent, 10);

        _queuedUnitUIItems.Add(_firstQueuedUnit);

        // start from 1 because we already have _firstQueuedUnit in scene
        for (int i = 1; i < SimpleBuilding.MAX_UNITS_QUEUE; i++)
        {
            var queuedUnitUI = Instantiate(_queuedUnitUIPrefab, _buildQueueParent);
            _queuedUnitUIItems.Add(queuedUnitUI);
        }

        _playerInteractionManager.ObserveSelectedBuilding().Subscribe(selectedBuilding =>
        {
            _selectedBuildingDisposable.Clear();  
            _unitCostPool.ReleaseAll();

            if (selectedBuilding == null)
            {
                gameObject.SetActive(false);
            }
            else
            {
                for (int i = 0; i < _queuedUnitUIItems.Count; i++)
                {
                    int index = i; // to avoid wrong value in observable callback
                    _queuedUnitUIItems[index].ObserveOnClick()
                        .Where(x => x != null)
                        .Subscribe(x => selectedBuilding.TryRemoveUnitFromQueue(index))
                        .AddTo(this);
                }

                selectedBuilding.ObserveUnitBuildQueue().Subscribe(queuedUnits =>
                {
                    for (int i = 0; i < SimpleBuilding.MAX_UNITS_QUEUE; i++)
                    {
                        UnitCost unitCost = null;

                        if (i < queuedUnits.Count)
                        {
                            unitCost = queuedUnits[i];
                        }

                        _queuedUnitUIItems[i].Setup(unitCost);
                    }
                }).AddTo(_disposables);


                selectedBuilding.ObserveUnitBuildProgress().DistinctUntilChanged().Subscribe(x =>
                {
                    _firstQueuedUnit.UpdateProgress(x);
                }).AddTo(_selectedBuildingDisposable);

                foreach (var unitCost in selectedBuilding.SpawnableUnits)
                {
                    var unitCostObject = _unitCostPool.Get();
                    unitCostObject.transform.SetAsLastSibling();
                    unitCostObject.Setup(unitCost);
                    unitCostObject.ObserveOnClick().Subscribe(x =>
                    {
                        selectedBuilding.TryAddUnitToQueue(unitCost);
                    }).AddTo(_selectedBuildingDisposable);
                }
                gameObject.SetActive(true);
            }
        }).AddTo(_disposables);
    }
    void OnDestoy()
    {
        _disposables.Clear();
        _selectedBuildingDisposable.Clear();
    }
}

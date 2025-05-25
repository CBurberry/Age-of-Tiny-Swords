using AYellowpaper.SerializedCollections;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UniDi;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.Progress;

public class SelectedBuildingUI : MonoBehaviour
{
    [Inject] PlayerInteractionManager _playerInteractionManager;

    [SerializeField] Image _buildingImage;
    [SerializeField] Image _constructionProgressFill;
    [SerializeField] SerializedDictionary<BuildingStates, GameObject[]> _objectsRequiredForState;
    [SerializeField] GameObject _unitsBuildingContainer;
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
                SetupQueuedUnitsUI(selectedBuilding);
                SetupUnitBuildProgress(selectedBuilding);
                SetupConstructionProgress(selectedBuilding);
                SetupSpawnableUnits(selectedBuilding);
                SetupBuildingState(selectedBuilding);

                gameObject.SetActive(true);
            }
        }).AddTo(_disposables);
    }
    void OnDestoy()
    {
        _disposables.Clear();
        _selectedBuildingDisposable.Clear();
    }

    void SetupSpawnableUnits(SimpleBuilding selectedBuilding)
    {
        if (selectedBuilding.SpawnableUnits.Count > 0)
        {
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
            _unitsBuildingContainer.gameObject.SetActive(true);
        }
        else
        {
            _unitsBuildingContainer.gameObject.SetActive(false);
        }
    }

    void SetupQueuedUnitsUI(SimpleBuilding selectedBuilding)
    {
        for (int i = 0; i < _queuedUnitUIItems.Count; i++)
        {
            int index = i; // to avoid wrong value in observable callback
            _queuedUnitUIItems[index].ObserveOnClick()
                .Where(x => x != null)
                .Subscribe(x => selectedBuilding.TryRemoveUnitFromQueue(index))
                .AddTo(_selectedBuildingDisposable);
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
        }).AddTo(_selectedBuildingDisposable);
    }

    void SetupUnitBuildProgress(SimpleBuilding selectedBuilding)
    {
        selectedBuilding.ObserveUnitBuildProgress().DistinctUntilChanged().Subscribe(x =>
        {
            _firstQueuedUnit.UpdateProgress(x);
        }).AddTo(_selectedBuildingDisposable);
    }

    void SetupConstructionProgress(SimpleBuilding selectedBuilding)
    {
        selectedBuilding.ObserveConstructionProgress().DistinctUntilChanged().Subscribe(x =>
        {
            _constructionProgressFill.fillAmount = x;
        }).AddTo(_selectedBuildingDisposable);
    }

    void SetupBuildingState(SimpleBuilding selectedBuilding)
    {
        selectedBuilding.ObserveBuildingState().Subscribe(buildingState =>
        {
            _buildingImage.sprite = selectedBuilding.Icon;

            // disable all first to avoid bugs
            foreach (var iter in _objectsRequiredForState)
            {
                foreach (var stateObjects in iter.Value)
                {
                    stateObjects.gameObject.SetActive(false);
                }
            }

            foreach (var stateObjects in _objectsRequiredForState[buildingState])
            {
                stateObjects.gameObject.SetActive(true);
            }
        }).AddTo(_selectedBuildingDisposable);

    }
}

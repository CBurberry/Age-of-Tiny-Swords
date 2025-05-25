using System.Collections.Generic;
using System.Linq;
using System.Resources;
using UniRx;
using UnityEngine;

public class BuildArea : MonoBehaviour
{
    [SerializeField] BuildAreaData _buildAreaData;
    [SerializeField] Transform _buildingsParent;

    ResourceManager _resourceManager;
    CompositeDisposable _buildingDisposable = new();
    SimpleBuilding _currentBuilding;
    public IReadOnlyList<ConstructionData> Constructions => _buildAreaData.Constructions;

    void Start()
    {
        _resourceManager = GameManager.GetPlayer(GameManager.Instance.CurrentPlayerFaction).Resources;
    }

    void OnDestroy()
    {
        _buildingDisposable.Clear();
    }

    public void Construct(ConstructionData constructionData)
    {
        if (!_buildAreaData.Constructions.Contains(constructionData))
        {
            return;
        }

        if (!_resourceManager.HaveResources(constructionData.Cost))
        {
            return;
        }

        _resourceManager.RemoveResources(constructionData.Cost);

        _buildingDisposable.Clear();
        if (_currentBuilding)
        {
            Destroy(_currentBuilding.gameObject);
        }

        _currentBuilding = Instantiate(constructionData.Prefab, transform.position, Quaternion.identity, _buildingsParent);
        _currentBuilding
            .ObserveBuildingState()
            .Select(x => x == BuildingStates.Destroyed)
            .Subscribe(isDestroyed =>
            {
                gameObject.SetActive(isDestroyed);
            })
            .AddTo(_buildingDisposable);
        _currentBuilding.Construct();
    }
}

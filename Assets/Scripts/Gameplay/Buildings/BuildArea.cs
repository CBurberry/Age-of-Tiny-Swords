using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using UniDi;
using UniRx;
using UnityEngine;

public class BuildArea : MonoBehaviour
{
    [Inject] FogOfWarManager _fogOfWarManager;
    [SerializeField] BuildAreaData _buildAreaData;
    [SerializeField] Transform _buildingsParent;

    BoxCollider2D _boxCollider;
    ResourceManager _resourceManager;
    CompositeDisposable _buildingDisposable = new();
    SimpleBuilding _currentBuilding;
    public IReadOnlyList<ConstructionData> Constructions => _buildAreaData.Constructions;

    private void Awake()
    {
        _boxCollider = GetComponent<BoxCollider2D>();
    }

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
        float moreThanHalf = 0.6f; // need to take more than half to cover a little bit more than a radius
        _fogOfWarManager.UpdateArea(
            transform.position + (Vector3)_boxCollider.offset, 
            Mathf.Max(_boxCollider.size.x, _boxCollider.size.y) * moreThanHalf).Forget();
    }
}

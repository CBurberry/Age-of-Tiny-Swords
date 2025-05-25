using AYellowpaper.SerializedCollections;
using System;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

public class ConstructionUI : MonoBehaviour
{
    [SerializeField] Button _button;
    [SerializeField] Image _buildingImage;
    [SerializeField] SerializedDictionary<ResourceType, ResourceCostUI> _resourceCosts;

    public IObservable<Unit> ObserveOnClick() => _button.OnClickAsObservable();

    public void Setup(ConstructionData constructionData)
    {
        _buildingImage.sprite = constructionData.BuildingData.BuildingSpriteVisuals[BuildingStates.Constructed];

        foreach (var iter in _resourceCosts)
        {
            iter.Value.Container.gameObject.SetActive(false);
        }

        foreach (var iter in constructionData.Cost)
        {
            _resourceCosts[iter.Key].Container.gameObject.SetActive(true);
            _resourceCosts[iter.Key].CostText.text = iter.Value.ToString();
        }
    }
}

using AYellowpaper.SerializedCollections;
using System;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

public class UnitCostUI : MonoBehaviour
{
    [SerializeField] Image _unitImage;
    [SerializeField] Button _button;
    [SerializeField] SerializedDictionary<ResourceType, ResourceCostUI> _resourceCosts;

    public IObservable<Unit> ObserveOnClick() => _button.OnClickAsObservable();

    public void Setup(UnitCost unitCost)
    {
        _unitImage.sprite = unitCost.UnitToSpawn.GetComponent<SpriteRenderer>().sprite;

        // hide everything and only enable what we need
        foreach (var iter in _resourceCosts)
        {
            iter.Value.Container.gameObject.SetActive(false);
        }
        foreach (var iter in unitCost.Cost)
        {
            var resourceCostUI = _resourceCosts[iter.Key];
            resourceCostUI.CostText.text = iter.Value.ToString();
            resourceCostUI.Container.gameObject.SetActive(true);
        }
    }
}

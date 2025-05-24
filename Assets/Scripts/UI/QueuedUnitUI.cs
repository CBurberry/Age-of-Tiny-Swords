using System;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using UnityEngine.EventSystems;

public class QueuedUnitUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] Button _button;
    [SerializeField] Image _unitImage;
    [SerializeField] Image _noItemsImage;
    [SerializeField] Image _fillBar;
    [SerializeField] GameObject _removeImage;

    UnitCost _unitCost;

    public IObservable<UnitCost> ObserveOnClick() => _button.OnClickAsObservable().Select(x => _unitCost);

    void Awake()
    {
        _removeImage.gameObject.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_unitCost != null)
        {
            _removeImage.gameObject.SetActive(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_unitCost != null)
        {
            _removeImage.gameObject.SetActive(false);
        }
    }

    public void Setup(UnitCost cost)
    {
        _removeImage.gameObject.SetActive(false);
        _fillBar.fillAmount = 1f;
        _unitCost = cost;
        if (cost == null)
        {
            _unitImage.gameObject.SetActive(false);
            _noItemsImage.gameObject.SetActive(true);
        }
        else
        {
            _unitImage.sprite = cost.UnitToSpawn.GetComponent<SpriteRenderer>().sprite;
            _unitImage.gameObject.SetActive(true);
            _noItemsImage.gameObject.SetActive(false);
        }
    }

    public void UpdateProgress(float progress)
    {
        _fillBar.fillAmount = 1f - progress;
    }

}

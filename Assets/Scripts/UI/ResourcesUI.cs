using AYellowpaper.SerializedCollections;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UniRx;
using UnityEngine;

public class ResourcesUI : MonoBehaviour
{
    [SerializeField] SerializedDictionary<ResourceType, TextMeshProUGUI> _resourceTexts;

    CompositeDisposable _disposables = new();

    // Start is called before the first frame update
    void Start()
    {
        GameManager.GetPlayer(GameManager.Instance.CurrentPlayerFaction).Resources
            .ObserveCurrentResourcesUpdated()
            .Subscribe(currentResources =>
            {
                foreach (var iter in currentResources)
                {
                    _resourceTexts[iter.Key].text = iter.Value.ToString();
                }
            })
            .AddTo(_disposables);
    }

    private void OnDestroy()
    {
        _disposables.Clear();
    }
}

using AYellowpaper.SerializedCollections;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UniRx;
using UnityEngine;

public class ResourcesUI : MonoBehaviour
{
    [SerializeField] SerializedDictionary<ResourceType, TextMeshProUGUI> _resourceTexts;
    [SerializeField] TextMeshProUGUI _populationText;

    CompositeDisposable _disposables = new();

    // Start is called before the first frame update
    void Start()
    {
        var player = GameManager.GetPlayer(GameManager.Instance.CurrentPlayerFaction);
        player.Resources
            .ObserveCurrentResourcesUpdated()
            .Subscribe(currentResources =>
            {
                foreach (var iter in currentResources)
                {
                    _resourceTexts[iter.Key].text = iter.Value.ToString();
                }
            })
            .AddTo(_disposables);

        Observable.CombineLatest(
            player.ObserveCurrentPopulation(),
            player.ObservePopuplationCap(),
            (population, cap) => (population, cap)
        ).Subscribe(x => 
        {
            _populationText.text = $"{x.population}/{x.cap}";
            _populationText.color = x.population >= x.cap ? Color.red : Color.white;
        }).AddTo(_disposables);
    }

    private void OnDestroy()
    {
        _disposables.Clear();
    }
}

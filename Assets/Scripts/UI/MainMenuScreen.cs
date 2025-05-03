using System;
using UniDi;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuViewModel
{
    public Action OnPlay;
}


public class MainMenuScreen : MonoBehaviour
{
    [Inject] UIService _uiService;

    [SerializeField] Button _playBtn;

    MainMenuViewModel _model;
    CompositeDisposable _disposables = new();

    void Awake()
    {
        _playBtn
            .OnClickAsObservable()
            .Subscribe(_ => _model?.OnPlay?.Invoke())
            .AddTo(_disposables);

        _uiService
            .ObserveMainMenuViewModel()
            .Subscribe(x => _model = x)
            .AddTo(_disposables);
    }

    void OnDestroy()
    {
        _disposables.Clear();
    }
}

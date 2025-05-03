using System;
using System.Collections;
using System.Collections.Generic;
using UniDi;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

public class GameViewModel
{
    public Action OnBack;
}

public class GameScreen : MonoBehaviour
{
    [Inject] UIService _uiService;
    [SerializeField] Button _backBtn;

    GameViewModel _model;
    CompositeDisposable _disposables = new();

    void Awake()
    {
        _backBtn.OnClickAsObservable().Subscribe(_ => _model?.OnBack?.Invoke()).AddTo(_disposables);
        _uiService.ObserveGameViewModel().Subscribe(x => _model = x).AddTo(_disposables);
    }

    void OnDestroy()
    {
        _disposables.Clear();
    }
}

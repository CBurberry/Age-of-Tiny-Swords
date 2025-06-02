using System;
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
    [SerializeField] Button _homeBtn;
    [SerializeField] Button _pauseBtn;
    [SerializeField] Transform _cameraResetTransform;

    GameViewModel _model;
    CompositeDisposable _disposables = new();

    void Awake()
    {
        var camera = Camera.main;
        _backBtn.OnClickAsObservable().Subscribe(_ => _model?.OnBack?.Invoke()).AddTo(_disposables);
        _uiService.ObserveGameViewModel().Subscribe(x => _model = x).AddTo(_disposables);
        _homeBtn.OnClickAsObservable().Subscribe(_ =>
        {
            var pos = _cameraResetTransform.position;
            pos.z = camera.transform.position.z;
            camera.transform.position = pos;
        }).AddTo(_disposables);
        _pauseBtn.OnClickAsObservable().Subscribe(_ => GameManager.Instance.TogglePause()).AddTo(_disposables);
    }

    void OnDestroy()
    {
        _disposables.Clear();
    }
}

using Cysharp.Threading.Tasks;
using UniDi;
using UniRx;
using UnityEngine;

public class LoadingScreen : MonoBehaviour
{
    [Inject] SceneLoadingService _sceneLoadingService;

    CompositeDisposable _disposables = new();

    void Awake()
    {
        _sceneLoadingService
            .ObserveIsLoading()
            .Subscribe(isLoading => gameObject.SetActive(isLoading))
            .AddTo(_disposables);
    }

    void OnDestroy()
    {
        _disposables.Clear();
    }
}

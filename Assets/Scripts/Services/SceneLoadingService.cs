
using Cysharp.Threading.Tasks;
using System;
using UniRx;
using UnityEngine.SceneManagement;

public class SceneLoadingService
{
    BehaviorSubject<bool> _isLoading = new(false);

    public IObservable<bool> ObserveIsLoading() => _isLoading;

    public void LoadScene(string sceneName)
    {
        LoadSceneAsync(sceneName).Forget();
    }

    async UniTask LoadSceneAsync(string sceneName)
    {
        _isLoading.OnNext(true);
        await SceneManager.LoadSceneAsync(sceneName);
        _isLoading.OnNext(false);
    }

}

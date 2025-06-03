using System;
using UniDi;
using UniRx;

public class UIService
{
    [Inject] SceneLoadingService _service;

    public IObservable<MainMenuViewModel> ObserveMainMenuViewModel() => Observable.Return(new MainMenuViewModel
    {
        OnPlay = () => _service.LoadScene("Game")
    });

    public IObservable<GameViewModel> ObserveGameViewModel() => Observable.Return(new GameViewModel
    {
        OnBack = () => _service.LoadScene("MainMenu")
    });
}

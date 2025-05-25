using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UniDi;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

public class GameOverScreen : MonoBehaviour
{
    [Inject] SceneLoadingService _sceneLoadingService;
    [SerializeField] GameObject _container;
    [SerializeField] TextMeshProUGUI _winLoseText;
    [SerializeField] Button _exitBtn;

    CompositeDisposable _disposables = new();
    
    void Start()
    {
        _container.gameObject.SetActive(false);
        GameManager.Instance.ObserveGameOver().Subscribe(isWin =>
        {
            _winLoseText.text = isWin ? "You won!" : "You Lost!";
            _container.gameObject.SetActive(true);
        }).AddTo(_disposables);

        _exitBtn.OnClickAsObservable().Subscribe(x => _sceneLoadingService.LoadScene("MainMenu")).AddTo(_disposables);
    }

    void OnDestroy()
    {
        _disposables.Clear();
    }
}

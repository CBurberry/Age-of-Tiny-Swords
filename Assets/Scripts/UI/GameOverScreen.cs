using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UniRx;
using UnityEngine;

public class GameOverScreen : MonoBehaviour
{
    [SerializeField] GameObject _container;
    [SerializeField] TextMeshProUGUI _winLoseText;

    CompositeDisposable _disposables = new();
    
    void Start()
    {
        _container.gameObject.SetActive(false);
        GameManager.Instance.ObserveGameOver().Subscribe(isWin =>
        {
            _winLoseText.text = isWin ? "You won!" : "You Lost!";
            _container.gameObject.SetActive(true);
        }).AddTo(_disposables);
    }

    void OnDestroy()
    {
        _disposables.Clear();
    }
}

using System.Collections;
using System.Collections.Generic;
using UniDi;
using UniRx;
using UnityEngine;

public class SelectedBuildAreaUI : MonoBehaviour
{
    [Inject] PlayerInteractionManager playerInteractionManager;

    [SerializeField] ConstructionUI _contructionUIPrefab;
    [SerializeField] Transform _constructionUIParent;

    PrefabsPool<ConstructionUI> _constructionUIPool;
    CompositeDisposable _disposables = new();
    CompositeDisposable _buildAreaDisposables = new();

    void Awake()
    { 
        _constructionUIPool = new (_contructionUIPrefab, _constructionUIParent, 10);
        playerInteractionManager.ObserveSelectedBuildArea().Subscribe(buildArea =>
        {
            _constructionUIPool.ReleaseAll();
            _buildAreaDisposables.Clear();
            if (buildArea != null)
            {
                foreach (var construction in buildArea.Constructions)
                {
                    var constructionUI = _constructionUIPool.Get();
                    constructionUI.Setup(construction);
                    constructionUI.transform.SetAsLastSibling();
                    constructionUI.ObserveOnClick().Subscribe(x =>
                    {
                        buildArea.Construct(construction);
                    }).AddTo(_buildAreaDisposables);
                }
                gameObject.SetActive(true);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }).AddTo(_disposables);
    }

    void OnDestroy()
    {
        _buildAreaDisposables.Clear();
        _disposables.Clear();
    }
}

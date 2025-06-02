using UniRx;
using UnityEngine;
using UnityEngine.UI;

public class SpriteToggle : MonoBehaviour
{
    public Image Graphic;
    public Sprite UntoggledSprite;
    public Sprite ToggledSprite;

    CompositeDisposable _disposables = new();

    private void Start()
    {
        GameManager.Instance.ObservePause().Subscribe(isPaused =>
        {
            Graphic.sprite = isPaused ? ToggledSprite : UntoggledSprite;
        }).AddTo(_disposables);

        Graphic.sprite = GameManager.Instance.IsPaused ? ToggledSprite : UntoggledSprite;
    }

    private void OnDestroy()
    {
        _disposables.Clear();
    }
}

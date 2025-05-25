using System;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InputManager : MonoBehaviour
{
    const string SCROLL_INPUT = "Mouse ScrollWheel";

    [SerializeField] float _topOffset = 100;
    [SerializeField] float _panEdgePerc = 0.15f;
    [SerializeField] float _dragTriggerScreenDistance = 30;
    [SerializeField] InputActionReference _mouseScrollRef;
    [SerializeField] RawImage _fogOfWar;

    Subject<Vector2> _singleSelect = new Subject<Vector2>();
    Subject<(Vector2, Vector2)> _groupSelect = new Subject<(Vector2, Vector2)>();
    Subject<Vector2> _interact = new Subject<Vector2>();
    Subject<Vector2> _panDirection = new Subject<Vector2>();
    Subject<float> _zoom = new Subject<float>();

    Camera _camera;
    Vector3 _mouseStartPos;
    bool _isDragging;
    bool _clickProcessed;
    Texture2D _readableTexture;

    public IObservable<Vector2> ObserveSingleSelect() => _singleSelect;
    public IObservable<(Vector2, Vector2)> ObserveGroupSelect() => _groupSelect;
    public IObservable<Vector2> ObserveInteract() => _interact;
    public IObservable<Vector2> ObservePanDirection() => _panDirection;
    public IObservable<float> ObserveZoom() => _zoom;


    void Awake()
    {
        _camera = Camera.main;
        _mouseScrollRef.action.Enable();
    }

    void OnDisable()
    {
        _mouseScrollRef.action.Disable();
    }

    // Update is called once per frame
    void Update()
    {
        HandleMouseButtons();
        HandleMousePan();
        HandleZoom();
    }

    void HandleMouseButtons()
    {
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject() && IsRevealedArea(Input.mousePosition))
        {
            _mouseStartPos = Input.mousePosition;
            _isDragging = false;
            _clickProcessed = true;
        }
        else if (_clickProcessed && Input.GetMouseButton(0))
        {
            if (!_isDragging && (_mouseStartPos - Input.mousePosition).magnitude >= _dragTriggerScreenDistance)
            {
                _isDragging = true;
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            if (_clickProcessed)
            {
                if (_isDragging)
                {
                    _groupSelect.OnNext((MouseToWorldPos(_mouseStartPos), MouseToWorldPos(Input.mousePosition)));
                }
                else
                {
                    _singleSelect.OnNext(MouseToWorldPos(Input.mousePosition));
                }
            }
            else if (!_isDragging && !EventSystem.current.IsPointerOverGameObject() && !IsRevealedArea(Input.mousePosition))
            {
                _singleSelect.OnNext(new Vector3(float.MaxValue, float.MaxValue, -100));
            }
           
            _clickProcessed = false;
        }
        // interact
        else if (Input.GetMouseButtonUp(1))
        {
            _interact.OnNext(MouseToWorldPos(Input.mousePosition));
        }
    }

    void HandleMousePan()
    {
        var mousePos = Input.mousePosition;
        Vector2 panDirection = Vector2.zero;

        if (EventSystem.current.IsPointerOverGameObject())
        {
            _panDirection.OnNext(panDirection);
            return;
        }

        if (mousePos.x < 0 || mousePos.y < 0 
            || mousePos.x > Screen.width || mousePos.y > Screen.height)
        {
            _panDirection.OnNext(panDirection);
            return;
        }


        float panEdge = Screen.width * _panEdgePerc;
     
        if (mousePos.x < panEdge || mousePos.x > Screen.width - panEdge - _topOffset
            || mousePos.y < panEdge || mousePos.y > Screen.height - panEdge) 
        {
            panDirection = new Vector2(mousePos.x - Screen.width * 0.5f, mousePos.y - Screen.height * 0.5f).normalized;
        }
        _panDirection.OnNext(panDirection);
    }

    void HandleZoom()
    {
        Vector2 axis = _mouseScrollRef.action.ReadValue<Vector2>() / 360f;
        _zoom.OnNext(axis.y);
    }

    Vector3 MouseToWorldPos(Vector3 mousePos)
    {
        var worldPos = _camera.ScreenToWorldPoint(mousePos);
        worldPos.z = 0;
        return worldPos;
    }

    public bool IsRevealedArea(Vector2 screenPoint)
    {
        if (!_fogOfWar || _fogOfWar.texture == null)
            return true;

        RectTransform rectTransform = _fogOfWar.rectTransform;
        Vector2 localPoint;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, _camera, out localPoint))
            return false;

        Rect rect = rectTransform.rect;
        float normalizedX = (localPoint.x - rect.x) / rect.width;
        float normalizedY = (localPoint.y - rect.y) / rect.height;

        if (normalizedX < 0 || normalizedX > 1 || normalizedY < 0 || normalizedY > 1)
            return false;

        var renderTexture = _fogOfWar.texture as RenderTexture;

        _readableTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        int x = Mathf.FloorToInt(normalizedX * _fogOfWar.texture.width);
        int y = Mathf.FloorToInt(normalizedY * _fogOfWar.texture.height);


        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = _fogOfWar.texture as RenderTexture;

        _readableTexture.ReadPixels(new Rect(x, y, 1, 1), 0, 0);
        _readableTexture.Apply();

        RenderTexture.active = currentRT;

        Color color = _readableTexture.GetPixel(x, y);

        return color != Color.black;
    }
}

using System;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    const string SCROLL_INPUT = "Mouse ScrollWheel";

    [SerializeField] float _topOffset = 100;
    [SerializeField] float _panEdgePerc = 0.15f;
    [SerializeField] float _dragTriggerScreenDistance = 30;
    [SerializeField] InputActionReference _mouseScrollRef;

    Subject<Vector2> _singleSelect = new Subject<Vector2>();
    Subject<(Vector2, Vector2)> _groupSelect = new Subject<(Vector2, Vector2)>();
    Subject<Vector2> _interact = new Subject<Vector2>();
    Subject<Vector2> _panDirection = new Subject<Vector2>();
    Subject<float> _zoom = new Subject<float>();

    Camera _camera;
    Vector3 _mouseStartPos;
    bool _isDragging;

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
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            _mouseStartPos = Input.mousePosition;
            _isDragging = false;
        }
        else if (Input.GetMouseButton(0))
        {
            if (!_isDragging && (_mouseStartPos - Input.mousePosition).magnitude >= _dragTriggerScreenDistance)
            {
                _isDragging = true;
            }
        }
        else if (Input.GetMouseButtonUp(0))
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
}

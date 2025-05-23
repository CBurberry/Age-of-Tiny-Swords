using System;
using UniRx;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    [SerializeField] int _panEdge = 30;
    [SerializeField] float _dragTriggerScreenDistance = 30;
    
    Subject<Vector2> _singleSelect = new Subject<Vector2>();
    Subject<(Vector2, Vector2)> _groupSelect = new Subject<(Vector2, Vector2)>();
    Subject<Vector2> _interact = new Subject<Vector2>();
    Subject<Vector2?> _panDirection = new Subject<Vector2?>();

    Camera _camera;
    Vector3 _mouseStartPos;
    bool _isDragging;

    public IObservable<Vector2> ObserveSingleSelect() => _singleSelect;
    public IObservable<(Vector2, Vector2)> ObserveGroupSelect() => _groupSelect;
    public IObservable<Vector2> ObserveInteract() => _interact;
    public IObservable<Vector2?> ObservePanDirection() => _panDirection;

    void Awake()
    {
        _camera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
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

    Vector3 MouseToWorldPos(Vector3 mousePos)
    {
        var worldPos = _camera.ScreenToWorldPoint(mousePos);
        worldPos.z = 0;
        return worldPos;
    }
}

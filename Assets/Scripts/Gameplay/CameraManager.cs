using NaughtyAttributes;
using UniDi;
using UniRx;
using Unity.VisualScripting;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [Inject] InputManager _inputManager;

    [SerializeField] Vector2 _minWorldBounds;
    [SerializeField] Vector2 _maxWorldBounds;
    [SerializeField, MinMaxSlider(0f, 25f)] Vector2 _orthoRange;
    [SerializeField] float _panSpeed;
    [SerializeField] float _zoomSensitivity = 5f;

    Camera _camera;
    Vector2 _inputPanDirection;
    float _zoom;
    CompositeDisposable _disposables = new();
    Vector2 _cameraXClamp;
    Vector2 _cameraYClamp;

    void Awake()
    {
        _camera = Camera.main;
        UpdateClampPosition();

        _inputManager.ObservePanDirection().DistinctUntilChanged().Subscribe(x =>
        {
            _inputPanDirection = x;
        }).AddTo(_disposables);

        _inputManager.ObserveZoom().DistinctUntilChanged().Subscribe(x =>
        {
            _zoom = x;
        }).AddTo(_disposables);
    }

    void OnDestroy()
    {
        _disposables.Clear();
    }

    void LateUpdate()
    {
        if (_zoom != 0f)
        {
            var newOrthoSize = _camera.orthographicSize;
            newOrthoSize += _zoom * _zoomSensitivity;
            newOrthoSize = Mathf.Clamp(newOrthoSize, _orthoRange.x, _orthoRange.y);
            _camera.orthographicSize = newOrthoSize;
            _camera.transform.position = ClampPosToWorld(_camera.transform.position);
            UpdateClampPosition();
        }

        if (_inputPanDirection != Vector2.zero)
        {
            var newPos = _camera.transform.position;
            newPos += (Vector3)_inputPanDirection * _panSpeed * Time.deltaTime;
            _camera.transform.position = ClampPosToWorld(newPos);
        }
    }

    void UpdateClampPosition()
    {
        float cameraHalfHeight = _camera.orthographicSize;
        float cameraHalfWidth = cameraHalfHeight * _camera.aspect;

        _cameraXClamp = new Vector2(_minWorldBounds.x + cameraHalfWidth, _maxWorldBounds.x - cameraHalfWidth);
        _cameraYClamp = new Vector2(_minWorldBounds.y + cameraHalfHeight, _maxWorldBounds.y - cameraHalfHeight);
    }

    Vector3 ClampPosToWorld(Vector3 pos)
    {
        Vector3 clampedPos = new Vector3(
                    Mathf.Clamp(pos.x, _cameraXClamp.x, _cameraXClamp.y),
                    Mathf.Clamp(pos.y, _cameraYClamp.x, _cameraYClamp.y),
                    pos.z
                );
        return clampedPos;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;

        Vector3 topLeft = new Vector3(_minWorldBounds.x, _maxWorldBounds.y, 0);
        Vector3 topRight = _maxWorldBounds;
        Vector3 bottomRight = new Vector3(_maxWorldBounds.x, _minWorldBounds.y, 0);
        Vector3 bottomLeft = _minWorldBounds;

        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
        Gizmos.DrawLine(bottomLeft, topLeft);
    }
}

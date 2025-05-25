using UnityEngine;
using UniDi;

public class GameInstaller : MonoInstaller
{
    [SerializeField] InputManager _inputManager;
    [SerializeField] PlayerInteractionManager _playerInteractionManager;
    [SerializeField] CameraManager _cameraManager;
    [SerializeField] FogOfWarManager _fogOfWarManager;

    public override void InstallBindings()
    {
        Container.Bind<InputManager>().FromInstance(_inputManager);
        Container.Bind<PlayerInteractionManager>().FromInstance(_playerInteractionManager);
        Container.Bind<CameraManager>().FromInstance(_cameraManager);
        Container.Bind<FogOfWarManager>().FromInstance(_fogOfWarManager);
    }
}
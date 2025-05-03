using UniDi;

public class ProjectInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        Container.BindInterfacesAndSelfTo<SceneLoadingService>().AsSingle();
        Container.BindInterfacesAndSelfTo<UIService>().AsSingle();
    }
}
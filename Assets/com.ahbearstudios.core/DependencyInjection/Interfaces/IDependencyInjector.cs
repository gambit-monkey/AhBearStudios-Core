namespace AhBearStudios.Core.DependencyInjection
{
    public interface IDependencyInjector
    {
        T Resolve<T>();
    }
}
namespace AhBearStudios.Core.BootStrapping
{
    public interface IBootstrapper
    {
        ApplicationState CurrentState { get; }
        void Initialized();
        void Initialize();
        void Loading();
        void Running();
        void Start();
        void Shutdown();
        void Pause();
        void Resume();
    }
}
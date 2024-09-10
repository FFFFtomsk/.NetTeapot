namespace Teapot.Services
{
    public interface IHardwareService
    {
        TeapotState GetState();
        void SetRelay(bool value);
    }
}



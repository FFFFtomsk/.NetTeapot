namespace Teapot.Services
{
    public interface IStateService
    {
        public TeapotState GetState();
        public void SetState(TeapotState state);

        public TeapotTargetState GetTargetState();
        public void SetTargetState(TeapotTargetState state);
    }
}

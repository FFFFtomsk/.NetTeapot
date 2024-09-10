using nanoFramework.Hosting;
using System;

namespace Teapot.Services
{
    public class TeapotService : SchedulerService
    {
        IStateService _stateService;
        IHardwareService _hardwareService;
        int _deltaT = 3;

        public TeapotService(IStateService stateService, IHardwareService hardwareService) : base(TimeSpan.FromSeconds(1))
        {
            _stateService = stateService;
            _hardwareService = hardwareService;
        }

        protected override void ExecuteAsync()
        {
            var state = _stateService.GetState();
            var targetState = _stateService.GetTargetState();
            var currentState = _hardwareService.GetState();

            if (currentState.WaterLevel < 10)
                return;

            if (currentState.Temperature < targetState.Temperature && targetState.KeepTemperature)
            {
                _hardwareService.SetRelay(true);
                if (targetState.KeepDuration == 0 || currentState.Interval.TotalSeconds > targetState.KeepDuration)
                {
                    var newTargetState = new TeapotTargetState() { Temperature = targetState.Temperature, KeepTemperature = false, KeepDuration = 0 };
                    _stateService.SetTargetState(newTargetState);
                }
            }

            if (currentState.Temperature >= targetState.Temperature - _deltaT)
            {
                _hardwareService.SetRelay(false);
            }

            _stateService.SetState(currentState);
        }
    }
}

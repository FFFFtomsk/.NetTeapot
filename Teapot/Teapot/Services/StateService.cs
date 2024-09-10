using nanoFramework.M2Mqtt;
using nanoFramework.M2Mqtt.Messages;
using System;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Teapot.Services
{

    public class StateService : IStateService
    {
        private const int _maxTemp = 85;
        private Stopwatch _stopwatch;
        private TeapotState _teapotState;
        private TeapotTargetState _teapotTargetState;

        private const string _mqttServer = "mqtt.cloud.yandex.net";
        private const int _mqttPort = 8883;
        private MqttClient _mqttDeviceClient;
        private const string _deviceId = "your_device_id";
        private const string _devicePassword = "uoyr_device_password";
        private MqttClient _mqttRegistryClient;
        private const string _registryId = "your_registry_id";
        private const string _registryPassword = "your_registry_password";
        private const string s_certificate =
@"-----BEGIN CERTIFICATE-----
cert==
-----END CERTIFICATE-----";

        private string TopicName(string entityId, EntityType entity, TopicType topic)
        {
            string result = (entity == EntityType.Registry) ? "$registries/" : "$devices/";
            result += entityId;
            result += (topic == TopicType.Events) ? "/events" : "/commands";
            return result;
        }

        public StateService()
        {
            _teapotState = new TeapotState();
            _teapotTargetState = new TeapotTargetState();
            _stopwatch = new Stopwatch();

            var caCert = new X509Certificate(s_certificate);
            _mqttDeviceClient = new MqttClient(_mqttServer, _mqttPort, true, caCert, null, MqttSslProtocols.TLSv1_2);
            _mqttDeviceClient.Connect(Guid.NewGuid().ToString(), _deviceId, _devicePassword);

            _mqttDeviceClient.Subscribe(new[] { TopicName(_deviceId, EntityType.Device, TopicType.Commands) }, new[] { MqttQoSLevel.AtLeastOnce });
            _mqttDeviceClient.MqttMsgPublishReceived += HandleIncomingMessage;
        }
        public TeapotState GetState()
        {
            _teapotState.Interval = _stopwatch.Elapsed;
            return _teapotState;
        }

        public TeapotTargetState GetTargetState()
        {
            return _teapotTargetState;
        }

        public void SetState(TeapotState state)
        {
            if (_teapotState.Temperature == state.Temperature && _teapotState.WaterLevel == state.WaterLevel && _teapotState.IsOn == state.IsOn)
            {
                return;
            }
            _teapotState = state;
            _teapotState.Interval = _stopwatch.Elapsed;
            ReportState(_teapotState);
        }

        public void SetTargetState(TeapotTargetState state)
        {
            state.Temperature = Math.Min(state.Temperature, _maxTemp);

            if (_teapotTargetState.KeepTemperature == state.KeepTemperature &&
                _teapotTargetState.Temperature == state.Temperature &&
                _teapotTargetState.KeepDuration == state.KeepDuration
                )
            {
                return;
            }

            if (!_teapotTargetState.KeepTemperature && state.KeepTemperature)
            {
                _stopwatch = Stopwatch.StartNew();
            }
            if (_teapotTargetState.KeepTemperature && !state.KeepTemperature)
            {
                _stopwatch.Reset();
            }

            _teapotTargetState = state;
        }

        private void ReportState(TeapotState state)
        {
            Debug.WriteLine($"Temperature: {state.Temperature}");
            Debug.WriteLine($"Water level {state.WaterLevel}%");
            Debug.WriteLine($"IsOn {state.IsOn}");
            Debug.WriteLine($"Elapsed {state.Interval}");
            Debug.WriteLine($"///////////////////////////////////////");

            _mqttDeviceClient.Publish(TopicName(_deviceId, EntityType.Device, TopicType.Events), Encoding.UTF8.GetBytes($"{state.Temperature};{state.WaterLevel};{state.IsOn}"));
        }

        private void HandleIncomingMessage(object sender, MqttMsgPublishEventArgs e)
        {
            var messageStr = Encoding.UTF8.GetString(e.Message, 0, e.Message.Length);
            int temp = 0;
            if (int.TryParse(messageStr, out temp))
            {
                SetTargetState(new TeapotTargetState() { Temperature = temp, KeepDuration = 100, KeepTemperature = true });
            }
            Debug.WriteLine($"Message received: {messageStr}");
        }
    }

    public enum EntityType
    {
        Registry = 0,
        Device = 1
    }

    public enum TopicType
    {
        Events = 0,
        Commands = 1
    }
}

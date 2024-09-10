using System;
using System.Device.Gpio;
using nanoFramework.Device.OneWire;
using Iot.Device.Ds18b20;
using nanoFramework.Hardware.Esp32;
using System.Device.Spi;
using Iot.Device.Hx711;
using System.Device.Adc;
using System.Threading;
using System.Diagnostics;

namespace Teapot.Services
{
    public class HardwareService : IHardwareService, IDisposable
    {
        const int minLevel = -144197;//350000;
        const int maxLevel = -1193129;//1066000;

        private AdcController _adc;
        private AdcChannel _adcChannel;
        private GpioController _gpioController;
        private GpioPin _relay;
        private GpioPin _led;
        private OneWireHost _oneWire;
        private Ds18b20 _ds18b20;
        private Scale _scale;

        public HardwareService()
        {
            //ADC init
            _adc = new AdcController();
            _adcChannel = _adc.OpenChannel(4);

            //relay init
            _gpioController = new GpioController();
            _relay = _gpioController.OpenPin(25, PinMode.Output);
            _relay.Write(PinValue.High);

            _led = _gpioController.OpenPin(27, PinMode.Output);
            _led.Write(PinValue.Low);
            //Thread.Sleep(1000);
            _led.Write(PinValue.High);

            //ds18b20 init
            Configuration.SetPinFunction(18, DeviceFunction.COM3_RX);
            Configuration.SetPinFunction(19, DeviceFunction.COM3_TX);
            _oneWire = new OneWireHost();
            _ds18b20 = new Ds18b20(_oneWire, null, false, TemperatureResolution.VeryHigh);
            _ds18b20.IsAlarmSearchCommandEnabled = false;
            _ds18b20.Initialize();

            //scale init
            Configuration.SetPinFunction(21, DeviceFunction.SPI1_MOSI);
            Configuration.SetPinFunction(22, DeviceFunction.SPI1_MISO);
            Configuration.SetPinFunction(23, DeviceFunction.SPI1_CLOCK);
            var spisettings = new SpiConnectionSettings(1)
            {
                ClockFrequency = Scale.DefaultClockFrequency
            };
            var spidev = SpiDevice.Create(spisettings);
            _scale = new Scale(spidev);
        }

        public GpioController GpioController { get { return _gpioController; } }

        private int ReadTemperature()
        {
            int temperature;
            if (_ds18b20.TryReadTemperature(out var currentTemperature))
            {
                temperature = (int)currentTemperature.DegreesCelsius;
            }
            else
            {
                temperature = -255;
            }
            return temperature;
        }

        private int ReadWaterLevel()
        {
            var weight = (int)_scale.Read();
            int level = (weight - minLevel) * 100 / (maxLevel - minLevel);
            return level;
        }

        private bool ReadIsOn()
        {
            int avgValue = 0;
            for (int i = 0; i < 50; i++)
            {
                int myAdcRawvalue = _adcChannel.ReadValue();
                if (myAdcRawvalue > avgValue)
                    avgValue = myAdcRawvalue;
            }
            return avgValue > 2300;
        }

        public TeapotState GetState()
        {
            TeapotState info = new TeapotState() { Temperature = ReadTemperature(), WaterLevel = ReadWaterLevel(), IsOn = ReadIsOn() };
            return info;
        }

        public void SetRelay(bool value)
        {
            if (value)
            {
                _relay.Write(PinValue.Low);
            }
            else
            {
                _relay.Write(PinValue.High);
            }
        }

        public void Dispose()
        {
            _gpioController.Dispose();
            _oneWire.Dispose();
        }
    }
}

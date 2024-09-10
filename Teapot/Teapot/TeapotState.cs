
using System;

namespace Teapot
{
    public class TeapotState
    {
        public int Temperature {  get; set; }
        public int WaterLevel {  get; set; }
        public bool IsOn {  get; set; }
        public TimeSpan Interval { get; set; }

    }
}

namespace YcFunction
{
    public class EventObject
    {
        public Message[] messages { get; set; }
    }

    public class Message
    {
        public Event_Metadata event_metadata { get; set; }
        public Details details { get; set; }
    }

    public class Event_Metadata
    {
        public string event_id { get; set; }
        public string event_type { get; set; }
        public string created_at { get; set; }
    }

    public class Details
    {
        public string registry_id { get; set; }
        public string device_id { get; set; }
        public string mqtt_topic { get; set; }
        public string payload { get; set; }
    }



    public class Payload
    {
        public float Temperature { get; set; }
        public int WaterLevel { get; set; }
        public bool IsOn { get; set; }
    }
}

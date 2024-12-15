using System;

namespace PluginBase
{
    public class ServerDataObjectChange
    {
        public string ConnectorName{ get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public DateTimeOffset Timestamp { get; set; }

        public ServerDataObjectChange(string connectorName, string name, string value, DateTimeOffset timestamp)
        {
            ConnectorName = connectorName;
            Name = name;
            Value = value;
            Timestamp = timestamp;
        }

        public override string ToString()
        {
            return $"ConnectorName: {ConnectorName}, Name: {Name}, Value: {Value}";
        }
    }
}

namespace PluginBase
{
    public class ServerDataObjectChange
    {
        public string ConnectorName{ get; set; }
        public string Name { get; set; }
        public string Value { get; set; }

        public ServerDataObjectChange(string connectorName, string name, string value)
        {
            ConnectorName = connectorName;
            Name = name;
            Value = value;
        }
    }
}

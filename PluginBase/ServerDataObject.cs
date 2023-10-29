namespace AllOnOnePage.Plugins
{
    public class ServerDataObject
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public ServerDataObject(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }
}
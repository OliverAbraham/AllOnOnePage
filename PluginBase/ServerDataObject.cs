using System;

namespace AllOnOnePage.Plugins
{
    public class ServerDataObject
    {
        public string         Name      { get; private set; }
        public string         Value     { get; private set; }
        public DateTimeOffset Timestamp { get; private set; }

        public ServerDataObject(string name, string value, DateTimeOffset dateTime)
        {
            Name      = name;
            Value     = value;
            Timestamp = dateTime;
        }
    }
}
using AllOnOnePage.Plugins;
using PluginBase;

namespace AllOnOnePage.Connectors
{
    internal delegate void ServerDataObjectChange_Handler(ServerDataObjectChange eventData);

    internal interface IConnector
    {
        public string Name { get; }
        public bool ConnectionIsInProgress { get; }
        public bool IsConnected { get; }
        public string ConnectionStatus { get; }
        public IServerGetter Getter { get; }
        public ServerDataObjectChange_Handler OnDataobjectChange { get; set; }
        public void Connect(Configuration config);
        public void Reconnect();
        public void Stop();

    }
}
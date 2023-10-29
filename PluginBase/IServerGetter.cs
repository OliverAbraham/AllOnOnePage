namespace AllOnOnePage.Plugins
{
    public interface IServerGetter
    {
        public ServerDataObject TryGet(string dataObjectName);
    }
}
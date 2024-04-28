namespace UniEvent
{
    public enum SyncType
    {
        Sync,
        ASync,
        ASyncCancelable,
    }

    public sealed class EventFactory
    {
        Options options;
        DiagnosticsInfo diagnosticsInfo;
        HandlerFactory factory;

        public EventFactory(Options _options, DiagnosticsInfo _diagnosticsInfo, HandlerFactory _factory)
        {
            options = _options;
            diagnosticsInfo = _diagnosticsInfo;
            factory = _factory;
        }

        public IEvent<T> NewEvent<T>() => new Event<T>(options, factory, diagnosticsInfo);
        public ITopic<K, T> NewTopic<K, T>() => new Topic<K, T>(options, factory, diagnosticsInfo);
        public IEvent<T, R> NewEvent<T, R>() => new Event<T, R>(options, factory, diagnosticsInfo);
        public ITopic<K, T, R> NewTopic<K, T, R>() => new Topic<K, T, R>(options, factory, diagnosticsInfo);
    }
}
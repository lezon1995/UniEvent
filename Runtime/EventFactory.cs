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

        public IEventBroker<T> EventBroker<T>()
        {
            var broker = new Event.Broker<T>(options, factory, diagnosticsInfo);
            return broker;
        }

        public ITopicBroker<K, T> TopicBroker<K, T>()
        {
            var broker = new Topic.Broker<K, T>(options, factory, diagnosticsInfo);
            return broker;
        }

        public IEventRequester<T, R> EventRequester<T, R>()
        {
            var requester = new Event.Requester<T, R>(options, factory, diagnosticsInfo);
            return requester;
        }

        public ITopicRequester<K, T, R> TopicRequester<K, T, R>()
        {
            var requester = new Topic.Requester<K, T, R>(options, factory, diagnosticsInfo);
            return requester;
        }
    }
}
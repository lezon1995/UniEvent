namespace UniEvent
{
    public static class Events
    {
        static Options options;
        static EventFactory factory;
        static HandlerFactory handlerFactory;
        static DiagnosticsInfo diagnosticsInfo;
        public static bool IsInitialized;
        public static DiagnosticsInfo DiagnosticsInfo => diagnosticsInfo;

        static Events()
        {
            Initialize();
        }

        static void Initialize()
        {
            if (IsInitialized) return;

            options = new Options();
            handlerFactory = new HandlerFactory(options);
            diagnosticsInfo = new DiagnosticsInfo(options);
            factory = new EventFactory(options, diagnosticsInfo, handlerFactory);
            IsInitialized = true;
        }

        internal static IEventBroker<T> EventBroker<T>()
        {
            return factory.EventBroker<T>();
        }

        internal static ITopicBroker<K, T> TopicBroker<K, T>()
        {
            return factory.TopicBroker<K, T>();
        }

        internal static IEventRequester<T, R> EventRequester<T, R>()
        {
            return factory.EventRequester<T, R>();
        }

        internal static ITopicRequester<K, T, R> TopicRequester<K, T, R>()
        {
            return factory.TopicRequester<K, T, R>();
        }

        public static void AddBrokerDecorator<T>(T decorator) where T : IBrokerHandlerDecorator
        {
            options.AddBrokerDecorator(decorator);
        }

        public static void AddRequesterDecorator<T>(T decorator) where T : IRequesterHandlerDecorator
        {
            options.AddRequesterDecorator(decorator);
        }
    }
}
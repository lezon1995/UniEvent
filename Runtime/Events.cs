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

        internal static IEvent<T> NewEvent<T>() => factory.NewEvent<T>();
        internal static IEvent<T, R> NewEvent<T, R>() => factory.NewEvent<T, R>();
        internal static ITopic<K, T> NewTopic<K, T>() => factory.NewTopic<K, T>();
        internal static ITopic<K, T, R> NewTopic<K, T, R>() => factory.NewTopic<K, T, R>();

        public static void AddBrokerDecorator<T>(T decorator) where T : IMsgHandlerDecorator
        {
            options.AddBrokerDecorator(decorator);
        }

        public static void AddRequesterDecorator<T>(T decorator) where T : IReqHandlerDecorator
        {
            options.AddRequesterDecorator(decorator);
        }
    }
}
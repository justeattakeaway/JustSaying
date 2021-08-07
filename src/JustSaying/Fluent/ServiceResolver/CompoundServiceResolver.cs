namespace JustSaying.Fluent.ServiceResolver
{
    internal class CompoundServiceResolver : IServiceResolver
    {
        private readonly ServiceBuilderServiceResolver _serviceBuilderResolver;
        private readonly IServiceResolver _serviceResolver;

        public CompoundServiceResolver(ServiceBuilderServiceResolver serviceBuilderResolver, IServiceResolver serviceResolver)
        {
            _serviceBuilderResolver = serviceBuilderResolver;
            _serviceResolver = serviceResolver;
        }

        public T ResolveService<T>() where T : class
        {
            var result = _serviceBuilderResolver.ResolveOptionalService<T>();
            result ??= _serviceResolver.ResolveService<T>();
            return result;
        }

        public T ResolveOptionalService<T>() where T : class
        {
            var result = _serviceBuilderResolver.ResolveOptionalService<T>();
            result ??= _serviceResolver.ResolveOptionalService<T>();
            return result;
        }
    }
}

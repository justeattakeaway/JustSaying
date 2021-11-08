namespace JustSaying.Fluent.ServiceResolver;

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
        return _serviceBuilderResolver.ResolveOptionalService<T>() ??
               _serviceResolver.ResolveService<T>();
    }

    public T ResolveOptionalService<T>() where T : class
    {
        return _serviceBuilderResolver.ResolveOptionalService<T>() ??
               _serviceResolver.ResolveOptionalService<T>();
    }
}
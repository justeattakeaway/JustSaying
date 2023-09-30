namespace JustSaying.Fluent.ServiceResolver;

internal class CompoundServiceResolver(ServiceBuilderServiceResolver serviceBuilderResolver, IServiceResolver serviceResolver) : IServiceResolver
{
    public T ResolveService<T>() where T : class
    {
        return serviceBuilderResolver.ResolveOptionalService<T>() ??
               serviceResolver.ResolveService<T>();
    }

    public T ResolveOptionalService<T>() where T : class
    {
        return serviceBuilderResolver.ResolveOptionalService<T>() ??
               serviceResolver.ResolveOptionalService<T>();
    }
}

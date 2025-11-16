using System.Collections.Concurrent;

namespace Rauch.Core;

/// <summary>
/// Simple dependency injection container for commands
/// </summary>
public class ServiceContainer : IServiceProvider
{
    private readonly ConcurrentDictionary<Type, object> _singletons = new();
    private readonly ConcurrentDictionary<Type, Func<IServiceProvider, object>> _factories = new();

    /// <summary>
    /// Registers a singleton service
    /// </summary>
    public void RegisterSingleton<TService>(TService instance) where TService : class
    {
        _singletons[typeof(TService)] = instance;
    }

    /// <summary>
    /// Registers a factory for a service
    /// </summary>
    public void RegisterFactory<TService>(Func<IServiceProvider, TService> factory) where TService : class
    {
        _factories[typeof(TService)] = sp => factory(sp);
    }

    /// <summary>
    /// Gets a service from the container
    /// </summary>
    public object GetService(Type serviceType)
    {
        // Try singleton
        if (_singletons.TryGetValue(serviceType, out var singleton))
        {
            return singleton;
        }

        // Try factory
        if (_factories.TryGetValue(serviceType, out var factory))
        {
            return factory(this);
        }

        return null;
    }

    /// <summary>
    /// Gets a service type-safely
    /// </summary>
    public TService GetService<TService>() where TService : class
    {
        return GetService(typeof(TService)) as TService;
    }
}

/// <summary>
/// Extension methods for IServiceProvider
/// </summary>
public static class ServiceProviderExtensions
{
    public static TService GetService<TService>(this IServiceProvider provider) where TService : class
    {
        return provider.GetService(typeof(TService)) as TService;
    }

    public static TService GetRequiredService<TService>(this IServiceProvider provider) where TService : class
    {
        return provider.GetService<TService>()
            ?? throw new InvalidOperationException($"Service {typeof(TService).Name} not found");
    }
}

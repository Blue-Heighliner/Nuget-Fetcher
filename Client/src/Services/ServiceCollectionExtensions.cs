namespace BlueHeighliner.NugetFetcher.Services;

/// <summary>
/// Extension methods for registering services by naming convention.
/// </summary>
internal static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers every interface in <paramref name="assembly"/> whose name matches a same-named
    /// implementation class (e.g. <c>IThing</c> resolves to <c>Thing</c>) as a singleton service.
    /// </summary>
    /// <param name="services">The service collection to add registrations to.</param>
    /// <param name="assembly">The assembly to scan for interfaces and implementations.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddByConvention(this IServiceCollection services, Assembly assembly)
    {
        Type[] types = assembly.GetTypes();

        foreach (Type interfaceType in types.Where(t => t.IsInterface))
        {
            string implementationName = interfaceType.Name.StartsWith('I') ? interfaceType.Name[1..] : interfaceType.Name;

            Type? implementationType = types.FirstOrDefault(t =>
                t.IsClass && !t.IsAbstract && t.Name == implementationName && interfaceType.IsAssignableFrom(t));

            if (implementationType is not null)
                services.AddSingleton(interfaceType, implementationType);
        }

        return services;
    }
}

using MultiShop.Catalog.Settings;
using System.Reflection;

namespace MultiShop.Catalog.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static void AddServicesFromAttributes(this IServiceCollection services, Assembly assembly)
        {
            var typesWithAttributes = assembly.GetTypes()
                .Where(t => t.GetCustomAttributes<ServiceAttribute>().Any())
                .ToList();

            foreach (var type in typesWithAttributes)
            {
                var attribute = type.GetCustomAttribute<ServiceAttribute>();
                var interfaces = type.GetInterfaces();

                foreach (var interfaceType in interfaces)
                {
                    switch (attribute.Lifetime)
                    {
                        case ServiceLifetime.Singleton:
                            services.AddSingleton(interfaceType, type);
                            break;
                        case ServiceLifetime.Scoped:
                            services.AddScoped(interfaceType, type);
                            break;
                        case ServiceLifetime.Transient:
                            services.AddTransient(interfaceType, type);
                            break;
                    }
                }
            }
        }
    }
}

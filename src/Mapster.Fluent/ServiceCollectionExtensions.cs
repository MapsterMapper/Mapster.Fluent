using Mapster.Fluent.Configs;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Linq;
using System.Reflection;

namespace Mapster.Fluent
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Mapster with fluent configuration using IFluentMapperConfig, enabling chainable mapping rules, assembly scanning, and DI support with ServiceMapper.
        /// </summary>
        /// <param name="serviceCollection">The service collection to add Mapster services to.</param>
        /// <param name="configure">Action to configure fluent mapping rules using IFluentMapperConfig.</param>
        /// <param name="options">Optional action to configure Mapster options, including assembly scanning and mapper selection.</param>
        /// <returns>The service collection for method chaining.</returns>
        public static IServiceCollection AddMapsterFluent(
            this IServiceCollection serviceCollection,
            Action<FrozenTypeAdapterConfig> configure,
            Action<MapsterOptions> options = null)
        {
            if (serviceCollection == null) throw new ArgumentNullException(nameof(serviceCollection));
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            var mapsterOptions = new MapsterOptions();
            options?.Invoke(mapsterOptions);

            var innerConfig = new FrozenTypeAdapterConfig();
           
            configure.Invoke(innerConfig);
            mapsterOptions.ConfigureAction?.Invoke(innerConfig);

            // Assembly scanning
            if (mapsterOptions.AssembliesToScan?.Any() == true)
            {
                innerConfig.Scan(mapsterOptions.AssembliesToScan.ToArray());
            }

            serviceCollection.TryAddSingleton<ITypeAdapterConfig>(innerConfig);
            if (mapsterOptions.UseServiceMapper)
            {
                serviceCollection.TryAddTransient<IMapper, ServiceMapper>();
                serviceCollection.TryAddSingleton<IMapContextFactory, DefaultMapContextFactory>();
            }
            else
            {
                serviceCollection.TryAddTransient<IMapper, Mapper>();
            }

            return serviceCollection;
        }

        /// <summary>
        /// Adds Mapster using an existing TypeAdapterConfig, enabling DI support with ServiceMapper and IMapContextFactory.
        /// </summary>
        /// <param name="serviceCollection">The service collection to add Mapster services to.</param>
        /// <param name="existingConfig">The pre-configured TypeAdapterConfig to use for mapping.</param>
        /// <returns>The service collection for method chaining.</returns>
        public static IServiceCollection AddMapsterWithConfig(
            this IServiceCollection serviceCollection,
            ITypeAdapterConfig existingConfig)
        {
            if (serviceCollection == null) throw new ArgumentNullException(nameof(serviceCollection));
            if (existingConfig == null) throw new ArgumentNullException(nameof(existingConfig));

            ITypeAdapterConfig config = new FrozenTypeAdapterConfig(existingConfig);

            serviceCollection.TryAddSingleton<ITypeAdapterConfig>(config);
            serviceCollection.TryAddTransient<IMapper, ServiceMapper>();
            serviceCollection.TryAddSingleton<IMapContextFactory, DefaultMapContextFactory>();

            return serviceCollection;
        }

        /// <summary>
        /// Scans assemblies for IRegister and IMapFrom implementations.
        /// </summary>
        /// <param name="serviceCollection">The service collection.</param>
        /// <param name="assemblies">Assemblies to scan.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection ScanMapster(
            this IServiceCollection serviceCollection,
            params Assembly[] assemblies)
        {
            if (serviceCollection == null) throw new ArgumentNullException(nameof(serviceCollection));
            if (assemblies == null || assemblies.Length == 0)
                throw new ArgumentException("At least one assembly must be provided", nameof(assemblies));

            return serviceCollection.AddMapsterFluent(
                config => { }, // No additional fluent config
                options =>
                {
                    options.AssembliesToScan = assemblies;
                    options.UseServiceMapper = true;
                });
        }
    }
}

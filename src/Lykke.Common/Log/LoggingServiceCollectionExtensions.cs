﻿using System;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Lykke.Common.Log
{
    /// <summary>
    /// Extension methods to register Lykke logging in the app services
    /// </summary>
    [PublicAPI]
    public static class LoggingServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Lykke logging services to the specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add services to.</param>
        /// <returns>The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> so that additional calls can be chained.</returns>
        public static IServiceCollection AddLykkeLogging([NotNull] this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return services.AddLykkeLogging(builder => { });
        }

        /// <summary>
        /// Adds Lykke logging services to the specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add services to.</param>
        /// <param name="configure">The <see cref="T:Microsoft.Extensions.Logging.ILoggingBuilder" /> configuration delegate.</param>
        /// <returns>The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> so that additional calls can be chained.</returns>
        public static IServiceCollection AddLykkeLogging([NotNull] this IServiceCollection services, [NotNull] Action<ILoggingBuilder> configure)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            services.TryAdd(ServiceDescriptor.Singleton(typeof(ILogFactory), typeof(LogFactory)));

            return services.AddLogging(buidler =>
            {
                buidler.AddFilter("System", LogLevel.Warning);
                buidler.AddFilter("Microsoft", LogLevel.Warning);

                configure(buidler);
            });
        }
    }
}
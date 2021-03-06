﻿using Microsoft.ServiceFabric.Services.Runtime;
using System;
using System.Fabric;
using System.Threading;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using ServiceFabric.Logging;
using ServiceFabric.Logging.Extensions;

namespace WebApi
{
    internal static class Program
    {
        /// <summary>
        /// This is the entry point of the service host process.
        /// </summary>
        private static void Main()
        {
            ILogger logger = null;

            var applicationInsightsKey = FabricRuntime.GetActivationContext()
                .GetConfigurationPackageObject("Config")
                .Settings
                .Sections["ConfigurationSection"]
                .Parameters["ApplicationInsightsKey"]
                .Value;

            try
            {
                // The ServiceManifest.XML file defines one or more service type names.
                // Registering a service maps a service type name to a .NET type.
                // When Service Fabric creates an instance of this service type,
                // an instance of the class is created in this host process.

                ServiceRuntime.RegisterServiceAsync("WebApiType",
                    context =>
                    {
                        LogContext.PushProperty(nameof(context.ServiceTypeName), context.ServiceTypeName);
                        LogContext.PushProperty(nameof(context.ServiceName), context.ServiceName);
                        LogContext.PushProperty(nameof(context.PartitionId), context.PartitionId);
                        LogContext.PushProperty(nameof(context.ReplicaOrInstanceId), context.ReplicaOrInstanceId);
                        LogContext.PushProperty(nameof(context.NodeContext.NodeName), context.NodeContext.NodeName);
                        LogContext.PushProperty(nameof(context.CodePackageActivationContext.ApplicationName), context.CodePackageActivationContext.ApplicationName);
                        LogContext.PushProperty(nameof(context.CodePackageActivationContext.ApplicationTypeName), context.CodePackageActivationContext.ApplicationTypeName);
                        LogContext.PushProperty(nameof(context.CodePackageActivationContext.CodePackageVersion), context.CodePackageActivationContext.CodePackageVersion);
                        
                        var loggerFactory = new LoggerFactoryBuilder(context).CreateLoggerFactory(applicationInsightsKey);
                        logger = loggerFactory.CreateLogger<WebApi>();

                        return new WebApi(context, logger);
                    }).GetAwaiter().GetResult();

                // Prevents this host process from terminating so services keeps running. 
                Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception e)
            {
                logger?.LogStatelessServiceInitalizationFailed<WebApi>(e);
                throw;
            }
        }
    }
}

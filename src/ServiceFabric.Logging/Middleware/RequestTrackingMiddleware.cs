﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Fabric;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ServiceFabric.Logging.Extensions;
using ServiceFabric.Logging.PropertyMap;

namespace ServiceFabric.Logging.Middleware
{
    public class RequestTrackingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        public RequestTrackingMiddleware(RequestDelegate next, ILogger logger)
        {
            this._next = next;
            this._logger = logger;
        }

        public async Task Invoke(HttpContext context, StatelessServiceContext serviceContext)
        {
            using (_logger.BeginScope(new Dictionary<string, object>
            {
                [SharedProperties.TraceId] = context.Request.HttpContext.TraceIdentifier
            }))
            {
                CallContext.SetData(HeaderIdentifiers.TraceId, context.Request.HttpContext.TraceIdentifier);

                AddTracingDetailsOnRequest(context, serviceContext);
                
                var stopwatch = Stopwatch.StartNew();
                var started = DateTime.Now;
                var success = false;

                try
                {
                    await _next(context);
                    success = true;
                }
                catch (Exception exception)
                {
                    _logger.LogCritical((int)ServiceFabricEvent.Exception, exception, exception.Message);
                    throw;
                }
                finally
                {
                    stopwatch.Stop();
                    _logger.LogRequest(context, started, stopwatch.Elapsed, success);
                }
            }
        }

        private static void AddTracingDetailsOnRequest(HttpContext context, ServiceContext serviceContext)
        {
            if (!context.Request.Headers.ContainsKey("X-Fabric-AddTracingDetails")) return;

            context.Response.Headers.Add("X-Fabric-NodeName", serviceContext.NodeContext.NodeName);
            context.Response.Headers.Add("X-Fabric-InstanceId", serviceContext.ReplicaOrInstanceId.ToString(CultureInfo.InvariantCulture));
            context.Response.Headers.Add("X-Fabric-TraceId", context.Request.HttpContext.TraceIdentifier);
        }
    }
}

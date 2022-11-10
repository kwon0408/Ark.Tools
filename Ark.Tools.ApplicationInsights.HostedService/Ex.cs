﻿using Ark.Tools.AspNetCore.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.SnapshotCollector;
using Microsoft.ApplicationInsights.WorkerService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.ApplicationInsights.WindowsServer.Channel.Implementation;
using System;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Options;
using Microsoft.ApplicationInsights.DependencyCollector;

namespace Ark.Tools.ApplicationInsights.HostedService
{
    public static partial class Ex
    {
        public static IHostBuilder AddApplicationInsightsForHostedService(this IHostBuilder builder)
        {
            return builder.ConfigureServices((ctx, services) =>
            {
                services.AddSingleton<ITelemetryInitializer, GlobalInfoTelemetryInitializer>();
                services.AddSingleton<ITelemetryInitializer, DoNotSampleFailures>();

                services.AddApplicationInsightsTelemetryProcessor<ArkSkipUselessSpamTelemetryProcessor>();

                services.Configure<SamplingPercentageEstimatorSettings>(o =>
                {
                    o.MovingAverageRatio = 0.5;
                    o.MaxTelemetryItemsPerSecond = 1;
                    o.SamplingPercentageDecreaseTimeout = TimeSpan.FromMinutes(1);
                });

                services.Configure<SamplingPercentageEstimatorSettings>(o =>
                {
                    ctx.Configuration.GetSection("ApplicationInsights").GetSection("EstimatorSettings").Bind(o);
                });

                services.AddApplicationInsightsTelemetryWorkerService(o =>
                {           
                    o.ApplicationVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString();
                    o.ConnectionString = ctx.Configuration["ApplicationInsights:ConnectionString"] ?? $"InstrumentationKey=" +
                        ctx.Configuration["ApplicationInsights:InstrumentationKey"] ?? Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY");
                    o.EnableAdaptiveSampling = false; // ENABLED BELOW by ConfigureTelemetryOptions with custom settings
                    o.EnableHeartbeat = true;
                    o.EnableDebugLogger = Debugger.IsAttached;
                    o.EnableDependencyTrackingTelemetryModule = true;
                    
                });

                services.ConfigureTelemetryModule<DependencyTrackingTelemetryModule>((module, o) => { module.EnableSqlCommandTextInstrumentation = true; });

                // this MUST be after the MS AddApplicationInsightsTelemetry to work. IPostConfigureOptions is NOT working as expected.
                services.AddSingleton<IConfigureOptions<TelemetryConfiguration>, EnableAdaptiveSamplingWithCustomSettings>();

                if (!string.IsNullOrWhiteSpace(ctx.Configuration.GetConnectionString(NLog.NLogDefaultConfigKeys.SqlConnStringName)))
                    services.AddSingleton<ITelemetryProcessorFactory>(
                        new SkipSqlDatabaseDependencyFilterFactory(ctx.Configuration.GetConnectionString(NLog.NLogDefaultConfigKeys.SqlConnStringName)));

#if NET5_0
                services.Configure<SnapshotCollectorConfiguration>(o =>
                {
                });
                services.Configure<SnapshotCollectorConfiguration>(ctx.Configuration.GetSection(nameof(SnapshotCollectorConfiguration)));
                services.AddSnapshotCollector();
#endif

            });
        }
    }
}

﻿// Copyright (c) 2018 Ark S.r.l. All rights reserved.
// Licensed under the MIT License. See LICENSE file for license information. 
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NLog.Extensions.Logging;

using System;
using System.Reflection;

namespace Ark.Tools.NLog
{
    using static Ark.Tools.NLog.NLogConfigurer;

    public static class NLogConfigurerConfiguration
    {
        public static Configurer WithDefaultTargetsAndRulesFromConfiguration(this Configurer @this, IConfiguration cfg, bool async = true)
        {
            @this.WithArkDefaultTargetsAndRules(
                @this.AppName, cfg.GetConnectionString(NLogDefaultConfigKeys.SqlConnStringName),
                cfg[NLogDefaultConfigKeys.MailNotificationAddresses.Replace('.', ':')], cfg.GetConnectionString(NLogDefaultConfigKeys.SmtpConnStringName),
                async:async);

            var cfgSlack = cfg[NLogDefaultConfigKeys.SlackWebHook.Replace('.', ':')];
            if (!string.IsNullOrWhiteSpace(cfgSlack))
                @this.WithSlackDefaultTargetsAndRules(cfgSlack, async);

            var key = cfg["APPINSIGHTS_INSTRUMENTATIONKEY"] ?? cfg["ApplicationInsights:InstrumentationKey"];
            @this.WithApplicationInsightsTargetsAndRules(key, async);

            return @this;
        }

        public static Configurer WithDefaultTargetsAndRulesFromConfiguration(this Configurer @this, IConfiguration cfg, string logTableName, string mailFrom = null, bool async = true)
        {
            @this.WithArkDefaultTargetsAndRules(
                logTableName, cfg.GetConnectionString(NLogDefaultConfigKeys.SqlConnStringName),
                cfg[NLogDefaultConfigKeys.MailNotificationAddresses.Replace('.', ':')], cfg.GetConnectionString(NLogDefaultConfigKeys.SmtpConnStringName),
                mailFrom, async: async);

            var cfgSlack = cfg[NLogDefaultConfigKeys.SlackWebHook.Replace('.', ':')];
            if (!string.IsNullOrWhiteSpace(cfgSlack))
                @this.WithSlackDefaultTargetsAndRules(cfgSlack, async);

            var key = cfg["APPINSIGHTS_INSTRUMENTATIONKEY"] ?? cfg["ApplicationInsights:InstrumentationKey"];
            @this.WithApplicationInsightsTargetsAndRules(key, async);

            return @this;
        }

        public static IHostBuilder ConfigureNLog(this IHostBuilder builder, string appName = null, string mailFrom = null)
        {
            appName ??= Assembly.GetEntryAssembly()?.GetName().Name ?? AppDomain.CurrentDomain.FriendlyName ?? "Unknown";

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                global::NLog.LogManager.GetLogger("Main").Fatal(e.ExceptionObject as Exception, "UnhandledException");
                global::NLog.LogManager.Flush();
            };

            return builder.ConfigureLogging((ctx, logging) =>
            {
                NLogConfigurer.For(appName)
                   .WithDefaultTargetsAndRulesFromConfiguration(ctx.Configuration, appName, mailFrom, async: !ctx.HostingEnvironment.IsEnvironment("SpecFlow"))
                   .Apply();

                logging.ClearProviders();
                logging.AddNLog();
            });

        }
    }
}

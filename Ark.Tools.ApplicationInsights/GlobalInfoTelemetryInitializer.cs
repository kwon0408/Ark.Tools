﻿// Copyright (c) 2018 Ark S.r.l. All rights reserved.
// Licensed under the MIT License. See LICENSE file for license information. 
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using System.Reflection;

namespace Ark.Tools.AspNetCore.ApplicationInsights
{
    public class GlobalInfoTelemetryInitializer : ITelemetryInitializer
    {
        private const string _processNameProperty = "ProcessName";
        private readonly string _processName;

        public GlobalInfoTelemetryInitializer()
        {
            _processName = Assembly.GetEntryAssembly().GetName().Name;
        }

        public void Initialize(ITelemetry telemetry)
        {
            if (telemetry != null && !telemetry.Context.GlobalProperties.ContainsKey(_processNameProperty))
            {
                telemetry.Context.GlobalProperties.Add(_processNameProperty, _processName);
            }
        }
    }
}

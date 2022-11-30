﻿using Microsoft.Extensions.Configuration;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;

namespace Microsoft.Extensions.Configuration.EnvironmentVariables
{
    /// <summary>
    /// An environment variable based <see cref="ConfigurationProvider"/>.
    /// </summary>
    public class ArkLegacyEnvironmentVariablesConfigurationProvider : ConfigurationProvider
    {
        private const string _mySqlServerPrefix = "MYSQLCONNSTR_";
        private const string _sqlAzureServerPrefix = "SQLAZURECONNSTR_";
        private const string _sqlServerPrefix = "SQLCONNSTR_";
        private const string _customPrefix = "CUSTOMCONNSTR_";

        private const string _connStrKeyFormat = _connStrKey+"{0}";
        private const string _connStrKey = "ConnectionStrings:";
        private const string _providerKeyFormat = "ConnectionStrings:{0}_ProviderName";

        private readonly string _prefix;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public ArkLegacyEnvironmentVariablesConfigurationProvider() : this(string.Empty)
        { }

        /// <summary>
        /// Initializes a new instance with the specified prefix.
        /// </summary>
        /// <param name="prefix">A prefix used to filter the environment variables.</param>
        public ArkLegacyEnvironmentVariablesConfigurationProvider(string prefix)
        {
            _prefix = prefix ?? string.Empty;
        }

        /// <summary>
        /// Loads the environment variables.
        /// </summary>
        public override void Load()
        {
            Load(Environment.GetEnvironmentVariables());
        }

        internal void Load(IDictionary envVariables)
        {
            var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var filteredEnvVariables = envVariables
                .Cast<DictionaryEntry>()
                .SelectMany(_azureEnvToAppEnv);

            filteredEnvVariables = _duplicateConnectionStringForDifferentEnvinroment(filteredEnvVariables)
                .Where(entry => ((string)entry.Key).StartsWith(_prefix, StringComparison.OrdinalIgnoreCase));

            foreach (var envVariable in filteredEnvVariables)
            {
                var key = ((string)envVariable.Key).Substring(_prefix.Length);
                data[key] = envVariable.Value != null ? (string)envVariable.Value : String.Empty;
            }

            Data = data;
        }

        private static string _normalizeConnectionStringKey(string key)
        {
            return key.Replace("__", ConfigurationPath.KeyDelimiter);
        }

        private static string _normalizeAppSettingsKey(string key)
        {
            return _normalizeConnectionStringKey(key).Replace(".", ConfigurationPath.KeyDelimiter);
        }

        private IEnumerable<DictionaryEntry> _duplicateConnectionStringForDifferentEnvinroment(IEnumerable<DictionaryEntry> filteredEnvVariables)
        {
            var newfilteredEnvVariables = new List<DictionaryEntry>(filteredEnvVariables);

            foreach (var entry in filteredEnvVariables)
            {
                var key = (string)entry.Key;

                if (key.StartsWith(_connStrKey, StringComparison.OrdinalIgnoreCase))
                {
                    var entryNew = new DictionaryEntry(entry.Key, entry.Value);
                    entryNew.Key = key.Replace("_", ".");
                    newfilteredEnvVariables.Add(entryNew);
                }
            }

            return newfilteredEnvVariables;
        }

        private static IEnumerable<DictionaryEntry> _azureEnvToAppEnv(DictionaryEntry entry)
        {
            var key = (string)entry.Key;
            var prefix = string.Empty;
            var provider = string.Empty;

            if (key.StartsWith(_mySqlServerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                prefix = _mySqlServerPrefix;
                provider = "MySql.Data.MySqlClient";
            }
            else if (key.StartsWith(_sqlAzureServerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                prefix = _sqlAzureServerPrefix;
                provider = "Microsoft.Data.SqlClient";
            }
            else if (key.StartsWith(_sqlServerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                prefix = _sqlServerPrefix;
                provider = "Microsoft.Data.SqlClient";
            }
            else if (key.StartsWith(_customPrefix, StringComparison.OrdinalIgnoreCase))
            {
                prefix = _customPrefix;
            }
            else
            {
                entry.Key = _normalizeAppSettingsKey(key);
                yield return entry;
                yield break;
            }

            // Return the key-value pair for connection string
            yield return new DictionaryEntry(
                string.Format(_connStrKeyFormat, _normalizeConnectionStringKey(key.Substring(prefix.Length))),
                entry.Value);

            if (!string.IsNullOrEmpty(provider))
            {
                // Return the key-value pair for provider name
                yield return new DictionaryEntry(
                    string.Format(_providerKeyFormat, _normalizeConnectionStringKey(key.Substring(prefix.Length))),
                    provider);
            }
        }
    }
}
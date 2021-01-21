﻿using Ark.Tools.Sql;

using Dapper;

using MoreLinq;

using NLog;

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text;

namespace Ark.Tools.Outbox.SqlServer
{
    /// <summary>
    /// Simple serializer that can be used to encode/decode headers to/from bytes
    /// </summary>
    public class HeaderSerializer
    {
        private static readonly JsonSerializerOptions _options = new JsonSerializerOptions().ConfigureArkDefaults();

        /// <summary>
        /// Encodes the headers into a string
        /// </summary>
        public string SerializeToString(Dictionary<string, string> headers)
        {
            return JsonSerializer.Serialize(headers, _options);
        }

        /// <summary>
        /// Encodes the headers into a byte array
        /// </summary>
        public byte[] Serialize(Dictionary<string, string> headers)
        {
            return JsonSerializer.SerializeToUtf8Bytes(headers, _options);
        }

        /// <summary>
        /// Decodes the headers from the given byte array
        /// </summary>
        public Dictionary<string, string> Deserialize(byte[] bytes)
        {
            var readOnlySpan = new ReadOnlySpan<byte>(bytes);

            return JsonSerializer.Deserialize<Dictionary<string, string>>(readOnlySpan, _options);
        }

        /// <summary>
        /// Decodes the headers from the given string
        /// </summary>
        public Dictionary<string, string> DeserializeFromString(string str)
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(str, _options);
        }
    }
}

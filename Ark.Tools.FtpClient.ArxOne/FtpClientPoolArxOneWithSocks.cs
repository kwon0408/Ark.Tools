﻿// Copyright (c) 2023 Ark Energy S.r.l. All rights reserved.
// Licensed under the MIT License. See LICENSE file for license information. 
using Ark.Tools.FtpClient.Core;
using ArxOne.Ftp;
using NLog;
using Org.Mentalis.Network.ProxySocket;
using System;
using System.Net;
using System.Net.Sockets;

namespace Ark.Tools.FtpClient
{
    public class FtpClientPoolArxOneWithSocks : FtpClientPoolArxOne
    {
        private readonly ISocksConfig _config;

        public FtpClientPoolArxOneWithSocks(ISocksConfig config, int maxPoolSize, FtpConfig ftpConfig)
            : base(maxPoolSize, ftpConfig)
        {
            this._config = config;
        }

        private protected override ArxOne.Ftp.FtpClient _getClient()
        {
            var client = new ArxOne.Ftp.FtpClient(this.Uri, this.Credentials, new FtpClientParameters()
            {
                ConnectTimeout = TimeSpan.FromSeconds(60),
                ProxyConnect = e =>
                {
                    var s = new ProxySocket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                    {
                        ProxyEndPoint = new IPEndPoint(IPAddress.Parse(_config.IpAddress), _config.Port),
                        ProxyUser = _config.UserName,
                        ProxyPass = _config.Password,
                        ProxyType = _config.Type
                    };

                    switch (e)
                    {
                        case DnsEndPoint dns:
                            s.Connect(dns.Host, dns.Port);
                            break;
                        case IPEndPoint ip:
                            s.Connect(ip);
                            break;

                        default: throw new NotSupportedException();
                    }

                    return s;
                }
            });

            return client;
        }
    }
}

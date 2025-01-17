﻿// Copyright (c) 2023 Ark Energy S.r.l. All rights reserved.
// Licensed under the MIT License. See LICENSE file for license information. 
using Ark.Tools.FtpClient.Core;
using ArxOne.Ftp;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Ark.Tools.FtpClient
{

    public class FtpClientPoolArxOne : FtpClientBase, IFtpClientPool
    {
        private readonly ArxOne.Ftp.FtpClient _client;
        private readonly SemaphoreSlim _semaphore;

        private bool _isDisposed  =  false;

        public FtpClientPoolArxOne(int maxPoolSize, FtpConfig ftpConfig)
            : base(ftpConfig, maxPoolSize)
        {
            _client = _getClient();
            _semaphore = new SemaphoreSlim(maxPoolSize, maxPoolSize);
        }

        private protected virtual ArxOne.Ftp.FtpClient _getClient()
        {
            return new ArxOne.Ftp.FtpClient(
                this.Uri, this.Credentials, new FtpClientParameters()
                {
                    ConnectTimeout = TimeSpan.FromSeconds(60),
                    ReadWriteTimeout = TimeSpan.FromMinutes(3),
                    Passive = true,
                });
        }

        public override async Task<byte[]> DownloadFileAsync(string path, CancellationToken ctk = default)
        {
            await _semaphore.WaitAsync(ctk);
            try
            {
                using var istrm = _client.Retr(path, FtpTransferMode.Binary);
                using var ms = new MemoryStream(81920);
                await istrm.CopyToAsync(ms, 81920, ctk);
                return ms.ToArray();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public override async Task UploadFileAsync(string path, byte[] content, CancellationToken ctk = default)
        {
            await _semaphore.WaitAsync(ctk);
            try
            {
                using var ostrm = _client.Stor(path, FtpTransferMode.Binary);
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                await ostrm.WriteAsync(content, ctk);
#else
                await ostrm.WriteAsync(content, 0, content.Length, ctk);
#endif
                await ostrm.FlushAsync(ctk);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public override async Task DeleteFileAsync(string path, CancellationToken ctk = default)
        {
            await _semaphore.WaitAsync(ctk);
            try
            {
                _client.Delete(path, false);

            }
            finally
            {
                _semaphore.Release();
            }
        }

        public override async Task DeleteDirectoryAsync(string path, CancellationToken ctk = default)
        {
            await _semaphore.WaitAsync(ctk);
            try
            {
                _client.Delete(path, true);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private IEnumerable<ArxOne.Ftp.FtpEntry> _list(string path)
        {
            if (_client.ServerFeatures.HasFeature("MLSD"))
            {
                return _client.MlsdEntries(path);
            }
            else
            {
                return _client.ListEntries(path);
            }
        }

        public override async Task<IEnumerable<Core.FtpEntry>> ListDirectoryAsync(string path = "./", CancellationToken ctk = default)
        {
            path ??= "./";

            await _semaphore.WaitAsync(ctk);
            try
            {
                var list = _list(path);
                return list.Select(x => new Core.FtpEntry
                {
                    FullPath = x.Path.ToString(),
                    IsDirectory = x.Type == FtpEntryType.Directory,
                    Modified = x.Date,
                    Name = x.Name,
                    Size = x.Size.GetValueOrDefault(-1),
                }).ToList();
            }
            finally
            {
                _semaphore.Release();
            }
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            if (disposing)
            {
                _client?.Dispose();
                _semaphore?.Dispose();
            }

            _isDisposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}

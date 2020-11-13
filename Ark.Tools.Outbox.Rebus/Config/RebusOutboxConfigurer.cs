﻿using Rebus.Bus;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Transport;
using Rebus.Workers.ThreadPoolBased;

using System;

namespace Ark.Tools.Outbox.Rebus.Config
{
    public class RebusOutboxProcessorConfigurer
    {
        private StandardConfigurer<ITransport> _configurer;

        private OutboxOptions _options = new OutboxOptions();

        public RebusOutboxProcessorConfigurer(StandardConfigurer<ITransport> configurer)
        {
            _configurer = configurer;
            _configurer.OtherService<IRebusOutboxProcessor>()
                .Register(s =>
                {
                    return new RebusOutboxProcessor(_options.MaxMessagesPerBatch,
                        s.Get<ITransport>(),
                        s.Get<IBackoffStrategy>(),
                        s.Get<IRebusLoggerFactory>(),
                        s.Get<IOutboxContextFactory>());
                });

            _configurer.Decorate(c =>
            {
                var transport = c.Get<ITransport>();

                if (_options.StartProcessor)
                {
                    var p = c.Get<IRebusOutboxProcessor>();
                    var events = c.Get<BusLifetimeEvents>();
                    events.BusStarted += () => p.Start();
                    events.BusDisposing += () => p.Stop();
                }

                return new OutboxTransportDecorator(transport);
            });
        }

        public RebusOutboxProcessorConfigurer OutboxContextFactory(Action<StandardConfigurer<IOutboxContextFactory>> configurer)
        {
            configurer?.Invoke(_configurer.OtherService<IOutboxContextFactory>());
            return this;
        }

        public RebusOutboxProcessorConfigurer OutboxOptions(Action<OutboxOptions> configureOptions)
        {
            configureOptions?.Invoke(_options);
            return this;
        }

    }
}
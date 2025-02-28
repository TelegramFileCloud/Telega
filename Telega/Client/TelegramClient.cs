﻿using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Telega.CallMiddleware;
using Telega.Connect;
using Telega.Rpc.Dto;

namespace Telega.Client
{
    public sealed class TelegramClient : IDisposable
    {
        const string DefaultTelegramIp = "149.154.167.50";
        const int DefaultTelegramPort = 443;
        const string DefaultSessionName = "session.dat";

        readonly TgBellhop _bellhop;
        readonly SessionStoreSync _storeSync;

        public TelegramClientAuth Auth { get; }
        public TelegramClientContacts Contacts { get; }
        public TelegramClientChannels Channels { get; }
        public TelegramClientMessages Messages { get; }
        public TelegramClientUpload Upload { get; }
        public TelegramClientUpdates Updates { get; }

        static readonly IPEndPoint DefaultEndpoint = new(IPAddress.Parse(DefaultTelegramIp), DefaultTelegramPort);

        TelegramClient(
            ILogger logger,
            TgBellhop bellhop,
            ISessionStore sessionStore
        )
        {
            _bellhop = bellhop;
            _storeSync = SessionStoreSync.Init(_bellhop.SessionVar, sessionStore);

            Auth = new TelegramClientAuth(logger, _bellhop);
            Contacts = new TelegramClientContacts(_bellhop);
            Channels = new TelegramClientChannels(_bellhop);
            Messages = new TelegramClientMessages(_bellhop);
            Upload = new TelegramClientUpload(_bellhop);
            Updates = new TelegramClientUpdates(_bellhop);
        }

        public void Dispose()
        {
            _bellhop.ConnectionPool.Dispose();
            _storeSync.Stop();
        }


        static async Task<TelegramClient> Connect(
            ConnectInfo connectInfo,
            ISessionStore store,
            TgCallMiddlewareChain? callMiddlewareChain = null,
            TgProxy? proxy = null,
            TcpClientConnectionHandler? tcpClientConnectionHandler = null,
            ILogger? logger = null
        )
        {
            logger ??= NullLogger.Instance;
            var bellhop = await TgBellhop.Connect(
                logger,
                connectInfo,
                callMiddlewareChain,
                proxy,
                tcpClientConnectionHandler
            ).ConfigureAwait(false);
            return new TelegramClient(logger, bellhop, store);
        }

        public static async Task<TelegramClient> Connect(
            int apiId,
            ISessionStore? store = null,
            IPEndPoint? endpoint = null,
            TgCallMiddlewareChain? callMiddlewareChain = null,
            ILogger? logger = null,
            TgProxy? proxy = null,
            TcpClientConnectionHandler? tcpClientConnectionHandler = null
        )
        {
            store ??= new FileSessionStore(DefaultSessionName);
            var ep = endpoint ?? DefaultEndpoint;
            var session = await store.Load().ConfigureAwait(false);
            var connectInfo = session != null
                ? ConnectInfo.FromSession(session)
                : ConnectInfo.FromInfo(apiId, ep);

            return await Connect(connectInfo, store, callMiddlewareChain, proxy, tcpClientConnectionHandler, logger)
                .ConfigureAwait(false);
        }

        public static async Task<TelegramClient> Connect(
            Session session,
            ISessionStore? store = null,
            TgCallMiddlewareChain? callMiddlewareChain = null,
            ILogger? logger = null,
            TgProxy? proxy = null,
            TcpClientConnectionHandler? tcpClientConnectionHandler = null
        )
        {
            store ??= new FileSessionStore(DefaultSessionName);
            var connectInfo = ConnectInfo.FromSession(session);

            return await Connect(connectInfo, store, callMiddlewareChain, proxy, tcpClientConnectionHandler, logger)
                .ConfigureAwait(false);
        }

        public Task<T> Call<T>(ITgFunc<T> func) =>
            _bellhop.Call(func);
    }
}
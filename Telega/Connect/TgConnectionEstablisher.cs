using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ProxyLib.Proxy;
using Telega.Auth;
using Telega.CallMiddleware;
using Telega.Rpc;
using Telega.Rpc.Dto;
using Telega.Rpc.Dto.Functions;
using Telega.Rpc.Dto.Functions.Help;
using Telega.Rpc.Dto.Types;
using Telega.Rpc.ServiceTransport;
using Telega.Utils;

namespace Telega.Connect
{
    public record TgProxy(ProxyType ProxyType, string Address, int Port);

    static class TgConnectionEstablisher
    {
        private static readonly ProxyClientFactory ProxyClientFactory = new();

        static async Task<TcpClient> CreateTcpClient(
            IPEndPoint endpoint,
            TgProxy? proxy = null,
            TcpClientConnectionHandler? connHandler = null
        )
        {
            if (connHandler != null)
            {
                return await connHandler(endpoint).ConfigureAwait(false);
            }

            TcpClient res;
            if (proxy != null)
            {
                var pc = ProxyClientFactory.CreateProxyClient(proxy.ProxyType, proxy.Address, proxy.Port);
                res = pc.CreateConnection(endpoint.Address.ToString(), endpoint.Port);
            }
            else
            {
                res = new TcpClient(endpoint.AddressFamily);
                await res.ConnectAsync(endpoint.Address, endpoint.Port).ConfigureAwait(false);
            }

            return res;
        }

        public static async Task<TgConnection> EstablishConnection(
            ILogger logger,
            ConnectInfo connectInfo,
            TgCallMiddlewareChain callMiddlewareChain,
            TgProxy? proxy = null,
            TcpClientConnectionHandler? connHandler = null
        )
        {
            var endpoint = connectInfo.Endpoint;
            Helpers.Assert(endpoint != null, "endpoint == null");
            var tcpClient = await CreateTcpClient(endpoint!, proxy, connHandler).ConfigureAwait(false);
            var tcpTransport = new TcpTransport(tcpClient);

            if (connectInfo.NeedsInAuth)
            {
                var mtPlainTransport = new MtProtoPlainTransport(tcpTransport);
                var result = await Authenticator.DoAuthentication(mtPlainTransport).ConfigureAwait(false);
                connectInfo.SetAuth(result);
            }

            var session = connectInfo.ToSession().AsVar();
            var mtCipherTransport = new MtProtoCipherTransport(tcpTransport, session);
            var transport = new TgCustomizedTransport(new TgTransport(logger, mtCipherTransport, session),
                callMiddlewareChain);

            // TODO: separate Config
            var config = new GetConfig();
            var request = new InitConnection<GetConfig, Config>(
                apiId: session.Get().ApiId,
                appVersion: "1.0.0",
                deviceModel: "PC",
                langCode: "en",
                query: config,
                systemVersion: "Win 10.0",
                systemLangCode: "en",
                langPack: "tdesktop",
                proxy: null,
                @params: null
            );
            var invokeWithLayer = new InvokeWithLayer<InitConnection<GetConfig, Config>, Config>(
                layer: SchemeInfo.LayerVersion,
                query: request
            );
            var cfg = await transport.Call(invokeWithLayer).ConfigureAwait(false);

            DcInfoKeeper.Update(cfg);
            return new TgConnection(session, transport, cfg);
        }
    }
}
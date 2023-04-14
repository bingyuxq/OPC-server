using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Server;
using StackExchange.Redis;
using IServer = StackExchange.Redis.IServer;

namespace OPC_server
{
    internal class Program
    {
        public static bool autoAccept = false;
        public static bool renewCertificate = false;
        public static string password = null;
        public static string configSectionName = "OpcUaServer";
        public const string namespaceUris = "OpcUaServer";
        public static class RedisConnection
        {
            private static Lazy<ConnectionMultiplexer> lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
            {
                return ConnectionMultiplexer.Connect("localhost"); // 根据需要替换为您的 Redis 服务器地址
            });

            public static ConnectionMultiplexer Connection => lazyConnection.Value;
            public static IDatabase database = RedisConnection.Connection.GetDatabase();
            public static IServer server = RedisConnection.Connection.GetServer("localhost", 6379); // 请根据需要替换为您的 Redis 服务器地址和端口
        }

        static async Task Main(string[] args)
        {
            // create the UA server
            var server = new MainOpcUaServer<ReferenOpcUaReverseConnectServerceServer>
            {
                AutoAccept = autoAccept,
                Password = password
            };

            await server.LoadAsync(configSectionName).ConfigureAwait(false);

            await server.CheckCertificateAsync(renewCertificate).ConfigureAwait(false);

            server.Create(Utils.NodeManagerFactories);

            await server.StartAsync().ConfigureAwait(false);

            Console.ReadLine();

        }
    }
}
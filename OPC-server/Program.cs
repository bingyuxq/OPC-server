using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Server;
using StackExchange.Redis;


namespace OPC_server
{
    internal class Program
    {
        public static bool autoAccept = false;
        public static bool renewCertificate = false;
        public static string password = null;
        public static string configSectionName = "OpcUaServer";
        public const string namespaceUris = "http://opcfoundation.org/Quickstarts/ReferenceServer";

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
            //CertificatePasswordProvider PasswordProvider = new CertificatePasswordProvider("");
            //ApplicationInstance application = new ApplicationInstance
            //{
            //    ApplicationName = "MyOpcUaServer",
            //    ApplicationType = ApplicationType.Server,
            //    ConfigSectionName = "MyOpcUaServer",
            //    CertificatePasswordProvider = PasswordProvider
            //};


            //// load the application configuration.
            //await application.LoadApplicationConfiguration(false).ConfigureAwait(false);

            //// Load the application configuration.
            //ApplicationConfiguration config = await application.LoadApplicationConfiguration(false).ConfigureAwait(false);

            //// Check the application certificate.
            //bool haveAppCertificate = await application.CheckApplicationInstanceCertificate(false, 0);

            //if (!haveAppCertificate)
            //{
            //    throw new Exception("Application instance certificate invalid!");
            //}

            //// Start the server.
            //await application.Start(new MainOpcUaServer());
            //Console.WriteLine("Server started. Press Enter to exit...");
            //Console.ReadLine();

            //// Stop the server before exiting.
            //application.Stop();

        }
    }
}
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Server;
using StackExchange.Redis;
using static System.Net.Mime.MediaTypeNames;

namespace OPC_server
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            CertificatePasswordProvider PasswordProvider = new CertificatePasswordProvider("");
            ApplicationInstance application = new ApplicationInstance
            {
                ApplicationName = "MyOpcUaServer",
                ApplicationType = ApplicationType.Server,
                ConfigSectionName = "MyOpcUaServer",
                CertificatePasswordProvider = PasswordProvider
            };


            // load the application configuration.
            await application.LoadApplicationConfiguration(false).ConfigureAwait(false);

            // Load the application configuration.
            ApplicationConfiguration config = await application.LoadApplicationConfiguration(false).ConfigureAwait(false);

            // Check the application certificate.
            bool haveAppCertificate = await application.CheckApplicationInstanceCertificate(false, 0);

            if (!haveAppCertificate)
            {
                throw new Exception("Application instance certificate invalid!");
            }

            // Start the server.
            await application.Start(new MyOpcUaServer());
            Console.WriteLine("Server started. Press Enter to exit...");
            Console.ReadLine();

            // Stop the server before exiting.
            application.Stop();

        }
    }
}
using Opc.Ua.Server;
using Opc.Ua;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Opc.Ua.Configuration;
using static System.Net.Mime.MediaTypeNames;
using StackExchange.Redis.Extensions.Core.Implementations;
using LibUA.Server;
using Microsoft.AspNetCore.Hosting.Server;

namespace OPC_server
{

    public class MainOpcUaServer<T> where T : StandardServer, new()
    {
        public ApplicationInstance Application => m_application;
        public ApplicationConfiguration Configuration => m_application.ApplicationConfiguration;

        public bool AutoAccept { get; set; }
        public string Password { get; set; }

        public T Server => m_server;

        /// <summary>
        /// Ctor of the server.
        /// </summary>
        /// <param name="writer">The text output.</param>
        public MainOpcUaServer()
        {

        }

        /// <summary>
        /// Load the application configuration.
        /// </summary>
        public async Task LoadAsync(string configSectionName)
        {
            try
            {
                CertificatePasswordProvider PasswordProvider = new CertificatePasswordProvider(Password);
                m_application = new ApplicationInstance
                {
                    ApplicationType = ApplicationType.Server,
                    ConfigSectionName = configSectionName,
                    CertificatePasswordProvider = PasswordProvider
                };

                // load the application configuration.
                await m_application.LoadApplicationConfiguration(false).ConfigureAwait(false);

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// Load the application configuration.
        /// </summary>
        public async Task CheckCertificateAsync(bool renewCertificate)
        {
            try
            {
                var config = m_application.ApplicationConfiguration;
                if (renewCertificate)
                {
                    await m_application.DeleteApplicationInstanceCertificate().ConfigureAwait(false);
                }

                // check the application certificate.
                bool haveAppCertificate = await m_application.CheckApplicationInstanceCertificate(false, minimumKeySize: 0).ConfigureAwait(false);
                if (!haveAppCertificate)
                {
                    throw new Exception("Application instance certificate invalid!");
                }

                if (!config.SecurityConfiguration.AutoAcceptUntrustedCertificates)
                {
                    config.CertificateValidator.CertificateValidation += new CertificateValidationEventHandler(CertificateValidator_CertificateValidation);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// The certificate validator is used
        /// if auto accept is not selected in the configuration.
        /// </summary>
        private void CertificateValidator_CertificateValidation(CertificateValidator validator, CertificateValidationEventArgs e)
        {
            if (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted)
            {
                if (AutoAccept)
                {
                    Console.WriteLine("Accepted Certificate: [{0}] [{1}]", e.Certificate.Subject, e.Certificate.Thumbprint);
                    e.Accept = true;
                    return;
                }
            }
            Console.WriteLine("Rejected Certificate: {0} [{1}] [{2}]", e.Error, e.Certificate.Subject, e.Certificate.Thumbprint);
        }

        /// <summary>
        /// Create server instance and add node managers.
        /// </summary>
        public void Create(IList<INodeManagerFactory> nodeManagerFactories)
        {
            try
            {
                // create the server.
                m_server = new T();
                if (nodeManagerFactories != null)
                {
                    foreach (var factory in nodeManagerFactories)
                    {
                        m_server.AddNodeManager(factory);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// Start the server.
        /// </summary>
        public async Task StartAsync()
        {
            try
            {
                // create the server.
                m_server = m_server ?? new T();

                // start the server
                await m_application.Start(m_server).ConfigureAwait(false);

                // print endpoint info
                var endpoints = m_application.Server.GetEndpoints().Select(e => e.EndpointUrl).Distinct();
                foreach (var endpoint in endpoints)
                {
                    Console.WriteLine(endpoint);
                }

                // start the status thread
                m_status = Task.Run(StatusThreadAsync);

                // print notification on session events
                m_server.CurrentInstance.SessionManager.SessionActivated += EventStatus;
                m_server.CurrentInstance.SessionManager.SessionClosing += EventStatus;
                m_server.CurrentInstance.SessionManager.SessionCreated += EventStatus;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// Status thread, prints connection status every 10 seconds.
        /// </summary>
        private async Task StatusThreadAsync()
        {
            while (m_server != null)
            {
                if (DateTime.UtcNow - m_lastEventTime > TimeSpan.FromMilliseconds(10000))
                {
                    IList<Session> sessions = m_server.CurrentInstance.SessionManager.GetSessions();
                    for (int ii = 0; ii < sessions.Count; ii++)
                    {
                        Session session = sessions[ii];
                        PrintSessionStatus(session, "-Status-", true);
                    }
                    m_lastEventTime = DateTime.UtcNow;
                }
                await Task.Delay(1000).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Output the status of a connected session.
        /// </summary>
        private void PrintSessionStatus(Session session, string reason, bool lastContact = false)
        {
            StringBuilder item = new StringBuilder();
            lock (session.DiagnosticsLock)
            {
                item.AppendFormat("{0,9}:{1,20}:", reason, session.SessionDiagnostics.SessionName);
                if (lastContact)
                {
                    item.AppendFormat("Last Event:{0:HH:mm:ss}", session.SessionDiagnostics.ClientLastContactTime.ToLocalTime());
                }
                else
                {
                    if (session.Identity != null)
                    {
                        item.AppendFormat(":{0,20}", session.Identity.DisplayName);
                    }
                    item.AppendFormat(":{0}", session.Id);
                }
            }
            Console.WriteLine(item.ToString());
        }

        /// <summary>
        /// Update the session status.
        /// </summary>
        private void EventStatus(Session session, SessionEventReason reason)
        {
            m_lastEventTime = DateTime.UtcNow;
            PrintSessionStatus(session, reason.ToString());
        }











        #region Private Members
        private ApplicationInstance m_application;
        private T m_server;
        private Task m_status;
        private DateTime m_lastEventTime;
        #endregion

























    }

    public class MainNodeManager : CustomNodeManager2
    {
        private IDatabase redisDatabase;

        //public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        //{
        //    LoadDataFromRedis();
        //}

        public MainNodeManager(IServerInternal server, ApplicationConfiguration configuration, IDatabase redisDatabase)
            : base(server, configuration, Namespaces.OpcUa)
        {
            this.redisDatabase = redisDatabase;

            //SystemContext.NodeIdFactory.SetNamespaceIndex(1);

            // Load the initial data from Redis and create the corresponding OPC UA nodes
            //LoadDataFromRedis();
        }

        private void LoadDataFromRedis()
        {
            // Load data from Redis and create OPC UA nodes
            // Assuming the Redis keys are in the format "opcua:node:<id>"
            var keys = GetKeys(OpcUaServer.redisConnection, pattern: "opcua:node:*");
            foreach (var key in keys)
            {
                string nodeId = key.ToString().Substring("opcua:node:".Length);
                string value = redisDatabase.StringGet(key);
                Console.WriteLine(nodeId + ": " + value);

                // Create an OPC UA node for each Redis key
                AddVariableNode(nodeId, value);
            }
        }
        private IEnumerable<RedisKey> GetKeys(ConnectionMultiplexer redisConnection, string pattern)
        {
            var endpoints = redisConnection.GetEndPoints();
            var server = redisConnection.GetServer(endpoints[0]);
            return server.Keys(pattern: pattern);
        }

        private void AddVariableNode(string nodeId, string value)
        {
            // Implement the method to create an OPC UA variable node using the provided nodeId and value
            try
            {


                lock (Lock)
                {
                    FolderState dataFolder = CreateFolder(null, "data", "data");
                    dataFolder.AddReference(ReferenceTypes.Organizes, true, Opc.Ua.ObjectIds.ObjectsFolder);
                    dataFolder.EventNotifier = EventNotifiers.SubscribeToEvents;
                    AddRootNotifier(dataFolder);
                    AddPredefinedNode(SystemContext, dataFolder);



                    // Construct the NodeId using the custom namespace index
                    var variableNodeId = new NodeId(nodeId, NamespaceIndex);

                    // Create the variable node
                    var variableNode = new BaseDataVariableState(null)
                    {
                        NodeId = variableNodeId,
                        BrowseName = new QualifiedName(nodeId, NamespaceIndex),
                        DisplayName = new LocalizedText(nodeId),
                        DataType = DataTypeIds.String,
                        ValueRank = ValueRanks.Scalar,
                        AccessLevel = AccessLevels.CurrentReadOrWrite,
                        UserAccessLevel = AccessLevels.CurrentReadOrWrite,
                        Historizing = false,
                        MinimumSamplingInterval = 0,
                    };

                    // Set the initial value
                    variableNode.Value = value;

                    dataFolder.AddChild(variableNode);
                    // Add the node to the node manager
                    AddPredefinedNode(SystemContext, variableNode);
                    AddPredefinedNode(SystemContext, dataFolder);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding variable node '{nodeId}': {ex.Message}");
            }
        }

        private void RemoveVariableNode(string nodeId)
        {
            // Implement the method to remove an OPC UA variable node using the provided nodeId
        }

        // Subscribe to Redis events to handle data changes
        private void SubscribeToRedisEvents()
        {
            // Subscribe to Redis events (e.g., using Redis Pub/Sub)
            // Update the OPC UA nodes based on the changes in Redis
        }

        /// <summary>
        /// Creates a new folder.
        /// </summary>
        private FolderState CreateFolder(NodeState parent, string path, string name)
        {
            FolderState folder = new FolderState(parent);

            folder.SymbolicName = name;
            folder.ReferenceTypeId = ReferenceTypes.Organizes;
            folder.TypeDefinitionId = ObjectTypeIds.FolderType;
            folder.NodeId = new NodeId(path, NamespaceIndex);
            folder.BrowseName = new QualifiedName(path, NamespaceIndex);
            folder.DisplayName = new LocalizedText("en", name);
            folder.WriteMask = AttributeWriteMask.None;
            folder.UserWriteMask = AttributeWriteMask.None;
            folder.EventNotifier = EventNotifiers.None;

            if (parent != null)
            {
                parent.AddChild(folder);
            }

            return folder;
        }
        // Implement the methods to handle data changes in Redis, and update the OPC UA nodes accordingly
    }

    internal class OpcUaServer
    {
        public static ConnectionMultiplexer redisConnection;
        public static IDatabase redisDatabase;

    }
}

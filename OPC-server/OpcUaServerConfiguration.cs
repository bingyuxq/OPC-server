using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace OPC_server
{   /// <summary>
         /// Stores the configuration the data access node manager.
         /// </summary>
    [DataContract(Namespace = Program.namespaceUris)]
    public class OpcUaServerConfiguration
    {
        #region Constructors
        /// <summary>
        /// The default constructor.
        /// </summary>
        public OpcUaServerConfiguration()
        {
            Initialize();
        }

        /// <summary>
        /// Initializes the object during deserialization.
        /// </summary>
        [OnDeserializing()]
        private void Initialize(StreamingContext context)
        {
            Initialize();
        }

        /// <summary>
        /// Sets private members to default values.
        /// </summary>
        private static void Initialize()
        {
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Whether the user dialog for accepting invalid certificates should be displayed.
        /// </summary>
        [DataMember(Order = 1)]
        public bool ShowCertificateValidationDialog
        {
            get { return m_showCertificateValidationDialog; }
            set { m_showCertificateValidationDialog = value; }
        }
        #endregion

        #region Private Members
        private bool m_showCertificateValidationDialog;
        #endregion
    }
}

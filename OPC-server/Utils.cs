using Opc.Ua.Server;
using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace OPC_server
{

    /// <summary>
    /// Helpers to find node managers implemented in this library.
    /// </summary>

    public static class Utils
    {
        /// <summary>
        /// The property with available node manager factories.
        /// </summary>
        public static ReadOnlyList<INodeManagerFactory> NodeManagerFactories
        {
            get
            {
                if (m_nodeManagerFactories == null)
                {
                    m_nodeManagerFactories = GetNodeManagerFactories();
                }
                return new ReadOnlyList<INodeManagerFactory>(m_nodeManagerFactories);
            }
        }
        /// <summary>
        /// Enumerates all node manager factories.
        /// </summary>
        /// <returns></returns>
        private static IList<INodeManagerFactory> GetNodeManagerFactories()
        {
            var assembly = typeof(Utils).Assembly;
            var nodeManagerFactories = assembly.GetExportedTypes().Select(type => IsINodeManagerFactoryType(type)).Where(type => type != null);
            return nodeManagerFactories.ToList();
        }

        /// <summary>
        /// Helper to determine the INodeManagerFactory by reflection.
        /// </summary>
        private static INodeManagerFactory IsINodeManagerFactoryType(Type type)
        {
            var nodeManagerTypeInfo = type.GetTypeInfo();
            if (nodeManagerTypeInfo.IsAbstract ||
                !typeof(INodeManagerFactory).IsAssignableFrom(type))
            {
                return null;
            }
            return Activator.CreateInstance(type) as INodeManagerFactory;
        }

        #region Private Members
        private static IList<INodeManagerFactory> m_nodeManagerFactories;
        #endregion
    }
}

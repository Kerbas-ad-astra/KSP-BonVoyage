using System;
using System.Reflection;

namespace BonVoyage
{
    /// <summary>
    /// Wrapper for communication with RemoteTech
    /// </summary>
    internal static class RemoteTechWrapper
    {
        private static int remoteTechIndex = -1; // Index of RemoteTech assembly in AssemblyLoader.loadedAssemblies
        private static Type api = null; // API type


        /// <summary>
        /// Get API type from RemoteTech.API
        /// </summary>
        /// <returns></returns>
        private static Type GetAPI()
        {
            if (remoteTechIndex == -1)
                remoteTechIndex = Tools.GetAssemblyIndex("RemoteTech");
            if ((remoteTechIndex != -1) && (api == null))
                api = AssemblyLoader.loadedAssemblies[remoteTechIndex].assembly.GetType("RemoteTech.API.API");
            return api;
        }


        /// <summary>
        /// Check if RemoteTech is enabled
        /// </summary>
        /// <returns></returns>
        internal static bool IsRemoteTechEnabled()
        {
            bool enabled = false;

            if (GetAPI() != null)
            {
                MethodInfo isRemoteTechEnabled = api.GetMethod("IsRemoteTechEnabled", BindingFlags.Public | BindingFlags.Static);
                if (isRemoteTechEnabled != null)
                    enabled = (bool)isRemoteTechEnabled.Invoke(null, null);
            }

            return enabled;
        }


        /// <summary>
        /// Check if vessel has any connection
        /// </summary>
        /// <returns></returns>
        internal static bool HasAnyConnection(Guid id)
        {
            bool hasConnection = false;

            if (GetAPI() != null)
            {
                MethodInfo hasAnyConnection = api.GetMethod("HasAnyConnection", BindingFlags.Public | BindingFlags.Static);
                if (hasAnyConnection != null)
                    hasConnection = (bool)hasAnyConnection.Invoke(null, new object[] { id });
            }

            return hasConnection;
        }


        /// <summary>
        /// Check if vessel has local control
        /// </summary>
        /// <returns></returns>
        internal static bool HasLocalControl(Guid id)
        {
            bool hasControl = false;

            if (GetAPI() != null)
            {
                MethodInfo hasLocalControl = api.GetMethod("HasLocalControl", BindingFlags.Public | BindingFlags.Static);
                if (hasLocalControl != null)
                    hasControl = (bool)hasLocalControl.Invoke(null, new object[] { id });
            }

            return hasControl;
        }

    }

}

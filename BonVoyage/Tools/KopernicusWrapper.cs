using System;
using System.Reflection;

namespace BonVoyage
{
    /// <summary>
    /// Wrapper for communication with Kopernicus assemblies
    /// </summary>
    internal static class KopernicusWrapper
    {
        private static int kopernicusComponentsIndex = -1; // Index of Kopernicus.Components assembly in AssemblyLoader.loadedAssemblies
        private static Type kopernicusStar = null; // KopernicusStar type


        /// <summary>
        /// Get KopernicusStar type from Kopernicus.Components
        /// </summary>
        /// <returns></returns>
        private static Type GetKopernicusStar()
        {
            if (kopernicusComponentsIndex == -1)
                kopernicusComponentsIndex = Tools.GetAssemblyIndex("Kopernicus.Components");
            if ((kopernicusComponentsIndex != -1) && (kopernicusStar == null))
                kopernicusStar = AssemblyLoader.loadedAssemblies[kopernicusComponentsIndex].assembly.GetType("Kopernicus.Components.KopernicusStar");
            return kopernicusStar;
        }


        /// <summary>
        /// Get name of the current star
        /// </summary>
        /// <returns></returns>
        internal static string GetCurrentStarName()
        {
            string name = "";
            
            if (GetKopernicusStar() != null)
            {
                FieldInfo currentStar = kopernicusStar.GetField("Current", BindingFlags.Public | BindingFlags.Static);
                if (currentStar != null)
                    name = ((Sun)currentStar.GetValue(null)).name;
            }

            return name;
        }

    }

}

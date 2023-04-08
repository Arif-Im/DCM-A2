using UnityEngine;

namespace MD_Package
{
    /// <summary>
    /// Custom debug solution for MD Package. Outputs more info
    /// </summary>
    public static class MD_Debug
    {
        public const string ORGANISATION = "Matej Vanco";
        public const string PACKAGENAME = "/MD Package/";
        public const short VERSION = 17;
        /// <summary>
        /// dd/mm/yyyy
        /// </summary>
        public const string DATE = "19/01/2023";

        public enum DebugType { Error, Warning, Information };
        public static void Debug(MonoBehaviour sender, string message, DebugType debugType = DebugType.Information)
        {
            string senderName = !sender ? "(Unknown sender)" : sender.GetType().Name;
            string senderObjName = !sender ? "(Unknown sender)" : sender.gameObject.name;
            switch (debugType)
            {
                case DebugType.Information: UnityEngine.Debug.Log(senderName + " [" + senderObjName + "]: " + message); break;
                case DebugType.Warning: UnityEngine.Debug.LogWarning(senderName + " [" + senderObjName + "]: " + message); break;
                case DebugType.Error: UnityEngine.Debug.LogError(senderName + " [" + senderObjName + "]: " + message); break;
            }
        }
    }
}